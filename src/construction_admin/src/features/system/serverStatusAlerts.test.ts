import { describe, expect, it } from 'vitest';

import type { ServerStatus } from '../../api/system';
import { formatDuration } from './formatDuration';
import { serverAlerts, worstTone } from './serverStatusAlerts';

const GB = 1024 * 1024 * 1024;

/** A healthy server; each test spoils one thing. */
const healthy = (): ServerStatus => ({
  atUtc: '2026-11-13T22:00:00Z',
  backups: {
    known: true,
    items: [{ name: 'construction-20260813T020000Z.dump', takenAtUtc: new Date().toISOString(), sizeBytes: 1000, hasDocuments: true, offsite: true }],
    restoreCheckedAtUtc: new Date().toISOString(),
  },
  docker: { known: true, updatedAtUtc: new Date().toISOString(), containers: [{ name: 'api', image: 'api:latest', state: 'running', status: 'Up 3 days' }], images: null, lastPrune: null },
  uptime: { known: true, sinceUtc: '2026-11-01T00:00:00Z', windowDays: 30, availabilityPercent: 100, downSeconds: 0, outages: [] },
  host: {
    known: true,
    os: 'Ubuntu',
    uptimeSeconds: 86_400,
    cores: 2,
    load1: 0.2,
    load5: 0.3,
    load15: 0.4,
    memoryTotalBytes: 4 * GB,
    memoryAvailableBytes: 2 * GB,
    swapTotalBytes: 2 * GB,
    swapFreeBytes: 2 * GB,
  },
  application: {
    startedAtUtc: '2026-11-13T20:00:00Z',
    uptimeSeconds: 7_200,
    version: '1.0.0',
    environment: 'Production',
    runtime: '.NET 9',
    processMemoryBytes: 200 * 1024 * 1024,
    managedHeapBytes: 80 * 1024 * 1024,
    threads: 30,
  },
  database: {
    reachable: true,
    version: 'PostgreSQL 16',
    sizeBytes: 75 * 1024 * 1024,
    connections: 10,
    activeConnections: 1,
    maxConnections: 100,
    cacheHitPercent: 99,
    largestTables: [],
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
    documentsBytes: 52 * 1024 * 1024,
    documentsCount: 200,
  },
});

const keys = (status: ServerStatus, now?: number) => serverAlerts(status, now).map((alert) => alert.key);

describe('serverAlerts', () => {
  it('says nothing about a healthy server', () => {
    expect(serverAlerts(healthy())).toEqual([]);
    expect(worstTone([])).toBeNull();
  });

  it('flags a database that does not answer, and a scanner that is down only when it is switched on', () => {
    const down = healthy();
    down.database.reachable = false;
    expect(keys(down)).toContain('databaseDown');

    const scanner = healthy();
    scanner.services.virusScannerReachable = false;
    expect(keys(scanner)).toContain('scannerDown');

    scanner.services.virusScannerEnabled = false;
    expect(keys(scanner)).not.toContain('scannerDown');
  });

  it('follows the disk level the server reports', () => {
    const low = healthy();
    low.storage.level = 1;
    expect(serverAlerts(low)).toEqual([{ key: 'diskLow', tone: 'warning' }]);

    const critical = healthy();
    critical.storage.level = 2;
    expect(serverAlerts(critical)).toEqual([{ key: 'diskCritical', tone: 'error' }]);
  });

  it('warns under 15% free memory and calls it critical under 7%', () => {
    const low = healthy();
    low.host.memoryAvailableBytes = 0.5 * GB; // 12.5%
    expect(keys(low)).toEqual(['memoryLow']);

    const critical = healthy();
    critical.host.memoryAvailableBytes = 0.2 * GB; // 5%
    expect(keys(critical)).toEqual(['memoryCritical']);
  });

  it('compares the five-minute load with the number of cores', () => {
    const busy = healthy();
    busy.host.load5 = 3; // 2 cores
    expect(keys(busy)).toEqual(['loadHigh']);

    const overloaded = healthy();
    overloaded.host.load5 = 5;
    expect(keys(overloaded)).toEqual(['loadCritical']);
  });

  it('does not guess about figures the machine does not give', () => {
    const unknown = healthy();
    unknown.host = { ...unknown.host, known: false, load5: null, memoryTotalBytes: null, memoryAvailableBytes: null };
    expect(serverAlerts(unknown)).toEqual([]);
  });

  it('watches the connections to the database against its limit', () => {
    const high = healthy();
    high.database.connections = 80;
    expect(keys(high)).toEqual(['connectionsHigh']);

    const critical = healthy();
    critical.database.connections = 95;
    expect(keys(critical)).toEqual(['connectionsCritical']);
  });

  it('flags abandoned messages, and messages that have waited more than 15 minutes', () => {
    const abandoned = healthy();
    abandoned.services.outboxAbandoned = 2;
    expect(keys(abandoned)).toEqual(['outboxAbandoned']);

    const now = Date.parse('2026-11-13T22:00:00Z');
    const stuck = healthy();
    stuck.services.oldestPendingUtc = '2026-11-13T21:30:00Z';
    expect(keys(stuck, now)).toEqual(['outboxStuck']);

    const fresh = healthy();
    fresh.services.oldestPendingUtc = '2026-11-13T21:55:00Z';
    expect(keys(fresh, now)).toEqual([]);
  });

  it('watches the backups: none, older than 26 h, not off the server, restore not rehearsed', () => {
    const none = healthy();
    none.backups.items = [];
    expect(keys(none)).toEqual(['backupMissing']);

    const old = healthy();
    old.backups.items[0].takenAtUtc = new Date(Date.now() - 27 * 3_600_000).toISOString();
    expect(keys(old)).toEqual(['backupOld']);

    const local = healthy();
    local.backups.items[0].offsite = false;
    expect(keys(local)).toEqual(['backupNotOffsite']);

    const unchecked = healthy();
    unchecked.backups.restoreCheckedAtUtc = null;
    expect(keys(unchecked)).toEqual(['restoreNotChecked']);

    const unseen = healthy();
    unseen.backups = { known: false, items: [], restoreCheckedAtUtc: null };
    expect(keys(unseen)).toEqual([]);
  });

  it('flags a container that is not running, and a Docker report the host has stopped refreshing', () => {
    const down = healthy();
    down.docker.containers[0].state = 'restarting';
    expect(keys(down)).toEqual(['containerDown']);

    const stale = healthy();
    stale.docker.updatedAtUtc = new Date(Date.now() - 30 * 60_000).toISOString();
    expect(keys(stale)).toEqual(['dockerStale']);

    const absent = healthy();
    absent.docker = { known: false, updatedAtUtc: null, containers: [], images: null, lastPrune: null };
    expect(keys(absent)).toEqual([]);
  });

  it('takes the worst tone of all that is wrong', () => {
    const status = healthy();
    status.storage.level = 1;
    status.database.reachable = false;
    expect(worstTone(serverAlerts(status))).toBe('error');

    const onlyLow = healthy();
    onlyLow.storage.level = 1;
    expect(worstTone(serverAlerts(onlyLow))).toBe('warning');
  });
});

describe('formatDuration', () => {
  it.each([
    [30, '30 s'],
    [45 * 60, '45 min'],
    [3 * 3_600 + 20 * 60, '3 h 20 min'],
    [12 * 86_400 + 4 * 3_600 + 59, '12 d 4 h'],
    [-5, '0 s'],
  ])('writes %i seconds as %s', (seconds, text) => {
    expect(formatDuration(seconds)).toBe(text);
  });
});
