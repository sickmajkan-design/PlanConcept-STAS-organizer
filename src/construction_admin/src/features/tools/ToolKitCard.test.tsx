/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/** The card of a case shows what is in it and offers each item only the steps its state allows. */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function kit(state: string) {
  return {
    toolId: 'k1',
    name: 'Kofer E-014',
    items: [{ id: 'i1', name: 'Kliješta', quantity: 1, unitPrice: 12, lineTotal: 12, state, sortOrder: 1 }],
    events: [],
    totalValue: 12,
    missingValue: 0,
    brokenValue: 0,
    openCharges: 0,
    openRefunds: 0,
  };
}

async function renderCard() {
  const { ToolKitCard } = await import('./ToolKitCard');

  return renderScreen(<ToolKitCard toolId="k1" holderId={null} />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('ToolKitCard', () => {
  it('offers a report of missing or broken for an item in the case, and sends the one chosen', async () => {
    network.reply('/tools/k1/kit', 200, kit('InKit'));
    await renderCard();
    const user = userEvent.setup();

    expect(await screen.findByText('Kliješta')).toBeDefined();

    await user.click(screen.getByRole('button', { name: /šta dalje|what next/i }));

    // Nothing about charging or writing off is on offer while the item is where it should be.
    expect(screen.queryByRole('menuitem', { name: /zaduži radnika|charge the worker/i })).toBeNull();
    await user.click(screen.getByRole('menuitem', { name: /prijavi da nedostaje|report missing/i }));
    await user.click(await screen.findByRole('button', { name: /potvrdi|confirm/i }));

    await waitFor(() => {
      const posts = network.calls.filter((c) => c.method.toLowerCase() === 'post');
      expect(posts).toHaveLength(1);
      expect(posts[0].url).toContain('/tools/k1/kit/items/i1/actions');
      expect(posts[0].body).toMatchObject({ action: 'ReportMissing' });
    });
  }, SCREEN_TIMEOUT);

  it('lets a broken item only be handed in to the office first', async () => {
    network.reply('/tools/k1/kit', 200, kit('Broken'));
    await renderCard();
    const user = userEvent.setup();

    await screen.findByText('Kliješta');
    await user.click(screen.getByRole('button', { name: /šta dalje|what next/i }));

    expect(screen.getByRole('menuitem', { name: /uprava primila|office received/i })).toBeDefined();
    expect(screen.queryByRole('menuitem', { name: /otpiši|write off/i })).toBeNull();
  }, SCREEN_TIMEOUT);
});
