import { companyNow } from '../utils/companyTime';
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
export function getLastCompletedIsoWeek(today: Date = companyNow()): { isoYear: number; isoWeek: number } {
  const lastWeekDay = new Date(today);
  lastWeekDay.setDate(lastWeekDay.getDate() - 7);
  return getIsoWeek(lastWeekDay);
}

export function formatIsoWeek(isoYear: number, isoWeek: number): string {
  return `KW${String(isoWeek).padStart(2, '0')}/${isoYear}`;
}

export interface YearMonth {
  year: number;
  /** 1 to 12. */
  month: number;
}

/**
 * The month an ISO week begins in, and the next one when the week runs over a month's end (its Monday is in one
 * month and its Sunday in the next). A week's hours are entered into the monthly sheet of the month they fall in.
 */
export function isoWeekMonths(isoYear: number, isoWeek: number): { first: YearMonth; second: YearMonth | null } {
  const jan4 = new Date(Date.UTC(isoYear, 0, 4));
  const jan4Weekday = (jan4.getUTCDay() + 6) % 7;
  const monday = new Date(Date.UTC(isoYear, 0, 4 - jan4Weekday + (isoWeek - 1) * 7));
  const sunday = new Date(monday.getTime() + 6 * 86_400_000);

  const first = { year: monday.getUTCFullYear(), month: monday.getUTCMonth() + 1 };
  const last = { year: sunday.getUTCFullYear(), month: sunday.getUTCMonth() + 1 };

  return { first, second: last.month === first.month && last.year === first.year ? null : last };
}
