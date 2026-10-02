import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

import { branchScope } from '../../api/branchScope';
import { useAuth } from '../../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../../hooks/userScopedStorage';
import { useBranchesQuery } from './useBranches';

export type BranchBasis = 'Site' | 'Employer';

interface BranchFilterValue {
  /** The business unit every list is narrowed to, or undefined for "all". */
  branchId: string | undefined;
  setBranchId: (id: string | undefined) => void;
  /**
   * With a unit chosen, whose hours and pay count for it: those worked on its sites (the default) or
   * those of the people it employs. Everything that is not about people is the unit's own either way.
   */
  basis: BranchBasis;
  setBasis: (basis: BranchBasis) => void;
}

const STORAGE_KEY = 'branchFilter';
const BASIS_KEY = 'branchBasis';

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

  const [basis, setBasisState] = useState<BranchBasis>(() =>
    readScoped<string | null>(scope, BASIS_KEY, null) === 'Employer' ? 'Employer' : 'Site',
  );

  const setBasis = useCallback(
    (next: BranchBasis) => {
      setBasisState(next);
      writeScoped(scope, BASIS_KEY, next);
    },
    [scope],
  );

  const setBranchId = useCallback(
    (id: string | undefined) => {
      setSaved(id);
      writeScoped(scope, STORAGE_KEY, id ?? null);
    },
    [scope],
  );

  // Downloads are started from clicks, outside React, and read the choice from here.
  useEffect(() => {
    branchScope.set(branchId, basis);
    return () => branchScope.set(undefined);
  }, [branchId, basis]);

  const value = useMemo(() => ({ branchId, setBranchId, basis, setBasis }), [branchId, setBranchId, basis, setBasis]);

  return <BranchFilterContext.Provider value={value}>{children}</BranchFilterContext.Provider>;
}

const NO_FILTER: BranchFilterValue = {
  branchId: undefined,
  setBranchId: () => undefined,
  basis: 'Site',
  setBasis: () => undefined,
};

/** Without a provider (a screen under test, the pre-login pages) there is simply no filter. */
export function useBranchFilter(): BranchFilterValue {
  return useContext(BranchFilterContext) ?? NO_FILTER;
}

/**
 * A list query narrowed to the header's business unit, unless the caller already names one.
 * Without a unit selected the query is returned as it came.
 */
export function useBranchScoped<T extends { branchId?: string; basis?: 'Employer' }>(
  query: T,
  options: { people?: boolean } = {},
): T {
  const { branchId, basis } = useBranchFilter();
  if (!branchId || query.branchId) return query;
  // Only what is about people's work (hours, pay) has a choice of basis; the default is not sent.
  return options.people && basis === 'Employer' ? { ...query, branchId, basis } : { ...query, branchId };
}
