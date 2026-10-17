/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen } from '../../test/renderScreen';
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

beforeEach(() => {
  window.localStorage.clear();
  window.localStorage.setItem('files.hint.hub-search', 'done');
  const network = installFakeNetwork();
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
