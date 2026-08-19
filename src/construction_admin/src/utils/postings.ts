import type { Translate } from '../i18n/context';
import { formatDate, formatMoney, formatQuantity } from './formatting';

/**
 * The two shapes a posting comes in across the app — the assignment board's
 * postings, and the employee/project detail pages' richer assignment rows —
 * share exactly these fields, which is all either helper below needs.
 */
export interface PostingDates {
  startDate: string;
  endDate: string | null;
}

export interface PostingPay {
  workedHours: number;
  workedDays: number;
  totalPay: number | null;
}

/**
 * "Since 18.08.2026." for an open-ended posting, "18.08.2026. – 25.08.2026."
 * once it has a planned or actual end. The same phrasing everywhere a posting
 * appears, so "since" and a dash mean the same thing on the board as on a
 * person's own history.
 */
export function postingRange(posting: PostingDates, t: Translate): string {
  return posting.endDate
    ? `${formatDate(posting.startDate)} – ${formatDate(posting.endDate)}`
    : `${t('common.since')} ${formatDate(posting.startDate)}`;
}

/**
 * "12.5 h · 3 days · 4,200.00", built only from whichever parts are non-zero.
 * A posting with nothing recorded against it returns null rather than a
 * string of zeroes, so callers can drop it from a joined line entirely.
 */
export function workedSummary(posting: PostingPay, t: Translate, locale: string): string | null {
  const parts: string[] = [];

  if (posting.workedHours > 0) {
    // "h" reads the same abbreviation in both languages, so this skips the
    // translation table rather than smuggling a pre-formatted string through
    // a placeholder meant for a plural-selecting number.
    parts.push(`${formatQuantity(posting.workedHours, locale)} h`);
  }
  if (posting.workedDays > 0) {
    parts.push(t('common.days', { count: posting.workedDays }));
  }
  // Not "!== null": a nothing-recorded posting should read as nothing
  // recorded, not as a pointed "0.00" sitting next to postings that earned
  // real money.
  if (posting.totalPay) {
    parts.push(formatMoney(posting.totalPay, locale));
  }

  return parts.length > 0 ? parts.join(' · ') : null;
}
