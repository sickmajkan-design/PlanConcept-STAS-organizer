import type { ImportEmployeeRow } from '../api/onboarding';

/** Column headers the template uses, in order. Serbian first: that is who fills it in. */
export const EMPLOYEE_TEMPLATE_HEADERS = [
  'Ime',
  'Prezime',
  'Telefon',
  'Email',
  'Pozicija',
  'Datum zaposlenja',
  'Vrsta',
  'Broj radnika',
  'Adresa',
  'Datum rođenja',
] as const;

type Field = Exclude<keyof ImportEmployeeRow, 'line'> | 'fullName';

/**
 * What a header may be called, once lower-cased and stripped of accents,
 * spaces and punctuation. A customer's own spreadsheet rarely matches a
 * template exactly, so this recognises the usual names instead of refusing it.
 */
const SYNONYMS: Record<Field, string[]> = {
  firstName: ['ime', 'firstname', 'vorname', 'first'],
  lastName: ['prezime', 'lastname', 'nachname', 'surname', 'last'],
  fullName: ['imeiprezime', 'punoime', 'name', 'radnik', 'fullname', 'imeprezime', 'zaposlenik', 'zaposleni'],
  phone: ['telefon', 'tel', 'mobitel', 'mobilni', 'mobilnitelefon', 'phone', 'brojtelefona', 'gsm', 'handy'],
  email: ['email', 'mail', 'eposta', 'emailadresa'],
  address: ['adresa', 'address', 'prebivaliste', 'adresastanovanja'],
  position: ['pozicija', 'radnomjesto', 'zanimanje', 'position', 'funkcija', 'posao', 'struka'],
  employeeNumber: ['brojradnika', 'sifra', 'sifraradnika', 'employeenumber', 'id', 'redbroj', 'matbroj'],
  employmentDate: ['datumzaposlenja', 'zaposlenod', 'datumpocetka', 'employmentdate', 'pocetakrada', 'datumprijema', 'odkada'],
  dateOfBirth: ['datumrodjenja', 'datumrodenja', 'rodjenje', 'rodenje', 'dateofbirth', 'dob', 'godinarodjenja'],
  type: ['vrsta', 'tip', 'type', 'vrstaradnika'],
};

export function normaliseHeader(value: unknown): string {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/đ/gi, 'd')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

function fieldFor(header: unknown): Field | null {
  const key = normaliseHeader(header);

  if (!key) return null;

  for (const field of Object.keys(SYNONYMS) as Field[]) {
    if (SYNONYMS[field].includes(key)) return field;
  }

  return null;
}

const pad = (n: number) => String(n).padStart(2, '0');

/**
 * Turns whatever a spreadsheet holds for a date into `YYYY-MM-DD`: a real
 * date cell, an Excel serial number, or text as people write it
 * (`15.09.2026`, `15/9/2026`, `2026-09-15`). Returns null for anything else,
 * so the server can say the date is not valid rather than guess.
 */
