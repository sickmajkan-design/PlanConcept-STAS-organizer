import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import type { BookPaymentMethod } from './book';
import type { PayoutGroup } from './types';

export const payoutKinds = ['Advance', 'Final'] as const;
export type PayoutKind = (typeof payoutKinds)[number];

export interface WagePayout {
  id: string;
  year: number;
  month: number;
  group: PayoutGroup;
  employeeId: string | null;
  /** Null when the account may not see what individuals are paid. */
  employeeName: string | null;
  kind: PayoutKind;
  payDate: string;
  amount: number;
  method: BookPaymentMethod | null;
  /** Null for a final payment that is only planned. */
  paidOn: string | null;
  note: string | null;
  branchId: string | null;
  createdAt: string;
}

export interface PayoutGroupSummary {
  group: PayoutGroup;
  /** What the group earned in the month's settlement, after the advances typed in the sheet. */
  earned: number;
  /** Advances recorded here, on top of any typed in the settlement. */
  advances: number;
  finalPaid: number;
  finalPlanned: number;
  /** Earned, less advances, less final payments made; zero when overpaid. */
  remaining: number;
  /** What was paid out beyond what was earned, carried into the next month. */
  carryOver: number;
  rowCount: number;
}

export interface Payouts {
  year: number;
  month: number;
  hasSettlement: boolean;
  groups: PayoutGroupSummary[];
  totalEarned: number;
  totalRemaining: number;
  items: WagePayout[];
}

export interface WagePayoutInput {
  year: number;
  month: number;
  group: PayoutGroup;
  kind: PayoutKind;
  payDate: string;
  amount: number;
  employeeId?: string | null;
  method?: BookPaymentMethod | null;
  note?: string | null;
  branchId?: string | null;
}

export const payoutsApi = {
  get: (query: { year: number; month: number; branchId?: string }) =>
    request<Payouts>({
      method: 'GET',
      url: '/api/v1/payouts',
      params: { year: query.year, month: query.month, ...(query.branchId ? { branchId: query.branchId } : {}) },
    }),

  create: (input: WagePayoutInput, idempotencyKey?: string) =>
    request<WagePayout>({
      method: 'POST',
      url: '/api/v1/payouts',
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  update: (
    id: string,
    input: {
      payDate: string;
      amount: number;
      employeeId?: string | null;
      method?: BookPaymentMethod | null;
      note?: string | null;
      branchId?: string | null;
    },
  ) => request<WagePayout>({ method: 'PUT', url: `/api/v1/payouts/${id}`, data: input }),

  remove: (id: string) => request<void>({ method: 'DELETE', url: `/api/v1/payouts/${id}` }),

  markPaid: (id: string, input: { paidOn?: string | null; method?: BookPaymentMethod | null }) =>
    request<WagePayout>({ method: 'POST', url: `/api/v1/payouts/${id}/mark-paid`, data: input }),
};
