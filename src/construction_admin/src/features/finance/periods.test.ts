import { describe, expect, it } from 'vitest';

import {
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
