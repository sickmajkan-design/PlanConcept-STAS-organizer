/**
 * @vitest-environment jsdom
 */
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * The office reads a weekly site report in the panel, then approves it or sends it back with a reason; what
 * waits and what is late is one click away, and a report already approved offers nothing more to approve.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function report(overrides: Record<string, unknown> = {}) {
  return {
    id: 'w1',
    projectId: 'p1',
    projectName: 'Hala B',
    submittedByEmployeeId: 'e1',
    submittedByEmployeeName: 'Marko Marković',
    isoYear: 2026,
    isoWeek: 42,
    type: 'SignedHours',
    quantity: 212.5,
    note: null,
    fileName: 'satnica.pdf',
    status: 'Submitted',
    processedAt: null,
    processedByEmail: null,
    reviewNote: null,
    createdAt: '2026-10-19T08:00:00Z',
    ...overrides,
  };
}

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 20, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

function summary(overrides: Record<string, unknown> = {}) {
  return {
    awaitingReview: 2,
    correctionRequested: 1,
    waiting: [],
    isoYear: 2026,
    isoWeek: 42,
    deadlineUtc: '2026-10-19T10:00:00Z',
    overdue: [{ projectId: 'p9', projectName: 'Zgrada D', foremanNames: ['Ivan Horvat'] }],
    ...overrides,
  };
}

async function renderReports(items: unknown[]) {
  network.reply('/weeklysitereports/summary', 200, summary());
  network.reply('/weeklysitereports/reportable-projects', 200, []);
  network.reply('/employees', 200, page([]));
  network.reply('/weeklysitereports', 200, page(items));

  const { WeeklySiteReportsListPage } = await import('./WeeklySiteReportsListPage');

  return renderScreen(<WeeklySiteReportsListPage />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('WeeklySiteReportsListPage', () => {
  it('counts what waits, what was sent back and what is late on the chips', async () => {
    await renderReports([report()]);

    expect(await screen.findByRole('button', { name: /(Čeka pregled|Waiting for review) \(2\)/ })).toBeDefined();
    expect(screen.getByRole('button', { name: /(Potrebna korekcija|Needs correction) \(1\)/ })).toBeDefined();
    expect(screen.getByRole('button', { name: /(Kasni|Late) \(1\)/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('says plainly what each button on a row does', async () => {
    await renderReports([report()]);
    await screen.findByText('Hala B');

    expect(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ })).toBeDefined();
    expect(screen.getByRole('button', { name: /^(Preuzmi|Download)$/ })).toBeDefined();
    expect(screen.getByRole('button', { name: /^(Odobri|Approve)$/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('offers nothing to approve on a report that is already approved', async () => {
    await renderReports([report({ status: 'Processed', processedAt: '2026-10-20T09:00:00Z', processedByEmail: 'a@b.c' })]);
    await screen.findByText('Hala B');

    expect(screen.queryByRole('button', { name: /^(Odobri|Approve)$/ })).toBeNull();
    expect(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('opens the report in a preview with the facts beside it and approves from there', async () => {
    await renderReports([report({ note: 'Kupac potpisao u petak' })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText('Kupac potpisao u petak')).toBeDefined();
    expect(within(dialog).getByText('Marko Marković')).toBeDefined();

    await userEvent.click(within(dialog).getByRole('button', { name: /^(Odobri|Approve)$/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/w1/approve'));
    expect(posts).toHaveLength(1);
  }, SCREEN_TIMEOUT);

  it('sends a report back only with a reason, and passes the reason on', async () => {
    await renderReports([report()]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const preview = await screen.findByRole('dialog');
    await userEvent.click(within(preview).getByRole('button', { name: /^(Potrebna korekcija|Needs correction)$/ }));

    const send = await screen.findByRole('button', { name: /^(Pošalji poslovođi|Send to the foreman)$/ });
    expect((send as HTMLButtonElement).disabled).toBe(true);

    await userEvent.type(screen.getByRole('textbox'), 'Nedostaje potpis kupca');
    expect((send as HTMLButtonElement).disabled).toBe(false);
    await userEvent.click(send);

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/w1/request-correction'));
    expect(posts).toHaveLength(1);
    expect(JSON.stringify(posts[0].body)).toContain('Nedostaje potpis kupca');
  }, SCREEN_TIMEOUT);

  it('lists the late sites with the foreman to ask when Late is chosen', async () => {
    await renderReports([report()]);

    await userEvent.click(await screen.findByRole('button', { name: /(Kasni|Late) \(1\)/ }));

    expect(await screen.findByText('Zgrada D')).toBeDefined();
    expect(screen.getByText(/Ivan Horvat/)).toBeDefined();
  }, SCREEN_TIMEOUT);
});
