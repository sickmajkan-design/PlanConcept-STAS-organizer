import { employeesApi } from '../../api/employees';
import { useQuery } from '@tanstack/react-query';

import { branchesApi } from '../../api/branches';
import type { BranchInput } from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

export const branchKeys = createResourceKeys<never>('branches');

/** All business units. Cached for a minute: they change rarely and every list screen's filter reads them. */
export function useBranchesQuery(enabled = true) {
  return useQuery({
    queryKey: branchKeys.all,
    // Always a list: a screen under test, or a proxy answering with something else, must not take a form down.
    queryFn: async () => {
      const branches = await branchesApi.list();
      return Array.isArray(branches) ? branches : [];
    },
    staleTime: 60_000,
    enabled,
  });
}

// A branch's name and colour ride along on every project row, so a write refreshes projects too.
const affected = [branchKeys.all, ['projects']] as const;

export function useCreateBranch() {
  return useResourceMutation((input: BranchInput) => branchesApi.create(input), affected);
}

export function useUpdateBranch() {
  return useResourceMutation(
    ({ id, input }: { id: string; input: BranchInput }) => branchesApi.update(id, input),
    affected,
  );
}

export function useDeleteBranch() {
  return useResourceMutation((id: string) => branchesApi.remove(id), affected);
}

export function useSetBranchProjects() {
  return useResourceMutation(
    ({ id, projectIds }: { id: string; projectIds: string[] }) => branchesApi.setProjects(id, projectIds),
    [branchKeys.all, ['projects'], ['vehicles'], ['tools']],
  );
}

// A move changes who an employee's hours and pay belong to, so everything that groups by unit is dropped.
const employeeCaches = [branchKeys.all, ['employees'], ['timeEntries'], ['finance'], ['costs']] as const;

export function useSetEmployeeBranch(employeeId: string) {
  return useResourceMutation(
    (input: { branchId: string | null; from?: string }) => employeesApi.setBranch(employeeId, input),
    employeeCaches,
  );
}

export function useRemoveEmployeeBranchPeriod(employeeId: string) {
  return useResourceMutation(
    (periodId: string) => employeesApi.removeBranchPeriod(employeeId, periodId),
    employeeCaches,
  );
}

export function useAssignEmployeesToBranch() {
  return useResourceMutation(
    ({ id, employeeIds, from, backdateNewcomers }: { id: string; employeeIds: string[]; from?: string; backdateNewcomers?: boolean }) =>
      branchesApi.assignEmployees(id, { employeeIds, from, backdateNewcomers }),
    employeeCaches,
  );
}
