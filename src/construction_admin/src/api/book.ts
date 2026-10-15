import { request } from './client';
import { exportsApi } from './exports';
import { idempotencyHeaders } from './idempotency';
import { listParams } from './resource';
import type { LedgerCheck, PagedList } from './types';

export const bookEntryKinds = ['Cost', 'Income', 'Payment', 'Payout'] as const;
export type BookEntryKind = (typeof bookEntryKinds)[number];

export const bookPaymentMethods = ['Bank', 'Cash'] as const;
export type BookPaymentMethod = (typeof bookPaymentMethods)[number];

export const bookCategoryKinds = ['Income', 'Cost'] as const;
export type BookCategoryKind = (typeof bookCategoryKinds)[number];

export const bookCategoryBuckets = ['Collection', 'WorkerEarnings', 'SiteCosts', 'Fixed'] as const;
export type BookCategoryBucket = (typeof bookCategoryBuckets)[number];

/** Where a line is kept, and so where it is changed. Only "BookEntry" can be changed in the book. */
export type BookRowSource =
  | 'BookEntry'
  | 'VehicleExpense'
  | 'ToolExpense'
  | 'GeneralExpense'
  | 'FinanceEntry'
  | 'ProjectRevenue'
  | 'CompanyRevenue'
  | 'Invoice'
  | 'InvoicePayment'
  | 'WagePayout';

export interface BookRow {
  source: BookRowSource;
  /** Narrows the source: how an entry was made, whether a vehicle cost is fuel, which payout it is. */
  origin: string | null;
  id: string;
  kind: BookEntryKind;
  occurredOn: string;
  amount: number;
  categoryId: string | null;
  categoryName: string | null;
  projectId: string | null;
  projectName: string | null;
  counterparty: string | null;
  description: string | null;
  method: BookPaymentMethod | null;
  dueDate: string | null;
  paidOn: string | null;
  /** Not paid yet: a cost still owed or an invoice still open. */
  isOpen: boolean;
  editable: boolean;
  branchId: string | null;
  /** Documents on the record that owns this line. */
  documentCount: number;
}

export interface BookEntry {
  id: string;
  kind: BookEntryKind;
  occurredOn: string;
  amount: number;
  categoryId: string;
  categoryName: string;
  projectId: string | null;
  projectName: string | null;
  employeeId: string | null;
  customerId: string | null;
  counterparty: string | null;
  description: string | null;
  method: BookPaymentMethod | null;
  dueDate: string | null;
  paidOn: string | null;
  branchId: string | null;
  branchName: string | null;
  createdAt: string;
}

export interface BookEntryInput {
  kind: BookEntryKind;
  amount: number;
  occurredOn: string;
  categoryId: string | null;
  projectId: string | null;
  employeeId?: string | null;
  customerId?: string | null;
  branchId: string | null;
  counterparty: string | null;
  description: string | null;
  method: BookPaymentMethod | null;
  dueDate: string | null;
  paidOn: string | null;
  /** Makes a second send of the same entry a no-op; set it for a client that can retry late. */
  clientRequestId?: string | null;
}

export interface BookListQuery {
  pageNumber: number;
  pageSize: number;
  from?: string;
  to?: string;
  kind?: BookEntryKind | '';
  categoryId?: string;
  projectId?: string;
  open?: boolean;
  search?: string;
  branchId?: string;
}

export interface BookCategory {
  id: string;
  name: string;
  kind: BookCategoryKind;
  group: string | null;
  bucket: BookCategoryBucket;
  /** Set for the categories the system seeds; they can be renamed but never switched off. */
  systemKey: string | null;
  isActive: boolean;
  sortOrder: number;
  entryCount: number;
  /** How many of them are dated in the month asked for (the current one by default). */
  monthEntryCount: number;
}

export interface BookCategoryRule {
  id: string;
  text: string;
  categoryId: string;
  categoryName: string;
  priority: number;
}

export interface BookCategorySuggestion {
  categoryId: string | null;
  categoryName: string | null;
  matchedRule: string | null;
}

export type DebtAge = 'NotDue' | 'Days1To30' | 'Days31To60' | 'Over60';

export interface DebtRow {
  /** "Invoice" or "BookEntry": where the debt is kept, and so where it is settled. */
  source: 'Invoice' | 'BookEntry' | 'Payroll';
  id: string;
  title: string;
  projectId: string | null;
  projectName: string | null;
  counterparty: string | null;
  issuedOn: string;
  dueDate: string | null;
  amount: number;
  paidAmount: number;
  openAmount: number;
  daysOverdue: number;
  age: DebtAge;
  branchId: string | null;
}

export interface DebtBucket {
  age: DebtAge;
  amount: number;
  count: number;
}

export interface Debts {
  asOf: string;
  openTotal: number;
  overdueTotal: number;
  buckets: DebtBucket[];
  rows: DebtRow[];
}

