import { keepPreviousData, useQuery } from '@tanstack/react-query';

import {
  fuelTransactionsApi,
  type FuelTransactionListQuery,
  type ResolveFuelTransactionInput,
} from '../../api/fuelTransactions';
import { useResourceMutation } from '../resourceQueries';
import { vehicleExpenseKeys } from '../costs/useCosts';

const root = ['fuelTransactions'] as const;

export const fuelTransactionKeys = {
  all: root,
  list: (query: FuelTransactionListQuery) => [...root, 'list', query] as const,
  counts: [...root, 'counts'] as const,
  batches: [...root, 'batches'] as const,
  candidates: (id: string) => [...root, 'candidates', id] as const,
};

/** Everything that can change when a row is settled or a statement lands. */
const affected = [fuelTransactionKeys.all, vehicleExpenseKeys.all] as const;

export function useFuelTransactionsQuery(query: FuelTransactionListQuery) {
  return useQuery({
    queryKey: fuelTransactionKeys.list(query),
    queryFn: () => fuelTransactionsApi.list(query),
    placeholderData: keepPreviousData,
  });
}

export function useFuelTransactionCountsQuery() {
  return useQuery({
    queryKey: fuelTransactionKeys.counts,
    queryFn: fuelTransactionsApi.counts,
  });
}

export function useFuelImportBatchesQuery() {
  return useQuery({
    queryKey: fuelTransactionKeys.batches,
    queryFn: () => fuelTransactionsApi.batches(),
  });
}

export function useFuelExpenseCandidatesQuery(id: string | undefined) {
  return useQuery({
    queryKey: fuelTransactionKeys.candidates(id ?? ''),
    queryFn: () => fuelTransactionsApi.candidates(id!),
    enabled: !!id,
  });
}

/** The preview reads a file the user has on screen right now; nothing to cache. */
export function usePreviewDkvImport() {
  return useResourceMutation((file: File) => fuelTransactionsApi.preview(file), []);
}

export function useImportDkvStatement() {
  return useResourceMutation((file: File) => fuelTransactionsApi.commit(file), [...affected]);
}

export function useResolveFuelTransaction() {
  return useResourceMutation(
    (variables: { id: string; input: ResolveFuelTransactionInput }) =>
      fuelTransactionsApi.resolve(variables.id, variables.input),
    [...affected],
  );
}

export function useAssignDkvCard() {
  return useResourceMutation(
    (variables: { cardNumber: string; vehicleId: string }) =>
      fuelTransactionsApi.assignCard(variables.cardNumber, variables.vehicleId),
    [...affected],
  );
}

export function useRecheckDkvTransactions() {
  return useResourceMutation(() => fuelTransactionsApi.recheck(), [...affected]);
}
