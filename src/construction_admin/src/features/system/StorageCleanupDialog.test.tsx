/** @vitest-environment jsdom */
import { cleanup, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { StorageCleanupScan } from '../../api/system';
import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';
import { StorageCleanupDialog } from './StorageCleanupDialog';

/**
 * Nothing is removed on the first click: each kind says what it would take and how much it frees, and removing it asks
 * first. What is already clean cannot be pressed.
 */
let network: FakeNetwork;

const MB = 1024 * 1024;

const scan: StorageCleanupScan = {
  orphanGraceHours: 24,
  categories: [
    { kind: 'OrphanFiles', count: 4, bytes: 30 * MB, examples: ['orphans/a/old.pdf'], keptForRetention: 0 },
    { kind: 'DeletedRecordDocuments', count: 8, bytes: 12 * MB, examples: ['ugovor.pdf'], keptForRetention: 2 },
    { kind: 'DeletionTraces', count: 0, bytes: 0, examples: [], keptForRetention: 0 },
  ],
};

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

afterEach(() => cleanup());

describe('StorageCleanupDialog', () => {
  it('shows what each kind would remove, and what is kept for retention', async () => {
    network.reply('/system/storage/cleanup', 200, scan);

    renderScreen(<StorageCleanupDialog open onClose={() => {}} />);

    const orphans = await screen.findByTestId('cleanup-OrphanFiles');
    expect(within(orphans).getByText('4 items, 30 MB')).toBeDefined();
    expect(within(orphans).getByText('orphans/a/old.pdf')).toBeDefined();

    const dead = screen.getByTestId('cleanup-DeletedRecordDocuments');
    expect(within(dead).getByText('2 kept because of a retention date.')).toBeDefined();

    const traces = screen.getByTestId('cleanup-DeletionTraces');
    expect(within(traces).getByText('Nothing to clean.')).toBeDefined();
    expect((within(traces).getByRole('button', { name: 'Delete for good' }) as HTMLButtonElement).disabled).toBe(true);
  });

  it('deletes nothing until the person confirms, then deletes that kind only and says what it freed', async () => {
    const user = userEvent.setup();
    network.reply('/system/storage/cleanup', 200, scan);

    renderScreen(<StorageCleanupDialog open onClose={() => {}} />);

    const orphans = await screen.findByTestId('cleanup-OrphanFiles');
    network.reply('/system/storage/cleanup', 200, { kind: 'OrphanFiles', removedFiles: 4, removedRows: 0, freedBytes: 30 * MB, keptForRetention: 0 });

    await user.click(within(orphans).getByRole('button', { name: 'Delete for good' }));

    expect(network.calls.some((call) => call.method === 'POST')).toBe(false);
    expect(await screen.findByText('Delete for good?')).toBeDefined();

    const confirm = await screen.findAllByRole('button', { name: 'Delete for good' });
    await user.click(confirm[confirm.length - 1]);

    expect((await screen.findByTestId('cleanup-result')).textContent).toBe('Deleted: 4 files, 0 records; freed 30 MB.');

    const post = network.calls.find((call) => call.method === 'POST');
    expect(post?.body).toEqual({ kind: 'OrphanFiles' });
  });
});
