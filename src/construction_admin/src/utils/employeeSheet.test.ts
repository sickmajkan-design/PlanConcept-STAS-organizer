import { describe, expect, it } from 'vitest';

import { normaliseHeader, parseEmployeeRows, toIsoDate } from './employeeSheet';

describe('normaliseHeader', () => {
  it('ignores case, accents, spaces and punctuation', () => {
    expect(normaliseHeader(' Datum Rođenja ')).toBe('datumrodenja');
    expect(normaliseHeader('E-mail')).toBe('email');
  });
});

describe('toIsoDate', () => {
  it('reads dates the way people type them', () => {
    expect(toIsoDate('15.09.2026')).toBe('2026-09-15');
    expect(toIsoDate('5/9/2026')).toBe('2026-09-05');
    expect(toIsoDate('2026-09-15')).toBe('2026-09-15');
  });

  it('reads an Excel serial number', () => {
    expect(toIsoDate(46266)).toBe('2026-09-01');
  });

  it('reads a real date cell in local time', () => {
    expect(toIsoDate(new Date(2026, 8, 1))).toBe('2026-09-01');
  });

  it('returns null for what it cannot read', () => {
    expect(toIsoDate('sutra')).toBeNull();
    expect(toIsoDate('')).toBeNull();
  });
});

describe('parseEmployeeRows', () => {
  it('recognises both spellings of a birth-date header', () => {
    const a = parseEmployeeRows([['Ime', 'Prezime', 'Datum rođenja'], ['A', 'B', '01.02.1990']]);
    const b = parseEmployeeRows([['Ime', 'Prezime', 'Datum rodjenja'], ['A', 'B', '01.02.1990']]);

    expect(a.rows[0]).toMatchObject({ dateOfBirth: '1990-02-01' });
    expect(b.rows[0]).toMatchObject({ dateOfBirth: '1990-02-01' });
  });

  it('finds the header below a title and reports the file line of each row', () => {
    const parsed = parseEmployeeRows([
      ['Spisak radnika'],
      ['Ime', 'Prezime', 'Telefon', 'Vrsta'],
      ['Marko', 'Petrović', '061 111', 'Kooperant'],
      ['', '', '', ''],
      ['Ana', 'Ilić', '', 'Radnik'],
    ]);

    expect(parsed.ok).toBe(true);
    expect(parsed.rows).toEqual([
      { line: 3, firstName: 'Marko', lastName: 'Petrović', phone: '061 111', type: 'Subcontractor' },
      { line: 5, firstName: 'Ana', lastName: 'Ilić', type: 'Employee' },
    ]);
  });

  it('splits a single "Ime i prezime" column', () => {
    const parsed = parseEmployeeRows([['Ime i prezime'], ['Marko Petrović Mlađi']]);

    expect(parsed.rows[0]).toMatchObject({ firstName: 'Marko', lastName: 'Petrović Mlađi' });
  });

  it('lists headers it did not understand instead of dropping them silently', () => {
    const parsed = parseEmployeeRows([['Ime', 'Prezime', 'Broj cipela'], ['A', 'B', '43']]);

    expect(parsed.ignored).toEqual(['Broj cipela']);
    expect(parsed.rows[0]).not.toHaveProperty('Broj cipela');
  });

  it('keeps an unreadable date as text so the server can name it', () => {
    const parsed = parseEmployeeRows([['Ime', 'Prezime', 'Datum zaposlenja'], ['A', 'B', 'prošle godine']]);

    expect(parsed.rows[0]).toMatchObject({ employmentDate: 'prošle godine' });
  });

  it('is not ok when there is no name column', () => {
    expect(parseEmployeeRows([['Telefon'], ['061']]).ok).toBe(false);
  });
});
