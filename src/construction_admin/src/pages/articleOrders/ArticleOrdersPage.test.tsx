/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * The orders page offers each person only the step that is theirs: the office
 * orders and sends, the person who asked confirms it arrived. A button offered to
 * the wrong person is refused by the API, but it should not be drawn.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;
const ME = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function order(overrides: Record<string, unknown> = {}) {
  return {
    id: 'o1',
    status: 'Requested',
    urgent: false,
    note: null,
    reviewNote: null,
    requestedByUserId: 'someone-else',
    requestedByName: 'Ana Novak',
    projectId: null,
    projectName: null,
    handledByName: null,
    orderedAt: null,
    shippedAt: null,
    deliveredAt: null,
    createdAt: '2026-09-26T08:00:00Z',
    items: [{ id: 'i1', name: 'Radne cipele', quantity: 1, unit: 'par', note: 'broj 43' }],
    ...overrides,
  };
}

function page(items: unknown[]) {
  return {
    items,
    pageNumber: 1,
    pageSize: 100,
    totalCount: items.length,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

async function renderOrders(role: Parameters<typeof signedIn>[0] = 'Admin') {
  const { ArticleOrdersPage } = await import('./ArticleOrdersPage');

  return renderScreen(<ArticleOrdersPage />, { route: '/', path: '/', user: signedIn(role) });
}

describe('ArticleOrdersPage', () => {
  it('shows what was asked for, by whom and where it stands', async () => {
    network.reply('/articleorders', 200, page([order()]));

    await renderOrders();

    expect(await screen.findByText('Ana Novak')).toBeDefined();
    expect(screen.getByText(/Radne cipele/)).toBeDefined();
    expect(screen.getAllByText(/^(Requested|Zatraženo)$/).length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);

  it('offers the office the step to order, and sends that step for that request', async () => {
    network.reply('/articleorders', 200, page([order()]));
    network.reply('/status', 200, order({ status: 'Ordered' }));

    await renderOrders('Admin');
    await userEvent.click(await screen.findByRole('button', { name: /(Mark as ordered|Označi kao naručeno)/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.includes('/o1/status'));
    expect(posts).toHaveLength(1);
    expect(JSON.stringify(posts[0].body)).toContain('Ordered');
  }, SCREEN_TIMEOUT);

  it('does not offer a worker the office steps', async () => {
    network.reply('/articleorders', 200, page([order({ requestedByUserId: ME })]));

    await renderOrders('Worker');
    await screen.findByText('Ana Novak');

    expect(screen.queryByRole('button', { name: /(Mark as ordered|Označi kao naručeno)/ })).toBeNull();
    // Their own request, not yet ordered: they may withdraw it.
    expect(screen.getByRole('button', { name: /(Withdraw|Povuci)/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('lets the person who asked confirm it arrived once it is in delivery', async () => {
    network.reply('/articleorders', 200, page([order({ status: 'InDelivery', requestedByUserId: ME })]));

    await renderOrders('Worker');

    expect(await screen.findByRole('button', { name: /(I received it|Primio sam)/ })).toBeDefined();
  }, SCREEN_TIMEOUT);
});
