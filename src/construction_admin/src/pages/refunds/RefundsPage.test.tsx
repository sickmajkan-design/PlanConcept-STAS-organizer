/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * Money: the office decides, nobody decides their own request, and the person who asked
 * can only withdraw it. A button drawn for the wrong person is refused by the API, but it
 * should not be offered.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;
const ME = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function refund(overrides: Record<string, unknown> = {}) {
  return {
    id: 'r1',
    employeeId: 'e1',
    employeeName: 'Ana Novak',
    requestedByUserId: 'someone-else',
    projectId: null,
    projectName: null,
    amount: 25.5,
    currency: 'EUR',
    expenseDate: '2026-09-20',
    description: 'Radne rukavice',
    status: 'Requested',
    reviewedByName: null,
    reviewedAt: null,
    reviewNote: null,
    payrollYear: null,
    payrollMonth: null,
    createdAt: '2026-09-26T08:00:00Z',
    ...overrides,
  };
}

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 100, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

async function renderRefunds(role: Parameters<typeof signedIn>[0] = 'Admin') {
  const { RefundsPage } = await import('./RefundsPage');

  return renderScreen(<RefundsPage />, { route: '/', path: '/', user: signedIn(role) });
}

describe('RefundsPage', () => {
  it('shows who asked, how much and why', async () => {
    network.reply('/refunds', 200, page([refund()]));

    await renderRefunds();

    expect(await screen.findByText('Ana Novak')).toBeDefined();
    expect(screen.getByText('Radne rukavice')).toBeDefined();
    expect(screen.getByText(/25[.,]50 EUR/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('offers the office to approve or decline somebody else\'s request', async () => {
    network.reply('/refunds', 200, page([refund()]));

    await renderRefunds('Admin');

    expect(await screen.findByRole('button', { name: /^(Approve|Odobri)$/ })).toBeDefined();
    expect(screen.getByRole('button', { name: /^(Decline|Odbij)$/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('does not let anybody decide their own request, only withdraw it', async () => {
    network.reply('/refunds', 200, page([refund({ requestedByUserId: ME })]));

    await renderRefunds('Admin');
    await screen.findByText('Ana Novak');

    expect(screen.queryByRole('button', { name: /^(Approve|Odobri)$/ })).toBeNull();
    expect(screen.getByRole('button', { name: /^(Withdraw|Povuci)$/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('does not offer a worker the decision', async () => {
    network.reply('/refunds', 200, page([refund({ requestedByUserId: ME })]));

    await renderRefunds('Worker');
    await screen.findByText('Ana Novak');

    expect(screen.queryByRole('button', { name: /^(Approve|Odobri)$/ })).toBeNull();
    await userEvent.click(screen.getByRole('button', { name: /^(Withdraw|Povuci)$/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.includes('/r1/review'));
    expect(JSON.stringify(posts[0]?.body)).toContain('Cancelled');
  }, SCREEN_TIMEOUT);
});
