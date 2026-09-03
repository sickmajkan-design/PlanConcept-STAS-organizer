import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { listParams } from './resource';
import type {
  AccommodationRate,
  AccommodationRateInput,
  AccommodationRateSummary,
  EmployeeRate,
  EmployeeRateInput,
  EmployeeRateSummary,
  FinanceEntry,
  FinanceEntryInput,
  FinanceEntryKind,
  FinanceEntrySummary,
  GeneralExpense,
  GeneralExpenseCategory,
  GeneralExpenseInput,
  GeneralExpenseSummary,
  ListQuery,
  MaterialMovement,
  MaterialMovementInput,
  MaterialMovementKind,
  MaterialMovementSummary,
  PagedList,
  ProjectCostReport,
  ToolCostReport,
  ToolExpense,
  ToolExpenseInput,
  ToolExpenseKind,
  ToolExpenseSummary,
  VehicleCostReport,
  VehicleExpense,
  VehicleExpenseInput,
  VehicleExpenseKind,
  VehicleExpenseSummary,
  VehicleRentalRate,
  VehicleRentalRateInput,
  VehicleRentalRateSummary,
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

export interface ToolExpenseListQuery extends ListQuery {
  toolId?: string;
  kind?: ToolExpenseKind;
  from?: string;
  to?: string;
}

export interface VehicleRentalRateListQuery extends ListQuery {
  vehicleId?: string;
  /** Only the rate in force today. */
  currentOnly?: boolean;
}

export interface CostReportQuery {
  from: string;
  to: string;
}

export interface FinanceEntryListQuery extends ListQuery {
  employeeId?: string;
  projectId?: string;
  kind?: FinanceEntryKind;
  /** `YYYY-MM-DD`. */
  from?: string;
  to?: string;
}

export interface GeneralExpenseListQuery extends ListQuery {
  category?: GeneralExpenseCategory;
  projectId?: string;
  employeeId?: string;
  /** `YYYY-MM-DD`. */
  from?: string;
  to?: string;
}

export interface AccommodationRateListQuery extends ListQuery {
  accommodationId?: string;
  /** Only the rate in force today. */
  currentOnly?: boolean;
}

/** The five ledgers and the three reports. */
export const costsApi = {
  rates: {
    list: (query: EmployeeRateListQuery) =>
      request<PagedList<EmployeeRate>>({
        method: 'GET',
        url: '/api/v1/employee-rates',
        params: listParams(query),
      }),

    summary: (query: Omit<EmployeeRateListQuery, keyof ListQuery>) =>
      request<EmployeeRateSummary>({
        method: 'GET',
        url: '/api/v1/employee-rates/summary',
        params: listParams(query),
      }),

    set: (input: EmployeeRateInput, idempotencyKey?: string) =>
      request<EmployeeRate>({
        method: 'POST',
        url: '/api/v1/employee-rates',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: EmployeeRateInput, idempotencyKey?: string) =>
      request<EmployeeRate>({
        method: 'PUT',
        url: `/api/v1/employee-rates/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
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

    summary: (query: Omit<MaterialMovementListQuery, keyof ListQuery>) =>
      request<MaterialMovementSummary>({
        method: 'GET',
        url: '/api/v1/material-movements/summary',
        params: listParams(query),
      }),

    record: (input: MaterialMovementInput, idempotencyKey?: string) =>
      request<MaterialMovement>({
        method: 'POST',
        url: '/api/v1/material-movements',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: MaterialMovementInput, idempotencyKey?: string) =>
      request<MaterialMovement>({
        method: 'PUT',
        url: `/api/v1/material-movements/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
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

    summary: (query: Omit<VehicleExpenseListQuery, keyof ListQuery>) =>
      request<VehicleExpenseSummary>({
        method: 'GET',
        url: '/api/v1/vehicle-expenses/summary',
        params: listParams(query),
      }),

    record: (input: VehicleExpenseInput, idempotencyKey?: string) =>
      request<VehicleExpense>({
        method: 'POST',
        url: '/api/v1/vehicle-expenses',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: VehicleExpenseInput, idempotencyKey?: string) =>
      request<VehicleExpense>({
        method: 'PUT',
        url: `/api/v1/vehicle-expenses/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/vehicle-expenses/${id}` }),
  },

  vehicleRentalRates: {
    list: (query: VehicleRentalRateListQuery) =>
      request<PagedList<VehicleRentalRate>>({
        method: 'GET',
        url: '/api/v1/vehicle-rental-rates',
        params: listParams(query),
      }),

    summary: (query: Omit<VehicleRentalRateListQuery, keyof ListQuery>) =>
      request<VehicleRentalRateSummary>({
        method: 'GET',
        url: '/api/v1/vehicle-rental-rates/summary',
        params: listParams(query),
      }),

    set: (input: VehicleRentalRateInput, idempotencyKey?: string) =>
      request<VehicleRentalRate>({
        method: 'POST',
        url: '/api/v1/vehicle-rental-rates',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: VehicleRentalRateInput, idempotencyKey?: string) =>
      request<VehicleRentalRate>({
        method: 'PUT',
        url: `/api/v1/vehicle-rental-rates/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/vehicle-rental-rates/${id}` }),
  },

  toolExpenses: {
    list: (query: ToolExpenseListQuery) =>
      request<PagedList<ToolExpense>>({
        method: 'GET',
        url: '/api/v1/tool-expenses',
        params: listParams(query),
      }),

    summary: (query: Omit<ToolExpenseListQuery, keyof ListQuery>) =>
      request<ToolExpenseSummary>({
        method: 'GET',
        url: '/api/v1/tool-expenses/summary',
        params: listParams(query),
      }),

    record: (input: ToolExpenseInput, idempotencyKey?: string) =>
      request<ToolExpense>({
        method: 'POST',
        url: '/api/v1/tool-expenses',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: ToolExpenseInput, idempotencyKey?: string) =>
      request<ToolExpense>({
        method: 'PUT',
        url: `/api/v1/tool-expenses/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/tool-expenses/${id}` }),
  },

  financeEntries: {
    list: (query: FinanceEntryListQuery) =>
      request<PagedList<FinanceEntry>>({
        method: 'GET',
        url: '/api/v1/finance-entries',
        params: listParams(query),
      }),

    summary: (query: Omit<FinanceEntryListQuery, keyof ListQuery>) =>
      request<FinanceEntrySummary>({
        method: 'GET',
        url: '/api/v1/finance-entries/summary',
        params: listParams(query),
      }),

    record: (input: FinanceEntryInput, idempotencyKey?: string) =>
      request<FinanceEntry>({
        method: 'POST',
        url: '/api/v1/finance-entries',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: FinanceEntryInput, idempotencyKey?: string) =>
      request<FinanceEntry>({
        method: 'PUT',
        url: `/api/v1/finance-entries/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/finance-entries/${id}` }),
  },

  generalExpenses: {
    list: (query: GeneralExpenseListQuery) =>
      request<PagedList<GeneralExpense>>({
        method: 'GET',
        url: '/api/v1/general-expenses',
        params: listParams(query),
      }),

    summary: (query: Omit<GeneralExpenseListQuery, keyof ListQuery>) =>
      request<GeneralExpenseSummary>({
        method: 'GET',
        url: '/api/v1/general-expenses/summary',
        params: listParams(query),
      }),

    record: (input: GeneralExpenseInput, idempotencyKey?: string) =>
      request<GeneralExpense>({
        method: 'POST',
        url: '/api/v1/general-expenses',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: GeneralExpenseInput, idempotencyKey?: string) =>
      request<GeneralExpense>({
        method: 'PUT',
        url: `/api/v1/general-expenses/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/general-expenses/${id}` }),
  },

  accommodationRates: {
    list: (query: AccommodationRateListQuery) =>
      request<PagedList<AccommodationRate>>({
        method: 'GET',
        url: '/api/v1/accommodation-rates',
        params: listParams(query),
      }),

    summary: (query: Omit<AccommodationRateListQuery, keyof ListQuery>) =>
      request<AccommodationRateSummary>({
        method: 'GET',
        url: '/api/v1/accommodation-rates/summary',
        params: listParams(query),
      }),

    set: (input: AccommodationRateInput, idempotencyKey?: string) =>
      request<AccommodationRate>({
        method: 'POST',
        url: '/api/v1/accommodation-rates',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: AccommodationRateInput, idempotencyKey?: string) =>
      request<AccommodationRate>({
        method: 'PUT',
        url: `/api/v1/accommodation-rates/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/accommodation-rates/${id}` }),
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

  toolReport: (query: CostReportQuery & { toolId?: string }) =>
    request<ToolCostReport>({
      method: 'GET',
      url: '/api/v1/costs/tools',
      params: listParams(query),
    }),
};
