import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { vehiclesApi, type VehicleListQuery } from '../../api/vehicles';
import type { VehicleInput } from '../../api/types';

export const vehicleKeys = {
  all: ['vehicles'] as const,
  list: (query: VehicleListQuery) => [...vehicleKeys.all, 'list', query] as const,
  detail: (id: string) => [...vehicleKeys.all, 'detail', id] as const,
};

export function useVehiclesQuery(query: VehicleListQuery) {
  return useQuery({
    queryKey: vehicleKeys.list(query),
    queryFn: () => vehiclesApi.list(query),
    placeholderData: keepPreviousData,
  });
}

export function useVehicleQuery(id: string | undefined) {
  return useQuery({
    queryKey: vehicleKeys.detail(id ?? ''),
    queryFn: () => vehiclesApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: VehicleInput) => vehiclesApi.create(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}

export function useUpdateVehicle(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: VehicleInput) => vehiclesApi.update(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}

export function useDeleteVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vehiclesApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}

export function useAssignVehicle(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (employeeId: string) => vehiclesApi.assign(id, employeeId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}

export function useUnassignVehicle(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => vehiclesApi.unassign(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}
