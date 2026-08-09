import { useQuery } from '@tanstack/react-query';
import {
  costsApi,
  type CostReportQuery,
  type EmployeeRateListQuery,
  type MaterialMovementListQuery,
  type VehicleExpenseListQuery,
} from '../../api/costs';
import type {
  EmployeeRateInput,
  MaterialMovementInput,
  VehicleExpenseInput,
} from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';
import { materialKeys } from '../materials/useMaterials';

export const rateKeys = createResourceKeys<EmployeeRateListQuery>('employeeRates');
export const movementKeys = createResourceKeys<MaterialMovementListQuery>('materialMovements');
export const vehicleExpenseKeys = createResourceKeys<VehicleExpenseListQuery>('vehicleExpenses');

export const costReportKeys = {
  all: ['costReports'] as const,
  projects: (query: object) => ['costReports', 'projects', query] as const,
  vehicles: (query: object) => ['costReports', 'vehicles', query] as const,
};

// ---- pay rates -------------------------------------------------------------

export function useEmployeeRatesQuery(query: EmployeeRateListQuery) {
  return useResourceList(rateKeys, costsApi.rates.list, query);
}

export function useSetEmployeeRate() {
  return useResourceMutation(
    (input: EmployeeRateInput, key: string) => costsApi.rates.set(input, key),
    // A new rate changes both the rate list and every report that prices
    // hours with it.
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

export function useRecordVehicleExpense() {
  return useResourceMutation(
    (input: VehicleExpenseInput, key: string) =>
      costsApi.vehicleExpenses.record(input, key),
    [vehicleExpenseKeys.all, costReportKeys.all],
  );
}

export function useDeleteVehicleExpense() {
  return useResourceMutation((id: string) => costsApi.vehicleExpenses.remove(id), [
    vehicleExpenseKeys.all,
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
