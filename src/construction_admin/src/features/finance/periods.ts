import type { FinanceGranularity } from '../../api/finance';

export const periodPresets = [
  'today',
  'thisWeek',
  'thisMonth',
  'lastMonth',
  'thisQuarter',
  'thisYear',
  'custom',
] as const;
export type PeriodPreset = (typeof periodPresets)[number];

export interface Period {
  preset: PeriodPreset;
  /** ISO dates, inclusive on both ends. */
  from: string;
  to: string;
}

/** The API refuses anything longer. */
export const MAX_PERIOD_DAYS = 732;

/** A local calendar date as `YYYY-MM-DD` — never via UTC, which shifts the day near midnight. */
export function isoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function parseIso(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year!, month! - 1, day!);
}

export function daysInPeriod(from: string, to: string): number {
  return Math.round((parseIso(to).getTime() - parseIso(from).getTime()) / 86_400_000) + 1;
}

/**
 * The dates a preset stands for, relative to `now`. Weeks start on Monday, the
 * same as the server's bars, so "this week" and a weekly chart agree.
 */
export function resolvePreset(
  preset: Exclude<PeriodPreset, 'custom'>,
  now: Date = new Date(),
): { from: string; to: string } {
  const y = now.getFullYear();
  const m = now.getMonth();
  const d = now.getDate();

  switch (preset) {
    case 'today':
      return { from: isoDate(now), to: isoDate(now) };
    case 'thisWeek': {
      // getDay(): Sunday is 0, so Monday-based offset is (day + 6) % 7.
      const monday = new Date(y, m, d - ((now.getDay() + 6) % 7));
      return { from: isoDate(monday), to: isoDate(new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6)) };
    }
    case 'thisMonth':
      return { from: isoDate(new Date(y, m, 1)), to: isoDate(new Date(y, m + 1, 0)) };
    case 'lastMonth':
      return { from: isoDate(new Date(y, m - 1, 1)), to: isoDate(new Date(y, m, 0)) };
    case 'thisQuarter': {
      const first = Math.floor(m / 3) * 3;
      return { from: isoDate(new Date(y, first, 1)), to: isoDate(new Date(y, first + 3, 0)) };
    }
    case 'thisYear':
      return { from: isoDate(new Date(y, 0, 1)), to: isoDate(new Date(y, 11, 31)) };
  }
}

export function periodFromPreset(preset: PeriodPreset, custom?: { from: string; to: string }, now?: Date): Period {
  if (preset === 'custom') {
    const range = custom ?? resolvePreset('thisMonth', now);
    return { preset, ...range };
  }
  return { preset, ...resolvePreset(preset, now) };
}

/**
 * How a period is cut into bars unless the user says otherwise: a day at a
 * time up to a month, months for a quarter or a year, and — for a range they
 * picked themselves — by length: up to 31 days by day, up to six months by
 * week, longer by month.
 */
export function defaultGranularity(period: Period): FinanceGranularity {
  switch (period.preset) {
    case 'today':
    case 'thisWeek':
    case 'thisMonth':
    case 'lastMonth':
      return 'Day';
    case 'thisQuarter':
    case 'thisYear':
      return 'Month';
    case 'custom': {
      const days = daysInPeriod(period.from, period.to);
      if (days <= 31) return 'Day';
      if (days <= 183) return 'Week';
      return 'Month';
    }
  }
}

/** Why a custom range cannot be used, or null when it can. */
export function validateCustomRange(from: string, to: string): 'incomplete' | 'reversed' | 'tooLong' | null {
  if (!from || !to) return 'incomplete';
  if (to < from) return 'reversed';
  if (daysInPeriod(from, to) > MAX_PERIOD_DAYS) return 'tooLong';
  return null;
}

/**
 * Change against the earlier period, in percent. Null when there is nothing to
 * compare with — a dash, never an infinite percentage.
 */
export function percentChange(current: number, previous: number): number | null {
  if (previous === 0) return null;
  return ((current - previous) / Math.abs(previous)) * 100;
}

function dotted(iso: string): string {
  const [year, month, day] = iso.split('-');
  return `${day}.${month}.${year}`;
}

/** "24.09.2026", or "01.09.2026 – 30.09.2026" — for a widget title, so a screenshot says what it covers. */
export function formatPeriod(from: string, to: string): string {
  return from === to ? dotted(from) : `${dotted(from)} – ${dotted(to)}`;
}
