import { useQuery } from '@tanstack/react-query';
import {
  costsApi,
  type CostReportQuery,
  type EmployeeRateListQuery,
  type FinanceEntryListQuery,
  type MaterialMovementListQuery,
  type ToolExpenseListQuery,
  type VehicleExpenseListQuery,
  type VehicleRentalRateListQuery,
} from '../../api/costs';
import type {
  EmployeeRateInput,
  FinanceEntryInput,
  ListQuery,
  MaterialMovementInput,
  ToolExpenseInput,
  VehicleExpenseInput,
  VehicleRentalRateInput,
} from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';
import { materialKeys } from '../materials/useMaterials';

export const rateKeys = createResourceKeys<EmployeeRateListQuery>('employeeRates');
export const movementKeys = createResourceKeys<MaterialMovementListQuery>('materialMovements');
export const vehicleExpenseKeys = createResourceKeys<VehicleExpenseListQuery>('vehicleExpenses');
export const vehicleRentalRateKeys =
  createResourceKeys<VehicleRentalRateListQuery>('vehicleRentalRates');
export const toolExpenseKeys = createResourceKeys<ToolExpenseListQuery>('toolExpenses');
export const financeEntryKeys = createResourceKeys<FinanceEntryListQuery>('financeEntries');

export const costReportKeys = {
  all: ['costReports'] as const,
  projects: (query: object) => ['costReports', 'projects', query] as const,
  vehicles: (query: object) => ['costReports', 'vehicles', query] as const,
  tools: (query: object) => ['costReports', 'tools', query] as const,
};

/** The filter half of a list query, with paging/sorting stripped for a summary call. */
function summaryParams<TQuery extends ListQuery>(
  query: TQuery,
): Omit<TQuery, keyof ListQuery> {
  const { pageNumber: _pageNumber, pageSize: _pageSize, search: _search,
    sortBy: _sortBy, sortDescending: _sortDescending, ...rest } = query;
  return rest;
}

// ---- pay rates -------------------------------------------------------------

export function useEmployeeRatesQuery(query: EmployeeRateListQuery) {
  return useResourceList(rateKeys, costsApi.rates.list, query);
}

export function useEmployeeRatesSummaryQuery(query: EmployeeRateListQuery) {
  const params = summaryParams(query);
  return useQuery({
    queryKey: [...rateKeys.all, 'summary', params],
    queryFn: () => costsApi.rates.summary(params),
  });
}

export function useSetEmployeeRate() {
  return useResourceMutation(
    (input: EmployeeRateInput, key: string) => costsApi.rates.set(input, key),
    // A new rate changes both the rate list and every report that prices
    // hours with it.
    [rateKeys.all, costReportKeys.all],
  );
}

export function useUpdateEmployeeRate() {
  return useResourceMutation(
    (variables: { id: string; input: EmployeeRateInput }, key: string) =>
      costsApi.rates.update(variables.id, variables.input, key),
    [rateKeys.all, costReportKeys.all],
  );
}

export function useDeleteEmployeeRate() {
  return useResourceMutation((id: string) => costsApi.rates.remove(id), [
    rateKeys.all,
    costReportKeys.all,
  ]);
}

// ---- stock movements -------------------------------------------------------

export function useMaterialMovementsQuery(query: MaterialMovementListQuery) {
  return useResourceList(movementKeys, costsApi.movements.list, query);
}

export function useMaterialMovementsSummaryQuery(query: MaterialMovementListQuery) {
  const params = summaryParams(query);
  return useQuery({
    queryKey: [...movementKeys.all, 'summary', params],
    queryFn: () => costsApi.movements.summary(params),
  });
}

/**
 * A movement also moves the stock, so the materials list is refreshed
 * alongside it — otherwise the quantity on the stock screen stays at whatever
 * it was before the delivery was recorded.
 */
const movementCaches = [movementKeys.all, materialKeys.all, costReportKeys.all];

export function useRecordMaterialMovement() {
  return useResourceMutation(
    (input: MaterialMovementInput, key: string) =>
      costsApi.movements.record(input, key),
    movementCaches,
  );
}

export function useUpdateMaterialMovement() {
  return useResourceMutation(
    (variables: { id: string; input: MaterialMovementInput }, key: string) =>
      costsApi.movements.update(variables.id, variables.input, key),
    movementCaches,
  );
}

export function useDeleteMaterialMovement() {
  return useResourceMutation(
    (id: string) => costsApi.movements.remove(id),
    movementCaches,
  );
}

// ---- vehicle expenses ------------------------------------------------------

export function useVehicleExpensesQuery(query: VehicleExpenseListQuery) {
  return useResourceList(vehicleExpenseKeys, costsApi.vehicleExpenses.list, query);
}

export function useVehicleExpensesSummaryQuery(query: VehicleExpenseListQuery) {
  const params = summaryParams(query);
  return useQuery({
    queryKey: [...vehicleExpenseKeys.all, 'summary', params],
    queryFn: () => costsApi.vehicleExpenses.summary(params),
  });
}

export function useRecordVehicleExpense() {
  return useResourceMutation(
    (input: VehicleExpenseInput, key: string) =>
      costsApi.vehicleExpenses.record(input, key),
    [vehicleExpenseKeys.all, costReportKeys.all],
  );
}

