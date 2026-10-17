/**
 * @vitest-environment jsdom
 */
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import type { DocumentRequest } from '../../api/documentRequests';
import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';
import { DocumentRequestsPage } from './DocumentRequestsPage';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const request = (overrides: Partial<DocumentRequest> = {}): DocumentRequest => ({
  id: 'r1',
  employeeId: 'e1',
  employeeName: 'Marko Radnik',
  category: 'Licence',
  title: 'Vozačka dozvola',
  note: null,
  dueDate: null,
  status: 'Submitted',
  attempts: 1,
  requestedByName: 'uprava@example.test',
  createdAt: '2026-10-15T09:00:00Z',
  reviewedAt: null,
  returnReason: null,
  files: [{ id: 'f1', fileName: 'vozacka.jpg', contentType: 'image/jpeg', sizeBytes: 1000 }],
  events: [
    { type: 'Created', at: '2026-10-15T09:00:00Z', byEmployee: false, actorName: 'uprava@example.test', reason: null, files: [] },
    { type: 'Submitted', at: '2026-10-15T10:00:00Z', byEmployee: true, actorName: null, reason: null, files: [] },
  ],
  ...overrides,
});

const page = (items: DocumentRequest[]) => ({
  items,
  pageNumber: 1,
  pageSize: 25,
  totalCount: items.length,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
});

function renderPage() {
  return renderScreen(<DocumentRequestsPage />, { user: signedIn('Admin') });
}

describe('document requests', () => {
  it('lists what is waiting to be checked and opens it', async () => {
    network.reply('/documentrequests', 200, page([request()]));
    network.reply('/documentrequests/r1', 200, request());
    renderPage();

    expect(await screen.findByText('Marko Radnik')).toBeTruthy();
    expect(screen.getByText('Vozačka dozvola')).toBeTruthy();

    await userEvent.click(screen.getByRole('button', { name: /Provjeri|Check/ }));

    const dialog = await screen.findByRole('dialog');
    expect(await within(dialog).findByText('vozacka.jpg')).toBeTruthy();
  }, SCREEN_TIMEOUT);

  it('will not accept a document that lapses without the date it is valid until', async () => {
    network.reply('/documentrequests', 200, page([request()]));
    network.reply('/documentrequests/r1', 200, request());
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: /Provjeri|Check/ }));

    const dialog = await screen.findByRole('dialog');
    const accept = await within(dialog).findByRole('button', { name: /^(Prihvati|Accept)$/ });

    expect((accept as HTMLButtonElement).disabled).toBe(true);
  }, SCREEN_TIMEOUT);

  it('sends a document back only with a reason, and says what status it is now in', async () => {
    network.reply('/documentrequests', 200, page([request()]));
    network.reply('/documentrequests/r1', 200, request());
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: /Provjeri|Check/ }));

    const dialog = await screen.findByRole('dialog');
    await userEvent.click(await within(dialog).findByRole('button', { name: /Vrati…|Send back…/ }));

    const sendBack = within(dialog).getByRole('button', { name: /^(Vrati osobi|Send back)$/ });
    expect((sendBack as HTMLButtonElement).disabled).toBe(true);

    network.reply('/documentrequests/r1/return', 200, request({ status: 'Returned', returnReason: 'Slika je mutna' }));

    await userEvent.type(within(dialog).getByRole('textbox'), 'Slika je mutna');
    expect((sendBack as HTMLButtonElement).disabled).toBe(false);

    await userEvent.click(sendBack);

    await waitFor(() => {
      const call = network.calls.find((c) => c.method === 'POST' && c.url.endsWith('/documentrequests/r1/return'));
      expect(call).toBeTruthy();
      expect(call!.body).toEqual({ reason: 'Slika je mutna' });
    });

    expect(await within(dialog).findByText(/Obaviješten|told|obaviještena/)).toBeTruthy();
  }, SCREEN_TIMEOUT);

  it('marks a request past its date as overdue', async () => {
    network.reply(
      '/documentrequests',
      200,
      page([request({ status: 'Open', dueDate: '2020-01-01', files: [], attempts: 0 })]),
    );
    renderPage();

    expect(await screen.findByText(/Kasni|Overdue/)).toBeTruthy();
  }, SCREEN_TIMEOUT);

  it('deletes a withdrawn request only after being asked to confirm', async () => {
    const withdrawn = request({ status: 'Cancelled', files: [], attempts: 0 });
    network.reply('/documentrequests', 200, page([withdrawn]));
    network.reply('/documentrequests/r1', 200, withdrawn);
    renderPage();

    await userEvent.click(await screen.findByRole('tab', { name: /Povučeno|Withdrawn/ }));
    await userEvent.click(await screen.findByRole('button', { name: /^(Otvori|Open)$/ }));

    const dialog = await screen.findByRole('dialog');
    await userEvent.click(await within(dialog).findByRole('button', { name: /Obriši zahtjev|Delete request/ }));

    // Nothing is sent until it is confirmed.
    expect(network.calls.some((c) => c.method === 'DELETE')).toBe(false);

    const confirm = (await screen.findAllByRole('dialog')).at(-1)!;
    network.reply('/documentrequests/r1', 204);
    await userEvent.click(within(confirm).getByRole('button', { name: /^(Obriši|Delete)$/ }));

    await waitFor(() => {
      const call = network.calls.find((c) => c.method === 'DELETE' && c.url.endsWith('/documentrequests/r1'));
      expect(call).toBeTruthy();
    });
  }, SCREEN_TIMEOUT);
});
