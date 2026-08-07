import { describe, expect, it } from 'vitest';

import {
  addDays,
  barPlacement,
  daysBetween,
  fromIsoDate,
  isWeekend,
  startOfWeek,
  toIsoDate,
  todayIsoDate,
  weekDays,
} from './weekWindow';

/**
 * The board's arithmetic.
 *
 * The reason it is worth its own suite: every one of these functions used to
 * be a `new Date('2026-08-03')` away from being wrong by a day for every user
 * west of Greenwich, and a schedule off by a day is worse than no schedule —
 * it looks right.
 */
describe('week arithmetic', () => {
  it('reads a date the same way in any timezone', () => {
    // A date has no timezone. Parsed as local time, `2026-08-03` is the 2nd of
    // August in New York, and the board would draw Sunday's bar on Monday.
    expect(toIsoDate(fromIsoDate('2026-08-03'))).toBe('2026-08-03');
    expect(fromIsoDate('2026-08-03').getUTCDate()).toBe(3);
  });

  it('adds and subtracts days across a month boundary', () => {
    expect(addDays('2026-08-31', 1)).toBe('2026-09-01');
    expect(addDays('2026-09-01', -1)).toBe('2026-08-31');
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01');
  });

  it('adds days across a daylight-saving change', () => {
    // Central European Summer Time ends on the last Sunday of October. On a
    // local-time clock that day is 25 hours long, so adding 86 400 000
    // milliseconds to local midnight lands on the same date again. At UTC it
    // is just a day.
    expect(addDays('2026-10-24', 1)).toBe('2026-10-25');
    expect(addDays('2026-10-25', 1)).toBe('2026-10-26');
  });

  it('counts days between two dates', () => {
    expect(daysBetween('2026-08-03', '2026-08-03')).toBe(0);
    expect(daysBetween('2026-08-03', '2026-08-09')).toBe(6);
    expect(daysBetween('2026-02-28', '2026-03-01')).toBe(1);
    // 2028 is a leap year, so February has a 29th.
    expect(daysBetween('2028-02-28', '2028-03-01')).toBe(2);
  });

  it('starts the week on Monday', () => {
    // 2026-08-03 is a Monday.
    expect(startOfWeek('2026-08-03')).toBe('2026-08-03');
    expect(startOfWeek('2026-08-06')).toBe('2026-08-03');

    // Sunday belongs to the week that began six days earlier, not to the one
    // starting tomorrow. Getting this backwards is the classic off-by-a-week.
    expect(startOfWeek('2026-08-09')).toBe('2026-08-03');
    expect(startOfWeek('2026-08-10')).toBe('2026-08-10');
  });

  it('lays out seven consecutive days from a Monday', () => {
    expect(weekDays('2026-08-03')).toEqual([
      '2026-08-03',
      '2026-08-04',
      '2026-08-05',
      '2026-08-06',
      '2026-08-07',
      '2026-08-08',
      '2026-08-09',
    ]);
  });

  it('knows which two of them are the weekend', () => {
    expect(isWeekend('2026-08-08')).toBe(true);
    expect(isWeekend('2026-08-09')).toBe(true);
    expect(isWeekend('2026-08-07')).toBe(false);
    expect(isWeekend('2026-08-03')).toBe(false);
  });

  it("today's date comes back as a date, not a timestamp", () => {
    expect(todayIsoDate()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });
});

describe('barPlacement', () => {
  const monday = '2026-08-03';

  it('places a whole week as one bar across seven columns', () => {
    expect(barPlacement(monday, '2026-08-03', '2026-08-09')).toEqual({
      column: 1,
      span: 7,
    });
  });

  it('places a single day as a one-column bar', () => {
    expect(barPlacement(monday, '2026-08-05', '2026-08-05')).toEqual({
      column: 3,
      span: 1,
    });
  });

  it('clamps a range that starts before the window', () => {
    // The API clips to the window, so this should not arrive — and if it does,
    // a bar starting in column -2 is a layout bug far harder to spot than a
    // bar of the wrong length.
    expect(barPlacement(monday, '2026-07-30', '2026-08-05')).toEqual({
      column: 1,
      span: 3,
    });
  });

  it('clamps a range that runs past the window', () => {
    expect(barPlacement(monday, '2026-08-07', '2026-09-30')).toEqual({
      column: 5,
      span: 3,
    });
  });

  it('never produces a bar shorter than a day', () => {
    // An inverted range would otherwise give a negative span, which collapses
    // the grid rather than showing anything.
    const { span } = barPlacement(monday, '2026-08-07', '2026-08-04');

    expect(span).toBe(1);
  });
});
