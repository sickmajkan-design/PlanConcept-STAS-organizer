import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';

import { employeesApi } from '../../api/employees';
import { canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { employeeKeys } from './useEmployees';

const PAGE = 100;
/** shortcut: 1000 contractors at most are marked; a company with more would need the flag on each row's DTO. */
const MAX_PAGES = 10;

/**
 * The ids of the people who are subcontractors, so any screen that shows a name next to an employee id can mark it
 * purple without every DTO carrying the flag. One cheap request for the whole panel, cached for five minutes; an
 * account that may not read the directory gets an empty set (and its screens do not mark anyone).
 */
export function useContractorIds(): ReadonlySet<string> {
  const { user } = useAuth();

  const { data } = useQuery({
    queryKey: [...employeeKeys.all, 'contractor-ids'],
    enabled: canViewDirectory(user),
    staleTime: 5 * 60_000,
    queryFn: async () => {
      const ids: string[] = [];

      for (let pageNumber = 1; pageNumber <= MAX_PAGES; pageNumber += 1) {
        const page = await employeesApi.list({ type: 'Subcontractor', pageNumber, pageSize: PAGE });
        ids.push(...page.items.map((e) => e.id));

        if (page.items.length < PAGE) break;
      }

      return ids;
    },
  });

  return useMemo(() => new Set(data ?? []), [data]);
}
