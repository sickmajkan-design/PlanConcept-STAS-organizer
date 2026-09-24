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
