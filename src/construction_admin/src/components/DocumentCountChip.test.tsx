/** @vitest-environment jsdom */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, type FakeNetwork } from '../test/renderScreen';
import { DocumentCountChip } from './DocumentCountChip';

/**
 * The number in a record's header is the number of documents its list shows. It reads the same
 * query, which every upload, edit and delete invalidates, so it changes as soon as they are saved.
 */
let network: FakeNetwork;

const document = (id: string, expiresAt: string | null) => ({
  id,
  fileName: `${id}.pdf`,
  category: 'Certificate',
  expiresAt,
  reminderDays: [],
});

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

describe('DocumentCountChip', () => {
  it('shows how many documents the record has', async () => {
    network.reply('/attachments', 200, [document('a', null), document('b', '2999-01-01'), document('c', null)]);

    renderScreen(<DocumentCountChip ownerType="Employee" ownerId="e1" />);

    expect((await screen.findByTestId('document-count')).textContent).toBe('Documents: 3');
  });

  it('shows zero for a record with none', async () => {
    network.reply('/attachments', 200, []);

    renderScreen(<DocumentCountChip ownerType="Vehicle" ownerId="v1" />);

    expect((await screen.findByTestId('document-count')).textContent).toBe('Documents: 0');
  });

  it('says how many have already lapsed', async () => {
    network.reply('/attachments', 200, [document('a', '2001-01-01'), document('b', null)]);

    renderScreen(<DocumentCountChip ownerType="Tool" ownerId="t1" />);

    expect((await screen.findByTestId('document-count')).textContent).toBe('Documents: 2 · 1 expired');
  });
});
