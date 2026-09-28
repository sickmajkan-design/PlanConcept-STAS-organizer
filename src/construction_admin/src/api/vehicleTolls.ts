import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import type {
  AddVehicleTollInput,
  MarkVehicleTollPaidInput,
  VehicleToll,
  VehicleTollInput,
} from './types';

// The controller sets no explicit route, so it answers on the literal
// controller name (`VehicleTolls`), which the API matches case-insensitively —
// there is no slugifying route convention, so no hyphen.
const BASE_PATH = '/api/v1/vehicletolls';

export const vehicleTollsApi = {
  /** One vehicle's tolls, not paged. The API already sorts expired and expiring-soon first. */
  list: (vehicleId: string) =>
    request<VehicleToll[]>({
      method: 'GET',
      url: BASE_PATH,
      params: { vehicleId },
    }),

  add: (input: AddVehicleTollInput, idempotencyKey?: string) =>
    request<VehicleToll>({
      method: 'POST',
      url: BASE_PATH,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  /** Marks paid or renews; always appends to the payment history. */
  markPaid: (id: string, input: MarkVehicleTollPaidInput, idempotencyKey?: string) =>
    request<VehicleToll>({
      method: 'PUT',
      url: `${BASE_PATH}/${id}/pay`,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  /** Type, country and route segment only; payment state is never touched. */
  update: (id: string, input: VehicleTollInput, idempotencyKey?: string) =>
    request<VehicleToll>({
      method: 'PUT',
      url: `${BASE_PATH}/${id}`,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  remove: (id: string) => request<void>({ method: 'DELETE', url: `${BASE_PATH}/${id}` }),
};
