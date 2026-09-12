import { fuelCardsApi, type FuelCardListQuery } from '../../api/fuelCards';
import type { FuelCardInput } from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';
import { costReportKeys, vehicleExpenseKeys } from '../costs/useCosts';

export const fuelCardKeys = createResourceKeys<FuelCardListQuery>('fuelCards');

export function useFuelCardsQuery(query: FuelCardListQuery) {
  return useResourceList(fuelCardKeys, fuelCardsApi.list, query);
}

export function useAddFuelCard() {
  return useResourceMutation(
    (input: FuelCardInput, key: string) => fuelCardsApi.add(input, key),
    [fuelCardKeys.all],
  );
}

export function useDeleteFuelCard() {
  return useResourceMutation((id: string) => fuelCardsApi.remove(id), [fuelCardKeys.all]);
}

/**
 * The import wizard's preview step reads nothing that needs caching — every
 * call is a fresh parse of whatever file and mapping the user has on screen
 * right now — so this is a plain mutation, not a query.
 */
export function usePreviewFuelImport() {
  return useResourceMutation(
    (variables: { file: File; mapping: Parameters<typeof fuelCardsApi.import.preview>[1]; hasHeaderRow: boolean }) =>
      fuelCardsApi.import.preview(variables.file, variables.mapping, variables.hasHeaderRow),
    [],
  );
}

/** Commit changes vehicle costs, so both the fuel-card list and the vehicle cost report need refreshing. */
export function useImportFuelTransactions() {
  return useResourceMutation(
    (variables: { file: File; mapping: Parameters<typeof fuelCardsApi.import.commit>[1]; hasHeaderRow: boolean }) =>
      fuelCardsApi.import.commit(variables.file, variables.mapping, variables.hasHeaderRow),
    [vehicleExpenseKeys.all, costReportKeys.all],
  );
}