export interface BookTotals {
  /** Income and payments received. */
  inflow: number;
  /** Costs and payouts. */
  outflow: number;
  count: number;
  /** How many lines of each kind the selection holds. */
  countsByKind: Record<string, number>;
}

export interface SettlementSite {
  sectionId: string;
  projectId: string | null;
  name: string;
  billingMode: 'Hourly' | 'FlatRate' | 'Measured' | null;
  hours: number;
  clientRate: number | null;
  invoiced: number;
  billing: number;
  wage: number;
  margin: number;
  costs: number;
  result: number;
  /** What the month would have billed by the hour, for a site billed by a sum. */
  byHours: number | null;
}

export interface SettlementSummary {
  year: number;
  month: number;
  hasSettlement: boolean;
  ledgerId: string | null;
  lockedAt: string | null;
  unlockNote: string | null;
  sites: SettlementSite[];
  totalHours: number;
  totalBilling: number;
  totalWage: number;
  totalMargin: number;
  totalCosts: number;
  totalResult: number;
  fixedLines: { name: string; amount: number }[];
  fixedTotal: number;
  firmResult: number;
  checks: LedgerCheck[];
  /** Costs of the month with no document attached, and their total. */
  costsWithoutDocumentCount: number;
  costsWithoutDocumentAmount: number;
}

export const bookApi = {
  settlement: (query: { year: number; month: number; branchId?: string }) =>
    request<SettlementSummary>({
      method: 'GET',
      url: '/api/v1/book/settlement',
      params: listParams(query),
    }),

  updateSiteBilling: (
    projectId: string,
    input: { billingMode: 'Hourly' | 'FlatRate' | 'Measured'; clientHourlyRate: number | null },
  ) =>
    request<void>({ method: 'PUT', url: `/api/v1/book/settlement/sites/${projectId}`, data: input }),
  receivables: (query: { branchId?: string; projectId?: string } = {}) =>
    request<Debts>({ method: 'GET', url: '/api/v1/receivables', params: listParams(query) }),

  payables: (query: { branchId?: string; projectId?: string } = {}) =>
    request<Debts>({ method: 'GET', url: '/api/v1/payables', params: listParams(query) }),

  entries: {
    export: (query: Omit<BookListQuery, 'pageNumber' | 'pageSize'>) => exportsApi.book(listParams(query)),

    list: (query: BookListQuery) =>
      request<PagedList<BookRow>>({
        method: 'GET',
        url: '/api/v1/book/entries',
        params: listParams(query),
      }),

    totals: (query: Omit<BookListQuery, 'pageNumber' | 'pageSize'>) =>
      request<BookTotals>({
        method: 'GET',
        url: '/api/v1/book/entries/totals',
        params: listParams(query),
      }),

    create: (input: BookEntryInput, idempotencyKey?: string) =>
      request<BookEntry>({
        method: 'POST',
        url: '/api/v1/book/entries',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: BookEntryInput) =>
      request<BookEntry>({ method: 'PUT', url: `/api/v1/book/entries/${id}`, data: input }),

    remove: (id: string) => request<void>({ method: 'DELETE', url: `/api/v1/book/entries/${id}` }),

    markPaid: (id: string, input: { paidOn?: string | null; method?: BookPaymentMethod | null }) =>
      request<BookEntry>({ method: 'POST', url: `/api/v1/book/entries/${id}/mark-paid`, data: input }),

    suggestCategory: (text: string) =>
      request<BookCategorySuggestion>({
        method: 'GET',
        url: '/api/v1/book/entries/suggest-category',
        params: { text },
      }),
  },

  categories: {
    list: (includeInactive = true, month?: { year: number; month: number }) =>
      request<BookCategory[]>({
        method: 'GET',
        url: '/api/v1/book/categories',
        params: { includeInactive, ...(month ?? {}) },
      }),

    create: (input: {
      name: string;
      kind: BookCategoryKind;
      group: string | null;
      bucket: BookCategoryBucket;
      sortOrder: number;
    }) => request<BookCategory>({ method: 'POST', url: '/api/v1/book/categories', data: input }),

    update: (
      id: string,
      input: {
        name: string;
        group: string | null;
        sortOrder: number;
        isActive: boolean;
        bucket?: BookCategoryBucket | null;
      },
    ) => request<BookCategory>({ method: 'PUT', url: `/api/v1/book/categories/${id}`, data: input }),
  },

  rules: {
    list: () => request<BookCategoryRule[]>({ method: 'GET', url: '/api/v1/book/category-rules' }),

    create: (input: { text: string; categoryId: string; priority: number }) =>
      request<BookCategoryRule>({ method: 'POST', url: '/api/v1/book/category-rules', data: input }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/book/category-rules/${id}` }),
  },
};
