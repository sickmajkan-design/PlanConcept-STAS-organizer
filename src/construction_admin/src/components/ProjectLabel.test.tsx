/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../test/renderScreen';

/** A project name carries its customer, and the customer opens that customer's projects. */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function project(overrides: Record<string, unknown>) {
  return { id: 'p1', name: 'Hala Logistik Park', customerId: 'c1', customerName: 'LogPark a.d.', ...overrides };
}

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 500, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

async function renderLabel(props: Record<string, unknown>) {
  const { ProjectLabel } = await import('./ProjectLabel');

  return renderScreen(<ProjectLabel name="Hala Logistik Park" {...props} />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('ProjectLabel', () => {
  it('finds the customer by the project and links to the projects of that customer', async () => {
    network.reply('/projects', 200, page([project({})]));
    await renderLabel({ projectId: 'p1' });

    const customer = await screen.findByText('LogPark a.d.');
    expect(customer.closest('a')?.getAttribute('href')).toBe('/projects?customer=c1');
  }, SCREEN_TIMEOUT);

  it('says there is no customer, without a link', async () => {
    network.reply('/projects', 200, page([project({ customerId: null, customerName: null })]));
    await renderLabel({ projectId: 'p1' });

    const none = await screen.findByText(/Bez kupca|No customer/);
    expect(none.closest('a')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('opens the customer without opening the row it sits in', async () => {
    network.reply('/projects', 200, page([project({})]));
    let rowClicked = false;
    const { ProjectLabel } = await import('./ProjectLabel');
    renderScreen(
      <div onClick={() => (rowClicked = true)}>
        <ProjectLabel name="Hala Logistik Park" projectId="p1" />
      </div>,
      { route: '/', path: '/', user: signedIn('Admin') },
    );

    await userEvent.setup().click(await screen.findByText('LogPark a.d.'));

    expect(rowClicked).toBe(false);
  }, SCREEN_TIMEOUT);

  it('shows the flag of the customer, and the site only when it is somewhere else', async () => {
    network.reply('/projects', 200, page([
      project({ customerCountryCode: 'CH', countryCode: 'DE' }),
      project({ id: 'p2', name: 'Hala B', customerCountryCode: 'BA', countryCode: 'BA' }),
    ]));
    await renderLabel({ projectId: 'p1' });

    // The customer's country, with its name for a screen reader, and the site's own because it differs.
    expect(await screen.findByLabelText('Švicarska')).toBeDefined();
    expect(await screen.findByLabelText('Njemačka')).toBeDefined();

    // Same country on both: no second flag.
    await renderLabel({ name: 'Hala B', projectId: 'p2' });
    expect(await screen.findAllByLabelText('Bosna i Hercegovina')).toHaveLength(1);
  }, SCREEN_TIMEOUT);

  it('shows no flag for a customer without a country', async () => {
    network.reply('/projects', 200, page([project({ customerCountryCode: null })]));
    await renderLabel({ projectId: 'p1' });

    await screen.findByText('LogPark a.d.');
    expect(screen.queryByRole('img')).toBeNull();
  }, SCREEN_TIMEOUT);
});
