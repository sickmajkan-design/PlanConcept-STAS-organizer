import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

import { useAuth } from '../../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../../hooks/userScopedStorage';
import { useBranchesQuery } from './useBranches';

interface BranchFilterValue {
  /** The business unit every list is narrowed to, or undefined for "all". */
  branchId: string | undefined;
  setBranchId: (id: string | undefined) => void;
}

const STORAGE_KEY = 'branchFilter';

const BranchFilterContext = createContext<BranchFilterValue | null>(null);

/**
 * The one business unit (poslovna jedinica) the whole panel is narrowed to, remembered per
 * account. A UI filter only — it is not data isolation: everybody who may open a record can still
 * reach it by link.
 */
export function BranchFilterProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const scope = storageScope(user);
  const [saved, setSaved] = useState<string | undefined>(() =>
    readScoped<string | null>(scope, STORAGE_KEY, null) ?? undefined,
  );

  // A unit that was deleted since the choice was saved would filter everything out of sight.
  const { data: branches } = useBranchesQuery(!!user);
  const branchId = saved && branches && !branches.some((b) => b.id === saved) ? undefined : saved;

  const setBranchId = useCallback(
    (id: string | undefined) => {
      setSaved(id);
      writeScoped(scope, STORAGE_KEY, id ?? null);
    },
    [scope],
  );

  const value = useMemo(() => ({ branchId, setBranchId }), [branchId, setBranchId]);

  return <BranchFilterContext.Provider value={value}>{children}</BranchFilterContext.Provider>;
}

const NO_FILTER: BranchFilterValue = { branchId: undefined, setBranchId: () => undefined };

/** Without a provider (a screen under test, the pre-login pages) there is simply no filter. */
export function useBranchFilter(): BranchFilterValue {
  return useContext(BranchFilterContext) ?? NO_FILTER;
}

/**
 * A list query narrowed to the header's business unit, unless the caller already names one.
 * Without a unit selected the query is returned as it came.
 */
export function useBranchScoped<T extends { branchId?: string }>(query: T): T {
  const { branchId } = useBranchFilter();
  return branchId && !query.branchId ? { ...query, branchId } : query;
}
