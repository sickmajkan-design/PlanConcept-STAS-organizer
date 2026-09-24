import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { listParams } from './resource';
import type { PagedList } from './types';

export const financeGranularities = ['Day', 'Week', 'Month'] as const;
export type FinanceGranularity = (typeof financeGranularities)[number];

export interface FinanceBucket {
  from: string;
  to: string;
  expense: number;
  revenue: number;
  revenueProject: number;
  revenueOther: number;
  profit: number;
}

export interface FinanceTotals {
  from: string;
  to: string;
  expense: number;
  revenue: number;
  revenueProject: number;
  revenueOther: number;
  profit: number;
  /** Null when there is no revenue for the profit to be a share of. */
  marginPercent: number | null;
}

export interface FinanceSeries {
  granularity: FinanceGranularity;
  includesLabour: boolean;
  buckets: FinanceBucket[];
  /** The sum of `buckets`. */
  totals: FinanceTotals;
  /** The period of the same length that ends the day before this one starts. */
  previous: FinanceTotals;
}

export interface FinanceSeriesQuery {
  from: string;
  to: string;
  granularity: FinanceGranularity;
}

export interface FinanceProjectRow {
  projectId: string;
  projectName: string;
  contractValue: number | null;
  budget: number | null;
  /** What came in against the project's contract in the period. */
  revenue: number;
  /** What the project cost in the period — the project cost report's total, manual pay included. */
  expense: number;
  profit: number;
  marginPercent: number | null;
}

export interface FinanceMoney {
  revenue: number;
  expense: number;
  profit: number;
}

export interface FinanceByProject {
  from: string;
  to: string;
  includesLabour: boolean;
  /** Busiest first — by spending, then by revenue. */
  rows: FinanceProjectRow[];
  /** How many projects had money move in the period, before the list was cut to `top`. */
  totalProjects: number;
  /** The company's figures — the ones the overview shows. */
  company: FinanceMoney;
  /** What the company's figures hold beyond the projects: fleet, tools, empty housing, untied costs and pay, rental income. */
  unallocated: FinanceMoney;
  /** Days a pay entry tied to no site sits beside hours the same person clocked at a site. */
  unassignedPayOverlaps: number;
  /** Housing expenses dated on a day an accommodation rate is in force — the rent is counted twice. */
  housingDoubleEntries: number;
}

export interface FinanceBreakdownItem {
  /** "Labour", "ManualPay", "Material", "GeneralExpenses", "Accommodation", "Vehicles" or "Tools". */
  kind: string;
  amount: number;
}

export interface FinanceBreakdown {
  from: string;
  to: string;
  projectId: string | null;
  includesLabour: boolean;
  /** Every kind, zero or not, in a fixed order. */
  items: FinanceBreakdownItem[];
  /** The sum of `items`. */
  total: number;
}

export interface FinanceMoneyFigures {
  revenue: number;
  expense: number;
  profit: number;
  marginPercent: number | null;
}

export interface ProjectFinanceSummary {
  projectId: string;
  projectName: string;
  contractValue: number | null;
  budget: number | null;
  includesLabour: boolean;
  period: FinanceMoneyFigures;
  /** From the start of the project to today — what a budget is measured against. */
  toDate: FinanceMoneyFigures;
  toDateFrom: string;
  budgetUsedPercent: number | null;
  contractCollectedPercent: number | null;
}

export interface ProjectBudget {
  projectId: string;
  contractValue: number | null;
  budget: number | null;
}

export const companyRevenueSources = ['VehicleRental', 'ToolRental', 'Other'] as const;
export type CompanyRevenueSource = (typeof companyRevenueSources)[number];

export interface CompanyRevenue {
  id: string;
  amount: number;
  occurredOn: string;
  source: CompanyRevenueSource;
  vehicleId: string | null;
  vehicleName: string | null;
  toolId: string | null;
  toolName: string | null;
  note: string | null;
  createdAt: string;
}

export interface CompanyRevenueInput {
  amount: number;
  occurredOn?: string | null;
  source: CompanyRevenueSource;
  vehicleId?: string | null;
  toolId?: string | null;
  note?: string | null;
}

export interface CompanyRevenueListQuery {
  pageNumber: number;
  pageSize: number;
  from?: string;
  to?: string;
  source?: CompanyRevenueSource;
}

export const financeApi = {
  series: (query: FinanceSeriesQuery) =>
    request<FinanceSeries>({
      method: 'GET',
      url: '/api/v1/finance/series',
      params: listParams(query),
    }),

  byProject: (query: { from: string; to: string; top?: number }) =>
    request<FinanceByProject>({
      method: 'GET',
      url: '/api/v1/finance/by-project',
      params: listParams(query),
    }),

  breakdown: (query: { from: string; to: string; projectId?: string }) =>
    request<FinanceBreakdown>({
      method: 'GET',
      url: '/api/v1/finance/breakdown',
      params: listParams(query),
    }),

  projectSummary: (projectId: string, query: { from: string; to: string }) =>
    request<ProjectFinanceSummary>({
      method: 'GET',
      url: `/api/v1/finance/projects/${projectId}/summary`,
      params: listParams(query),
    }),

  budget: {
    get: (projectId: string) =>
      request<ProjectBudget>({ method: 'GET', url: `/api/v1/finance/projects/${projectId}/budget` }),

    set: (projectId: string, budget: number | null) =>
      request<ProjectBudget>({
        method: 'PUT',
        url: `/api/v1/finance/projects/${projectId}/budget`,
        data: { budget },
      }),
  },

  companyRevenues: {
    list: (query: CompanyRevenueListQuery) =>
      request<PagedList<CompanyRevenue>>({
        method: 'GET',
        url: '/api/v1/finance/company-revenues',
        params: listParams(query),
      }),

    record: (input: CompanyRevenueInput, idempotencyKey?: string) =>
      request<CompanyRevenue>({
        method: 'POST',
        url: '/api/v1/finance/company-revenues',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: CompanyRevenueInput) =>
      request<CompanyRevenue>({
        method: 'PUT',
        url: `/api/v1/finance/company-revenues/${id}`,
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/finance/company-revenues/${id}` }),
  },
};
