/** Turns API enum names such as `ProjectManager` into `Project Manager`. */
export function humanizeEnum(value: string): string {
  return value.replace(/(?<=[a-z])[A-Z]/g, (match) => ` ${match}`).trim();
}

/** Day-first date, the convention on site paperwork. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  return `${day}.${month}.${date.getFullYear()}.`;
}

/** `YYYY-MM-DD` for a day offset from today, in the browser's local time. */
export function dateOnlyOffset(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);

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

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${formatDate(value)} ${hours}:${minutes}`;
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

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  return `${String(date.getHours()).padStart(2, '0')}:${String(
    date.getMinutes(),
  ).padStart(2, '0')}`;
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
