/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../../test/renderScreen';
import { daysUntil } from './ActiveProjectsWidget';

/**
 * The customer's own request: how many projects are in each stage, and which of the
 * running ones want a look — no crew, ending soon, or already past their end date.
 * The API itself narrows the list to what the signed-in role may see (a foreman gets
 * only their own sites), so the widget needs no role logic of its own.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 20_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function project(overrides: Record<string, unknown> = {}) {
  return {
    id: 'p1',
    name: 'Rezidencija Vrbas',
    status: 'Active',
    employeeCount: 4,
    endDate: null,
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

async function renderWidget(user = signedIn('Admin')) {
  const { ActiveProjectsWidget } = await import('./ActiveProjectsWidget');

  return renderScreen(<ActiveProjectsWidget instanceId="w1" />, { route: '/', path: '/', user });
}

describe('ActiveProjectsWidget', () => {
  it('counts every status, not only the active ones', async () => {
    network.reply('/projects', 200, page([
      project({ id: 'p1', status: 'Active' }),
      project({ id: 'p2', status: 'Active' }),
      project({ id: 'p3', status: 'Planned' }),
      project({ id: 'p4', status: 'Completed' }),
    ]));

    await renderWidget();

    await screen.findByText(/: 2/);
    // Two statuses share a count of 1 (Planned and Completed); both chips must be there.
    expect(screen.getAllByText(/: 1/)).toHaveLength(2);
  }, SCREEN_TIMEOUT);

  it('lists an active project with its crew size', async () => {
    network.reply('/projects', 200, page([project({ name: 'Stambena zgrada Centar', employeeCount: 7 })]));

    await renderWidget();

    expect(await screen.findByText('Stambena zgrada Centar')).toBeDefined();
    expect(screen.getByText(/7 (people|osoba)/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('flags a project with nobody on it, and puts it first', async () => {
    network.reply('/projects', 200, page([
      project({ id: 'staffed', name: 'Sa timom', employeeCount: 5 }),
      project({ id: 'empty', name: 'Nema radnika', employeeCount: 0 }),
    ]));

    await renderWidget();

    await screen.findByText(/^No crew$/);
    const names = screen.getAllByRole('link').map((el) => el.textContent);
    expect(names[0]).toBe('Nema radnika');
  }, SCREEN_TIMEOUT);

  it('flags a project ending soon and one already overdue, and sorts the overdue one first', async () => {
    const today = new Date();
    const soon = new Date(today);
    soon.setDate(soon.getDate() + 5);
    const past = new Date(today);
    past.setDate(past.getDate() - 3);
    const iso = (d: Date) => d.toISOString().slice(0, 10);

    network.reply('/projects', 200, page([
      project({ id: 'soon', name: 'Skoro gotov', endDate: iso(soon) }),
      project({ id: 'over', name: 'Kasni', endDate: iso(past) }),
    ]));

    await renderWidget();

    expect(await screen.findByText(/^Past its end date$/)).toBeDefined();
    expect(screen.getByText(/^Ends in 5 days$/)).toBeDefined();

    const names = screen.getAllByRole('link').map((el) => el.textContent);
    expect(names[0]).toBe('Kasni');
  }, SCREEN_TIMEOUT);

  it('does not flag a project with no end date or one far in the future', async () => {
    network.reply('/projects', 200, page([
      project({ id: 'noend', name: 'Bez roka', endDate: null }),
      project({ id: 'far', name: 'Daleki rok', endDate: '2099-01-01' }),
    ]));

    await renderWidget();

    await screen.findByText('Bez roka');
    expect(screen.queryByText(/Ends in|Past its end date|za \d+ dana|Rok prošao/)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('opens the project on click and offers "view all"', async () => {
    network.reply('/projects', 200, page([project()]));

    await renderWidget();

    const link = await screen.findByRole('link', { name: 'Rezidencija Vrbas' });
    expect(link.getAttribute('href')).toBe('/projects/p1');
    expect(screen.getByRole('link', { name: /View all|Prika/i }).getAttribute('href')).toBe('/projects');
  }, SCREEN_TIMEOUT);

  it('says so when there are no active projects', async () => {
    network.reply('/projects', 200, page([project({ status: 'Planned' })]));

    await renderWidget();

    expect(await screen.findByText(/no active projects|Nema aktivnih/i)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('shows a foreman only what the API already narrowed the list to', async () => {
    // The widget itself does no role filtering: whatever /projects returns for this
    // account is exactly what is shown.
    network.reply('/projects', 200, page([project({ name: 'Moje gradiliste' })]));

    await renderWidget(signedIn('Foreman'));

    expect(await screen.findByText('Moje gradiliste')).toBeDefined();
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
