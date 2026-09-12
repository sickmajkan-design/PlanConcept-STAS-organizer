/**
 * ISO-8601 week numbering (Monday-start, week 1 is the week containing the
 * year's first Thursday) — the "KW" convention the German/Austrian-market
 * paperwork this feature mirrors already uses.
 */
export function getIsoWeek(date: Date): { isoYear: number; isoWeek: number } {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  // Shift to the Thursday of this date's week: ISO weekday 1 (Mon) .. 7 (Sun).
  const isoWeekday = (d.getUTCDay() + 6) % 7;
  d.setUTCDate(d.getUTCDate() - isoWeekday + 3);

  const isoYear = d.getUTCFullYear();
  const yearStart = new Date(Date.UTC(isoYear, 0, 1));
  const isoWeek = Math.ceil(((d.getTime() - yearStart.getTime()) / 86_400_000 + 1) / 7);

  return { isoYear, isoWeek };
}

/** The most recently completed ISO week — what a Monday submission is normally reporting on. */
export function getLastCompletedIsoWeek(today: Date = new Date()): { isoYear: number; isoWeek: number } {
  const lastWeekDay = new Date(today);
  lastWeekDay.setDate(lastWeekDay.getDate() - 7);
  return getIsoWeek(lastWeekDay);
}

export function formatIsoWeek(isoYear: number, isoWeek: number): string {
  return `KW${String(isoWeek).padStart(2, '0')}/${isoYear}`;
}
