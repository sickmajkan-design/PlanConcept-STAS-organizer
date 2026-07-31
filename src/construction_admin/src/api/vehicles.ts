import { request } from './client';
import type {
  FuelType,
  ListQuery,
  PagedList,
  Vehicle,
  VehicleInput,
  VehicleStatus,
} from './types';

export interface VehicleListQuery extends ListQuery {
  status?: VehicleStatus | '';
  fuelType?: FuelType | '';
  unassigned?: boolean;
}

export const vehiclesApi = {
  list: (query: VehicleListQuery) =>
    request<PagedList<Vehicle>>({
      method: 'GET',
      url: '/api/vehicles',
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
        status: query.status || undefined,
        fuelType: query.fuelType || undefined,
        unassigned: query.unassigned || undefined,
        sortBy: query.sortBy || undefined,
        sortDescending: query.sortDescending || undefined,
      },
    }),

  get: (id: string) =>
    request<Vehicle>({ method: 'GET', url: `/api/vehicles/${id}` }),

  create: (input: VehicleInput) =>
    request<Vehicle>({ method: 'POST', url: '/api/vehicles', data: input }),

  update: (id: string, input: VehicleInput) =>
    request<Vehicle>({
      method: 'PUT',
      url: `/api/vehicles/${id}`,
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/vehicles/${id}` }),

  assign: (id: string, employeeId: string) =>
    request<Vehicle>({
      method: 'POST',
      url: `/api/vehicles/${id}/assign/${employeeId}`,
    }),

  unassign: (id: string) =>
    request<Vehicle>({ method: 'POST', url: `/api/vehicles/${id}/unassign` }),
};
