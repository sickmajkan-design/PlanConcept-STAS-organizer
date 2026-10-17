/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../test/renderScreen';
import { AttachmentList } from './AttachmentList';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const document = (id: string, shared: boolean) => ({
  id,
  fileName: `${id}.pdf`,
  contentType: 'application/pdf',
  sizeBytes: 1000,
  category: 'Certificate',
  description: null,
  title: `Dokument ${id}`,
  tags: [],
  expiresAt: null,
  retainUntil: null,
  reminderDays: [],
  sharedWithEmployee: shared,
  ownerType: 'Employee',
  ownerId: 'e1',
  ownerName: 'Marko',
  uploadedByName: null,
  createdAt: '2026-10-01T00:00:00Z',
});

describe('sharing documents with the person they are about', () => {
  it('marks what is shared and shares one document on request', async () => {
    network.reply('/attachments', 200, [document('a', false), document('b', true)]);
    network.reply('/attachments/sharing', 200, 1);

    renderScreen(
      <AttachmentList ownerType="Employee" ownerId="e1" categories={['Certificate']} canDelete />,
      { user: signedIn('Admin') },
    );

    expect(await screen.findByText(/Dokument a/)).toBeTruthy();
    expect(screen.getAllByText(/Samo uprava|Office only/)).toHaveLength(1);
    expect(screen.getAllByText(/Vidljivo osobi|Visible to the person/)).toHaveLength(1);

    await userEvent.click(screen.getByRole('button', { name: /Podijeli s osobom|Share with the person/ }));

    await waitFor(() => {
      const call = network.calls.find((c) => c.url.includes('/attachments/sharing'));
      expect(call).toBeTruthy();
      expect(call!.body).toEqual({ ids: ['a'], shared: true });
    });
  }, SCREEN_TIMEOUT);

  it('offers no sharing on a project, or where the account may not manage documents', async () => {
    network.reply('/attachments', 200, [document('a', false)]);

    renderScreen(
      <AttachmentList ownerType="Project" ownerId="p1" categories={['Certificate']} canDelete />,
      { user: signedIn('Admin') },
    );

    expect(await screen.findByText(/Dokument a/)).toBeTruthy();
    expect(screen.queryByRole('button', { name: /Podijeli s osobom|Share with the person/ })).toBeNull();
  }, SCREEN_TIMEOUT);
});
