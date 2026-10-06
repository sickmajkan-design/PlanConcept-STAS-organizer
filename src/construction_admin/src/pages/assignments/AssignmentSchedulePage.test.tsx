/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * The planning board has to make the problems visible without anyone hunting for them: who did
 * not clock in, who is late, who is posted while on approved leave, and who is simply free.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;
const SITE = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const today = new Date().toISOString().slice(0, 10);

function employee(id: string, fullName: string, status: string, overrides: Record<string, unknown> = {}) {
  return {
    id,
    fullName,
    position: 'Zidar',
    status,
    clockedInAt: null,
    todayProjectId: SITE,
    postings: [{ projectId: SITE, startDate: today, endDate: null }],
    absences: [],
    vehicles: [],
    tools: [],
    ...overrides,
  };
}

function schedule(employees: unknown[]) {
  return {
    from: today,
    days: 14,
    today,
    lateToleranceMinutes: 15,
    projects: [{ id: SITE, name: 'Aldi Hall', status: 'Active', shiftStartTime: '07:00:00', endDate: null }],
    employees,
  };
}

async function renderBoard() {
  const { AssignmentSchedulePage } = await import('./AssignmentSchedulePage');
  return renderScreen(<AssignmentSchedulePage />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('AssignmentSchedulePage', () => {
  it('lists the problems of the day above the grid', async () => {
    network.reply(
      '/assignment-board/schedule',
      200,
      schedule([
        employee('e1', 'Luka Babic', 'NoShow'),
        employee('e2', 'Dino Hodzic', 'Late', { clockedInAt: `${today}T08:00:00Z` }),
        employee('e3', 'Ivan Peric', 'OnSite', { clockedInAt: `${today}T07:02:00Z` }),
        employee('e4', 'Stefan Maric', 'Free', { todayProjectId: null, postings: [] }),
      ]),
    );

    await renderBoard();

    expect(await screen.findByText(/Luka Babic .*Aldi Hall/)).toBeDefined();
    expect(screen.getByText(/Dino Hodzic .*Aldi Hall/)).toBeDefined();
    // The on-time worker is not a problem.
    expect(screen.queryByText(/Ivan Peric .*(nije|not clocked)/i)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('lists an empty position and offers replacements who are free', async () => {
    const day = new Date();
    // The next working day, so the leave is not on a weekend.
    do day.setUTCDate(day.getUTCDate() + 1); while ([0, 6].includes(day.getUTCDay()));
    const leaveDay = day.toISOString().slice(0, 10);

    network.reply(
      '/assignment-board/schedule',
      200,
      schedule([
        employee('e1', 'Emir Delic', 'Expected', { absences: [{ type: 'AnnualLeave', startDate: leaveDay, endDate: leaveDay }] }),
        employee('e2', 'Kenan Dizdar', 'Free', { todayProjectId: null, postings: [] }),
      ]),
    );

    await renderBoard();

    expect(await screen.findByText(/(Approved leave . replacement needed|Odobrena odsustva . treba zamjena)/i)).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: /(Find replacement|Nađi zamjenu)/ }));

    expect((await screen.findAllByText('Kenan Dizdar')).length).toBeGreaterThan(0);
    await userEvent.click(screen.getByRole('button', { name: /^(Assign|Dodijeli)$/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.includes('/e2/projects/'));
    expect(posts).toHaveLength(1);
    expect(JSON.stringify(posts[0].body)).toContain(leaveDay);
  }, SCREEN_TIMEOUT);

  it('asks for a different period when the number of weeks changes', async () => {
    network.reply('/assignment-board/schedule', 200, schedule([employee('e1', 'Ivan Peric', 'OnSite')]));

    await renderBoard();
    await screen.findAllByText('Ivan Peric');
    await userEvent.click(screen.getByRole('button', { name: /^(4 weeks|4 sedmice)$/i }));

    await screen.findAllByText('Ivan Peric');
    const calls = network.calls.filter((call) => call.url.includes('/assignment-board/schedule'));
    expect(calls.some((call) => call.params.days === 28)).toBe(true);
  }, SCREEN_TIMEOUT);
});
