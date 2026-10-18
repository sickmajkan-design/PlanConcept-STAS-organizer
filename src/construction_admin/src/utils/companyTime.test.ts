import { afterEach, describe, expect, it, vi } from 'vitest';

import {
  companyDateOf,
  companyDayBounds,
  companyTimeOf,
  DEFAULT_COMPANY_TIME_ZONE,
  fromDateTimeInput,
  getCompanyTimeZone,
  instantOfWallClock,
  setCompanyTimeZone,
  todayCompany,
  toDateTimeInput,
  wallClock,
} from './companyTime';
import { formatDate, formatDateTime, formatTimeOfDay, toLocalDateOnly } from './formatting';

/**
 * The organisation's calendar: one zone, chosen in the company settings, that decides what day it is, what day a
 * shift belongs to and what time it reads. The suite runs on a Belgrade machine (see vite.config.ts); several of these
 * set another zone to show that the organisation's choice, not the machine's, is what counts.
 */
afterEach(() => {
  vi.useRealTimers();
  setCompanyTimeZone(DEFAULT_COMPANY_TIME_ZONE);
});

describe('the organisation zone', () => {
  it('is Belgrade until the organisation has chosen another', () => {
    expect(getCompanyTimeZone()).toBe('Europe/Belgrade');
  });

  it('is taken up when it is a real zone and ignored when it is not', () => {
    expect(setCompanyTimeZone('America/New_York')).toBe(true);
    expect(getCompanyTimeZone()).toBe('America/New_York');

    expect(setCompanyTimeZone('Mars/Olympus')).toBe(false);
    expect(setCompanyTimeZone(undefined)).toBe(false);
    expect(setCompanyTimeZone('America/New_York')).toBe(false);
    expect(getCompanyTimeZone()).toBe('America/New_York');
  });
});

describe('today and the day an instant falls on', () => {
  // 02:30 UTC on 19 October 2026: 04:30 that day in Belgrade (UTC+2), still the evening of the 18th in New York (UTC-4).
  const instant = new Date('2026-10-19T02:30:00Z');

  it('is the organisation\'s day, whichever zone the screen is in', () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(instant);

    expect(todayCompany()).toBe('2026-10-19');

    setCompanyTimeZone('America/New_York');
    expect(todayCompany()).toBe('2026-10-18');
    expect(companyDateOf(instant)).toBe('2026-10-18');
  });

  it('puts the first hours after midnight on the new day, and not on the UTC one', () => {
    // 22:30 UTC on the 18th is 00:30 on the 19th in Belgrade.
    expect(companyDateOf('2026-10-18T22:30:00Z')).toBe('2026-10-19');
    expect(companyTimeOf('2026-10-18T22:30:00Z')).toBe('00:30');
  });

  it('reads a wall clock Date whose own fields are the organisation\'s clock', () => {
    const wall = wallClock(instant);

    expect([wall.getFullYear(), wall.getMonth() + 1, wall.getDate(), wall.getHours(), wall.getMinutes()]).toEqual([2026, 10, 19, 4, 30]);
  });
});

describe('the bounds of a day', () => {
  it('run from one midnight of the organisation to the next', () => {
    expect(companyDayBounds('2026-10-19')).toEqual({ from: '2026-10-18T22:00:00.000Z', to: '2026-10-19T22:00:00.000Z' });

    setCompanyTimeZone('America/New_York');
    expect(companyDayBounds('2026-10-18')).toEqual({ from: '2026-10-18T04:00:00.000Z', to: '2026-10-19T04:00:00.000Z' });
  });

  it('are 25 hours long when the clocks go back and 23 when they go forward', () => {
    const hours = (day: string) => {
      const { from, to } = companyDayBounds(day);
      return (Date.parse(to) - Date.parse(from)) / 3_600_000;
    };

    expect(hours('2026-10-25')).toBe(25);
    expect(hours('2026-03-29')).toBe(23);

    // New York changes on other days.
    setCompanyTimeZone('America/New_York');
    expect(hours('2026-11-01')).toBe(25);
    expect(hours('2026-03-08')).toBe(23);
  });

  it('give a shift that starts at midnight to exactly one day', () => {
    const midnight = Date.parse('2026-10-18T22:00:00.000Z');
    const today = companyDayBounds('2026-10-19');
    const yesterday = companyDayBounds('2026-10-18');

    expect(midnight >= Date.parse(today.from) && midnight < Date.parse(today.to)).toBe(true);
    expect(midnight >= Date.parse(yesterday.from) && midnight < Date.parse(yesterday.to)).toBe(false);
  });
});

describe('instants and the clock reading', () => {
  it('turn into each other and back, in every zone, across the days the clocks change', () => {
    for (const zone of ['Europe/Belgrade', 'America/New_York', 'Asia/Tokyo', 'Australia/Sydney', 'UTC']) {
      setCompanyTimeZone(zone);

      // Not the hour the clocks repeat (02:30 on the night they go back is two different instants, so a clock reading
      // cannot name one of them): the moments either side of it are.
      for (const iso of ['2026-01-15T12:00:00Z', '2026-03-29T00:30:00Z', '2026-07-04T23:00:00Z', '2026-10-24T22:30:00Z', '2026-10-25T02:30:00Z', '2026-11-01T08:30:00Z']) {
        const wall = wallClock(iso);
        const back = instantOfWallClock(wall.getFullYear(), wall.getMonth() + 1, wall.getDate(), wall.getHours(), wall.getMinutes(), wall.getSeconds());

        expect(back.toISOString(), `${zone} ${iso}`).toBe(new Date(iso).toISOString());
      }
    }
  });

  it('are shown in a datetime-local input as the organisation\'s clock and read back from it', () => {
    const instant = '2026-10-18T22:30:00.000Z';

    expect(toDateTimeInput(instant)).toBe('2026-10-19T00:30');
    expect(fromDateTimeInput('2026-10-19T00:30')).toBe(instant);

    setCompanyTimeZone('America/New_York');
    expect(toDateTimeInput(instant)).toBe('2026-10-18T18:30');
    expect(fromDateTimeInput('2026-10-18T18:30')).toBe(instant);
  });
});

describe('what is shown', () => {
  it('writes an instant as the day and time on the organisation\'s clock', () => {
    const instant = '2026-10-18T22:30:00.000Z';

    expect(formatDate(instant)).toBe('19.10.2026.');
    expect(formatTimeOfDay(instant)).toBe('00:30');
    expect(formatDateTime(instant)).toBe('19.10.2026. 00:30');

    setCompanyTimeZone('America/New_York');
    expect(formatDate(instant)).toBe('18.10.2026.');
    expect(formatTimeOfDay(instant)).toBe('18:30');
  });

  it('writes a date as it is, in any zone: a date is not a moment', () => {
    setCompanyTimeZone('America/Los_Angeles');

    expect(formatDate('2026-10-19')).toBe('19.10.2026.');
    expect(toLocalDateOnly('2026-10-19T03:00:00Z')).toBe('2026-10-18');
  });
});
