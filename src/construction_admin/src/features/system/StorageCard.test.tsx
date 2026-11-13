/** @vitest-environment jsdom */
import { cleanup, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { StorageStatus } from '../../api/system';
import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';
import { formatSize, StorageCard } from './StorageCard';

/**
 * The Super Admin always sees how much room the server has left, and the card says so in words and in colour when it
 * is running low.
 */
let network: FakeNetwork;

const GB = 1024 * 1024 * 1024;

const status = (over: Partial<StorageStatus>): StorageStatus => ({
  known: true,
  totalBytes: 38 * GB,
  freeBytes: Math.round(7.6 * GB),
  usedBytes: Math.round(30.4 * GB),
  freePercent: 20,
  level: 1,
  documentsBytes: 52 * 1024 * 1024,
  documentsCount: 214,
  ...over,
});

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

// Each card is removed before the next test, so one test never finds another's.
afterEach(() => cleanup());

describe('formatSize', () => {
  it('shows gigabytes with one decimal and smaller sizes in megabytes', () => {
    expect(formatSize(7.6 * GB)).toBe('7.6 GB');
    expect(formatSize(52 * 1024 * 1024)).toBe('52 MB');
    expect(formatSize(0)).toBe('0 MB');
  });
});

describe('StorageCard', () => {
  it('shows how much is free, how much the documents take, and that it is running low', async () => {
    network.reply('/system/storage', 200, status({}));

    renderScreen(<StorageCard />);

    expect(await screen.findByText('7.6 GB free of 38.0 GB (20%)')).toBeDefined();
    expect(screen.getByText('Running low')).toBeDefined();
    expect(screen.getByText('Documents: 52 MB, 214 files')).toBeDefined();
  });

  it('says almost full when it is close to out, and fine when there is room', async () => {
    network.reply('/system/storage', 200, status({ level: 2, freeBytes: 2 * GB, freePercent: 5 }));
    renderScreen(<StorageCard />);
    expect(await screen.findByText('Almost full')).toBeDefined();
  });

  it('says so when the disk cannot be read, instead of showing zeros', async () => {
    network.reply('/system/storage', 200, status({ known: false, totalBytes: 0, freeBytes: 0, usedBytes: 0, freePercent: 0, level: 0 }));

    renderScreen(<StorageCard />);

    expect(await screen.findByText('The disk space cannot be read.')).toBeDefined();
  });

  it('shows nothing while it has no answer', () => {
    network.reply('/system/storage', 403, { title: 'Forbidden' });

    renderScreen(<StorageCard />);

    expect(screen.queryByTestId('storage-card')).toBeNull();
  });
});
