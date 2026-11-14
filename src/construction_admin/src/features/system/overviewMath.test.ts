import { describe, expect, it } from 'vitest';

import type { HostStatus } from '../../api/system';
import { containerUptime, cpuPercent, memoryUsedPercent, outageDays, toneAt } from './overviewMath';

const host = (over: Partial<HostStatus> = {}): HostStatus => ({
  known: true,
  os: 'Ubuntu',
  uptimeSeconds: 1,
  cores: 2,
  load1: 0.5,
  load5: 0.5,
  load15: 0.5,
  memoryTotalBytes: 4_000,
  memoryAvailableBytes: 1_000,
  swapTotalBytes: 0,
  swapFreeBytes: 0,
  ...over,
});

describe('overviewMath', () => {
  it('reads load per core and memory in use as percentages, and says nothing when the host does not report', () => {
    expect(cpuPercent(host())).toBe(25);
    expect(memoryUsedPercent(host())).toBe(75);
    expect(cpuPercent(host({ load1: null }))).toBeNull();
    expect(cpuPercent(host({ cores: 0 }))).toBeNull();
    expect(memoryUsedPercent(host({ memoryTotalBytes: null }))).toBeNull();
  });

  it('colours a ring by two lines', () => {
    expect(toneAt(40, 85, 93)).toBe('success');
    expect(toneAt(85, 85, 93)).toBe('warning');
    expect(toneAt(95, 85, 93)).toBe('error');
  });

  it('marks the days an outage touched, oldest first', () => {
    const now = Date.parse('2026-11-14T12:00:00Z');
    const flags = outageDays(
      [{ startedAtUtc: '2026-11-12T10:00:00Z', endedAtUtc: '2026-11-12T10:30:00Z', seconds: 1800 }],
      5,
      now,
    );

    expect(flags).toEqual([false, false, true, false, false]);
  });

  it.each([
    ['Up 3 days (healthy)', '3 d'],
    ['Up 2 weeks', '14 d'],
    ['Up About an hour', '1 h'],
    ['Up 12 minutes', '12 min'],
    ['Up Less than a second', '< 1 s'],
    ['Up 4 hours (healthy)', '4 h'],
    ['Restarting (1) 5 seconds ago', 'Restarting (1) 5 seconds ago'],
  ])('words the container age %s as %s', (status, expected) => {
    expect(containerUptime(status)).toBe(expected);
  });
});
