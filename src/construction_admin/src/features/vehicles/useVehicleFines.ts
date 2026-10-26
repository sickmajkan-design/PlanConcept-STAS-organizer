import { useQuery } from '@tanstack/react-query';

import { vehicleFinesApi, type VehicleFineInput } from '../../api/vehicleFines';
import { useResourceMutation } from '../resourceQueries';
import { vehicleKeys } from './useVehicles';

const fineKey = (vehicleId: string) => ['vehicleFines', vehicleId] as const;

// A fine changes the vehicle's own row too (the count and the sum of the open ones) and the home board's list.
const caches = (vehicleId: string) => [fineKey(vehicleId), vehicleKeys.all, ['attention']];

export function useVehicleFinesQuery(vehicleId: string | undefined) {
  return useQuery({
    queryKey: fineKey(vehicleId ?? ''),
    queryFn: () => vehicleFinesApi.list(vehicleId!),
    enabled: !!vehicleId,
  });
}

export function useAddVehicleFine(vehicleId: string) {
  return useResourceMutation(
    (input: VehicleFineInput, key: string) => vehicleFinesApi.add(vehicleId, input, key),
    caches(vehicleId),
  );
}

export function useUpdateVehicleFine(vehicleId: string) {
  return useResourceMutation(
    (variables: { id: string; input: VehicleFineInput }, key: string) =>
      vehicleFinesApi.update(variables.id, variables.input, key),
    caches(vehicleId),
  );
}

export function useDeleteVehicleFine(vehicleId: string) {
  return useResourceMutation((id: string) => vehicleFinesApi.remove(id), caches(vehicleId));
}