export function toIsoDate(value: unknown): string | null {
  if (value === null || value === undefined || value === '') return null;

  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
  }

  if (typeof value === 'number' && value > 20000 && value < 80000) {
    // Excel serial days since 1899-12-30.
    const date = new Date(Date.UTC(1899, 11, 30) + Math.round(value) * 86_400_000);

    return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`;
  }

  const text = String(value).trim();

  const iso = /^(\d{4})-(\d{1,2})-(\d{1,2})/.exec(text);
  if (iso) return `${iso[1]}-${pad(Number(iso[2]))}-${pad(Number(iso[3]))}`;

  const local = /^(\d{1,2})[./-](\d{1,2})[./-](\d{4})\.?$/.exec(text);
  if (local) return `${local[3]}-${pad(Number(local[2]))}-${pad(Number(local[1]))}`;

  return null;
}

function toType(value: unknown): 'Employee' | 'Subcontractor' | undefined {
  const key = normaliseHeader(value);

  if (!key) return undefined;
  if (['kooperant', 'kooperanti', 'subcontractor', 'podizvodjac', 'vanjski', 'eksterni'].includes(key)) {
    return 'Subcontractor';
  }

  // Anything else that names a type is passed through so the server can refuse
  // it with a message, rather than the client quietly deciding what it meant.
  if (['radnik', 'zaposlenik', 'zaposleni', 'employee', 'redovan', 'direktan'].includes(key)) {
    return 'Employee';
  }

  return String(value).trim() as 'Employee';
}

/** "Marko Petrović" → first name "Marko", last name "Petrović". */
function splitFullName(value: string): { firstName: string; lastName: string } {
  const parts = value.trim().split(/\s+/);

  return { firstName: parts[0] ?? '', lastName: parts.slice(1).join(' ') };
}

export interface ParsedEmployeeSheet {
  rows: ImportEmployeeRow[];
  /** The columns the file had that were recognised, for the "we read these" summary. */
  recognised: Field[];
  /** Headers that were present but not understood — shown so nothing is silently dropped. */
  ignored: string[];
  /** False when no header row with a name column could be found. */
  ok: boolean;
}

/**
 * Reads a sheet given as rows of cells. The header is the first row that has a
 * recognisable name column, so a title or a blank line above it is harmless.
 */
export function parseEmployeeRows(cells: unknown[][]): ParsedEmployeeSheet {
  const empty: ParsedEmployeeSheet = { rows: [], recognised: [], ignored: [], ok: false };

  const headerIndex = cells.findIndex((row) => {
    const fields = row.map(fieldFor);

    return fields.includes('firstName') || fields.includes('fullName');
  });

  if (headerIndex < 0) return empty;

  const header = cells[headerIndex]!;
  const columns = header.map(fieldFor);
  const hasSplitName = columns.includes('firstName') && columns.includes('lastName');

  const rows: ImportEmployeeRow[] = [];

  for (let i = headerIndex + 1; i < cells.length; i++) {
    const cellsOfRow = cells[i] ?? [];

    if (cellsOfRow.every((c) => String(c ?? '').trim() === '')) continue;

    const row: Record<string, unknown> = { line: i + 1 };
    const text = (index: number) => String(cellsOfRow[index] ?? '').trim();

    columns.forEach((field, index) => {
      if (!field) return;

      const raw = cellsOfRow[index];

      if (field === 'fullName') {
        if (!hasSplitName && text(index)) {
          const { firstName, lastName } = splitFullName(text(index));
          row.firstName = firstName;
          row.lastName = lastName;
        }
      } else if (field === 'employmentDate' || field === 'dateOfBirth') {
        const iso = toIsoDate(raw);

        // Keep the original text when it is not a date the client understands,
        // so the server's message names what was actually in the file.
        row[field] = iso ?? (text(index) || undefined);
      } else if (field === 'type') {
        row.type = toType(raw);
      } else if (text(index)) {
        row[field] = text(index);
      }
    });

    rows.push(row as unknown as ImportEmployeeRow);
  }

  return {
    rows,
    recognised: [...new Set(columns.filter((c): c is Field => c !== null))],
    ignored: header
      .map((h, index) => ({ h: String(h ?? '').trim(), field: columns[index] }))
      .filter((x) => x.h !== '' && x.field === null)
      .map((x) => x.h),
    ok: true,
  };
}

/** Reads the first sheet of an .xlsx or .csv file the user picked. */
export async function parseEmployeeFile(file: File): Promise<ParsedEmployeeSheet> {
  // Loaded on demand: the spreadsheet library is large and this is used rarely.
  const XLSX = await import('xlsx');
  const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array', cellDates: true });
  const sheet = workbook.Sheets[workbook.SheetNames[0]!];

  if (!sheet) return { rows: [], recognised: [], ignored: [], ok: false };

  const cells = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1, raw: true, defval: '' });

  return parseEmployeeRows(cells);
}

/** Downloads an empty template with the headers and one example row. */
export async function downloadEmployeeTemplate(): Promise<void> {
  const XLSX = await import('xlsx');
  const sheet = XLSX.utils.aoa_to_sheet([
    [...EMPLOYEE_TEMPLATE_HEADERS],
    ['Marko', 'Petrović', '+387 61 123 456', 'marko@primjer.ba', 'Zidar', '01.09.2026', 'Radnik', '', 'Ulica 1, Sarajevo', '12.05.1990'],
  ]);

  sheet['!cols'] = EMPLOYEE_TEMPLATE_HEADERS.map(() => ({ wch: 20 }));

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, sheet, 'Radnici');
  XLSX.writeFile(workbook, 'radnici-sablon.xlsx');
}
