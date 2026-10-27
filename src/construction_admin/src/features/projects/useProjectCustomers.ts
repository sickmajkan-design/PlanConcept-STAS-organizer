import { queryOptions, useQuery } from '@tanstack/react-query';

import { projectsApi } from '../../api/projects';
import { fetchAllPages } from '../dashboard/dashboardData';
import { projectKeys } from './useProjects';

export interface ProjectCustomer {
  id: string | null;
  name: string | null;
  /** The customer's country. */
  countryCode: string | null;
  /** The country of the site itself. */
  siteCountryCode: string | null;
}

/** Customer of every project, by project id and by name, so a screen that only has a project name can still show it. */
export const projectCustomersQuery = queryOptions({
  queryKey: [...projectKeys.all, 'customers'],
  queryFn: async () => {
    const { items } = await fetchAllPages((pageNumber) =>
      projectsApi.list({ pageNumber, pageSize: 500, sortBy: 'name' }),
    );
    const byId = new Map<string, ProjectCustomer>();
    const byName = new Map<string, ProjectCustomer>();
    const shared = new Set<string>();
    for (const p of items) {
      const customer = { id: p.customerId, name: p.customerName, countryCode: p.customerCountryCode, siteCountryCode: p.countryCode };
      byId.set(p.id, customer);
      // Two projects can share a name; a name lookup is only trusted when it is unambiguous.
      if (byName.has(p.name)) shared.add(p.name);
      byName.set(p.name, customer);
    }
    shared.forEach((name) => byName.delete(name));
    return { byId, byName };
  },
  staleTime: 5 * 60_000,
});

export function useProjectCustomers() {
  return useQuery(projectCustomersQuery);
}
