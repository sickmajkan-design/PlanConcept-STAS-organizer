/**
 * @vitest-environment jsdom
 */
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * The schedule has to show where a site is short without anyone hunting for it, offer a stand-in for
 * somebody who is away, and move people with one click that can be taken back.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;
const HALL = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const BRIDGE = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const pad = (n: number) => String(n).padStart(2, '0');
const local = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const addDays = (d: Date, n: number) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);

const now = new Date();
const today = local(now);

/** A working day at least two days ahead, so the leave sits inside the next 30 days whatever day this runs. */
function workdayAhead(): Date {
  let d = addDays(now, 2);
  while (d.getDay() === 0 || d.getDay() === 6) d = addDays(d, 1);
  return d;
}

const leave = workdayAhead();

function site(id: string, name: string, needs: { position: string; count: number }[]) {
  return { id, name, status: 'Active', customerName: null, startDate: null, endDate: null, worksSaturdays: false, worksSundays: false, needs };
}

function person(id: string, fullName: string, position: string, projectId: string | null, away = false) {
  return {
    id,
    fullName,
    position,
    postings: projectId ? [{ projectId, startDate: local(addDays(now, -30)), endDate: null }] : [],
    absences: away ? [{ type: 'AnnualLeave', startDate: local(leave), endDate: local(leave) }] : [],
  };
}

function plan(overrides: Record<string, unknown> = {}) {
  return {
    from: local(addDays(now, -31)),
    to: local(addDays(now, 60)),
    today,
    positions: ['Tesar', 'Zidar'],
    projects: [site(HALL, 'Aldi Hall', [{ position: 'Zidar', count: 2 }]), site(BRIDGE, 'Sava Bridge', [])],
    employees: [
      person('e1', 'Ana Novak', 'Zidar', HALL),
      person('e2', 'Marko Horvat', 'Zidar', HALL, true),
      person('e3', 'Ivo Babić', 'Zidar', null),
    ],
    ...overrides,
  };
}

async function renderPage() {
  const { PlanningPage } = await import('./PlanningPage');
  return renderScreen(<PlanningPage />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('PlanningPage', () => {
  it('lists an absence that leaves a site short, with how many people could stand in', async () => {
    network.reply('/planning', 200, plan());

    await renderPage();

    expect(await screen.findByText('Needs a stand-in')).toBeDefined();
    expect(screen.getAllByText(/Marko Horvat/).length).toBeGreaterThan(0);
    expect(screen.getByText('1 suggestions')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('says so when every absence is covered', async () => {
    network.reply('/planning', 200, plan({ employees: [person('e1', 'Ana Novak', 'Zidar', HALL), person('e3', 'Ivo Babić', 'Zidar', HALL)] }));

    await renderPage();

    expect(await screen.findByText('Every absence in the next 30 days is covered')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('posts the chosen stand-in for the days of the absence', async () => {
    network.reply('/planning', 200, plan());
    network.reply('/planning/assign', 204);

    await renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Find a stand-in' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByText(/Ivo Babić/));

    await waitFor(() => {
      const assign = network.calls.find((c) => c.url.includes('/planning/assign'));
      expect(assign?.body).toMatchObject({
        employeeId: 'e3',
        projectId: HALL,
        from: local(leave),
        to: local(leave),
        onlyFreeDays: false,
      });
    });

    expect(await screen.findByText(/Ivo Babić → Aldi Hall/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('shows what each project needs against who is there, with the arrow opening the skills', async () => {
    network.reply('/planning', 200, plan({ employees: [person('e1', 'Ana Novak', 'Zidar', HALL)] }));

    await renderPage();
    await userEvent.click(await screen.findByRole('button', { name: /Needs by project/ }));

    expect(await screen.findByText('Aldi Hall')).toBeDefined();
    await userEvent.click(screen.getAllByRole('button', { name: 'Show skills' })[0]);
    expect(await screen.findByText(/Zidar · needs 2/)).toBeDefined();
    expect(screen.getAllByText('1/2').length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);

  it('frees somebody for a day from the list and can take it back', async () => {
    network.reply('/planning', 200, plan({ employees: [person('e1', 'Ana Novak', 'Zidar', HALL)] }));
    network.reply('/planning/assign', 204);

    await renderPage();
    await userEvent.click(await screen.findByRole('button', { name: /^List/ }));

    await userEvent.click(await screen.findByRole('combobox', { name: 'Ana Novak' }));
    await userEvent.click(await screen.findByRole('option', { name: 'Free' }));

    await waitFor(() => {
      const assign = network.calls.find((c) => c.url.includes('/planning/assign'));
      expect(assign?.body).toMatchObject({ employeeId: 'e1', projectId: null, from: today, to: today });
    });

    await userEvent.click(await screen.findByRole('button', { name: 'Undo' }));

    await waitFor(() => {
      const restores = network.calls.filter((c) => c.url.includes('/planning/assign') && (c.body as { projectId?: string })?.projectId === HALL);
      expect(restores.length).toBeGreaterThan(0);
    });
  }, SCREEN_TIMEOUT);
});
