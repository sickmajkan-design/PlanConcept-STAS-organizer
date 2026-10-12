import { describe, expect, it } from 'vitest';

import { detectHeaderRow, detectMapping, extractRows, normalizeHeader } from './importSheet';

const header = [
  'Ime i prezime',
  'KW36',
  'KW37',
  'KW38',
  'KW39',
  'SATNICA',
  'SATNICA WH/TRANE',
  'AKONTACIJA',
  'GORIVO',
  'Doprinosi',
  'Godišnji',
];

describe('normalizeHeader', () => {
  it('drops diacritics and case', () => {
    expect(normalizeHeader('  Godišnji  Odmor ')).toBe('godisnji odmor');
    expect(normalizeHeader('Đuro')).toBe('duro');
  });
});

describe('detectMapping', () => {
  it('finds each column by the words in its header', () => {
    const mapping = detectMapping(header);

    expect(mapping.fields.label).toBe(0);
    expect(mapping.weeks).toEqual([1, 2, 3, 4]);
    expect(mapping.fields.workerRate).toBe(5);
    expect(mapping.fields.clientRate).toBe(6);
    expect(mapping.fields.advance).toBe(7);
    expect(mapping.fields.fuel).toBe(8);
    expect(mapping.fields.contributions).toBe(9);
    expect(mapping.fields.holiday).toBe(10);
  });

  it('reports what it cannot find as -1 and never uses a column twice', () => {
    const mapping = detectMapping(['Radnik', 'KW1', 'Nesto drugo']);

    expect(mapping.fields.workerRate).toBe(-1);
    expect(mapping.fields.clientRate).toBe(-1);
    expect(mapping.weeks).toEqual([1]);
  });

  it('keeps at most six weeks', () => {
    const mapping = detectMapping(['Ime', 'KW1', 'KW2', 'KW3', 'KW4', 'KW5', 'KW6', 'KW7']);

    expect(mapping.weeks).toHaveLength(6);
  });
});

describe('detectHeaderRow', () => {
  it('skips a title above the header', () => {
    expect(detectHeaderRow([['Obračun oktobar 2026'], [], header, ['Marko', '40']])).toBe(2);
  });
});

describe('extractRows', () => {
  const matrix = [
    header,
    ['Klijent A', '', '', '', '', '', '', '', '', '', ''],
    ['Marko Horvat', '40', '40', '40', '40', '20', '33', '', '120', '', ''],
    ['Ivan I.', '8', '', '', '', '20', '33', '100', '', '', ''],
    [''],
    ['Klijent B', '', '', '', '', '', '', '', '', '', ''],
    ['Kooperanti', '100', '', '', '', '15', '28', '', '', '', ''],
    ['UKUPNO', '148', '', '', '', '', '', '', '', '', ''],
  ];

  it('puts people under the client heading above them', () => {
    const rows = extractRows(matrix, 0, detectMapping(header));

    expect(rows).toHaveLength(3);
    expect(rows[0]).toMatchObject({ section: 'Klijent A', label: 'Marko Horvat', workerRate: '20', clientRate: '33' });
    expect(rows[0]!.weeks).toEqual(['40', '40', '40', '40']);
    expect(rows[0]!.fuel).toBe('120');
    expect(rows[1]).toMatchObject({ section: 'Klijent A', label: 'Ivan I.', advance: '100' });
    expect(rows[2]).toMatchObject({ section: 'Klijent B', label: 'Kooperanti' });
  });

  it('leaves out totals and empty rows', () => {
    const rows = extractRows(matrix, 0, detectMapping(header));

    expect(rows.some((r) => r.label === 'UKUPNO')).toBe(false);
  });

  it('reads numbers written with a decimal comma as text for the server to judge', () => {
    const rows = extractRows(
      [header, ['Klijent', '', '', '', '', '', '', '', '', '', ''], ['Ana', '37,5', '', '', '', '20,5', '', '', '', '', '']],
      0,
      detectMapping(header),
    );

    expect(rows[0]!.weeks[0]).toBe('37,5');
    expect(rows[0]!.workerRate).toBe('20,5');
  });
});
