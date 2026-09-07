/**
 * @vitest-environment jsdom
 */
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';

/**
 * The screen the office reads money off.
 *
 * Every number here comes from the API — the client sums nothing — so what
 * these tests guard is not arithmetic but the handful of ways a correct number
 * still reaches the wrong place, or a missing one gets shown as though it were
 * a real zero. Both are quiet: nobody double-checks a table that looks
 * plausible, and a project reported at 0.0% realized reads as a project in
 * trouble rather than a project nobody typed a contract value for.
 */
let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

/** See the note in EmployeesListPage.test.tsx: the first import is slow. */
const SCREEN_TIMEOUT = 20_000;

function row(overrides: Record<string, unknown> = {}) {
  return {
    projectId: '11111111-1111-1111-1111-111111111111',
    projectName: 'Vrbas — most',
    status: 'InProgress',
    contractValue: 1_000_000,
    realizedThisYear: 250_000,
    realizedToDate: 400_000,
    remaining: 600_000,
    percentOfContract: 0.4,
    ...overrides,
  };
}

function plan(rows: unknown[], totals: Record<string, unknown> = {}) {
  return {
    year: 2026,
    rows,
    totalContractValue: 1_000_000,
    totalRealizedThisYear: 250_000,
    totalRealizedToDate: 400_000,
    totalRemaining: 600_000,
    percentRealized: 0.4,
    ...totals,
  };
}

async function renderPlan() {
  // The record-payment dialog is mounted alongside the table and loads the
  // project list for its picker, so every test needs an answer for it.
  network.reply('/api/v1/projects?', 200, {
    items: [],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  });

  const { AnnualRealizationPlanPage } = await import('./AnnualRealizationPlanPage');

  return renderScreen(<AnnualRealizationPlanPage />, {
    route: '/projects/realization',
    path: '/projects/realization',
  });
}

function planCalls() {
  return network.calls.filter(
    (call) => call.method === 'GET' && call.url.includes('/projects/annual-realization'),
  );
}

describe('AnnualRealizationPlanPage', () => {
  it('puts each project\'s money in its own row', async () => {
    network.reply('/projects/annual-realization', 200, plan([row()]));

    await renderPlan();

    const cells = within(
      (await screen.findByText('Vrbas — most')).closest('tr') as HTMLElement,
    );

    // Read off the row rather than the page, so a column mapped to the wrong
    // field — realized-to-date shown as realized-this-year, say — cannot pass
    // by being present somewhere else on the screen.
    expect(cells.getByText('1,000,000.00')).toBeDefined();
    expect(cells.getByText('250,000.00')).toBeDefined();
    expect(cells.getByText('400,000.00')).toBeDefined();
    expect(cells.getByText('600,000.00')).toBeDefined();
    expect(cells.getByText('40.0%')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('says a project has no contract value rather than showing it as zero', async () => {
    // A project nobody has typed a contract value for is not a project worth
    // nothing, and 0.00 against it is a number somebody will act on.
    network.reply(
      '/projects/annual-realization',
      200,
      plan(
        [
          row({
            projectName: 'Novi Sad — hala',
            contractValue: 0,
            realizedThisYear: 80_000,
            realizedToDate: 80_000,
            remaining: 0,
            percentOfContract: null,
          }),
        ],
        { totalContractValue: 0, percentRealized: null },
      ),
    );

    await renderPlan();

    const cells = within(
      (await screen.findByText('Novi Sad — hala')).closest('tr') as HTMLElement,
    );

    expect(cells.getByText('No contract value set')).toBeDefined();

    // And no percentage at all, rather than 0.0% — which would read as a
    // project that has realized nothing.
    expect(cells.getByText('—')).toBeDefined();
    expect(cells.queryByText('0.0%')).toBeNull();

    // What has been realized is still money, and still shown.
    expect(cells.getAllByText('80,000.00').length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);

  it('shows the totals the API sent, not a sum of the rows on screen', async () => {
    // The rows are one page of a longer list on the server. Summing what is
    // visible would quietly under-report the year the moment there is a
    // second page, so the footer has to carry the server's own totals.
    network.reply(
      '/projects/annual-realization',
      200,
      plan([row({ contractValue: 10, realizedThisYear: 10, realizedToDate: 10, remaining: 0 })], {
        totalContractValue: 9_000_000,
        totalRealizedThisYear: 3_000_000,
        totalRealizedToDate: 4_500_000,
        totalRemaining: 4_500_000,
        percentRealized: 0.5,
      }),
    );

    await renderPlan();

    // Read the footer row itself. Asserting the number is somewhere on the
    // page passes on the summary card alone, which would let a footer that
    // summed the visible rows through — the exact mistake being guarded.
    const footer = within(
      (await screen.findByText('Total')).closest('tr') as HTMLElement,
    );

    expect(footer.getByText('9,000,000.00')).toBeDefined();
    expect(footer.getByText('3,000,000.00')).toBeDefined();
    expect(footer.getByText('50.0%')).toBeDefined();

    // And the row above it is the small one, so the two cannot be confused.
    expect(screen.getAllByText('10.00').length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);

  it('asks the API for the year that is selected', async () => {
    // Last year's figures under this year's heading is the one mistake on
    // this screen nobody would catch by looking at it.
    network.reply('/projects/annual-realization', 200, plan([row()]));

    await renderPlan();
    await screen.findByText('Vrbas — most');

    // The year rides as a query parameter rather than in the path, so read it
    // off the request rather than the URL string.
    const thisYear = new Date().getFullYear();
    expect(planCalls().at(-1)?.params.year).toBe(thisYear);

    await userEvent.click(screen.getByLabelText('Year'));
    await userEvent.click(await screen.findByRole('option', { name: String(thisYear - 1) }));

    await waitFor(() => {
      expect(planCalls().at(-1)?.params.year).toBe(thisYear - 1);
    });
  }, SCREEN_TIMEOUT);

  it('says so when there is nothing to report', async () => {
    network.reply('/projects/annual-realization', 200, plan([], { totalContractValue: 0 }));

    await renderPlan();

    expect(await screen.findByText('No projects yet.')).toBeDefined();
  }, SCREEN_TIMEOUT);
});
