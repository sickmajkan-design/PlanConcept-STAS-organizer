import { useBranchFilter, useBranchScoped } from '../branches/BranchContext';
import { useQuery } from '@tanstack/react-query';

import { employeesApi, type EmployeeListQuery } from '../../api/employees';
import type { EmployeeInput, OrganizationRank } from '../../api/types';
import { projectKeys } from '../projects/useProjects';
import { toolKeys } from '../tools/useTools';
import { vehicleKeys } from '../vehicles/useVehicles';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const employeeKeys = createResourceKeys<EmployeeListQuery>('employees');

// Written as the literal `useAssignmentBoard.ts` uses for `assignmentBoardKeys.all`
// rather than importing it — that file already imports `employeeKeys` from here,
// and importing back would make the two modules circular.
const assignmentBoardKey = ['assignmentBoard'] as const;

// Assigning or removing an employee moves their held tools/vehicles onto the
// new (or no) project as part of the same backend operation — see
// `EmployeeEquipmentSync` — so Tools/Vehicles and the Assignment Board would
// otherwise show a stale project for that equipment until something else
// refreshed them.
const assignmentCaches = [toolKeys.all, vehicleKeys.all, assignmentBoardKey];

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: EmployeeListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'lastName',
};

export function useEmployeesQuery(query: EmployeeListQuery) {
  // Narrowed to the unit chosen in the header: the employees that unit employs now.
  const scoped = useBranchScoped(query);
  return useResourceList(employeeKeys, employeesApi.list, scoped);
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

const EVERY_EMPLOYEE_PAGE = 100;

/**
 * Every employee, for bulk assignment to a unit. The API serves at most 100 a page, so this reads
 * the pages one after another and hands back the people as one list.
 */
export function useEveryEmployeeQuery() {
  return useQuery({
    queryKey: [...employeeKeys.all, 'every'],
    queryFn: async () => {
      const first = await employeesApi.list({ pageNumber: 1, pageSize: EVERY_EMPLOYEE_PAGE, sortBy: 'lastName' });
      const pages = Math.ceil((first.totalCount ?? first.items.length) / EVERY_EMPLOYEE_PAGE);
      const rest = await Promise.all(
        Array.from({ length: Math.max(0, pages - 1) }, (_, i) =>
          employeesApi.list({ pageNumber: i + 2, pageSize: EVERY_EMPLOYEE_PAGE, sortBy: 'lastName' }),
        ),
      );

      return { ...first, items: [first, ...rest].flatMap((page) => page.items) };
    },
    staleTime: 60_000,
  });
}

export function useOrganizationHierarchyQuery() {
  const { branchId } = useBranchFilter();
  return useQuery({
    queryKey: [...employeeKeys.all, 'hierarchy', branchId ?? null],
    queryFn: () => employeesApi.hierarchy({ branchId }),
    staleTime: 60_000,
  });
}

export function useSetEmployeeRank() {
  return useResourceMutation(
    (variables: { id: string; rank: OrganizationRank | null }) =>
      employeesApi.setRank(variables.id, variables.rank),
    // The chart lives under the same key as the list, so one invalidation
    // refreshes the chart, the list and the detail page together.
    [employeeKeys.all],
  );
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
    (projectId: string, key: string) =>
      employeesApi.assignToProject(employeeId, projectId, key),
    [employeeKeys.detail(employeeId), projectKeys.all, ...assignmentCaches],
  );
}

export function useRemoveEmployeeFromProject(employeeId: string) {
  return useResourceMutation(
    (projectId: string) =>
      employeesApi.removeFromProject(employeeId, projectId),
    [employeeKeys.detail(employeeId), projectKeys.all, ...assignmentCaches],
  );
}

// The same relationship, initiated from the project's side instead of the
// employee's — same endpoint, so both caches are invalidated the same way.
// A plain employeeId string is still accepted (most callers name no company);
// an object form adds the company, B13's whole point in being on this side —
// the project's crew card is where its client's companies are already known.
export function useAssignProjectEmployee(projectId: string) {
  return useResourceMutation(
    (
      variables: string | { employeeId: string; customerCompanyId?: string | null },
      key: string,
    ) => {
      const { employeeId, customerCompanyId } =
        typeof variables === 'string' ? { employeeId: variables, customerCompanyId: undefined } : variables;

      return employeesApi.assignToProject(
        employeeId,
        projectId,
        key,
        customerCompanyId !== undefined ? { customerCompanyId } : undefined,
      );
    },
    [projectKeys.detail(projectId), employeeKeys.all, ...assignmentCaches],
  );
}

export function useRemoveProjectEmployee(projectId: string) {
  return useResourceMutation(
    (employeeId: string) => employeesApi.removeFromProject(employeeId, projectId),
    [projectKeys.detail(projectId), employeeKeys.all, ...assignmentCaches],
  );
}

/** Changes which of the client's companies an existing posting is worked for. Null clears it. */
export function useSetEmployeeProjectCompany(employeeId: string, projectId: string) {
  return useResourceMutation(
    (customerCompanyId: string | null) =>
      employeesApi.setProjectCompany(employeeId, projectId, customerCompanyId),
    [employeeKeys.detail(employeeId), projectKeys.detail(projectId)],
  );
}
