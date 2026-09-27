/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * A person's leave in working days: the right, what carried over and when it runs out, what
 * was taken and what is left. Only management may write a correction, and a correction needs
 * whole days and a reason.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function balance(overrides: Record<string, unknown> = {}) {
  return {
    employeeId: 'e1',
    year: new Date().getFullYear(),
    allowanceDays: 25,
    usedDays: 8,
    remainingDays: 17,
    entitlementDays: 20,
    adjustmentDays: 0,
    carriedOverDays: 8,
    carriedOverUsedDays: 5,
    carriedOverExpiredDays: 3,
    carryOverExpiresOn: `${new Date().getFullYear()}-06-01`,
    carryingIntoNextYearDays: 12,
    ...overrides,
  };
}

async function renderCard(role: Parameters<typeof signedIn>[0] = 'Admin') {
  const { EmployeeLeaveCard } = await import('./EmployeeLeaveCard');

  return renderScreen(<EmployeeLeaveCard employeeId="e1" />, { route: '/', path: '/', user: signedIn(role) });
}

describe('EmployeeLeaveCard', () => {
  it('shows the right, the carried-over days with what expired, what was taken and what is left', async () => {
    network.reply('/absences/balance', 200, balance());
    network.reply('/absences/adjustments', 200, []);

    await renderCard();

    expect(await screen.findByText('Right for the year')).toBeDefined();
    expect(screen.getByText('Carried over from last year')).toBeDefined();
    expect(screen.getByText(/Carried-over days valid until/)).toBeDefined();
    expect(screen.getByText('3 lost')).toBeDefined();
    expect(screen.getByText('Remaining')).toBeDefined();
    expect(screen.getByText('17')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('shows who made a correction and why', async () => {
    network.reply('/absences/balance', 200, balance({ adjustmentDays: 3 }));
    network.reply('/absences/adjustments', 200, [
      { id: 'a1', employeeId: 'e1', year: 2026, days: 3, reason: 'Prenos iz stare evidencije', createdAt: '2026-09-27T08:00:00Z', createdBy: 'admin@example.test' },
    ]);

    await renderCard();

    expect(await screen.findByText(/Prenos iz stare evidencije/)).toBeDefined();
    expect(screen.getByText(/admin@example\.test/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('offers management the correction and nobody below', async () => {
    network.reply('/absences/balance', 200, balance());
    network.reply('/absences/adjustments', 200, []);

    await renderCard('Admin');
    expect(await screen.findByRole('button', { name: 'Correct leave' })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('does not offer a project manager the correction', async () => {
    network.reply('/absences/balance', 200, balance());
    network.reply('/absences/adjustments', 200, []);

    await renderCard('ProjectManager');
    await screen.findByText('Right for the year');

    expect(screen.queryByRole('button', { name: 'Correct leave' })).toBeNull();
  }, SCREEN_TIMEOUT);

  it('refuses zero days and a missing reason before asking the server, and sends whole days with the reason', async () => {
    network.reply('/absences/balance', 200, balance());
    network.reply('/absences/adjustments', 200, []);
    network.reply('/absences/adjustments', 201, { id: 'a2' });

    await renderCard('Admin');
    await userEvent.click(await screen.findByRole('button', { name: 'Correct leave' }));

    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    expect(await screen.findByText('Enter a whole number of days, not zero.')).toBeDefined();

    await userEvent.type(screen.getByLabelText('Days (+ or −)'), '2');
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    expect(await screen.findByText('A reason is required.')).toBeDefined();

    expect(network.calls.filter((c) => c.method === 'POST')).toHaveLength(0);

    await userEvent.type(screen.getByLabelText('Reason'), 'Prenos');
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    const posts = network.calls.filter((c) => c.method === 'POST' && c.url.includes('/absences/adjustments'));
    expect(posts).toHaveLength(1);
    expect(JSON.stringify(posts[0]?.body)).toContain('"days":2');
    expect(JSON.stringify(posts[0]?.body)).toContain('Prenos');
  }, SCREEN_TIMEOUT);
});
