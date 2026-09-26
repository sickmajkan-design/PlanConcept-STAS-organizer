import { refundsApi, type RefundListQuery, type ReviewRefundInput } from '../../api/refunds';
import type { RefundInput } from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const refundKeys = createResourceKeys<RefundListQuery>('refunds');

export function useRefundsQuery(query: RefundListQuery, enabled = true) {
  return useResourceList(refundKeys, refundsApi.list, query, { enabled });
}

/** A decision changes what the payroll ledger adds up, so its lists are dropped as well. */
const caches = [refundKeys.all, ['ledgers']];

export function useCreateRefund() {
  return useResourceMutation((input: RefundInput) => refundsApi.create(input), caches);
}

export function useReviewRefund() {
  return useResourceMutation(
    ({ id, input }: { id: string; input: ReviewRefundInput }) => refundsApi.review(id, input),
    caches,
  );
}
