import { useQueries, useQuery } from '@tanstack/react-query';

import { costsApi } from '../../api/costs';
import { employeesApi } from '../../api/employees';
import { projectsApi } from '../../api/projects';
import { timeEntriesApi } from '../../api/timeEntries';
import type { PagedList } from '../../api/types';
import { config } from '../../config';
import { useBranchFilter, useUnitParams } from '../branches/BranchContext';
import { monthToDate, samePartOfPreviousMonth } from '../finance/periods';
import { companyNow } from '../../utils/companyTime';

/**
 * What the dashboard's figures are counted from.
 *
 * A figure on the board is a count the server made, never the length of a page of results: a list call returns at
 * most 100 rows, so counting what came back says "100" for a fleet of 140. Every count here is the `totalCount` of
 * a call that asks for one row, and what has to be read row by row (a headcount per site) reads every page.
 *
 * Every call is narrowed to the business unit chosen in the header, the same way the finance figures are: a board
 * that shows a unit's cost beside the whole company's head count is not a board anyone can read.
 */

/** The page size the list endpoints allow at most; a bigger request is refused. */
const PAGE_SIZE = 100;

/** Pages read before giving up, so a runaway list cannot turn one widget into a hundred requests. */
const MAX_PAGES = 20;

export { useUnitParams };

export interface UnitParams {
  branchId?: string;
  basis?: 'Employer';
}

/** Every row of a list, page after page. `truncated` says so when the cap was reached before the end. */
export async function fetchAllPages<T>(
  load: (pageNumber: number) => Promise<PagedList<T>>,
  maxPages = MAX_PAGES,
): Promise<{ items: T[]; totalCount: number; truncated: boolean }> {
  const first = await load(1);
  const items = [...first.items];
  const pages = Math.min(first.totalPages, maxPages);

  for (let page = 2; page <= pages; page++) {
    items.push(...(await load(page)).items);
  }

  return { items, totalCount: first.totalCount, truncated: first.totalPages > maxPages };
}

type CountLoader = (unit: UnitParams) => Promise<PagedList<unknown>>;

/**
 * Several counts at once, each the `totalCount` of a one-row call. The ids name the counts in the result.
 * `group` keeps the cache of one widget's counts apart from another's, and the unit is part of every key.
 */
export function useCounts<Id extends string>(group: string, loaders: Record<Id, CountLoader>) {
  const unit = useUnitParams();
  const ids = Object.keys(loaders) as Id[];

  const results = useQueries({
    queries: ids.map((id) => ({
      queryKey: ['dashboard', 'count', group, id, unit.branchId ?? null] as const,
      queryFn: async () => (await loaders[id](unit)).totalCount ?? 0,
      staleTime: 30_000,
    })),
  });

  const counts = {} as Record<Id, number | undefined>;
  ids.forEach((id, index) => {
    counts[id] = results[index]?.data;
  });

  return {
    counts,
    isLoading: results.some((r) => r.isLoading),
    error: results.find((r) => r.error)?.error,
  };
}

/** The people who work here: the denominator of "on site" and of the key figures. One shared request. */
export function useActiveEmployeesCount() {
  const unit = useUnitParams();

  return useQuery({
    queryKey: ['dashboard', 'count', 'employees', 'Active', unit.branchId ?? null] as const,
    queryFn: async () => (await employeesApi.list({ pageNumber: 1, pageSize: 1, status: 'Active', ...unit })).totalCount ?? 0,
    staleTime: 30_000,
  });
}

/**
 * Every shift that is running now, all pages: the head count, the hours so far and the split by site are all read
 * from it. Polled like the Work Time board itself, so it reads as live.
 */
export function useOpenTimeEntries() {
  const unit = useUnitParams(true);

  return useQuery({
    queryKey: ['dashboard', 'open-entries', unit.branchId ?? null, unit.basis ?? null] as const,
    queryFn: () => fetchAllPages((pageNumber) => timeEntriesApi.list({ pageNumber, pageSize: PAGE_SIZE, openOnly: true, ...unit })),
    refetchInterval: config.workTimeRefreshMs,
  });
}

/** Every running project, all pages: the widget sorts and flags them itself. */
export function useActiveProjects() {
  const unit = useUnitParams();

  return useQuery({
    queryKey: ['dashboard', 'active-projects', unit.branchId ?? null] as const,
    queryFn: () => fetchAllPages((pageNumber) => projectsApi.list({ pageNumber, pageSize: PAGE_SIZE, status: 'Active', ...unit })),
  });
}

/**
 * The company's cost this month so far and over the same days of last month, and how the first moved against the
 * second. Both are cut at the same day of the month, so the comparison is like for like.
 */
export function useMonthToDateCost(enabled: boolean) {
  const { branchId, basis } = useBranchFilter();
  const people = branchId && basis === 'Employer' ? 'Employer' : undefined;
  const now = companyNow();
  const current = monthToDate(now);
  const before = samePartOfPreviousMonth(now);

  const thisMonth = useQuery({
    queryKey: ['dashboard', 'kpi', 'company-cost-this-month', current.from, current.to, branchId ?? null, people ?? null] as const,
    // Everything the company spent, not only what is tied to a project.
    queryFn: () => costsApi.companyReport({ ...current, branchId, basis: people }),
    // The server refuses this to anyone without the finance right, which would put the whole widget into its
    // error state over one figure.
    enabled,
  });

  const lastMonth = useQuery({
    queryKey: ['dashboard', 'kpi', 'company-cost-previous-same-days', before.from, before.to, branchId ?? null, people ?? null] as const,
    queryFn: () => costsApi.companyReport({ ...before, branchId, basis: people }),
    enabled,
  });

  const now_ = thisMonth.data?.total;
  const then = lastMonth.data?.total;

  return {
    thisMonth,
    lastMonth,
    /** Percent, or null when last month's days cost nothing to compare with. */
    changePercent: now_ !== undefined && then !== undefined && then > 0 ? ((now_ - then) / then) * 100 : null,
  };
}
