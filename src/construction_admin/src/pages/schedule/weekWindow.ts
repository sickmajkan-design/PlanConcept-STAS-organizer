/**
 * The board's week arithmetic, kept out of the component so it can be reasoned
 * about — and tested — without rendering anything.
 *
 * Dates are handled as `YYYY-MM-DD` strings rather than `Date` objects. The API
 * speaks `DateOnly`, and a `Date` would drag a timezone into a value that has
 * none: parsing `2026-08-03` west of Greenwich yields the 2nd, which would
 * shift every bar on the board by a day.
 */

const MS_PER_DAY = 86_400_000;

export function toIsoDate(date: Date): string {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  const day = String(date.getUTCDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/** Parsed at UTC midnight, so arithmetic on it never crosses a day boundary. */
export function fromIsoDate(value: string): Date {
  return new Date(`${value}T00:00:00Z`);
}

export function addDays(value: string, days: number): string {
  return toIsoDate(new Date(fromIsoDate(value).getTime() + days * MS_PER_DAY));
}

/** Whole days between two dates, both ends counted. */
export function daysBetween(from: string, to: string): number {
  return Math.round((fromIsoDate(to).getTime() - fromIsoDate(from).getTime()) / MS_PER_DAY);
}

/**
 * The Monday of the week containing `value`.
 *
 * Monday rather than Sunday because that is where a working week starts in
 * Serbia, and the board is read as a working week.
 */
export function startOfWeek(value: string): string {
  const date = fromIsoDate(value);
  // getUTCDay: 0 is Sunday, so Sunday is six days after its Monday.
  const offset = (date.getUTCDay() + 6) % 7;
  return addDays(value, -offset);
}

export function todayIsoDate(): string {
  const now = new Date();
  return toIsoDate(
    new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())),
  );
}

/** The seven dates of the week beginning at `monday`. */
export function weekDays(monday: string): string[] {
  return Array.from({ length: 7 }, (_, index) => addDays(monday, index));
}

export function isWeekend(value: string): boolean {
  const day = fromIsoDate(value).getUTCDay();
  return day === 0 || day === 6;
}

/**
 * Where a bar sits in a seven-column week, as a 1-based grid column and a
 * span.
 *
 * The API has already clipped the range to the window, so this only has to
 * place it. Values are clamped anyway: a board rendering a bar outside its own
 * columns is a layout bug that is much harder to see than a wrong-length one.
 */
export function barPlacement(
  weekStart: string,
  from: string,
  to: string,
): { column: number; span: number } {
  const start = Math.min(Math.max(daysBetween(weekStart, from), 0), 6);
  const end = Math.min(Math.max(daysBetween(weekStart, to), start), 6);

  return { column: start + 1, span: end - start + 1 };
}
