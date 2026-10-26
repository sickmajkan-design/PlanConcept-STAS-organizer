import { useQuery } from '@tanstack/react-query';

import { projectsApi } from '../../api/projects';
import { fetchAllPages } from '../dashboard/dashboardData';
import { projectKeys } from './useProjects';

/** Customer of every project, by project id and by name, so a screen that only has a project name can still show it. */
export function useProjectCustomers() {
  return useQuery({
    queryKey: [...projectKeys.all, 'customers'],
    queryFn: async () => {
      const { items } = await fetchAllPages((pageNumber) =>
        projectsApi.list({ pageNumber, pageSize: 500, sortBy: 'name' }),
      );
      const byId = new Map<string, string | null>();
      const byName = new Map<string, string | null>();
      const shared = new Set<string>();
      for (const p of items) {
        byId.set(p.id, p.customerName);
        // Two projects can share a name; a name lookup is only trusted when it is unambiguous.
        if (byName.has(p.name)) shared.add(p.name);
        byName.set(p.name, p.customerName);
      }
      shared.forEach((name) => byName.delete(name));
      return { byId, byName };
    },
    staleTime: 5 * 60_000,
  });
}
