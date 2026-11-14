/** @vitest-environment jsdom */
import { cleanup, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { ServerStatus } from '../../api/system';
import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';
import { ServerStatusPage } from './ServerStatusPage';

/**
 * The page the Super Admin keeps open: the machine, the application, the database and the services, with one line at
 * the top that says whether all is well or what needs a person.
 */
let network: FakeNetwork;

const GB = 1024 * 1024 * 1024;
const MB = 1024 * 1024;

const status = (over: Partial<ServerStatus> = {}): ServerStatus => ({
  atUtc: '2026-11-13T22:00:00Z',
  backups: { known: false, items: [], restoreCheckedAtUtc: null },
  host: {
    known: true,
    os: 'Ubuntu 24.04',
    uptimeSeconds: 12 * 86_400 + 4 * 3_600,
    cores: 2,
    load1: 0.15,
    load5: 0.1,
    load15: 0.05,
    memoryTotalBytes: 4 * GB,
    memoryAvailableBytes: 2 * GB,
    swapTotalBytes: 2 * GB,
    swapFreeBytes: 2 * GB,
  },
  application: {
    startedAtUtc: '2026-11-13T20:00:00Z',
    uptimeSeconds: 7_200,
    version: '1.4.2',
    environment: 'Production',
    runtime: '.NET 9.0.1',
    processMemoryBytes: 200 * MB,
    managedHeapBytes: 80 * MB,
    threads: 31,
  },
  database: {
    reachable: true,
    version: 'PostgreSQL 16.4',
    sizeBytes: 75 * MB,
    connections: 12,
    activeConnections: 2,
    maxConnections: 100,
    cacheHitPercent: 99.2,
    largestTables: [
      { name: 'audit_entries', bytes: 30 * MB },
      { name: 'attachments', bytes: 5 * MB },
    ],
  },
  services: {
    virusScannerEnabled: true,
    virusScannerReachable: true,
    fileStorage: 'disk',
    outboxPending: 0,
    outboxAbandoned: 0,
    oldestPendingUtc: null,
  },
  storage: {
    known: true,
    totalBytes: 38 * GB,
    freeBytes: 26 * GB,
    usedBytes: 12 * GB,
    freePercent: 68,
    level: 0,
    documentsBytes: 52 * MB,
    documentsCount: 199,
  },
  ...over,
});

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

afterEach(() => cleanup());

describe('ServerStatusPage', () => {
  it('shows the four sections and says all is well', async () => {
    network.reply('/system/status', 200, status());
    network.reply('/system/storage', 200, status().storage);

    renderScreen(<ServerStatusPage />);

    expect((await screen.findByTestId('status-summary')).textContent).toBe('All is well.');
    expect(screen.getByText('12 d 4 h')).toBeDefined();
    expect(screen.getByText('0.15 / 0.10 / 0.05')).toBeDefined();
    expect(screen.getByText('2.0 GB free of 4.0 GB (50%)')).toBeDefined();
    expect(screen.getByText('1.4.2')).toBeDefined();
    expect(screen.getByText('PostgreSQL 16.4')).toBeDefined();
    expect(screen.getByText('12 of 100 (2 active)')).toBeDefined();
    expect(screen.getByText('audit_entries')).toBeDefined();
    expect(screen.getByText('199 files, 52 MB')).toBeDefined();
  });

  it('lists what needs attention at the top', async () => {
    const bad = status();
    bad.database = { ...bad.database, reachable: false };
    bad.services = { ...bad.services, virusScannerReachable: false, outboxAbandoned: 3 };
    network.reply('/system/status', 200, bad);
    network.reply('/system/storage', 200, bad.storage);

    renderScreen(<ServerStatusPage />);

    const summary = await screen.findByTestId('status-summary');
    expect(within(summary).getByText('The database does not answer.')).toBeDefined();
    expect(within(summary).getByText('The antivirus is not working: sending documents is stopped.')).toBeDefined();
    expect(within(summary).getByText('Some messages (e-mail, notifications) were given up on after repeated failures.')).toBeDefined();
  });

  it('shows dashes, not zeros, for what the machine does not report', async () => {
    const unknown = status();
    unknown.host = {
      known: false,
      os: 'Windows 11',
      uptimeSeconds: null,
      cores: 8,
      load1: null,
      load5: null,
      load15: null,
      memoryTotalBytes: null,
      memoryAvailableBytes: null,
      swapTotalBytes: null,
      swapFreeBytes: null,
    };
    network.reply('/system/status', 200, unknown);
    network.reply('/system/storage', 200, unknown.storage);

    renderScreen(<ServerStatusPage />);

    expect(await screen.findByText('This machine does not report on itself (running on Windows).')).toBeDefined();
    expect(screen.getAllByText('—').length).toBeGreaterThanOrEqual(3);
  });
});
