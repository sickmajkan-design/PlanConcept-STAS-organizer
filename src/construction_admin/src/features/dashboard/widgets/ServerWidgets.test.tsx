/** @vitest-environment jsdom */
import { cleanup, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../../test/renderScreen';
import { ServerBackupsWidget, ServerHealthWidget, ServerResourcesWidget, ServerSignInsWidget } from './ServerWidgets';
import { widgetAllowed } from '../widgetRegistry';

let network: FakeNetwork;

const GB = 1024 * 1024 * 1024;

const status = (over: Record<string, unknown> = {}) => ({
  atUtc: new Date().toISOString(),
  docker: { known: false, updatedAtUtc: null, containers: [], images: null, lastPrune: null, certificate: null, releases: [], security: [] },
  backups: { known: true, items: [{ name: 'b', takenAtUtc: new Date(Date.now() - 3_600_000).toISOString(), sizeBytes: 50 * 1024 * 1024, hasDocuments: true, offsite: true }], restoreCheckedAtUtc: new Date().toISOString() },
  uptime: { known: true, sinceUtc: null, windowDays: 30, availabilityPercent: 99.9, downSeconds: 0, outages: [] },
  host: { known: true, os: 'Ubuntu', uptimeSeconds: 1000, cores: 2, load1: 0.2, load5: 0.1, load15: 0.1, memoryTotalBytes: 4 * GB, memoryAvailableBytes: 2 * GB, swapTotalBytes: 0, swapFreeBytes: 0 },
  application: { startedAtUtc: new Date().toISOString(), uptimeSeconds: 100, version: '1', environment: 'Production', runtime: '.NET', processMemoryBytes: 1, managedHeapBytes: 1, threads: 1 },
  database: { reachable: true, version: 'PG', sizeBytes: 1, connections: 1, activeConnections: 1, maxConnections: 100, cacheHitPercent: 99, largestTables: [] },
  services: { virusScannerEnabled: true, virusScannerReachable: true, fileStorage: 'disk', outboxPending: 0, outboxAbandoned: 0, oldestPendingUtc: null },
  storage: { known: true, totalBytes: 38 * GB, freeBytes: 26 * GB, usedBytes: 12 * GB, freePercent: 68, level: 0, documentsBytes: 1, documentsCount: 1 },
  ...over,
});

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

afterEach(() => cleanup());

describe('server widgets', () => {
  it('says all is well, shows the three dots and the availability', async () => {
    network.reply('/system/status', 200, status());

    renderScreen(<ServerHealthWidget instanceId="a" />, { user: signedIn('SuperAdmin') });

    expect(await screen.findByText('All is well.')).toBeDefined();
    expect(screen.getByText('Database')).toBeDefined();
    expect(screen.getByText(/99\.90%/)).toBeDefined();
  });

  it('shows the disk, memory and processor rings', async () => {
    network.reply('/system/status', 200, status());

    renderScreen(<ServerResourcesWidget instanceId="a" />, { user: signedIn('SuperAdmin') });

    expect(await screen.findByText('Processor')).toBeDefined();
    expect(screen.getByText('Memory')).toBeDefined();
    expect(screen.getByText('Disk')).toBeDefined();
    expect(screen.getByText('32%')).toBeDefined();
  });

  it('shows the last backup and that no copy is off the server', async () => {
    const noCopy = status();
    noCopy.backups.items[0].offsite = false;
    network.reply('/system/status', 200, noCopy);

    renderScreen(<ServerBackupsWidget instanceId="a" />, { user: signedIn('SuperAdmin') });

    expect(await screen.findByText(/1 h ago · 50 MB/)).toBeDefined();
    expect(screen.getByText('Off the server')).toBeDefined();
  });

  it('shows failed sign-ins, blocked SSH addresses and errors today', async () => {
    network.reply('/system/login-attempts', 200, {
      last24Hours: 12,
      last7Days: 30,
      days: [],
      reasons: [],
      topIps: [],
      topAccounts: [],
      ssh: { known: true, bannedNow: 2, bannedTotal: 15, failedTotal: 340, bannedIps: [] },
    });
    network.reply('/system/errors', 200, [{ exceptionType: 'X', location: 'Y', route: 'GET /z', today: 3, total: 5, lastSeenAtUtc: new Date().toISOString(), firstVersion: 'v' }]);

    renderScreen(<ServerSignInsWidget instanceId="a" />, { user: signedIn('SuperAdmin') });

    expect(await screen.findByText('12')).toBeDefined();
    expect(screen.getByText('errors today')).toBeDefined();
  });

  it('is offered to a Super Admin and to nobody else', () => {
    expect(widgetAllowed('ServerHealth', signedIn('SuperAdmin'))).toBe(true);
    expect(widgetAllowed('ServerHealth', signedIn('Admin'))).toBe(false);
    expect(widgetAllowed('ServerSignIns', signedIn('ProjectManager'))).toBe(false);
  });
});
