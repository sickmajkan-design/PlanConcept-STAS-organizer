import { useQuery } from '@tanstack/react-query';

import { attentionApi } from '../../api/attention';
import { useUnitParams } from '../branches/BranchContext';

/**
 * What is waiting for this person, narrowed to the business unit chosen in the header. One request, one key, for
 * the "Needs attention" widget, the phone's "Today" card and the menu's badges: the three always say the same.
 * The key starts with `attention` so anything that invalidates that prefix reaches all of them.
 */
export function useAttentionQuery(enabled = true) {
  const unit = useUnitParams(true);

  return useQuery({
    queryKey: ['attention', unit.branchId ?? null, unit.basis ?? null] as const,
    queryFn: () => attentionApi.get(unit),
    refetchInterval: 60_000,
    enabled,
  });
}
