/**
 * The organisation's calendar.
 *
 * Which zone it is is a setting of the organisation (company settings), the same one the server keeps its own calendar
 * in: what day a shift belongs to, what "today" is, what a clock-in time reads as. It is never the browser's zone,
 * which is whatever the screen in front of someone happens to be set to; a foreman travelling, or a laptop set wrong,
 * must not move the day a shift belongs to.
 *
 * Two kinds of value come out of here, and the difference matters:
 *
 * - an INSTANT (a `Date`, or the ISO string with a `Z`): one moment, the same everywhere;
 * - a WALL CLOCK `Date`: a `Date` whose local fields (`getFullYear()`, `getHours()`…) read what the organisation's
 *   clock read at that moment. It is not a moment, only a way of carrying the clock's reading to code that already
 *   works with `getFullYear()` and `getMonth()`; it is never sent to the server or compared with an instant.
 */

const STORAGE_KEY = 'companyTimeZone';

/** What the platform uses until the organisation's own setting has been read. */
export const DEFAULT_COMPANY_TIME_ZONE = 'Europe/Belgrade';

function isZone(id: string): boolean {
  try {
    new Intl.DateTimeFormat('en-US', { timeZone: id });
    return true;
  } catch {
    return false;
  }
}

function readStored(): string | null {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    return stored && isZone(stored) ? stored : null;
  } catch {
    return null;
  }
}

// The last zone seen, so the first screen of the next visit already counts days the organisation's way.
let zone: string = readStored() ?? DEFAULT_COMPANY_TIME_ZONE;

const formatters = new Map<string, Intl.DateTimeFormat>();

function formatterFor(timeZone: string): Intl.DateTimeFormat {
  let formatter = formatters.get(timeZone);

  if (!formatter) {
    formatter = new Intl.DateTimeFormat('en-US', {
      timeZone,
      hourCycle: 'h23',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
    formatters.set(timeZone, formatter);
  }

  return formatter;
}

export function getCompanyTimeZone(): string {
  return zone;
}

/**
 * Takes up the organisation's zone. An id the browser does not know is ignored (the previous one stays), since a
 * wrong calendar is worse than a slightly old one. Returns whether the zone changed.
 */
export function setCompanyTimeZone(id: string | null | undefined): boolean {
  if (!id || id === zone || !isZone(id)) return false;

  zone = id;

  try {
    window.localStorage.setItem(STORAGE_KEY, id);
  } catch {
    // Storage blocked: the zone is read again on the next visit.
  }

  return true;
}

interface Parts {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  second: number;
}

function partsOf(instant: number, timeZone: string): Parts {
  const found: Record<string, number> = {};

  for (const part of formatterFor(timeZone).formatToParts(new Date(instant))) {
    if (part.type !== 'literal') found[part.type] = Number(part.value);
  }

  return {
    year: found.year!,
    month: found.month!,
    day: found.day!,
    hour: found.hour! === 24 ? 0 : found.hour!,
    minute: found.minute!,
    second: found.second!,
  };
}

/** What the organisation's clock reads at an instant, as a number of milliseconds since the epoch "in UTC". */
function wallClockMillis(instant: number, timeZone: string): number {
  const p = partsOf(instant, timeZone);
  return Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute, p.second);
}

/** The wall clock as a `Date` whose local fields read it (see the note at the top about what that is and is not). */
export function wallClock(instant: Date | number | string = Date.now()): Date {
  const millis = typeof instant === 'number' ? instant : new Date(instant).getTime();
  const p = partsOf(millis, zone);

  return new Date(p.year, p.month - 1, p.day, p.hour, p.minute, p.second);
}

/** Now, on the organisation's clock. */
export function companyNow(): Date {
  return wallClock(Date.now());
}

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

/** A calendar date from a wall clock `Date`: `YYYY-MM-DD`. */
export function dateOf(wall: Date): string {
  return `${wall.getFullYear()}-${pad(wall.getMonth() + 1)}-${pad(wall.getDate())}`;
}

/** Today on the organisation's calendar, as `YYYY-MM-DD`. */
export function todayCompany(): string {
  return dateOf(companyNow());
}

/** The calendar day an instant falls on, in the organisation's zone, as `YYYY-MM-DD`. */
export function companyDateOf(instant: Date | number | string): string {
  return dateOf(wallClock(instant));
}

/** The time of day an instant has on the organisation's clock, `HH:MM`. */
export function companyTimeOf(instant: Date | number | string): string {
  const wall = wallClock(instant);
  return `${pad(wall.getHours())}:${pad(wall.getMinutes())}`;
}

/**
 * The instant at which the organisation's clock reads the given date and time: the inverse of `wallClock`. The
 * offset is worked out twice, which is what settles it on the day the clocks change.
 */
export function instantOfWallClock(
  year: number,
  month: number,
  day: number,
  hour = 0,
  minute = 0,
  second = 0,
  timeZone: string = zone,
): Date {
  const asUtc = Date.UTC(year, month - 1, day, hour, minute, second);
  const first = asUtc - (wallClockMillis(asUtc, timeZone) - asUtc);
  const second_ = asUtc - (wallClockMillis(first, timeZone) - first);

  return new Date(second_);
}

/**
 * The two instants an organisation's calendar day lies between: its midnight and the next one. `to` is the start of
 * the following day, so the day is the half-open range [from, to): a shift starting at midnight belongs to exactly one
 * day, and the day is 23 or 25 hours long when the clocks change.
 */
export function companyDayBounds(day: string = todayCompany()): { from: string; to: string } {
  const [year, month, date] = day.split('-').map(Number) as [number, number, number];

  return {
    from: instantOfWallClock(year, month, date).toISOString(),
    to: instantOfWallClock(year, month, date + 1).toISOString(),
  };
}

/** A `datetime-local` input's value (`YYYY-MM-DDTHH:MM`) for an instant, on the organisation's clock. */
export function toDateTimeInput(instant: Date | number | string): string {
  const wall = wallClock(instant);
  return `${dateOf(wall)}T${pad(wall.getHours())}:${pad(wall.getMinutes())}`;
}

/** The instant a `datetime-local` input's value (read as the organisation's clock) stands for, as an ISO string. */
export function fromDateTimeInput(value: string): string {
  const [datePart, timePart = '00:00'] = value.split('T');
  const [year, month, day] = (datePart ?? '').split('-').map(Number) as [number, number, number];
  const [hour, minute] = timePart.split(':').map(Number) as [number, number];

  return instantOfWallClock(year, month, day, hour, minute).toISOString();
}
