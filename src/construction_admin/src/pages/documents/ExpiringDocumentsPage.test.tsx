/**
 * @vitest-environment jsdom
 */
import { cleanup, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';
import { ExpiringDocumentsPage } from './ExpiringDocumentsPage';

const SCREEN_TIMEOUT = 60_000;

function document(id: string, overrides: Record<string, unknown> = {}) {
  return {
    id,
    fileName: `${id}.pdf`,
    contentType: 'application/pdf',
    sizeBytes: 100,
    category: 'Other',
    description: null,
    title: null,
    tags: [],
    expiresAt: null,
    retainUntil: null,
    reminderDays: [],
    ownerType: 'Employee',
    ownerId: 'e1',
    ownerName: 'Marko Markovic',
    uploadedByName: null,
    createdAt: '2026-10-01T00:00:00Z',
    ...overrides,
  };
}

let network: FakeNetwork;

afterEach(() => cleanup());

beforeEach(() => {
  window.localStorage.clear();
  window.localStorage.setItem('files.hint.hub-search', 'done');
  network = installFakeNetwork();
  network.reply('/attachments/expiring', 200, [
    document('a', { title: 'Zaštita na radu', tags: ['bzr'], ownerName: 'Petar Petrovic' }),
    document('b', { fileName: 'ugovor-dobavljac.pdf', tags: ['ugovor', 'nabavka'] }),
    document('c', { fileName: 'racun.pdf', description: 'Račun za gorivo' }),
  ]);
});

describe('the all-documents search', () => {
  it(
    'narrows the list by title, owner, note and tag, and by a chosen tag',
    async () => {
      const user = userEvent.setup();
      renderScreen(<ExpiringDocumentsPage />);

      await screen.findByText('Zaštita na radu');
      expect(screen.getByText('ugovor-dobavljac.pdf')).toBeTruthy();

      const box = screen.getByRole('searchbox');

      await user.type(box, 'petrovic');
      await waitFor(() => expect(screen.queryByText('ugovor-dobavljac.pdf')).toBeNull());
      expect(screen.getByText('Zaštita na radu')).toBeTruthy();

      await user.clear(box);
      await user.type(box, 'gorivo');
      await waitFor(() => expect(screen.queryByText('Zaštita na radu')).toBeNull());
      expect(screen.getByText('racun.pdf')).toBeTruthy();

      await user.clear(box);
      await user.type(box, 'ugovor nabavka');
      await waitFor(() => expect(screen.queryByText('racun.pdf')).toBeNull());
      expect(screen.getByText('ugovor-dobavljac.pdf')).toBeTruthy();

      await user.clear(box);
      await user.type(box, 'nepostojece');
      expect(await screen.findByText('No document matches the search and filters.')).toBeTruthy();
    },
    SCREEN_TIMEOUT,
  );
});

describe('picking documents to delete for good', () => {
  const rowOf = (name: string) => screen.getByText(name).closest('tr') as HTMLElement;

  it(
    'deletes nothing until confirmed, then removes exactly the picked ones and says what it freed',
    async () => {
      const user = userEvent.setup();
      network.reply('/attachments/delete', 200, { removed: 2, freedBytes: 200, keptForRetention: 0, notAllowed: 0, notFound: 0 });
      renderScreen(<ExpiringDocumentsPage />, { user: signedIn('Admin') });

      await screen.findByText('Zaštita na radu');

      await user.click(within(rowOf('Zaštita na radu')).getByRole('checkbox'));
      await user.click(within(rowOf('racun.pdf')).getByRole('checkbox'));

      expect((await screen.findByTestId('selection-summary')).textContent).toBe('2 selected (0 MB)');
      await user.click(screen.getByRole('button', { name: 'Delete selected for good' }));

      expect(network.calls.some((call) => call.url.endsWith('/attachments/delete'))).toBe(false);
      expect(await screen.findByText('Delete the selected documents for good?')).toBeTruthy();

      const buttons = await screen.findAllByRole('button', { name: 'Delete selected for good' });
      await user.click(buttons[buttons.length - 1]);

      expect((await screen.findByTestId('bulk-result')).textContent).toBe('Documents deleted: 2; freed 0 MB.');

      const call = network.calls.find((c) => c.url.endsWith('/attachments/delete'));
      expect(call?.method).toBe('POST');
      expect((call?.body as { ids: string[] }).ids.sort()).toEqual(['a', 'c']);
    },
    SCREEN_TIMEOUT,
  );

  it(
    'cannot pick a document under a retention date or one that has its own screen',
    async () => {
      // A fresh network: answers queued on the shared one would be served first.
      network = installFakeNetwork();
      network.reply('/attachments/expiring', 200, [
        document('free', { fileName: 'slobodan.pdf' }),
        document('kept', { fileName: 'cuvan.pdf', retainUntil: '2999-01-01' }),
        document('book', { fileName: 'knjiga.pdf', ownerType: 'BookEntry' }),
      ]);
      renderScreen(<ExpiringDocumentsPage />, { user: signedIn('SuperAdmin') });

      await screen.findByText('slobodan.pdf');

      expect((within(rowOf('slobodan.pdf')).getByRole('checkbox') as HTMLInputElement).disabled).toBe(false);
      expect((within(rowOf('cuvan.pdf')).getByRole('checkbox') as HTMLInputElement).disabled).toBe(true);
      expect((within(rowOf('knjiga.pdf')).getByRole('checkbox') as HTMLInputElement).disabled).toBe(true);
    },
    SCREEN_TIMEOUT,
  );

  it(
    'shows no checkboxes to somebody who may not delete',
    async () => {
      renderScreen(<ExpiringDocumentsPage />, { user: signedIn('ProjectManager') });

      await screen.findByText('Zaštita na radu');

      expect(screen.queryAllByRole('checkbox')).toHaveLength(0);
    },
    SCREEN_TIMEOUT,
  );
});
