import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi } from './resource';
import type {
  FuelType,
  ListQuery,
  Vehicle,
  VehicleInput,
  VehicleOwnershipType,
  VehicleStatus,
} from './types';

export interface VehicleListQuery extends ListQuery {
  branchId?: string;
  status?: VehicleStatus | '';
  fuelType?: FuelType | '';
  ownershipType?: VehicleOwnershipType | '';
  assignedProjectId?: string;
  unassigned?: boolean;
  incompleteOnly?: boolean;
  /** Only vehicles with something that ran out or runs out within a month, an unpaid toll or an open fine. */
  needsAttention?: boolean;
}

export const vehiclesApi = {
  ...createCrudApi<Vehicle, Vehicle, VehicleInput, VehicleListQuery>(
    '/api/v1/vehicles',
  ),

  getByQrCode: (qrCode: string) =>
    request<Vehicle>({
      method: 'GET',
      url: `/api/v1/vehicles/by-qr/${encodeURIComponent(qrCode)}`,
    }),

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
