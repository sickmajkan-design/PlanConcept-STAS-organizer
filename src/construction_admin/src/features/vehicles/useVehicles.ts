import { useQuery } from '@tanstack/react-query';

import { vehiclesApi, type VehicleListQuery } from '../../api/vehicles';
import type { Vehicle, VehicleInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const vehicleKeys = createResourceKeys<VehicleListQuery>('vehicles');

// Written as literals rather than importing `employeeKeys`/`assignmentBoardKeys`
// from their own feature modules — those modules import `vehicleKeys` from
// here, and importing back would make them circular.
const employeeKey = ['employees'] as const;
const assignmentBoardKey = ['assignmentBoard'] as const;

// Assigning or unassigning a vehicle's employee changes what that employee's
// card shows on the Assignment Board (and, once a project owns the vehicle
// via EmployeeEquipmentSync, what their project crew list shows too).
const employeeAssignmentCaches = [employeeKey, assignmentBoardKey];

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: VehicleListQuery = {
  pageNumber: 1,
  pageSize: 100,
};

export function useVehiclesQuery(query: VehicleListQuery) {
  return useResourceList(vehicleKeys, vehiclesApi.list, query);
}

export function useVehicleQuery(id: string | undefined) {
  return useResourceDetail(vehicleKeys, vehiclesApi.get, id);
}

/** All vehicles for the expense picker. Cached like the other pickers. */
export function useAllVehiclesQuery() {
  return useQuery({
    queryKey: vehicleKeys.list(PICKER_QUERY),
    queryFn: () => vehiclesApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
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
    (employeeId: string, key: string) => vehiclesApi.assign(id, employeeId, key),
    [vehicleKeys.all, ...employeeAssignmentCaches],
  );
}

// `void` is explicit: the callback takes no argument, so there is nothing for
// the variables type to be inferred from, and the call site invokes `mutate()`.
export function useUnassignVehicle(id: string) {
  return useResourceMutation<void, Vehicle>((_, key) => vehiclesApi.unassign(id, key), [
    vehicleKeys.all,
    ...employeeAssignmentCaches,
  ]);
}

export function useAssignVehicleProject(id: string) {
  return useResourceMutation(
    (projectId: string, key: string) => vehiclesApi.assignProject(id, projectId, key),
    [vehicleKeys.all, ...employeeAssignmentCaches],
  );
}

export function useUnassignVehicleProject(id: string) {
  return useResourceMutation<void, Vehicle>((_, key) => vehiclesApi.unassignProject(id, key), [
    vehicleKeys.all,
    ...employeeAssignmentCaches,
  ]);
}

// Same relationship, initiated from the project's side: the project is fixed,
// the vehicle is chosen — the reverse of the two hooks above.
export function useAssignProjectVehicle(projectId: string) {
  return useResourceMutation(
    (vehicleId: string, key: string) => vehiclesApi.assignProject(vehicleId, projectId, key),
    [vehicleKeys.all, ...employeeAssignmentCaches],
  );
}

export function useUnassignProjectVehicle() {
  return useResourceMutation(
    (vehicleId: string, key: string) => vehiclesApi.unassignProject(vehicleId, key),
    [vehicleKeys.all, ...employeeAssignmentCaches],
  );
}
