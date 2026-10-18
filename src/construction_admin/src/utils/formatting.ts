import { companyDayBounds, companyNow, companyTimeOf, todayCompany, wallClock } from './companyTime';

// The organisation's calendar lives in companyTime.ts; these are re-exported where the formatting helpers have always lived.
export { companyDayBounds, companyNow, todayCompany };

/** Turns API enum names such as `ProjectManager` into `Project Manager`. */
export function humanizeEnum(value: string): string {
  return value.replace(/(?<=[a-z])[A-Z]/g, (match) => ` ${match}`).trim();
}

/**
 * The browser writes Serbian weekdays in the ekavian form (nedelja, sreda); the interface is ijekavian. The only
 * words that differ in a date are those two, in full and abbreviated, so they are put right here, once.
 */
export function ijekavianDate(text: string): string {
  return text
    .replace(/\bnedelja\b/g, 'nedjelja')
    .replace(/\bNedelja\b/g, 'Nedjelja')
    .replace(/\bsreda\b/g, 'srijeda')
    .replace(/\bSreda\b/g, 'Srijeda')
    .replace(/\bsre\b/g, 'sri')
    .replace(/\bSre\b/g, 'Sri');
}

const DAY_MS = 24 * 60 * 60 * 1000;

/** Whole days from `today` to the `YYYY-MM-DD` date, negative once it has passed. */
export function daysUntil(date: string, today: Date = companyNow()): number {
  const start = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const [year, month, day] = date.split('-').map(Number);

  return Math.round((Date.UTC(year, month - 1, day) - start) / DAY_MS);
}

/**
 * Day-first date, the convention on site paperwork. A date (`YYYY-MM-DD`) is written as it is; an instant is the day
 * it falls on in the organisation's zone, never the browser's.
 */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';

  const dateOnly = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (dateOnly) return `${dateOnly[3]}.${dateOnly[2]}.${dateOnly[1]}.`;

  const instant = new Date(value);
  if (Number.isNaN(instant.getTime())) return '—';

  const day = wallClock(instant);
  return `${String(day.getDate()).padStart(2, '0')}.${String(day.getMonth() + 1).padStart(2, '0')}.${day.getFullYear()}.`;
}

/** `YYYY-MM-DD` for a day offset from today, on the organisation's calendar. */
export function dateOnlyOffset(days: number): string {
  const date = companyNow();
  date.setDate(date.getDate() + days);
  return toLocalDateOnly(date);
}

/**
 * `YYYY-MM-DD` for an ISO instant, on the organisation's calendar; for a `Date` the date its own fields read (a wall
 * clock `Date` is the organisation's day already, and a day picked on a calendar is built from its fields).
 */
export function toLocalDateOnly(value: string | Date): string {
  const date = typeof value === 'string' ? wallClock(new Date(value)) : value;

  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/**
 * The period an export covers when the screen itself has no date filter.
 *
 * A year back to today. Long enough to be the document somebody actually
 * wants, short enough to stay inside the API's own two-year bound.
 */
export function lastYearRange(): { from: string; to: string } {
  return { from: dateOnlyOffset(-365), to: dateOnlyOffset(0) };
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—';

  const instant = new Date(value);
  if (Number.isNaN(instant.getTime())) return '—';

  return `${formatDate(value)} ${companyTimeOf(instant)}`;
}

/** Compact "how long ago" label, used for the last GPS fix on the live map. */
export function formatRelative(value: string | null | undefined): string {
  if (!value) return 'never';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'never';

  const elapsedMs = Date.now() - date.getTime();

  if (elapsedMs < 0 || elapsedMs < 60_000) return 'just now';

  const minutes = Math.floor(elapsedMs / 60_000);
  if (minutes < 60) return `${minutes} min ago`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} h ago`;

  const days = Math.floor(hours / 24);
  if (days < 7) return `${days} d ago`;

  return formatDate(value);
}

/** Avatar initials from a name, falling back to the first letter of an email. */
export function initialsOf(
  firstName?: string | null,
  lastName?: string | null,
  fallback?: string | null,
): string {
  const first = firstName?.trim() ?? '';
  const last = lastName?.trim() ?? '';

  if (first && last) return `${first[0]}${last[0]}`.toUpperCase();

  const single = first || last;
  if (single) return single[0]!.toUpperCase();

  const alt = fallback?.trim() ?? '';
  return alt ? alt[0]!.toUpperCase() : '?';
}

/**
 * Splits a minute count into whole hours and the remaining minutes.
 *
 * Returned as parts rather than a formatted string because the separator is
 * translated — the caller feeds these into `timeEntries.hoursShort`. Negative
 * input is clamped: the API never sends it, and a "-1 h 30 min" on a timesheet
 * would be read as a correction rather than as the bug it is.
 */
export function splitMinutes(total: number | null | undefined): {
  hours: number;
  minutes: number;
} {
  const safe = Math.max(0, Math.trunc(total ?? 0));

  return { hours: Math.floor(safe / 60), minutes: safe % 60 };
}

/** `HH:MM` for a timestamp, for grids where the date is already a column. */
export function formatTimeOfDay(value: string | null | undefined): string {
  if (!value) return '—';

  const instant = new Date(value);
  if (Number.isNaN(instant.getTime())) return '—';

  return companyTimeOf(instant);
}

/**
 * A money amount, grouped and to two decimals.
 *
 * No currency symbol. The system stores one currency and never says which, so
 * printing a symbol here would be the client inventing a fact the data does
 * not carry — and getting it wrong on the one deployment where it differs.
 * Grouping still follows the reader's locale, because that is presentation.
 */
export function formatMoney(
  value: number | null | undefined,
  locale: string,
): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';

  return new Intl.NumberFormat(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

/** A quantity, which unlike money may legitimately be a fraction of a unit. */
export function formatQuantity(
  value: number | null | undefined,
  locale: string,
): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';

  return new Intl.NumberFormat(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
    maximumFractionDigits: 3,
  }).format(value);
}
