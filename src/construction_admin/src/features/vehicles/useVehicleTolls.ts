import { useQuery } from '@tanstack/react-query';

import { vehicleTollsApi } from '../../api/vehicleTolls';
import type {
  AddVehicleTollInput,
  MarkVehicleTollPaidInput,
  VehicleTollInput,
} from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';
import { vehicleKeys } from './useVehicles';

/** The list is keyed by vehicle id: it is not paged, so there is no query object. */
export const vehicleTollKeys = createResourceKeys<string>('vehicleTolls');

// A toll's payment state feeds `hasExpiredOrExpiringTolls` on the vehicle
// itself, which the vehicles list and detail read, so every write refreshes
// both collections.
const affectedCaches = [vehicleTollKeys.all, vehicleKeys.all];

export function useVehicleTollsQuery(vehicleId: string | undefined) {
  return useQuery({
    queryKey: vehicleTollKeys.list(vehicleId ?? ''),
    queryFn: () => vehicleTollsApi.list(vehicleId!),
    enabled: !!vehicleId,
  });
}

export function useAddVehicleToll() {
  return useResourceMutation(
    (input: AddVehicleTollInput, key: string) => vehicleTollsApi.add(input, key),
    affectedCaches,
  );
}

export function useMarkVehicleTollPaid() {
  return useResourceMutation(
    (variables: { id: string; input: MarkVehicleTollPaidInput }, key: string) =>
      vehicleTollsApi.markPaid(variables.id, variables.input, key),
    affectedCaches,
  );
}

export function useUpdateVehicleToll() {
  return useResourceMutation(
    (variables: { id: string; input: VehicleTollInput }, key: string) =>
      vehicleTollsApi.update(variables.id, variables.input, key),
    affectedCaches,
  );
}

export function useDeleteVehicleToll() {
  return useResourceMutation((id: string) => vehicleTollsApi.remove(id), affectedCaches);
}
