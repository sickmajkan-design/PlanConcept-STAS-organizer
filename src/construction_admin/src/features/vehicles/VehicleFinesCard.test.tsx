/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/** The fines card: what is open, who bears it, and that a fine borne by the driver names the driver. */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const fine = (over: Record<string, unknown> = {}) => ({
  id: 'f1',
  vehicleId: 'v1',
  occurredOn: '2026-10-18',
  description: 'Prekoračenje brzine',
  amount: 120,
  dueDate: '2026-11-02',
  driverEmployeeId: 'e1',
  driverName: 'Marko Ilić',
  borneBy: 'Driver',
  status: 'Open',
  paidOn: null,
  overdue: false,
  note: null,
  createdAt: '2026-10-19T08:00:00Z',
  ...over,
});

async function renderCard(holderId: string | null = null) {
  const { VehicleFinesCard } = await import('./VehicleFinesCard');

  return renderScreen(<VehicleFinesCard vehicleId="v1" holderId={holderId} />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('VehicleFinesCard', () => {
  it('lists the fines with who bears them and the open sum', async () => {
    network.reply('/vehiclefines', 200, [fine(), fine({ id: 'f2', description: 'Parking', amount: 60, borneBy: 'Firm', status: 'Paid', paidOn: '2026-10-14', driverName: null })]);
    await renderCard();

    expect(await screen.findByText('Prekoračenje brzine')).toBeDefined();
    expect(screen.getByText('Parking')).toBeDefined();
    expect(screen.getByText(/otvoreno: 1|open: 1/i)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('will not save a fine the driver bears without naming the driver, and sends the one it saves', async () => {
    network.reply('/vehiclefines', 200, []);
    network.reply('/employees', 200, { items: [{ id: 'e1', fullName: 'Marko Ilić' }], pageNumber: 1, pageSize: 100, totalCount: 1, totalPages: 1 });
    await renderCard(null);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: /dodaj kaznu|add a fine/i }));
    await user.type(await screen.findByRole('textbox', { name: /opis|what for/i }), 'Parking');
    await user.type(screen.getByRole('textbox', { name: /iznos|amount/i }), '60');

    // The driver bears it, but nobody is named: the save button stays locked.
    await user.click(screen.getByRole('combobox', { name: /snosi|borne by/i }));
    await user.click(await screen.findByRole('option', { name: /^vozač$|^driver$/i }));
    expect((screen.getByRole('button', { name: /sačuvaj|save/i }) as HTMLButtonElement).disabled).toBe(true);

    await user.click(screen.getByRole('combobox', { name: /^vozač$|^driver$/i }));
    await user.click(await screen.findByRole('option', { name: 'Marko Ilić' }));
    await user.click(screen.getByRole('button', { name: /sačuvaj|save/i }));

    await waitFor(() => {
      const posts = network.calls.filter((c) => c.method.toLowerCase() === 'post' && c.url.includes('/vehiclefines'));
      expect(posts).toHaveLength(1);
      expect(posts[0].body).toMatchObject({ vehicleId: 'v1', description: 'Parking', amount: 60, borneBy: 'Driver', driverEmployeeId: 'e1', status: 'Open' });
    });
  }, SCREEN_TIMEOUT);
});
