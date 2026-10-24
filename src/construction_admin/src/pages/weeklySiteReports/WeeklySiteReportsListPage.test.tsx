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
    pageCount: 2,
    lineCount: 0,
    source: 'App',
    sourceNote: null,
    enteredByEmail: null,
    status: 'Submitted',
    processedAt: null,
    processedByEmail: null,
    reviewNote: null,
    createdAt: '2026-10-19T08:00:00Z',
    ...overrides,
  };
}

/** The report as the review dialog loads it: pictures, workers, and what the monthly sheet holds for each. */
function detail(overrides: Record<string, unknown> = {}) {
  return {
    ...report({ lineCount: 2 }),
    pages: [
      { id: 'pg1', fileName: 'strana1.jpg', contentType: 'image/jpeg', sizeBytes: 10 },
      { id: 'pg2', fileName: 'strana2.jpg', contentType: 'image/jpeg', sizeBytes: 10 },
    ],
    lines: [
      { id: 'l1', employeeId: 'e1', employeeName: 'Adin Čengić', hours: 45, hoursSecondMonth: 0, existingFirstMonth: null, existingSecondMonth: null },
      { id: 'l2', employeeId: 'e2', employeeName: 'Igor Pijetlović', hours: 42.5, hoursSecondMonth: 0, existingFirstMonth: 40, existingSecondMonth: null },
    ],
    firstMonth: { year: 2026, month: 10, ledgerFound: true, ledgerName: 'Oktobar', locked: false },
    secondMonth: null,
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

  it('opens the report with its pictures and the workers beside them, and enters the hours when approved', async () => {
    network.reply('/weeklysitereports/w1', 200, detail({ note: 'Kupac potpisao u petak' }));
    await renderReports([report({ lineCount: 2 })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));

    const dialog = await screen.findByRole('dialog');
    expect(await within(dialog).findByText('Adin Čengić')).toBeDefined();
    expect(within(dialog).getByText('Igor Pijetlović')).toBeDefined();
    expect(within(dialog).getByText('Kupac potpisao u petak')).toBeDefined();
    // Both pictures can be turned to.
    expect(within(dialog).getByRole('button', { name: /^(Slika 2|Picture 2)$/ })).toBeDefined();

    await userEvent.click(within(dialog).getByRole('button', { name: /(Odobri i upiši|Approve and enter)/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/w1/approve'));
    expect(posts).toHaveLength(1);
    const body = JSON.stringify(posts[0].body);
    expect(body).toContain('"employeeId":"e1"');
    expect(body).toContain('"hours":45');
  }, SCREEN_TIMEOUT);

  it('offers to keep what the sheet already says when it differs, and sends that choice', async () => {
    network.reply('/weeklysitereports/w1', 200, detail());
    await renderReports([report({ lineCount: 2 })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const dialog = await screen.findByRole('dialog');
    await within(dialog).findByText('Igor Pijetlović');

    expect(within(dialog).getByText(/(Razlikuje se: sada 40|Differs: now 40)/)).toBeDefined();
    await userEvent.click(within(dialog).getByRole('button', { name: /^(Zadrži staro|Keep the old)$/ }));
    await userEvent.click(within(dialog).getByRole('button', { name: /(Odobri i upiši|Approve and enter)/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/w1/approve'));
    expect(JSON.stringify(posts[0].body)).toContain('"keepExisting":["e2"]');
  }, SCREEN_TIMEOUT);

  it('lets the office correct the hours before approving', async () => {
    network.reply('/weeklysitereports/w1', 200, detail());
    await renderReports([report({ lineCount: 2 })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const dialog = await screen.findByRole('dialog');
    await within(dialog).findByText('Adin Čengić');

    const hours = within(dialog).getAllByRole('textbox', { name: /^(Sati|Hours)$/ });
    await userEvent.clear(hours[0]);
    await userEvent.type(hours[0], '40');
    await userEvent.click(within(dialog).getByRole('button', { name: /(Odobri i upiši|Approve and enter)/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/w1/approve'));
    expect(JSON.stringify(posts[0].body)).toContain('"hours":40');
  }, SCREEN_TIMEOUT);

  it('will not approve while the month has no sheet to enter the hours in', async () => {
    network.reply(
      '/weeklysitereports/w1',
      200,
      detail({ firstMonth: { year: 2026, month: 10, ledgerFound: false, ledgerName: null, locked: false } }),
    );
    await renderReports([report({ lineCount: 2 })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const dialog = await screen.findByRole('dialog');
    await within(dialog).findByText('Adin Čengić');

    const approve = within(dialog).getByRole('button', { name: /(Odobri i upiši|Approve and enter)/ }) as HTMLButtonElement;
    expect(approve.disabled).toBe(true);
    expect(within(dialog).getByText(/(nema mjesečne Evidencije|no monthly Ledger)/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('sends a report back only with a reason, and passes the reason on', async () => {
    network.reply('/weeklysitereports/w1', 200, detail());
    await renderReports([report({ lineCount: 2 })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const preview = await screen.findByRole('dialog');
    await within(preview).findByText('Adin Čengić');
    await userEvent.click(within(preview).getByRole('button', { name: /^(Potrebna korekcija|Needs correction)$/ }));

    const send = await screen.findByRole('button', { name: /^(Pošalji poslovođi|Send to the foreman)$/ });
    expect((send as HTMLButtonElement).disabled).toBe(true);

    const reason = screen.getAllByRole('textbox').find((box) => box.tagName === 'TEXTAREA') as HTMLElement;
    await userEvent.type(reason, 'Nedostaje potpis kupca');
    expect((send as HTMLButtonElement).disabled).toBe(false);
    await userEvent.click(send);

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/w1/request-correction'));
    expect(posts).toHaveLength(1);
    expect(JSON.stringify(posts[0].body)).toContain('Nedostaje potpis kupca');
  }, SCREEN_TIMEOUT);

  it('says which reports the office entered, and what it said about where they came from', async () => {
    network.reply('/weeklysitereports/w1', 200, detail({ source: 'Office', enteredByEmail: 'ured@firma.test', sourceNote: 'mail od kupca 20.10.' }));
    await renderReports([report({ lineCount: 2, source: 'Office', enteredByEmail: 'ured@firma.test', sourceNote: 'mail od kupca 20.10.' })]);
    await screen.findByText('Hala B');

    expect(screen.getByText(/^(Uprava|Office)$/)).toBeDefined();

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const dialog = await screen.findByRole('dialog');
    expect(await within(dialog).findByText(/mail od kupca 20\.10\./)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('saves the workers for later without approving, and the report keeps waiting', async () => {
    network.reply('/weeklysitereports/w1', 200, detail());
    await renderReports([report({ lineCount: 2 })]);
    await screen.findByText('Hala B');

    await userEvent.click(screen.getByRole('button', { name: /^(Pregledaj|Preview)$/ }));
    const dialog = await screen.findByRole('dialog');
    await within(dialog).findByText('Adin Čengić');

    await userEvent.click(within(dialog).getByRole('button', { name: /^(Sačuvaj za kasnije|Save for later)$/ }));

    const puts = network.calls.filter((call) => call.method === 'PUT' && call.url.endsWith('/w1/lines'));
    expect(puts).toHaveLength(1);
    expect(JSON.stringify(puts[0].body)).toContain('"employeeId":"e1"');
    expect(network.calls.filter((call) => call.url.endsWith('/w1/approve'))).toHaveLength(0);
    expect(await within(dialog).findByText(/(Sačuvano|Saved)\./)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('offers the office to enter a report that came by e-mail', async () => {
    await renderReports([report()]);

    expect(await screen.findByRole('button', { name: /(Unesi izvještaj|Enter a report)/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('lists the late sites with the foreman to ask when Late is chosen', async () => {
    await renderReports([report()]);

    await userEvent.click(await screen.findByRole('button', { name: /(Kasni|Late) \(1\)/ }));

    expect(await screen.findByText('Zgrada D')).toBeDefined();
    expect(screen.getByText(/Ivan Horvat/)).toBeDefined();
  }, SCREEN_TIMEOUT);
});