export function useUpdateVehicleExpense() {
  return useResourceMutation(
    (variables: { id: string; input: VehicleExpenseInput }, key: string) =>
      costsApi.vehicleExpenses.update(variables.id, variables.input, key),
    [vehicleExpenseKeys.all, costReportKeys.all],
  );
}

export function useDeleteVehicleExpense() {
  return useResourceMutation((id: string) => costsApi.vehicleExpenses.remove(id), [
    vehicleExpenseKeys.all,
    costReportKeys.all,
  ]);
}

// ---- vehicle rental/lease rates ---------------------------------------------

export function useVehicleRentalRatesQuery(query: VehicleRentalRateListQuery) {
  return useResourceList(vehicleRentalRateKeys, costsApi.vehicleRentalRates.list, query);
}

export function useVehicleRentalRatesSummaryQuery(query: VehicleRentalRateListQuery) {
  const params = summaryParams(query);
  return useQuery({
    queryKey: [...vehicleRentalRateKeys.all, 'summary', params],
    queryFn: () => costsApi.vehicleRentalRates.summary(params),
  });
}

export function useSetVehicleRentalRate() {
  return useResourceMutation(
    (input: VehicleRentalRateInput, key: string) =>
      costsApi.vehicleRentalRates.set(input, key),
    [vehicleRentalRateKeys.all, costReportKeys.all],
  );
}

export function useUpdateVehicleRentalRate() {
  return useResourceMutation(
    (variables: { id: string; input: VehicleRentalRateInput }, key: string) =>
      costsApi.vehicleRentalRates.update(variables.id, variables.input, key),
    [vehicleRentalRateKeys.all, costReportKeys.all],
  );
}

export function useDeleteVehicleRentalRate() {
  return useResourceMutation((id: string) => costsApi.vehicleRentalRates.remove(id), [
    vehicleRentalRateKeys.all,
    costReportKeys.all,
  ]);
}

// ---- tool expenses ----------------------------------------------------------

export function useToolExpensesQuery(query: ToolExpenseListQuery) {
  return useResourceList(toolExpenseKeys, costsApi.toolExpenses.list, query);
}

export function useToolExpensesSummaryQuery(query: ToolExpenseListQuery) {
  const params = summaryParams(query);
  return useQuery({
    queryKey: [...toolExpenseKeys.all, 'summary', params],
    queryFn: () => costsApi.toolExpenses.summary(params),
  });
}

export function useRecordToolExpense() {
  return useResourceMutation(
    (input: ToolExpenseInput, key: string) => costsApi.toolExpenses.record(input, key),
    [toolExpenseKeys.all, costReportKeys.all],
  );
}

export function useUpdateToolExpense() {
  return useResourceMutation(
    (variables: { id: string; input: ToolExpenseInput }, key: string) =>
      costsApi.toolExpenses.update(variables.id, variables.input, key),
    [toolExpenseKeys.all, costReportKeys.all],
  );
}

export function useDeleteToolExpense() {
  return useResourceMutation((id: string) => costsApi.toolExpenses.remove(id), [
    toolExpenseKeys.all,
    costReportKeys.all,
  ]);
}

// ---- finance entries --------------------------------------------------------

export function useFinanceEntriesQuery(query: FinanceEntryListQuery) {
  return useResourceList(financeEntryKeys, costsApi.financeEntries.list, query);
}

export function useFinanceEntriesSummaryQuery(query: FinanceEntryListQuery) {
  const params = summaryParams(query);
  return useQuery({
    queryKey: [...financeEntryKeys.all, 'summary', params],
    queryFn: () => costsApi.financeEntries.summary(params),
  });
}

export function useRecordFinanceEntry() {
  return useResourceMutation(
    (input: FinanceEntryInput, key: string) => costsApi.financeEntries.record(input, key),
    [financeEntryKeys.all, costReportKeys.all],
  );
}

export function useUpdateFinanceEntry() {
  return useResourceMutation(
    (variables: { id: string; input: FinanceEntryInput }, key: string) =>
      costsApi.financeEntries.update(variables.id, variables.input, key),
    [financeEntryKeys.all, costReportKeys.all],
  );
}

export function useDeleteFinanceEntry() {
  return useResourceMutation((id: string) => costsApi.financeEntries.remove(id), [
    financeEntryKeys.all,
    costReportKeys.all,
  ]);
}

// ---- the reports -----------------------------------------------------------

export function useProjectCostReport(query: CostReportQuery & { projectId?: string }) {
  return useQuery({
    queryKey: costReportKeys.projects(query),
    queryFn: () => costsApi.projectReport(query),
  });
}

export function useVehicleCostReport(query: CostReportQuery & { vehicleId?: string }) {
  return useQuery({
    queryKey: costReportKeys.vehicles(query),
    queryFn: () => costsApi.vehicleReport(query),
  });
}

export function useToolCostReport(query: CostReportQuery & { toolId?: string }) {
  return useQuery({
    queryKey: costReportKeys.tools(query),
    queryFn: () => costsApi.toolReport(query),
  });
}
