import { afterEach, describe, expect, it, vi } from 'vitest';

import {
  formatDate,
  formatDateTime,
  formatMoney,
  formatQuantity,
  formatTimeOfDay,
  humanizeEnum,
  ijekavianDate,
  initialsOf,
  companyDayBounds,
  splitMinutes,
  todayCompany,
} from './formatting';

/**
 * Dates are built from local components and turned into the ISO strings the
 * API would send, so these assertions hold in any timezone the suite runs in
 * — including a CI runner on UTC and a laptop that is not.
 */
const isoAt = (
  year: number,
  monthIndex: number,
  day: number,
  hours = 0,
  minutes = 0,
) => new Date(year, monthIndex, day, hours, minutes).toISOString();

describe('humanizeEnum', () => {
  it('splits a PascalCase API value into words', () => {
    expect(humanizeEnum('ProjectManager')).toBe('Project Manager');
    expect(humanizeEnum('OutOfService')).toBe('Out Of Service');
  });

  it('leaves a single word alone', () => {
    expect(humanizeEnum('Active')).toBe('Active');
  });
});

describe('formatDate', () => {
  it('is day-first, the convention on site paperwork', () => {
    expect(formatDate(isoAt(2026, 7, 3))).toBe('03.08.2026.');
  });

  it('pads a single-digit day and month', () => {
    expect(formatDate(isoAt(2026, 0, 9))).toBe('09.01.2026.');
  });

  it('shows a dash for nothing rather than "Invalid Date"', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate(undefined)).toBe('—');
    expect(formatDate('')).toBe('—');
    expect(formatDate('not a date')).toBe('—');
  });
});

describe('formatDateTime', () => {
  it('appends the time to the date', () => {
    expect(formatDateTime(isoAt(2026, 7, 3, 7, 5))).toBe('03.08.2026. 07:05');
  });

  it('shows a dash for nothing', () => {
    expect(formatDateTime(null)).toBe('—');
    expect(formatDateTime('rubbish')).toBe('—');
  });
});

describe('formatTimeOfDay', () => {
  it('is zero-padded on both halves', () => {
    expect(formatTimeOfDay(isoAt(2026, 7, 3, 6, 4))).toBe('06:04');
    expect(formatTimeOfDay(isoAt(2026, 7, 3, 23, 59))).toBe('23:59');
  });

  it('shows midnight as 00:00 and not as blank', () => {
    expect(formatTimeOfDay(isoAt(2026, 7, 3, 0, 0))).toBe('00:00');
  });

  it('shows a dash for nothing', () => {
    expect(formatTimeOfDay(undefined)).toBe('—');
  });
});

describe('initialsOf', () => {
  it('takes one letter from each name', () => {
    expect(initialsOf('Ivan', 'Horvat')).toBe('IH');
  });

  it('uppercases whatever it was given', () => {
    expect(initialsOf('ivan', 'horvat')).toBe('IH');
  });

  it('falls back to one name when only one is known', () => {
    expect(initialsOf('Ivan', null)).toBe('I');
    expect(initialsOf(null, 'Horvat')).toBe('H');
  });

  it('falls back to the email when there is no name at all', () => {
    expect(initialsOf(null, null, 'majstor@example.test')).toBe('M');
  });

  it('never renders an empty avatar', () => {
    expect(initialsOf(null, null, null)).toBe('?');
    expect(initialsOf('  ', '  ', '  ')).toBe('?');
  });
});

describe('splitMinutes', () => {
  it('splits a shift into hours and minutes', () => {
    expect(splitMinutes(0)).toEqual({ hours: 0, minutes: 0 });
    expect(splitMinutes(90)).toEqual({ hours: 1, minutes: 30 });
    expect(splitMinutes(1440)).toEqual({ hours: 24, minutes: 0 });
  });

  it('treats a missing total as zero rather than as NaN', () => {
    expect(splitMinutes(null)).toEqual({ hours: 0, minutes: 0 });
    expect(splitMinutes(undefined)).toEqual({ hours: 0, minutes: 0 });
  });

  it('clamps a negative total', () => {
    expect(splitMinutes(-30)).toEqual({ hours: 0, minutes: 0 });
  });
});

