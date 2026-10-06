/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * The planning board has to make the problems visible without anyone hunting for them: who did
 * not clock in, who is late, who is posted while on approved leave, and who is simply free.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;
const SITE = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const today = new Date().toISOString().slice(0, 10);

function employee(id: string, fullName: string, status: string, overrides: Record<string, unknown> = {}) {
  return {
    id,
    fullName,
    position: 'Zidar',
    status,
    clockedInAt: null,
    todayProjectId: SITE,
    postings: [{ projectId: SITE, startDate: today, endDate: null }],
    absences: [],
    vehicles: [],
    tools: [],
    ...overrides,
  };
}

function schedule(employees: unknown[]) {
  return {
    from: today,
    days: 14,
    today,
    lateToleranceMinutes: 15,
    projects: [{ id: SITE, name: 'Aldi Hall', status: 'Active', shiftStartTime: '07:00:00', endDate: null }],
    employees,
  };
}

async function renderBoard() {
  const { AssignmentSchedulePage } = await import('./AssignmentSchedulePage');
  return renderScreen(<AssignmentSchedulePage />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('AssignmentSchedulePage', () => {
  it('lists the problems of the day above the grid', async () => {
    network.reply(
      '/assignment-board/schedule',
      200,
      schedule([
        employee('e1', 'Luka Babic', 'NoShow'),
        employee('e2', 'Dino Hodzic', 'Late', { clockedInAt: `${today}T08:00:00Z` }),
        employee('e3', 'Ivan Peric', 'OnSite', { clockedInAt: `${today}T07:02:00Z` }),
        employee('e4', 'Stefan Maric', 'Free', { todayProjectId: null, postings: [] }),
      ]),
    );

    await renderBoard();

    expect(await screen.findByText(/Luka Babic .*Aldi Hall/)).toBeDefined();
    expect(screen.getByText(/Dino Hodzic .*Aldi Hall/)).toBeDefined();
    // The on-time worker is not a problem.
    expect(screen.queryByText(/Ivan Peric .*(nije|not clocked)/i)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('flags a worker posted on a day of approved leave', async () => {
    network.reply(
      '/assignment-board/schedule',
      200,
      schedule([
        employee('e1', 'Emir Delic', 'Expected', {
          absences: [{ type: 'AnnualLeave', startDate: today, endDate: today }],
        }),
      ]),
    );

    await renderBoard();

    // Either a weekday is posted during leave (flagged), or today is a weekend and there is nothing to flag.
    const day = new Date().getUTCDay();
    if (day === 0 || day === 6) return;
    expect((await screen.findAllByText(/Emir Delic/)).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/(approved leave|odobreno odsustvo)/i).length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);

  it('asks for a different period when the number of weeks changes', async () => {
    network.reply('/assignment-board/schedule', 200, schedule([employee('e1', 'Ivan Peric', 'OnSite')]));

    await renderBoard();
    await screen.findAllByText('Ivan Peric');
    await userEvent.click(screen.getByRole('button', { name: /^(4 weeks|4 sedmice)$/i }));

    await screen.findAllByText('Ivan Peric');
    const calls = network.calls.filter((call) => call.url.includes('/assignment-board/schedule'));
    expect(calls.some((call) => call.params.days === 28)).toBe(true);
  }, SCREEN_TIMEOUT);
});
