/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../../test/renderScreen';
import { daysUntil } from './ActiveProjectsWidget';

/**
 * The two widgets added for the board: time-off requests to answer, and the
 * state of the projects. Rendered whole, against a fake network, because what
 * can go wrong in them is silent: buttons that answer the wrong request, a
 * refusal sent without its reason, a person answering their own request.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 20_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function absence(overrides: Record<string, unknown> = {}) {
  return {
    id: 'a1',
    employeeId: 'e-ana',
    employeeName: 'Ana Novak',
    type: 'AnnualLeave',
    status: 'Requested',
    startDate: '2026-10-06',
    endDate: '2026-10-08',
    dayCount: 3,
    reason: null,
    createdAt: '2026-09-26T08:00:00Z',
    ...overrides,
  };
}

function page(items: unknown[]) {
  return {
    items,
    pageNumber: 1,
    pageSize: 6,
    totalCount: items.length,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

async function renderAbsenceWidget(user = signedIn('Admin')) {
  const { AbsenceRequestsWidget } = await import('./AbsenceRequestsWidget');

  return renderScreen(<AbsenceRequestsWidget instanceId="w1" />, { route: '/', path: '/', user });
}

function reviewCalls() {
  return network.calls.filter((call) => call.method === 'POST' && call.url.includes('/review'));
}

describe('AbsenceRequestsWidget', () => {
  it('lists who is asking, for what and how long', async () => {
    network.reply('/absences', 200, page([absence()]));

    await renderAbsenceWidget();

    expect(await screen.findByText('Ana Novak')).toBeDefined();
    expect(screen.getByText(/3 (days|dana)/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('grants the request that was pressed, and only that one', async () => {
    network.reply('/absences', 200, page([
      absence(),
      absence({ id: 'a2', employeeId: 'e-luka', employeeName: 'Luka Babić' }),
    ]));
    network.reply('/review', 200, absence({ status: 'Approved' }));

    await renderAbsenceWidget();
    await screen.findByText('Luka Babić');

    const grant = screen.getAllByRole('button', { name: /^(Grant|Odobri)/i });
    await userEvent.click(grant[1]);

    // The confirmation, then the answer: with the dialog open it is the only
    // Grant button left that a person could reach.
    const confirm = await screen.findAllByRole('button', { name: /^(Grant|Odobri)/i });
    await userEvent.click(confirm[confirm.length - 1]);

    await waitFor(() => expect(reviewCalls()).toHaveLength(1));

    expect(reviewCalls()[0].url).toContain('/absences/a2/review');
    expect(reviewCalls()[0].body).toMatchObject({ approve: true });
  }, SCREEN_TIMEOUT);

  it('switches the buttons off for the reviewer\'s own request', async () => {
    const admin = { ...signedIn('Admin'), employeeId: 'e-me' };
    network.reply('/absences', 200, page([absence({ employeeId: 'e-me', employeeName: 'Marko Kovač' })]));

    await renderAbsenceWidget(admin);

    await screen.findByText('Marko Kovač');

    for (const button of screen.getAllByRole('button', { name: /^(Grant|Refuse|Odobri|Odbij)/i })) {
      expect((button as HTMLButtonElement).disabled).toBe(true);
    }
  }, SCREEN_TIMEOUT);

  it('says so when nothing is waiting', async () => {
    network.reply('/absences', 200, page([]));

    await renderAbsenceWidget();

    expect(await screen.findByText(/No time-off requests|Nijedan zahtjev/)).toBeDefined();
  }, SCREEN_TIMEOUT);
});

describe('daysUntil', () => {
  const today = new Date(2026, 8, 26);

  it('counts whole days ahead', () => {
    expect(daysUntil('2026-10-10', today)).toBe(14);
  });

  it('is zero on the day itself', () => {
    expect(daysUntil('2026-09-26', today)).toBe(0);
  });

  it('is negative once the date has passed', () => {
    expect(daysUntil('2026-09-20', today)).toBe(-6);
  });
});
