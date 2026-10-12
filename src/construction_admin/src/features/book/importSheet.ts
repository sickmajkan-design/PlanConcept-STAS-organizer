/**
 * Reading the old monthly sheet: which column holds what, and which rows are clients and which are people.
 *
 * Pure functions over a matrix of text, so the layout rules can be tested without a spreadsheet. The server
 * decides what is a number and what is valid; this only finds where things are.
 */

export const IMPORT_WEEK_COLUMNS = 6;

export type SheetField =
  | 'label'
  | 'workerRate'
  | 'clientRate'
  | 'advance'
  | 'difference'
  | 'contributions'
  | 'fuel'
  | 'housing'
  | 'holiday'
  | 'bonus';

export const sheetFields: readonly SheetField[] = [
  'label',
  'workerRate',
  'clientRate',
  'advance',
  'difference',
  'contributions',
  'fuel',
  'housing',
  'holiday',
  'bonus',
];

/** Column index (zero-based) per field, or -1 when the sheet has none. */
export interface SheetMapping {
  fields: Record<SheetField, number>;
  /** Column index of each week, in order; at most six. */
  weeks: number[];
}

export interface ImportRow {
  section: string;
  label: string;
  weeks: (string | null)[];
  workerRate: string | null;
  clientRate: string | null;
  advance: string | null;
  difference: string | null;
  contributions: string | null;
  fuel: string | null;
  housing: string | null;
  holiday: string | null;
  bonus: string | null;
}

/** Lower case, no diacritics, single spaces. */
export function normalizeHeader(value: unknown): string {
  return String(value ?? '')
    .replace(/[đĐ]/g, 'd')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim();
}

const patterns: Record<Exclude<SheetField, 'label'>, RegExp[]> = {
  // "SATNICA" is the worker's rate; "SATNICA WH/TRANE" (or a client name) is the client's, tested first.
  clientRate: [/cijena.*klijent/, /satnica.*(wh|trane|klijent|kupac)/, /preis.*kunde/, /client.*rate/],
  workerRate: [/cijena radnika/, /^satnica$/, /^satnica radnika/, /stundenlohn/, /worker.*rate/, /^rate$/],
  advance: [/akontacij/, /vorschuss/, /advance/],
  difference: [/razlika/, /bonus(?! regres)/, /differenz/],
  contributions: [/doprinos/, /prispev/, /sozialabgab/, /contribution/],
  fuel: [/gorivo/, /treibstoff/, /fuel/],
  housing: [/stanovanj/, /smjestaj/, /smestaj/, /unterkunft/, /housing/],
  holiday: [/godisnji/, /urlaub/, /holiday/, /leave/],
  bonus: [/regres/],
};

const labelPatterns = [/^ime/, /^radnik/, /^naziv/, /^name/, /^mitarbeiter/, /^prezime/, /^zaposlen/];
const weekPattern = /^(kw|kalenderwoche|sedmica|tjedan|woche|week)\s*\d*/;

/** Finds the columns by the words in the header row; what it cannot find is -1, for the person to choose. */
export function detectMapping(header: readonly unknown[]): SheetMapping {
  const names = header.map(normalizeHeader);

  const fields = Object.fromEntries(sheetFields.map((f) => [f, -1])) as Record<SheetField, number>;

  fields.label = names.findIndex((n) => labelPatterns.some((p) => p.test(n)));

  for (const field of Object.keys(patterns) as Exclude<SheetField, 'label'>[]) {
    fields[field] = names.findIndex((n, index) => {
      const taken = Object.entries(fields).some(([key, value]) => key !== field && value === index);

      return !taken && patterns[field].some((p) => p.test(n));
    });
  }

  const weeks = names
    .map((n, index) => (weekPattern.test(n) || /^\d{1,2} ?(kw|sedm)/.test(n) ? index : -1))
    .filter((index) => index >= 0)
    .slice(0, IMPORT_WEEK_COLUMNS);

  return { fields, weeks };
}

/** The header is the first row, within the first twenty, that names a person and at least one other column. */
export function detectHeaderRow(matrix: readonly (readonly unknown[])[]): number {
  const limit = Math.min(matrix.length, 20);

  for (let index = 0; index < limit; index++) {
    const mapping = detectMapping(matrix[index] ?? []);
    const found =
      (mapping.fields.label >= 0 ? 1 : 0)
      + mapping.weeks.length
      + sheetFields.filter((f) => f !== 'label' && mapping.fields[f] >= 0).length;

    if (mapping.fields.label >= 0 && found >= 3) {
      return index;
    }
  }

  return 0;
}

function text(cell: unknown): string {
  return cell === null || cell === undefined ? '' : String(cell).trim();
}

const totalWords = /^(ukupno|zbir|total|summe|sum|gesamt|ukupan|svega)\b/;

function looksNumeric(value: string): boolean {
  return /^[-+]?[\d.,\s]+$/.test(value) && /\d/.test(value);
}

/**
 * Turns the rows under the header into people under clients. A row with a name and no number in any mapped
 * column is a client's heading; a row with a name and a number is a person under the heading above it.
 * Empty rows and total rows are skipped.
 */
export function extractRows(
  matrix: readonly (readonly unknown[])[],
  headerRow: number,
  mapping: SheetMapping,
): ImportRow[] {
  const rows: ImportRow[] = [];
  let section = '';

  const numericColumns = [
    ...mapping.weeks,
    ...sheetFields.filter((f) => f !== 'label').map((f) => mapping.fields[f]),
  ].filter((index) => index >= 0);

  for (let index = headerRow + 1; index < matrix.length; index++) {
    const line = matrix[index] ?? [];
    const label = mapping.fields.label >= 0 ? text(line[mapping.fields.label]) : '';

    if (label === '' || totalWords.test(normalizeHeader(label))) {
      continue;
    }

    const hasNumber = numericColumns.some((column) => looksNumeric(text(line[column])));

    if (!hasNumber) {
      section = label;
      continue;
    }

    const pick = (field: SheetField): string | null => {
      const column = mapping.fields[field];
      const value = column >= 0 ? text(line[column]) : '';

      return value === '' ? null : value;
    };

    rows.push({
      section,
      label,
      weeks: mapping.weeks.map((column) => {
        const value = text(line[column]);

        return value === '' ? null : value;
      }),
      workerRate: pick('workerRate'),
      clientRate: pick('clientRate'),
      advance: pick('advance'),
      difference: pick('difference'),
      contributions: pick('contributions'),
      fuel: pick('fuel'),
      housing: pick('housing'),
      holiday: pick('holiday'),
      bonus: pick('bonus'),
    });
  }

  return rows;
}
