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

async function renderPage(role: Parameters<typeof signedIn>[0] = 'Admin') {
  const { PlanningPage } = await import('./PlanningPage');
  return renderScreen(<PlanningPage />, { route: '/', path: '/', user: signedIn(role) });
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

  it('offers a guided tour the first time and walks through the views, switching them as it goes', async () => {
    network.reply('/planning', 200, plan());

    await renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Start the tour' }));
    expect(await screen.findByText('Welcome to the Schedule')).toBeDefined();
    expect(screen.getByText('Step 1 of 13')).toBeDefined();

    // Through to the needs step: the tour has switched to that view by itself.
    for (let i = 0; i < 6; i++) await userEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(await screen.findByText('Step 7 of 13')).toBeDefined();
    expect(await screen.findByText('Aldi Hall')).toBeDefined();

    await userEvent.click(screen.getByRole('button', { name: 'Back' }));
    expect(screen.getByText('Click to move or swap')).toBeDefined();

    await userEvent.keyboard('{Escape}');
    await waitFor(() => expect(screen.queryByText('Click to move or swap')).toBeNull());
  }, SCREEN_TIMEOUT);

  it('does not offer the tour again once it has been seen', async () => {
    network.reply('/planning', 200, plan());

    await renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Not now' }));

    expect(screen.queryByRole('button', { name: 'Start the tour' })).toBeNull();
    expect(screen.getByRole('button', { name: 'Schedule guide' })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('warns about somebody posted to two sites on the same days, and about days after a site ends', async () => {
    const both = person('e1', 'Ana Novak', 'Zidar', HALL);
    both.postings.push({ projectId: BRIDGE, startDate: local(addDays(now, -30)), endDate: null });
    network.reply('/planning', 200, plan({
      projects: [site(HALL, 'Aldi Hall', []), { ...site(BRIDGE, 'Sava Bridge', []), endDate: local(addDays(now, -5)) }],
      employees: [both],
    }));

    await renderPage();

    expect(await screen.findByText('Worth a look')).toBeDefined();
    expect(screen.getByText(/Ana Novak is posted to both Aldi Hall and Sava Bridge/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('shows no warnings for an ordinary plan', async () => {
    network.reply('/planning', 200, plan({ employees: [person('e1', 'Ana Novak', 'Zidar', HALL)] }));

    await renderPage();
    await screen.findByText('Every absence in the next 30 days is covered');

    expect(screen.queryByText('Worth a look')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('shows on the day view who has not yet confirmed their posting on the phone', async () => {
    const confirmed = person('e1', 'Ana Novak', 'Zidar', HALL);
    confirmed.postings[0] = { ...confirmed.postings[0], acknowledgedAt: '2026-10-01T08:00:00Z' } as typeof confirmed.postings[0];
    network.reply('/planning', 200, plan({ employees: [confirmed, person('e3', 'Ivo Babić', 'Zidar', HALL)] }));

    await renderPage();
    await userEvent.click(await screen.findByRole('button', { name: /By site/ }));

    expect(await screen.findByText('1 have not confirmed')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('downloads the schedule on paper for the period on screen, in the language of the screen', async () => {
    network.reply('/planning', 200, plan());

    await renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Export to Excel' }));

    await waitFor(() => {
      const call = network.calls.find((c) => c.url.includes('/exports/schedule'));
      expect(call).toBeDefined();
      expect(String(call?.params.from)).toMatch(/^\d{4}-\d{2}-01$/);
      expect(call?.params.language).toBe('en');
    });
  }, SCREEN_TIMEOUT);

  describe('labour cost', () => {
    const cost = {
      from: local(addDays(now, -5)),
      to: local(addDays(now, 25)),
      hoursPerDay: 8,
      months: ['2026-10'],
      unpricedDays: 3,
      projects: [
        {
          projectId: HALL,
          name: 'Aldi Hall',
          plannedDays: 10,
          plannedCost: 800,
          actualCost: 350.5,
          months: [{ month: '2026-10', plannedDays: 10, plannedCost: 800, actualCost: 350.5 }],
        },
      ],
    };

    it('is offered to somebody who may see pay, with planned beside recorded and a warning for unpriced days', async () => {
      network.reply('/planning/labour-cost', 200, cost);
      network.reply('/planning', 200, plan());

      await renderPage('SuperAdmin');
      await userEvent.click(await screen.findByRole('button', { name: /Labour cost/ }));

      expect(await screen.findByText('Aldi Hall')).toBeDefined();
      expect(screen.getAllByText('800.00').length).toBeGreaterThan(0);
      expect(screen.getAllByText('350.50').length).toBeGreaterThan(0);
      expect(screen.getByText(/3 planned working days belong to people with no rate set/)).toBeDefined();
    }, SCREEN_TIMEOUT);

    it('is not offered to somebody without the full finance grant', async () => {
      network.reply('/planning', 200, plan());

      await renderPage('Admin');
      await screen.findByRole('button', { name: /By site/ });

      expect(screen.queryByRole('button', { name: /Labour cost/ })).toBeNull();
    }, SCREEN_TIMEOUT);
  });

  describe('for a foreman', () => {
    const absent = () => plan({ canEdit: false, isScoped: true, scopeBranchName: 'Zagreb unit' });

    it('shows the unit they follow, with nothing to press when they may not move people', async () => {
      network.reply('/planning', 200, absent());

      await renderPage();

      expect(await screen.findByText(/You are viewing the schedule of Zagreb unit/)).toBeDefined();
      expect(screen.queryByRole('button', { name: 'Find a stand-in' })).toBeNull();
      expect(screen.queryByRole('button', { name: 'Schedule guide' })).toBeNull();
      expect(screen.queryByRole('button', { name: 'Start the tour' })).toBeNull();
    }, SCREEN_TIMEOUT);

    it('has no way to open a worker for changes', async () => {
      network.reply('/planning', 200, absent());

      await renderPage();
      await screen.findByText(/You are viewing the schedule of Zagreb unit/);
      await userEvent.click(await screen.findByRole('button', { name: /By site/ }));

      const button = (await screen.findByText('Ana Novak')).closest('button');
      expect(button?.hasAttribute('disabled')).toBe(true);
    }, SCREEN_TIMEOUT);

    it('says plainly when they belong to no unit yet', async () => {
      network.reply('/planning', 200, plan({ canEdit: false, isScoped: true, scopeBranchName: null, employees: [], projects: [] }));

      await renderPage();

      expect(await screen.findByText(/not placed in a business unit/)).toBeDefined();
    }, SCREEN_TIMEOUT);

    it('may move people once granted, but still cannot change what a project needs', async () => {
      network.reply('/planning', 200, plan({ canEdit: true, isScoped: true, scopeBranchName: 'Zagreb unit' }));

      await renderPage();

      expect(await screen.findByText(/You can move people and sites of Zagreb unit only/)).toBeDefined();
      expect(await screen.findByRole('button', { name: 'Find a stand-in' })).toBeDefined();

      await userEvent.click(screen.getByRole('button', { name: /Needs by project/ }));
      await screen.findByText('Aldi Hall');
      expect(screen.queryByRole('button', { name: 'Edit needs' })).toBeNull();
      expect(screen.queryByRole('button', { name: 'New project' })).toBeNull();
    }, SCREEN_TIMEOUT);
  });
});