describe('formatMoney', () => {
  it('groups and fixes two decimals for the reader s locale', () => {
    expect(formatMoney(1234567.5, 'sr')).toBe('1.234.567,50');
    expect(formatMoney(1234567.5, 'en')).toBe('1,234,567.50');
  });

  it('always shows both decimals, so a column lines up', () => {
    expect(formatMoney(1000, 'en')).toBe('1,000.00');
  });

  it('prints no currency symbol', () => {
    // The system stores one currency and never says which. A symbol here
    // would be the client inventing a fact the data does not carry.
    expect(formatMoney(10, 'sr')).not.toMatch(/[^\d.,\s]/);
  });

  it('shows a dash rather than a zero for a missing amount', () => {
    // Zero is a real figure — "this cost nothing" — and printing it for
    // "nobody recorded this" would be a different claim.
    expect(formatMoney(null, 'sr')).toBe('—');
    expect(formatMoney(undefined, 'en')).toBe('—');
    expect(formatMoney(Number.NaN, 'en')).toBe('—');
    expect(formatMoney(0, 'en')).toBe('0.00');
  });
});

describe('formatQuantity', () => {
  it('keeps a fraction of a unit, which a quantity may legitimately be', () => {
    expect(formatQuantity(1234.5678, 'sr')).toBe('1.234,568');
  });

  it('does not pad a whole number with decimals', () => {
    expect(formatQuantity(12, 'en')).toBe('12');
  });

  it('shows a dash for a missing quantity', () => {
    expect(formatQuantity(null, 'en')).toBe('—');
  });
});

describe('ijekavianDate', () => {
  it('puts the two weekdays that differ into the ijekavian form, in full and abbreviated', () => {
    expect(ijekavianDate('nedelja, 18. oktobar')).toBe('nedjelja, 18. oktobar');
    expect(ijekavianDate('sreda, 21. oktobar')).toBe('srijeda, 21. oktobar');
    expect(ijekavianDate('sre 21.10.')).toBe('sri 21.10.');
    expect(ijekavianDate('Ned 18.10.')).toBe('Ned 18.10.');
  });

  it('leaves other words, and words that merely contain these, alone', () => {
    expect(ijekavianDate('petak, 16. oktobar')).toBe('petak, 16. oktobar');
    expect(ijekavianDate('Sredina meseca')).toBe('Sredina meseca');
  });
});

describe('the local day', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('is today in the company time zone, not the UTC date, in the first hours after midnight', () => {
    // 22:30 UTC on the 18th is 00:30 on the 19th in Belgrade (summer time).
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-10-18T22:30:00Z'));

    expect(todayCompany()).toBe('2026-10-19');
  });

  it('runs from one local midnight to the next', () => {
    expect(companyDayBounds('2026-10-19')).toEqual({ from: '2026-10-18T22:00:00.000Z', to: '2026-10-19T22:00:00.000Z' });
  });

  it('is 25 hours long on the day the clocks go back, and 23 when they go forward', () => {
    const back = companyDayBounds('2026-10-25');
    expect((Date.parse(back.to) - Date.parse(back.from)) / 3_600_000).toBe(25);

    const forward = companyDayBounds('2026-03-29');
    expect((Date.parse(forward.to) - Date.parse(forward.from)) / 3_600_000).toBe(23);
  });

  it('gives a shift that starts at midnight to exactly one day', () => {
    const midnight = Date.parse('2026-10-18T22:00:00.000Z');
    const today = companyDayBounds('2026-10-19');
    const yesterday = companyDayBounds('2026-10-18');

    expect(midnight >= Date.parse(today.from) && midnight < Date.parse(today.to)).toBe(true);
    expect(midnight >= Date.parse(yesterday.from) && midnight < Date.parse(yesterday.to)).toBe(false);
  });
});
