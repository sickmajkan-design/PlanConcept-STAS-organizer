import { request } from './client';
import { listParams } from './resource';
import type { ListQuery, PagedList, Refund, RefundInput, RefundStatus } from './types';

export interface RefundListQuery extends ListQuery {
  status?: RefundStatus;
  /** Only what the signed-in account asked for itself. */
  mine?: boolean;
}

export interface ReviewRefundInput {
  status: 'Approved' | 'Rejected' | 'Cancelled';
  /** Required when declining. */
  note?: string | null;
  /** The payroll month to pay it with. Omitted means the month it is approved in. */
  payrollYear?: number | null;
  payrollMonth?: number | null;
}

export const refundsApi = {
  list: (query: RefundListQuery) =>
    request<PagedList<Refund>>({
      method: 'GET',
      url: '/api/v1/refunds',
      params: listParams(query),
    }),

  create: (input: RefundInput) =>
    request<Refund>({ method: 'POST', url: '/api/v1/refunds', data: input }),

  review: (id: string, input: ReviewRefundInput) =>
    request<Refund>({ method: 'POST', url: `/api/v1/refunds/${id}/review`, data: input }),
};
