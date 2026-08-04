/**
 * The report's period arithmetic, in `YYYY-MM-DD` strings.
 *
 * Strings rather than `Date` for the same reason the schedule board uses them:
 * the API speaks `DateOnly`, and parsing `2026-03-01` west of Greenwich yields
 * the previous February, which would silently shift a monthly report into the
 * wrong month.
 */

export interface Period {
  from: string;
  to: string;
}

function iso(year: number, month: number, day: number): string {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

/** Last day of a 1-based month; day 0 of the next month is the last of this one. */
function lastDayOf(year: number, month: number): number {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

export function monthOf(date: Date, monthsAgo = 0): Period {
  const anchor = new Date(
    Date.UTC(date.getFullYear(), date.getMonth() - monthsAgo, 1),
  );
  const year = anchor.getUTCFullYear();
  const month = anchor.getUTCMonth() + 1;

  return { from: iso(year, month, 1), to: iso(year, month, lastDayOf(year, month)) };
}

export function yearOf(date: Date): Period {
  const year = date.getFullYear();

  return { from: iso(year, 1, 1), to: iso(year, 12, 31) };
}

/** Whole hours and minutes from a minute count, for the labour column. */
export function splitHours(totalMinutes: number): { hours: number; minutes: number } {
  const safe = Math.max(0, Math.trunc(totalMinutes));

  return { hours: Math.floor(safe / 60), minutes: safe % 60 };
}
