/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/** Several people into one accommodation: choose, look at what would happen, then write. */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 100, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

const result = (preview: boolean, saved: number, status = 'Ok', extra: Record<string, unknown> = {}) => ({
  preview,
  beds: 1,
  peakOccupancy: 2,
  peakDate: '2030-03-01',
  saved,
  rows: [
    { employeeId: 'e1', employeeName: 'Marko Ilić', startDate: '2030-03-01', endDate: null, projectId: null, projectName: null, status, reason: null, place: null, placeUntil: null, stayId: preview ? null : 's1' },
    { employeeId: 'e2', employeeName: 'Ana Novak', startDate: '2030-03-01', endDate: null, projectId: null, projectName: null, status: 'Conflict', reason: 'AlreadyHoused', place: 'Kuća Lamela 3', placeUntil: '2030-11-30', stayId: null, ...extra },
  ],
});

async function open() {
  network.reply('/employees', 200, page([
    { id: 'e1', fullName: 'Marko Ilić', currentProjectNames: [] },
    { id: 'e2', fullName: 'Ana Novak', currentProjectNames: [] },
  ]));
  network.reply('/projects', 200, page([]));
  const { BulkStayDialog } = await import('./BulkStayDialog');

  renderScreen(<BulkStayDialog open accommodationId="a1" title="Apartman Kozarska 12" onClose={() => undefined} />, {
    route: '/',
    path: '/',
    user: signedIn('Admin'),
  });

  return userEvent.setup();
}

describe('BulkStayDialog', () => {
  it('shows what would happen to each person, then saves, and says who was not saved', async () => {
    const user = await open();
    network.reply('/stays/bulk', 200, result(true, 0, 'OverCapacity'));

    await user.click(await screen.findByRole('combobox', { name: /dodaj radnike|add people/i }));
    await user.click(await screen.findByRole('option', { name: 'Marko Ilić' }));
    await user.click(screen.getByRole('combobox', { name: /dodaj radnike|add people/i }));
    await user.click(await screen.findByRole('option', { name: 'Ana Novak' }));
    await user.click(screen.getByRole('button', { name: /dalje: pregled|next: check/i }));

    // The preview says the capacity is over (a warning) and why the second person cannot be housed.
    expect(await screen.findByText(/najviše 2 od 1 ležajeva|at most 2 of 1 beds/i)).toBeDefined();
    expect(screen.getByText(/Kuća Lamela 3/)).toBeDefined();

    network.reply('/stays/bulk', 200, result(false, 1));
    await user.click(screen.getByRole('button', { name: /snimi 1|save 1/i }));

    expect(await screen.findByText(/snimljeno 1 od 2|saved 1 of 2/i)).toBeDefined();

    await waitFor(() => {
      const posts = network.calls.filter((c) => c.method.toLowerCase() === 'post' && c.url.includes('/stays/bulk'));
      expect(posts).toHaveLength(2);
      expect(posts[0].body).toMatchObject({ preview: true });
      expect(posts[1].body).toMatchObject({ preview: false });
      expect((posts[1].body as { stays: unknown[] }).stays).toHaveLength(2);
      // The preview and the save are different requests, so they must not share an idempotency key.
      expect(posts[0].headers['idempotency-key']).not.toEqual(posts[1].headers['idempotency-key']);
    });
  }, SCREEN_TIMEOUT);
});
