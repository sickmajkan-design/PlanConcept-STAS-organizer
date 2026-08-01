import { useQuery } from '@tanstack/react-query';

import { employeesApi, type EmployeeListQuery } from '../../api/employees';
import type { EmployeeInput } from '../../api/types';
import { projectKeys } from '../projects/useProjects';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const employeeKeys = createResourceKeys<EmployeeListQuery>('employees');

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: EmployeeListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'lastName',
};

export function useEmployeesQuery(query: EmployeeListQuery) {
  return useResourceList(employeeKeys, employeesApi.list, query);
}

export function useEmployeeQuery(id: string | undefined) {
  return useResourceDetail(employeeKeys, employeesApi.get, id);
}

/**
 * All employees for the pickers that assign a vehicle or tool to someone.
 * Kept separate from `useEmployeesQuery` because it is cached for a minute
 * rather than paged: a picker is opened repeatedly and its contents rarely
 * change mid-session.
 */
export function useAllEmployeesQuery() {
  return useQuery({
    queryKey: employeeKeys.list(PICKER_QUERY),
    queryFn: () => employeesApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateEmployee() {
  return useResourceMutation(
    (input: EmployeeInput) => employeesApi.create(input),
    [employeeKeys.all],
  );
}

export function useUpdateEmployee(id: string) {
  return useResourceMutation(
    (input: EmployeeInput) => employeesApi.update(id, input),
    [employeeKeys.all],
  );
}

export function useDeleteEmployee() {
  return useResourceMutation((id: string) => employeesApi.remove(id), [
    employeeKeys.all,
  ]);
}

// Assignment changes the crew shown on the project side too, so both caches
// are refreshed. Only this employee's detail is invalidated, not the whole
// employee collection — the list columns do not show project membership.
export function useAssignEmployeeToProject(employeeId: string) {
  return useResourceMutation(
    (projectId: string) => employeesApi.assignToProject(employeeId, projectId),
    [employeeKeys.detail(employeeId), projectKeys.all],
  );
}

export function useRemoveEmployeeFromProject(employeeId: string) {
  return useResourceMutation(
    (projectId: string) =>
      employeesApi.removeFromProject(employeeId, projectId),
    [employeeKeys.detail(employeeId), projectKeys.all],
  );
}
