import { request } from './client';
import { idempotencyHeaders } from './idempotency';

// The controller answers on its literal name, matched case-insensitively (see vehicleTolls.ts).
const BASE_PATH = '/api/v1/vehiclefines';

export const vehicleFineStatuses = ['Open', 'Paid', 'Contested'] as const;
export type VehicleFineStatus = (typeof vehicleFineStatuses)[number];

export const vehicleFineBearers = ['Firm', 'Driver'] as const;
export type VehicleFineBearer = (typeof vehicleFineBearers)[number];

export interface VehicleFine {
  id: string;
  vehicleId: string;
  /** `YYYY-MM-DD`: the day of the offence. */
  occurredOn: string;
  description: string;
  amount: number;
  /** `YYYY-MM-DD`, or null when the notice gives none. */
  dueDate: string | null;
  driverEmployeeId: string | null;
  driverName: string | null;
  borneBy: VehicleFineBearer;
  status: VehicleFineStatus;
  paidOn: string | null;
  /** Open and past its due date. */
  overdue: boolean;
  note: string | null;
  createdAt: string;
}

export interface VehicleFineInput {
  occurredOn: string;
  description: string;
  amount: number;
  dueDate?: string | null;
  driverEmployeeId?: string | null;
  borneBy: VehicleFineBearer;
  status: VehicleFineStatus;
  paidOn?: string | null;
  note?: string | null;
}

export const vehicleFinesApi = {
  /** One vehicle's fines, open ones first. */
  list: (vehicleId: string) => request<VehicleFine[]>({ method: 'GET', url: BASE_PATH, params: { vehicleId } }),

  add: (vehicleId: string, input: VehicleFineInput, idempotencyKey?: string) =>
    request<VehicleFine>({
      method: 'POST',
      url: BASE_PATH,
      data: { ...input, vehicleId },
      headers: idempotencyHeaders(idempotencyKey),
    }),

  update: (id: string, input: VehicleFineInput, idempotencyKey?: string) =>
    request<VehicleFine>({
      method: 'PUT',
      url: `${BASE_PATH}/${id}`,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  remove: (id: string) => request<void>({ method: 'DELETE', url: `${BASE_PATH}/${id}` }),
};
