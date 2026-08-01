import { vehiclesApi, type VehicleListQuery } from '../../api/vehicles';
import type { Vehicle, VehicleInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const vehicleKeys = createResourceKeys<VehicleListQuery>('vehicles');

export function useVehiclesQuery(query: VehicleListQuery) {
  return useResourceList(vehicleKeys, vehiclesApi.list, query);
}

export function useVehicleQuery(id: string | undefined) {
  return useResourceDetail(vehicleKeys, vehiclesApi.get, id);
}

export function useCreateVehicle() {
  return useResourceMutation(
    (input: VehicleInput) => vehiclesApi.create(input),
    [vehicleKeys.all],
  );
}

export function useUpdateVehicle(id: string) {
  return useResourceMutation(
    (input: VehicleInput) => vehiclesApi.update(id, input),
    [vehicleKeys.all],
  );
}

export function useDeleteVehicle() {
  return useResourceMutation(
    (id: string) => vehiclesApi.remove(id),
    [vehicleKeys.all],
  );
}

export function useAssignVehicle(id: string) {
  return useResourceMutation(
    (employeeId: string) => vehiclesApi.assign(id, employeeId),
    [vehicleKeys.all],
  );
}

// `void` is explicit: the callback takes no argument, so there is nothing for
// the variables type to be inferred from, and the call site invokes `mutate()`.
export function useUnassignVehicle(id: string) {
  return useResourceMutation<void, Vehicle>(() => vehiclesApi.unassign(id), [
    vehicleKeys.all,
  ]);
}
