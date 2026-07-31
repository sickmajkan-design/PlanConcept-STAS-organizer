import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { employeesApi, type EmployeeListQuery } from '../../api/employees';
import type { EmployeeInput } from '../../api/types';

export const employeeKeys = {
  all: ['employees'] as const,
  list: (query: EmployeeListQuery) => [...employeeKeys.all, 'list', query] as const,
  detail: (id: string) => [...employeeKeys.all, 'detail', id] as const,
};

export function useEmployeesQuery(query: EmployeeListQuery) {
  return useQuery({
    queryKey: employeeKeys.list(query),
    queryFn: () => employeesApi.list(query),
    placeholderData: keepPreviousData,
  });
}

export function useEmployeeQuery(id: string | undefined) {
  return useQuery({
    queryKey: employeeKeys.detail(id ?? ''),
    queryFn: () => employeesApi.get(id!),
    enabled: !!id,
  });
}

/** All employees for pickers (assign vehicle/tool to an employee). */
export function useAllEmployeesQuery() {
  return useQuery({
    queryKey: employeeKeys.list({ pageNumber: 1, pageSize: 100, sortBy: 'lastName' }),
    queryFn: () => employeesApi.list({ pageNumber: 1, pageSize: 100, sortBy: 'lastName' }),
    staleTime: 60_000,
  });
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: EmployeeInput) => employeesApi.create(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.all });
    },
  });
}

export function useUpdateEmployee(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: EmployeeInput) => employeesApi.update(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.all });
    },
  });
}

export function useDeleteEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => employeesApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.all });
    },
  });
}

export function useAssignEmployeeToProject(employeeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) =>
      employeesApi.assignToProject(employeeId, projectId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.detail(employeeId) });
      void queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
}

export function useRemoveEmployeeFromProject(employeeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) =>
      employeesApi.removeFromProject(employeeId, projectId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.detail(employeeId) });
      void queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
}
