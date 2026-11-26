import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';

import {
  planningApi,
  type AssignInput,
  type EquipmentChoiceInput,
  type PlanningNeed,
  type SwapInput,
} from '../../api/planning';
import { useBranchFilter } from '../branches/BranchContext';
import { employeeKeys } from '../employees/useEmployees';
import { projectKeys } from '../projects/useProjects';
import { toolKeys } from '../tools/useTools';
import { vehicleKeys } from '../vehicles/useVehicles';
import { buildPlan, type NameOrder, type Plan } from './planningLogic';

export const planningKeys = {
  all: ['planning'] as const,
};

/** The longest window the API serves in one call. */
export const MAX_WINDOW_DAYS = 400;

export function usePlanningQuery(from: string, to: string, nameOrder: NameOrder = 'first') {
  const { branchId } = useBranchFilter();

  const query = useQuery({
    queryKey: [...planningKeys.all, branchId ?? null, from, to],
    queryFn: () => planningApi.get({ from, to, branchId }),
    placeholderData: keepPreviousData,
  });

  const plan = useMemo<Plan | null>(() => (query.data ? buildPlan(query.data, nameOrder) : null), [query.data, nameOrder]);

  return { ...query, plan };
}

/**
 * Who has clocked in today and where they are. It changes by the minute while the plan does not, so it is a request of its
 * own, repeated every minute while the board is on today and the tab is in view.
 */
export function usePlanningEquipmentOn(date: string | null) {
  const { branchId } = useBranchFilter();

  return useQuery({
    queryKey: [...planningKeys.all, 'equipment', branchId ?? null, date],
    queryFn: () => planningApi.equipmentOn(date!, { branchId }),
    enabled: !!date,
    staleTime: 30_000,
    placeholderData: keepPreviousData,
  });
}

export function usePlanningAttendance(enabled: boolean) {
  const { branchId } = useBranchFilter();

  return useQuery({
    queryKey: [...planningKeys.all, 'attendance', branchId ?? null],
    queryFn: () => planningApi.attendance({ branchId }),
    enabled,
    staleTime: 30_000,
    refetchInterval: enabled ? 60_000 : false,
    refetchIntervalInBackground: false,
  });
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
  const decideEquipment = useMutation({ mutationFn: ({ items, effectiveFrom }: { items: EquipmentChoiceInput[]; effectiveFrom?: string }) => planningApi.decideEquipment(items, effectiveFrom), onSuccess: refresh });
  const cancelPlanned = useMutation({ mutationFn: (id: string) => planningApi.cancelPlannedEquipment(id), onSuccess: refresh });
  const swap = useMutation({ mutationFn: (input: SwapInput) => planningApi.swap(input), onSuccess: refresh });
  const setNeeds = useMutation({
    mutationFn: ({ projectId, needs, requiredCertificates }: { projectId: string; needs: PlanningNeed[]; requiredCertificates?: string[] }) =>
      planningApi.setNeeds(projectId, needs, requiredCertificates),
    onSuccess: refresh,
  });

  return { assign, swap, setNeeds, decideEquipment, cancelPlanned };
}
