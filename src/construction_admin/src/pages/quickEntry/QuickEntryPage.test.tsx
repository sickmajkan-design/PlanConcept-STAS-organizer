/**
 * @vitest-environment jsdom
 */
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/** The quick-entry table sends only the rows that changed, with every field of the row intact. */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function tool(id: string, name: string) {
  return {
    id,
    name,
    category: 'Električarski',
    serialNumber: null,
    qrCode: null,
    status: 'Available',
    ownershipType: 'Owned',
    branchId: null,
  };
}

describe('QuickEntryPage', () => {
  it('applies one value to the selected rows and saves only those rows', async () => {
    network.reply('/tools', 200, {
      items: [tool('t1', 'Bušilica'), tool('t2', 'Brusilica')],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 2,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });
    const { QuickEntryPage } = await import('./QuickEntryPage');
    renderScreen(<QuickEntryPage kind="tools" />, { route: '/', path: '/', user: signedIn('Admin') });
    const user = userEvent.setup();

    expect(await screen.findByText('Bušilica')).toBeDefined();

    const rows = screen.getAllByRole('row');
    await user.click(within(rows[1]).getByRole('checkbox'));
    await user.click(screen.getByRole('combobox', { name: /polje|field/i }));
    await user.click(await screen.findByRole('option', { name: /kategorija|category/i }));
    await user.type(screen.getByRole('textbox', { name: '' }), 'Vozni park');
    await user.click(screen.getByRole('button', { name: /primijeni na 1|apply to 1/i }));
    await user.click(screen.getByRole('button', { name: /snimi izmjene \(1\)|save changes \(1\)/i }));

    await waitFor(() => {
      const puts = network.calls.filter((c) => c.method.toLowerCase() === 'put');
      expect(puts).toHaveLength(1);
      expect(puts[0].url).toContain('/tools/t1');
      expect(puts[0].body).toMatchObject({ name: 'Bušilica', category: 'Vozni park' });
    });
  }, SCREEN_TIMEOUT);
});
