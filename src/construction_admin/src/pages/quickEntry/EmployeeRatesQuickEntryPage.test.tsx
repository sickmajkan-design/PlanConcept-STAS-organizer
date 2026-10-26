/**
 * @vitest-environment jsdom
 */
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/** A raise for chosen people: worked out from the rate in force, looked at, and put in force from a date. */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 100, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

describe('EmployeeRatesQuickEntryPage', () => {
  it('raises the chosen rate by a percent, shows the review and saves it from the chosen date', async () => {
    network.reply('/employees', 200, page([
      { id: 'e1', fullName: 'Marko Ilić', position: 'Zidar', status: 'Active' },
      { id: 'e2', fullName: 'Ana Novak', position: 'Zidar', status: 'Active' },
    ]));
    network.reply('/employee-rates', 200, {
      ...page([
        { id: 'r1', employeeId: 'e1', employeeName: 'Marko Ilić', rateType: 'Hourly', hourlyRate: 10, weekendHourlyRate: 15, holidayHourlyRate: null, overtimeHourlyRate: null, travelHourlyRate: null, dailyRate: null, startDate: '2026-01-01', endDate: null },
      ]),
    });
    const { EmployeeRatesQuickEntryPage } = await import('./EmployeeRatesQuickEntryPage');
    renderScreen(<EmployeeRatesQuickEntryPage />, { route: '/', path: '/', user: signedIn('SuperAdmin') });
    const user = userEvent.setup();

    expect(await screen.findByText('Marko Ilić')).toBeDefined();

    // Choose the first person and raise by 10 %.
    await user.click(within(screen.getAllByRole('row')[1]).getByRole('checkbox'));
    const value = screen.getByRole('textbox', { name: /vrijednost|value/i });
    await user.clear(value);
    await user.type(value, '10');
    await user.click(screen.getByRole('button', { name: /primijeni na označene \(1\)|apply to selected \(1\)/i }));
    await user.click(await screen.findByRole('button', { name: /pregled i snimi \(1\)|review and save \(1\)/i }));

    // The review names the person and the difference before anything is written.
    expect(await screen.findByText(/razlika na sat|difference per hour/i)).toBeDefined();
    await user.click(screen.getByRole('button', { name: /snimi cijena: 1|save 1 rates/i }));

    await waitFor(() => {
      const posts = network.calls.filter((c) => c.method.toLowerCase() === 'post' && c.url.includes('/employee-rates'));
      expect(posts).toHaveLength(1);
      // 10 raised by 10 % is 11, and the weekend premium moves with it (15 becomes 16.5).
      expect(posts[0].body).toMatchObject({ employeeId: 'e1', rateType: 'Hourly', hourlyRate: 11, weekendHourlyRate: 16.5 });
      expect((posts[0].body as { startDate: string }).startDate).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    });
  }, SCREEN_TIMEOUT);
});
