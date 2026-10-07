import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';

import {
  planningApi,
  type AssignInput,
  type PlanningNeed,
  type SwapInput,
} from '../../api/planning';
import { useBranchFilter } from '../branches/BranchContext';
import { employeeKeys } from '../employees/useEmployees';
import { projectKeys } from '../projects/useProjects';
import { toolKeys } from '../tools/useTools';
import { vehicleKeys } from '../vehicles/useVehicles';
import { buildPlan, type Plan } from './planningLogic';

export const planningKeys = {
  all: ['planning'] as const,
};

/** The longest window the API serves in one call. */
export const MAX_WINDOW_DAYS = 400;

export function usePlanningQuery(from: string, to: string) {
  const { branchId } = useBranchFilter();

  const query = useQuery({
    queryKey: [...planningKeys.all, branchId ?? null, from, to],
    queryFn: () => planningApi.get({ from, to, branchId }),
    placeholderData: keepPreviousData,
  });

  const plan = useMemo<Plan | null>(() => (query.data ? buildPlan(query.data) : null), [query.data]);

  return { ...query, plan };
}

/**
 * The actions behind the screen. A posting also moves the person's tools and vehicles to their new
 * site, and shows on the employee and project pages, so those caches are refreshed along with the plan.
 */
export function usePlanningActions() {
  const queryClient = useQueryClient();

  const refresh = () => {
    for (const key of [planningKeys.all, employeeKeys.all, projectKeys.all, toolKeys.all, vehicleKeys.all]) {
      void queryClient.invalidateQueries({ queryKey: key });
    }
  };

  const assign = useMutation({ mutationFn: (input: AssignInput) => planningApi.assign(input), onSuccess: refresh });
  const swap = useMutation({ mutationFn: (input: SwapInput) => planningApi.swap(input), onSuccess: refresh });
  const setNeeds = useMutation({
    mutationFn: ({ projectId, needs, requiredCertificates }: { projectId: string; needs: PlanningNeed[]; requiredCertificates?: string[] }) =>
      planningApi.setNeeds(projectId, needs, requiredCertificates),
    onSuccess: refresh,
  });

  return { assign, swap, setNeeds };
}
