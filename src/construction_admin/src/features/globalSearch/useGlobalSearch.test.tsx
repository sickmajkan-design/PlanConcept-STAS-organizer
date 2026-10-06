/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';
import { useGlobalSearch } from './useGlobalSearch';

/**
 * What Ctrl+K finds. Each list the palette searches is asked the words typed, and a result is only offered when
 * the account may open the page it leads to.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 5, totalCount: items.length, totalPages: 1 };
}

function Probe({ term, role }: { term: string; role: Parameters<typeof signedIn>[0] }) {
  const { groups } = useGlobalSearch(term, signedIn(role));

  return (
    <ul>
      {groups.flatMap((group) =>
        group.results.map((result) => (
          <li key={`${group.key}:${result.id}`} data-testid={group.key}>
            {`${result.label}|${result.sublabel ?? ''}|${result.path}`}
          </li>
        )),
      )}
    </ul>
  );
}

async function search(term: string, role: Parameters<typeof signedIn>[0]) {
  return renderScreen(<Probe term={term} role={role} />, { route: '/', path: '/', user: signedIn(role) });
}

const calledWith = (fragment: string) => network.calls.some((c) => c.url.includes(fragment));

describe('global search', () => {
  it('finds a vehicle by its TD number and shows it beside the plate', async () => {
    network.reply('/vehicles', 200, page([{ id: 'v1', brand: 'Iveco', model: 'Daily', registrationNumber: 'ZG-1', tdNumber: '15' }]));

    await search('15', 'Admin');

    const row = await screen.findByTestId('vehicles', undefined, { timeout: 8000 });
    expect(row.textContent).toBe('Iveco Daily|ZG-1 · TD 15|/vehicles/v1');
  }, SCREEN_TIMEOUT);

  it('finds a fuel card by its number and opens the vehicle it is on', async () => {
    network.reply('/fuel-cards', 200, page([
      { id: 'c1', vehicleId: 'v1', vehicleName: 'Iveco Daily (ZG-1)', provider: 'DKV', cardNumber: '70431001138023874' },
    ]));

    // Fuel cards sit behind the finance grant, which a Super Admin always has.
    await search('874', 'SuperAdmin');

    const row = await screen.findByTestId('fuelCards', undefined, { timeout: 8000 });
    expect(row.textContent).toBe('70431001138023874|DKV · Iveco Daily (ZG-1)|/vehicles/v1');
  }, SCREEN_TIMEOUT);

  it('finds a statement row and opens the statement check filtered to what was typed', async () => {
    network.reply('/fuel-transactions', 200, page([
      {
        id: 't1', cardNumber: '70431001138023874', vehicleName: 'Iveco Daily (ZG-1)', occurredOn: '2026-09-01',
        productType: 'Dizelsko gorivo', amount: 140.02, currency: 'EUR',
      },
    ]));

    await search('874', 'Admin');

    const row = await screen.findByTestId('dkvRows', undefined, { timeout: 8000 });
    expect(row.textContent).toContain('70431001138023874 · 140.02 EUR');
    expect(row.textContent).toContain('/vehicle-expenses/dkv?search=874');
  }, SCREEN_TIMEOUT);

  it('finds leave by the person and lands on it', async () => {
    network.reply('/absences', 200, page([
      { id: 'a1', employeeName: 'Ana Novak', type: 'AnnualLeave', startDate: '2026-10-20', endDate: '2026-10-24' },
    ]));

    await search('novak', 'Admin');

    const row = await screen.findByTestId('absences', undefined, { timeout: 8000 });
    expect(row.textContent).toContain('Ana Novak');
    expect(row.textContent).toContain('/absences?highlight=a1');
  }, SCREEN_TIMEOUT);

  it('finds a bulletin post by its text', async () => {
    network.reply('/bulletin', 200, [
      { id: 'b1', title: 'Radno vrijeme', body: 'Od ponedjeljka radimo od 7h.', createdByName: 'admin' },
      { id: 'b2', title: 'Slobodan dan', body: 'Petak je neradni.', createdByName: 'admin' },
    ]);

    await search('ponedjeljka', 'Admin');

    const row = await screen.findByTestId('bulletin', undefined, { timeout: 8000 });
    expect(row.textContent).toContain('Radno vrijeme');
    expect(screen.queryByText(/Slobodan dan/)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('does not ask for what the account cannot open', async () => {
    await search('te', 'Foreman');

    await waitFor(() => expect(calledWith('/vehicles')).toBe(true), { timeout: 8000 });

    // The statement check, invoices, the ledger and accounts are not for a foreman.
    expect(calledWith('/fuel-transactions')).toBe(false);
    expect(calledWith('/invoices')).toBe(false);
    expect(calledWith('/ledgers')).toBe(false);
    expect(calledWith('/users')).toBe(false);
  }, SCREEN_TIMEOUT);

  it('asks the statement and the ledger only of the people who may open them', async () => {
    await search('te', 'SuperAdmin');

    await waitFor(() => expect(calledWith('/fuel-transactions')).toBe(true), { timeout: 8000 });
    expect(calledWith('/ledgers')).toBe(true);
  }, SCREEN_TIMEOUT);
});
