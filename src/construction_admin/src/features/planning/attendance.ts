import type { PlanningAttendance, PlanningAttendanceRow, PlanningProject } from '../../api/planning';
import type { Plan } from './planningLogic';

/*
 * Who has clocked in and where they are, for the planning board. The server sends facts (when they clocked in, how far from
 * the site, how long ago the last position came); what counts as late or on the wrong site is decided here, with the two
 * numbers the rest of the system already uses.
 */

/** Minutes after the start of the shift before somebody who has not clocked in is called late. */
export const LATE_AFTER_MINUTES = 15;

/** Metres from the site that still count as being on it (the same figure the timesheet uses). */
export const ON_SITE_METERS = 100;

/** A last position older than this says nothing about where somebody is now. */
export const FRESH_PING_MINUTES = 15;

export type ClockState = 'in' | 'out' | 'late' | 'wait';
export type PinState = 'ok' | 'wrong' | 'unsure' | 'none' | 'nosite';

export interface PersonAttendance {
  clock: {
    state: ClockState;
    /** When they clocked in (and out), `HH:mm`. */
    at?: string;
    until?: string;
    /** When the site's shift starts, if it says. */
    start?: string;
    minutesLate?: number;
    /** The last moment before "late" begins. */
    limit?: string;
  };
  /** Null once they have clocked out: where they are no longer matters. */
  pin: {
    state: PinState;
    meters?: number;
    /** What the state rests on: the last position, or where they clocked in. */
    source?: 'ping' | 'clockIn';
    pingAgo?: number;
    accuracy?: number;
  } | null;
}

const minutesOf = (time: string): number => {
  const [h, m] = time.split(':').map(Number);

  return h * 60 + m;
};

const clockOf = (minutes: number): string =>
  `${String(Math.floor(minutes / 60) % 24).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`;

/** The shift start of a site as `HH:mm`, from the API's `HH:mm:ss`. */
export const startOf = (site: PlanningProject | undefined): string | undefined => site?.shiftStartTime?.slice(0, 5);

export function attendanceFor(row: PlanningAttendanceRow | undefined, site: PlanningProject | undefined, nowLocal: string): PersonAttendance {
  const start = startOf(site);

  let clock: PersonAttendance['clock'];

  if (row?.clockedOutAt) {
    clock = { state: 'out', at: row.clockedInAt ?? undefined, until: row.clockedOutAt, start };
  } else if (row?.clockedInAt) {
    clock = { state: 'in', at: row.clockedInAt, start };
  } else if (start) {
    const minutesLate = minutesOf(nowLocal) - minutesOf(start);
    const limit = clockOf(minutesOf(start) + LATE_AFTER_MINUTES);

    clock = minutesLate >= LATE_AFTER_MINUTES ? { state: 'late', start, minutesLate, limit } : { state: 'wait', start, limit };
  } else {
    clock = { state: 'wait' };
  }

  if (clock.state === 'out') return { clock, pin: null };

  if (site?.latitude == null || site.longitude == null) return { clock, pin: { state: 'nosite' } };

  const fresh = row?.lastPingMinutesAgo != null && row.lastPingMinutesAgo <= FRESH_PING_MINUTES && row.lastPingMeters != null;

  if (fresh) {
    const accuracy = row!.lastPingAccuracy ?? undefined;
    const base = { source: 'ping' as const, meters: row!.lastPingMeters!, pingAgo: row!.lastPingMinutesAgo!, accuracy };

    // A reading less exact than the distance that matters proves nothing either way.
    if (accuracy != null && accuracy > ON_SITE_METERS) return { clock, pin: { state: 'unsure', ...base } };

    return { clock, pin: { state: row!.lastPingMeters! <= ON_SITE_METERS ? 'ok' : 'wrong', ...base } };
  }

  if (row?.clockInMeters != null) {
    return {
      clock,
      pin: {
        state: row.clockInMeters <= ON_SITE_METERS ? 'ok' : 'wrong',
        source: 'clockIn',
        meters: row.clockInMeters,
        pingAgo: row.lastPingMinutesAgo ?? undefined,
      },
    };
  }

  return { clock, pin: { state: 'none', pingAgo: row?.lastPingMinutesAgo ?? undefined } };
}

export const isLate = (a: PersonAttendance): boolean => a.clock.state === 'late';
export const isWrongPlace = (a: PersonAttendance): boolean => a.pin?.state === 'wrong';

/** Attendance of everybody who has a site on the day shown, by person. Only meaningful for today. */
export function attendanceMap(plan: Plan, data: PlanningAttendance | undefined, day: number): Map<string, PersonAttendance> {
  const result = new Map<string, PersonAttendance>();
  if (!data?.rows) return result;

  const rows = new Map(data.rows.map((r) => [r.employeeId, r]));
  const sites = new Map(plan.projects.map((p) => [p.id, p]));

  for (const person of plan.people) {
    const cell = person.cells[day];
    if (!cell.project || cell.away) continue;

    result.set(person.id, attendanceFor(rows.get(person.id), sites.get(cell.project), data.nowLocal));
  }

  return result;
}
