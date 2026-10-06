import { request } from './client';
import type { PagedList } from './types';

export const fuelTransactionStatuses = [
  'Matched',
  'NeedsReview',
  'NoDriverEntry',
  'UnknownCard',
  'Resolved',
  'Ignored',
] as const;

export type FuelTransactionStatus = (typeof fuelTransactionStatuses)[number];

export const fuelTransactionIssues = [
  'None',
  'UnknownCard',
  'TdMismatch',
  'FuelTypeMismatch',
  'AmountMismatch',
  'NoDriverEntry',
  'IncompleteEntry',
] as const;

export type FuelTransactionIssue = (typeof fuelTransactionIssues)[number];

export type DkvRowOutcome = 'New' | 'Duplicate' | 'Updated';

export type FuelTransactionResolution = 'LinkExpense' | 'CreateExpense' | 'Confirm' | 'Ignore';

/** Mirrors the API's FuelImportRules.MaxSizeBytes. */
export const MAX_DKV_STATEMENT_BYTES = 10 * 1024 * 1024;

export const DKV_STATEMENT_ACCEPTED_EXTENSIONS = '.csv,.xlsx';

export interface DkvPreviewRow {
  rowNumber: number;
  cardNumber: string;
  vehicleId: string | null;
  vehicleName: string | null;
  statementVehicleLabel: string | null;
  occurredOn: string;
  occurredAtTime: string;
  productType: string | null;
  amount: number;
  currency: string;
  country: string | null;
  isInvoiced: boolean;
  outcome: DkvRowOutcome;
  status: FuelTransactionStatus;
  issue: FuelTransactionIssue;
  issueDetail: string | null;
}

export interface DkvUnknownCard {
  cardNumber: string;
  statementVehicleLabel: string | null;
  rowCount: number;
  totalAmount: number;
  suggestedVehicleId: string | null;
  suggestedVehicleName: string | null;
}

export interface DkvImportPreview {
  totalRows: number;
  newCount: number;
  duplicateCount: number;
  updatedCount: number;
  matchedCount: number;
  needsReviewCount: number;
  noDriverEntryCount: number;
  unknownCardCount: number;
  newAmount: number;
  parseErrors: { rowNumber: number; reason: string }[];
  unknownCards: DkvUnknownCard[];
  rows: DkvPreviewRow[];
}

export interface DkvImportResult {
  batchId: string;
  totalRows: number;
  newCount: number;
  updatedCount: number;
  duplicateCount: number;
  skippedCount: number;
}

export interface FuelTransaction {
  id: string;
  cardNumber: string;
  vehicleId: string | null;
  vehicleName: string | null;
  vehicleTdNumber: string | null;
  statementVehicleLabel: string | null;
  occurredOn: string;
  occurredAtTime: string;
  productType: string | null;
  amount: number;
  currency: string;
  country: string | null;
  isInvoiced: boolean;
  status: FuelTransactionStatus;
  issue: FuelTransactionIssue;
  issueDetail: string | null;
  vehicleExpenseId: string | null;
  expenseAmount: number | null;
  expenseOccurredOn: string | null;
  expenseLitres: number | null;
  resolutionNote: string | null;
  resolvedAt: string | null;
  importBatchId: string;
}

export interface FuelImportBatch {
  id: string;
  fileName: string;
  importedAt: string;
  importedByEmail: string | null;
  totalRows: number;
  newCount: number;
  updatedCount: number;
  duplicateCount: number;
}

export interface FuelExpenseCandidate {
  expenseId: string;
  occurredOn: string;
  amount: number;
  litres: number | null;
  odometerKm: number | null;
  fuelProductType: string | null;
}

export interface FuelTransactionListQuery {
  pageNumber: number;
  pageSize: number;
  status?: FuelTransactionStatus[];
  vehicleId?: string;
  batchId?: string;
  cardNumber?: string;
  /** Card number, product, or the vehicle (name, plate or TD number). */
  search?: string;
}

export interface ResolveFuelTransactionInput {
  resolution: FuelTransactionResolution;
  expenseId?: string;
  note?: string;
  /** Required for `CreateExpense`: the statement has no litres. */
  litres?: number;
  odometerKm?: number;
}

const BASE = '/api/v1/fuel-transactions';

function statementForm(file: File) {
  const form = new FormData();
  form.append('file', file);
  return form;
}

export const fuelTransactionsApi = {
  list: (query: FuelTransactionListQuery) =>
    request<PagedList<FuelTransaction>>({
      method: 'GET',
      url: BASE,
      params: query,
      // ASP.NET binds repeated keys (status=A&status=B), not status[]=A.
      paramsSerializer: { indexes: null },
    }),

  counts: () => request<Record<FuelTransactionStatus, number>>({ method: 'GET', url: `${BASE}/counts` }),

  batches: (pageNumber = 1, pageSize = 10) =>
    request<PagedList<FuelImportBatch>>({
      method: 'GET',
      url: `${BASE}/batches`,
      params: { pageNumber, pageSize },
    }),

  preview: (file: File) =>
    request<DkvImportPreview>({ method: 'POST', url: `${BASE}/import/preview`, data: statementForm(file) }),

  commit: (file: File) =>
    request<DkvImportResult>({ method: 'POST', url: `${BASE}/import`, data: statementForm(file) }),

  candidates: (id: string) =>
    request<FuelExpenseCandidate[]>({ method: 'GET', url: `${BASE}/${id}/candidates` }),

  resolve: (id: string, input: ResolveFuelTransactionInput) =>
    request<FuelTransaction>({ method: 'POST', url: `${BASE}/${id}/resolve`, data: input }),

  assignCard: (cardNumber: string, vehicleId: string) =>
    request<number>({ method: 'POST', url: `${BASE}/assign-card`, data: { cardNumber, vehicleId } }),

  recheck: () => request<number>({ method: 'POST', url: `${BASE}/recheck` }),
};
