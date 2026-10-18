import { describe, expect, it } from 'vitest';

import {
  monthToDate,
  monthWindow,
  samePartOfPreviousMonth,
  daysInPeriod,
  defaultGranularity,
  percentChange,
  periodFromPreset,
  resolvePreset,
  validateCustomRange,
} from './periods';

// Thursday.
const NOW = new Date(2026, 8, 24, 13, 30);

describe('resolvePreset', () => {
  it('today is one day', () => {
    expect(resolvePreset('today', NOW)).toEqual({ from: '2026-09-24', to: '2026-09-24' });
  });

  it('this week runs Monday to Sunday', () => {
    expect(resolvePreset('thisWeek', NOW)).toEqual({ from: '2026-09-21', to: '2026-09-27' });
  });

  it('a Sunday belongs to the week that started the Monday before', () => {
    expect(resolvePreset('thisWeek', new Date(2026, 8, 27))).toEqual({ from: '2026-09-21', to: '2026-09-27' });
  });

  it('this and last month are whole calendar months', () => {
    expect(resolvePreset('thisMonth', NOW)).toEqual({ from: '2026-09-01', to: '2026-09-30' });
    expect(resolvePreset('lastMonth', NOW)).toEqual({ from: '2026-08-01', to: '2026-08-31' });
  });

  it('last month across a year boundary', () => {
    expect(resolvePreset('lastMonth', new Date(2026, 0, 15))).toEqual({ from: '2025-12-01', to: '2025-12-31' });
  });

  it('a quarter and a year', () => {
    expect(resolvePreset('thisQuarter', NOW)).toEqual({ from: '2026-07-01', to: '2026-09-30' });
    expect(resolvePreset('thisYear', NOW)).toEqual({ from: '2026-01-01', to: '2026-12-31' });
  });
});

describe('defaultGranularity', () => {
  it('cuts month-sized presets by day and long ones by month', () => {
    expect(defaultGranularity(periodFromPreset('thisMonth', undefined, NOW))).toBe('Day');
    expect(defaultGranularity(periodFromPreset('thisQuarter', undefined, NOW))).toBe('Month');
    expect(defaultGranularity(periodFromPreset('thisYear', undefined, NOW))).toBe('Month');
  });

  it('cuts a custom range by its length', () => {
    const custom = (from: string, to: string) => defaultGranularity({ preset: 'custom', from, to });
    expect(custom('2026-09-01', '2026-10-01')).toBe('Day');
    expect(custom('2026-09-01', '2026-10-02')).toBe('Week');
    expect(custom('2026-01-01', '2026-06-30')).toBe('Week');
    expect(custom('2026-01-01', '2026-07-31')).toBe('Month');
  });
});

describe('validateCustomRange', () => {
  it('accepts a sensible range', () => {
    expect(validateCustomRange('2026-09-01', '2026-09-24')).toBeNull();
  });

  it('refuses a missing, reversed or overlong range', () => {
    expect(validateCustomRange('', '2026-09-24')).toBe('incomplete');
    expect(validateCustomRange('2026-09-24', '2026-09-01')).toBe('reversed');
    expect(validateCustomRange('2024-01-01', '2026-12-31')).toBe('tooLong');
  });

  it('counts both ends of a range', () => {
    expect(daysInPeriod('2026-09-01', '2026-09-01')).toBe(1);
  });
});

describe('percentChange', () => {
  it('is null rather than infinite when there is nothing to compare with', () => {
    expect(percentChange(100, 0)).toBeNull();
  });

  it('is relative to the size of the earlier figure even when it was a loss', () => {
    expect(percentChange(150, 100)).toBe(50);
    expect(percentChange(-50, -100)).toBe(50);
  });
});

describe('month windows', () => {
  // Built from local components, as the app builds them. A month's first day must be that day, not the day before:
  // this is checked in the company's time zone (see src/test/globalSetup.ts), where the UTC date is a day earlier.
  const now = new Date(2026, 9, 18, 0, 30);

  it('cuts a month from its first to its last day', () => {
    const october = monthWindow(0, now);
    expect([october.from, october.to]).toEqual(['2026-10-01', '2026-10-31']);

    const september = monthWindow(1, now);
    expect([september.from, september.to]).toEqual(['2026-09-01', '2026-09-30']);
  });

  it('steps back across a year and into a leap February', () => {
    const lastYear = monthWindow(10, new Date(2026, 8, 5));
    expect([lastYear.from, lastYear.to]).toEqual(['2025-11-01', '2025-11-30']);

    const february = monthWindow(1, new Date(2028, 2, 10));
    expect([february.from, february.to]).toEqual(['2028-02-01', '2028-02-29']);
  });

  it('takes this month so far up to the local day, even just after midnight', () => {
    expect(monthToDate(now)).toEqual({ from: '2026-10-01', to: '2026-10-18' });
    // 00:30 on the 1st: the UTC date is still the last day of the month before.
    expect(monthToDate(new Date(2026, 10, 1, 0, 30))).toEqual({ from: '2026-11-01', to: '2026-11-01' });
  });

  it('compares with the same days of the month before, not the whole of it', () => {
    expect(samePartOfPreviousMonth(now)).toEqual({ from: '2026-09-01', to: '2026-09-18' });
    expect(samePartOfPreviousMonth(new Date(2026, 9, 1))).toEqual({ from: '2026-09-01', to: '2026-09-01' });
  });

  it('stops at the end of a shorter month before', () => {
    // 31 March against February: there is no 31 February.
    expect(samePartOfPreviousMonth(new Date(2026, 2, 31))).toEqual({ from: '2026-02-01', to: '2026-02-28' });
    expect(samePartOfPreviousMonth(new Date(2028, 2, 30))).toEqual({ from: '2028-02-01', to: '2028-02-29' });
    // And across the new year.
    expect(samePartOfPreviousMonth(new Date(2027, 0, 15))).toEqual({ from: '2026-12-01', to: '2026-12-15' });
  });
});
