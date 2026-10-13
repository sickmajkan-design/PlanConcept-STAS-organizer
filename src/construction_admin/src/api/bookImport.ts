import { request } from './client';

export interface BookImportRowInput {
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

export interface BookImportInput {
  fileName: string;
  year: number;
  month: number;
  /** For a month that already has entries: write only the cells that are still empty. */
  fillEmptyOnly: boolean;
  /** Make employees of the names the system does not know. */
  createUnknownEmployees: boolean;
  rows: BookImportRowInput[];
  /** The person's choice of project for a section name that did not match one. */
  sectionProjects: Record<string, string | null>;
}

export interface BookImportPlanRow {
  index: number;
  section: string;
  label: string;
  employeeId: string | null;
  employeeName: string | null;
  projectId: string | null;
  projectName: string | null;
  status: 'Ok' | 'Warning' | 'Error';
  messages: string[];
  hours: number;
}

export interface BookImportPlan {
  year: number;
  month: number;
  existingLedgerId: string | null;
  existingHasEntries: boolean;
  rows: BookImportPlanRow[];
  sections: string[];
  unmatchedSections: string[];
  okCount: number;
  warningCount: number;
  errorCount: number;
  sheetHours: number;
  sheetBilling: number;
  /** Hours times the worker rates typed in the sheet. */
  sheetWage: number;
}

export interface BookImportResult {
  batchId: string;
  ledgerId: string;
  ledgerCreated: boolean;
  imported: number;
  skipped: number;
  sheetHours: number;
  ledgerHours: number;
  sheetBilling: number;
  ledgerBilling: number;
  hoursDifference: number;
  billingDifference: number;
  plan: BookImportPlan;
}

export interface BookImportBatch {
  id: string;
  fileName: string;
  year: number;
  month: number;
  status: 'Completed' | 'Undone';
  rowCount: number;
  importedCount: number;
  skippedCount: number;
  ledgerId: string | null;
  ledgerCreated: boolean;
  createdAt: string;
  report: BookImportPlan | null;
}

export const bookImportApi = {
  list: () => request<BookImportBatch[]>({ method: 'GET', url: '/api/v1/book/imports' }),

  preview: (input: BookImportInput) =>
    request<BookImportPlan>({ method: 'POST', url: '/api/v1/book/imports/preview', data: input }),

  commit: (input: BookImportInput) =>
    request<BookImportResult>({ method: 'POST', url: '/api/v1/book/imports/commit', data: input }),

  undo: (id: string) => request<void>({ method: 'POST', url: `/api/v1/book/imports/${id}/undo` }),
};
