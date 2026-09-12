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
  ReturnRentalOutInput,
  ToolCostReport,
  ToolExpense,
  ToolExpenseInput,
  ToolExpenseKind,
  ToolExpenseSummary,
  ToolRentalOut,
  ToolRentalOutInput,
  ToolRentalOutSummary,
  ToolRentalRate,
  ToolRentalRateInput,
  ToolRentalRateSummary,
  UpdateToolRentalOutInput,
  UpdateVehicleRentalOutInput,
  VehicleCostReport,
  VehicleExpense,
  VehicleExpenseInput,
  VehicleExpenseKind,
  VehicleExpenseSummary,
  VehicleRentalOut,
  VehicleRentalOutInput,
  VehicleRentalOutSummary,
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

export interface ToolRentalRateListQuery extends ListQuery {
  toolId?: string;
  /** Only the rate in force today. */
  currentOnly?: boolean;
}

export interface VehicleRentalOutListQuery extends ListQuery {
  vehicleId?: string;
  /** Only loans still out. */
  openOnly?: boolean;
  /** `YYYY-MM-DD`. */
  from?: string;
  to?: string;
}

export interface ToolRentalOutListQuery extends ListQuery {
  toolId?: string;
  /** Only loans still out. */
  openOnly?: boolean;
  /** `YYYY-MM-DD`. */
  from?: string;
  to?: string;
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

  toolRentalRates: {
    list: (query: ToolRentalRateListQuery) =>
      request<PagedList<ToolRentalRate>>({
        method: 'GET',
        url: '/api/v1/tool-rental-rates',
        params: listParams(query),
      }),

    summary: (query: Omit<ToolRentalRateListQuery, keyof ListQuery>) =>
      request<ToolRentalRateSummary>({
        method: 'GET',
        url: '/api/v1/tool-rental-rates/summary',
        params: listParams(query),
      }),

    set: (input: ToolRentalRateInput, idempotencyKey?: string) =>
      request<ToolRentalRate>({
        method: 'POST',
        url: '/api/v1/tool-rental-rates',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: ToolRentalRateInput, idempotencyKey?: string) =>
      request<ToolRentalRate>({
        method: 'PUT',
        url: `/api/v1/tool-rental-rates/${id}`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/tool-rental-rates/${id}` }),
  },

  vehicleRentalsOut: {
    list: (query: VehicleRentalOutListQuery) =>
      request<PagedList<VehicleRentalOut>>({
        method: 'GET',
        url: '/api/v1/vehicle-rentals-out',
        params: listParams(query),
      }),

    summary: (query: Omit<VehicleRentalOutListQuery, keyof ListQuery>) =>
      request<VehicleRentalOutSummary>({
        method: 'GET',
        url: '/api/v1/vehicle-rentals-out/summary',
        params: listParams(query),
      }),

    record: (input: VehicleRentalOutInput, idempotencyKey?: string) =>
      request<VehicleRentalOut>({
        method: 'POST',
        url: '/api/v1/vehicle-rentals-out',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    return: (id: string, input: ReturnRentalOutInput) =>
      request<VehicleRentalOut>({
        method: 'PUT',
        url: `/api/v1/vehicle-rentals-out/${id}/return`,
        data: input,
      }),

    update: (id: string, input: UpdateVehicleRentalOutInput) =>
      request<VehicleRentalOut>({
        method: 'PUT',
        url: `/api/v1/vehicle-rentals-out/${id}`,
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/vehicle-rentals-out/${id}` }),
  },

  toolRentalsOut: {
    list: (query: ToolRentalOutListQuery) =>
      request<PagedList<ToolRentalOut>>({
        method: 'GET',
        url: '/api/v1/tool-rentals-out',
        params: listParams(query),
      }),

    summary: (query: Omit<ToolRentalOutListQuery, keyof ListQuery>) =>
      request<ToolRentalOutSummary>({
        method: 'GET',
        url: '/api/v1/tool-rentals-out/summary',
        params: listParams(query),
      }),

    record: (input: ToolRentalOutInput, idempotencyKey?: string) =>
      request<ToolRentalOut>({
        method: 'POST',
        url: '/api/v1/tool-rentals-out',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    return: (id: string, input: ReturnRentalOutInput) =>
      request<ToolRentalOut>({
        method: 'PUT',
        url: `/api/v1/tool-rentals-out/${id}/return`,
        data: input,
      }),

    update: (id: string, input: UpdateToolRentalOutInput) =>
      request<ToolRentalOut>({
        method: 'PUT',
        url: `/api/v1/tool-rentals-out/${id}`,
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/tool-rentals-out/${id}` }),
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
