import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi } from './resource';
import type {
  FuelType,
  ListQuery,
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
  ...createCrudApi<Vehicle, Vehicle, VehicleInput, VehicleListQuery>(
    '/api/v1/vehicles',
  ),

  assign: (id: string, employeeId: string, idempotencyKey?: string) =>
    request<Vehicle>({
      method: 'POST',
      url: `/api/v1/vehicles/${id}/assign/${employeeId}`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  unassign: (id: string, idempotencyKey?: string) =>
    request<Vehicle>({
      method: 'POST',
      url: `/api/v1/vehicles/${id}/unassign`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  assignProject: (id: string, projectId: string, idempotencyKey?: string) =>
    request<Vehicle>({
      method: 'POST',
      url: `/api/v1/vehicles/${id}/assign-project/${projectId}`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  unassignProject: (id: string, idempotencyKey?: string) =>
    request<Vehicle>({
      method: 'POST',
      url: `/api/v1/vehicles/${id}/unassign-project`,
      headers: idempotencyHeaders(idempotencyKey),
    }),
};
