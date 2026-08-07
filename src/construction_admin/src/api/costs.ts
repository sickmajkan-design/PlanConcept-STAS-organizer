import { request } from './client';
import { listParams } from './resource';
import type {
  EmployeeRate,
  EmployeeRateInput,
  ListQuery,
  MaterialMovement,
  MaterialMovementInput,
  MaterialMovementKind,
  PagedList,
  ProjectCostReport,
  VehicleCostReport,
  VehicleExpense,
  VehicleExpenseInput,
  VehicleExpenseKind,
} from './types';

export interface EmployeeRateListQuery extends ListQuery {
  employeeId?: string;
  /** Only the rate in force today. */
  currentOnly?: boolean;
}

export interface MaterialMovementListQuery extends ListQuery {
  materialId?: string;
  projectId?: string;
  kind?: MaterialMovementKind;
  /** `YYYY-MM-DD`. */
  from?: string;
  to?: string;
}

export interface VehicleExpenseListQuery extends ListQuery {
  vehicleId?: string;
  kind?: VehicleExpenseKind;
  from?: string;
  to?: string;
}

export interface CostReportQuery {
  from: string;
  to: string;
}

/**
 * The three ledgers and the two reports.
 *
 * Written out rather than built from `createCrudApi`: nothing here is updated
 * in place. A rate is superseded by a new one, and a movement is reversed
 * rather than edited, so there is no `PUT` to wrap.
 */
export const costsApi = {
  rates: {
    list: (query: EmployeeRateListQuery) =>
      request<PagedList<EmployeeRate>>({
        method: 'GET',
        url: '/api/v1/employee-rates',
        params: listParams(query),
      }),

    set: (input: EmployeeRateInput) =>
      request<EmployeeRate>({
        method: 'POST',
        url: '/api/v1/employee-rates',
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/employee-rates/${id}` }),
  },

  movements: {
    list: (query: MaterialMovementListQuery) =>
      request<PagedList<MaterialMovement>>({
        method: 'GET',
        url: '/api/v1/material-movements',
        params: listParams(query),
      }),

    record: (input: MaterialMovementInput) =>
      request<MaterialMovement>({
        method: 'POST',
        url: '/api/v1/material-movements',
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/material-movements/${id}` }),
  },

  vehicleExpenses: {
    list: (query: VehicleExpenseListQuery) =>
      request<PagedList<VehicleExpense>>({
        method: 'GET',
        url: '/api/v1/vehicle-expenses',
        params: listParams(query),
      }),

    record: (input: VehicleExpenseInput) =>
      request<VehicleExpense>({
        method: 'POST',
        url: '/api/v1/vehicle-expenses',
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/vehicle-expenses/${id}` }),
  },

  projectReport: (query: CostReportQuery & { projectId?: string }) =>
    request<ProjectCostReport>({
      method: 'GET',
      url: '/api/v1/costs/projects',
      params: listParams(query),
    }),

  vehicleReport: (query: CostReportQuery & { vehicleId?: string }) =>
    request<VehicleCostReport>({
      method: 'GET',
      url: '/api/v1/costs/vehicles',
      params: listParams(query),
    }),
};
