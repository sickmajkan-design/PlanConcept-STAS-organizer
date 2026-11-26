import { describe, expect, it } from 'vitest';

import type { PlanningAttendanceRow, PlanningProject } from '../../api/planning';
import { attendanceFor, isLate, isWrongPlace } from './attendance';

const site = (extra: Partial<PlanningProject> = {}): PlanningProject => ({
  id: 'A',
  name: 'A',
  status: 'Active',
  customerName: null,
  startDate: null,
  endDate: null,
  worksSaturdays: false,
  worksSundays: false,
  needs: [],
  latitude: 45.8,
  longitude: 15.98,
  shiftStartTime: '07:00:00',
  ...extra,
});

const row = (extra: Partial<PlanningAttendanceRow> = {}): PlanningAttendanceRow => ({
  employeeId: 'p1',
  siteId: 'A',
  clockedInAt: null,
  clockedOutAt: null,
  clockInMeters: null,
  lastPingMinutesAgo: null,
  lastPingMeters: null,
  lastPingAccuracy: null,
  ...extra,
});

describe('attendance on the board', () => {
  it('is not late until 15 minutes after the start of the shift, then it says how late', () => {
    expect(attendanceFor(row(), site(), '07:14').clock.state).toBe('wait');

    const late = attendanceFor(row(), site(), '07:22');
    expect(late.clock).toMatchObject({ state: 'late', minutesLate: 22, limit: '07:15', start: '07:00' });
    expect(isLate(late)).toBe(true);
  });

  it('calls nobody late on a site that does not say when the shift starts', () => {
    const a = attendanceFor(row(), site({ shiftStartTime: null }), '11:00');

    expect(a.clock.state).toBe('wait');
    expect(isLate(a)).toBe(false);
  });

  it('shows clocked in, and clocked out with no location any more', () => {
    expect(attendanceFor(row({ clockedInAt: '06:52' }), site(), '07:30').clock).toMatchObject({ state: 'in', at: '06:52' });

    const out = attendanceFor(row({ clockedInAt: '06:52', clockedOutAt: '15:01' }), site(), '16:00');
    expect(out.clock).toMatchObject({ state: 'out', at: '06:52', until: '15:01' });
    expect(out.pin).toBeNull();
  });

  it('puts somebody on the site within 100 m and off it beyond, by the last position when it is fresh', () => {
    const near = attendanceFor(row({ clockedInAt: '06:52', lastPingMinutesAgo: 3, lastPingMeters: 14, lastPingAccuracy: 10 }), site(), '07:10');
    expect(near.pin).toMatchObject({ state: 'ok', source: 'ping', meters: 14 });

    const far = attendanceFor(row({ clockedInAt: '06:52', lastPingMinutesAgo: 3, lastPingMeters: 340, lastPingAccuracy: 10 }), site(), '07:10');
    expect(far.pin).toMatchObject({ state: 'wrong', meters: 340 });
    expect(isWrongPlace(far)).toBe(true);
  });

  it('does not claim a place when the reading is less exact than 100 m', () => {
    const a = attendanceFor(row({ lastPingMinutesAgo: 1, lastPingMeters: 180, lastPingAccuracy: 180 }), site(), '07:10');

    expect(a.pin?.state).toBe('unsure');
    expect(isWrongPlace(a)).toBe(false);
  });

  it('falls back to where they clocked in when the last position is old, and says nothing with neither', () => {
    const old = attendanceFor(row({ clockedInAt: '06:52', clockInMeters: 40, lastPingMinutesAgo: 48, lastPingMeters: 900, lastPingAccuracy: 10 }), site(), '07:40');
    expect(old.pin).toMatchObject({ state: 'ok', source: 'clockIn', meters: 40, pingAgo: 48 });

    const none = attendanceFor(row({ lastPingMinutesAgo: 48, lastPingMeters: 900 }), site(), '07:40');
    expect(none.pin).toMatchObject({ state: 'none', pingAgo: 48 });
    expect(attendanceFor(undefined, site(), '07:10').pin?.state).toBe('none');
  });

  it('says so, and does not guess, when the site has no coordinates', () => {
    const a = attendanceFor(row({ lastPingMinutesAgo: 1, lastPingMeters: null }), site({ latitude: null, longitude: null }), '07:10');

    expect(a.pin?.state).toBe('nosite');
  });
});
