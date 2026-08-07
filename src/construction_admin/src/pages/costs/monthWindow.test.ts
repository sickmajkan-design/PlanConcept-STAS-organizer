import { describe, expect, it } from 'vitest';

import { monthOf, splitHours, yearOf } from './monthWindow';

/**
 * The period pickers on the cost report.
 *
 * A report that quietly covers the wrong month still adds up, which is what
 * makes this worth pinning down rather than eyeballing.
 */
describe('monthOf', () => {
  it('covers the whole month containing the date', () => {
    expect(monthOf(new Date(2026, 7, 15))).toEqual({
      from: '2026-08-01',
      to: '2026-08-31',
    });
  });

  it('ends a 30-day month on the 30th', () => {
    expect(monthOf(new Date(2026, 8, 15))).toEqual({
      from: '2026-09-01',
      to: '2026-09-30',
    });
  });

  it('knows February in an ordinary year and in a leap year', () => {
    expect(monthOf(new Date(2026, 1, 10)).to).toBe('2026-02-28');
    expect(monthOf(new Date(2028, 1, 10)).to).toBe('2028-02-29');
  });

  it('steps back over a year boundary', () => {
    expect(monthOf(new Date(2026, 0, 15), 1)).toEqual({
      from: '2025-12-01',
      to: '2025-12-31',
    });
  });

  it('steps back a full year', () => {
    expect(monthOf(new Date(2026, 4, 15), 12).from).toBe('2025-05-01');
  });

  it('does not slip a month on the first of the month', () => {
    // The date the local-vs-UTC confusion shows up on: parsed as local time
    // west of Greenwich, the 1st of March is the 28th of February, and the
    // report would silently cover the month before the one asked for.
    expect(monthOf(new Date(2026, 2, 1)).from).toBe('2026-03-01');
  });

  it('does not slip a month on the last of the month', () => {
    expect(monthOf(new Date(2026, 2, 31)).from).toBe('2026-03-01');
  });
});

describe('yearOf', () => {
  it('runs from January to December', () => {
    expect(yearOf(new Date(2026, 6, 4))).toEqual({
      from: '2026-01-01',
      to: '2026-12-31',
    });
  });
});

describe('splitHours', () => {
  it('splits minutes into hours and minutes', () => {
    expect(splitHours(0)).toEqual({ hours: 0, minutes: 0 });
    expect(splitHours(59)).toEqual({ hours: 0, minutes: 59 });
    expect(splitHours(60)).toEqual({ hours: 1, minutes: 0 });
    expect(splitHours(605)).toEqual({ hours: 10, minutes: 5 });
  });

  it('clamps a negative total rather than printing a negative shift', () => {
    // The API never sends one. "-1 h 30 min" on a timesheet reads as a
    // correction rather than as the bug it is.
    expect(splitHours(-90)).toEqual({ hours: 0, minutes: 0 });
  });

  it('truncates a fractional minute rather than rounding into an extra one', () => {
    expect(splitHours(59.9)).toEqual({ hours: 0, minutes: 59 });
  });
});
