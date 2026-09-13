import { useQuery } from '@tanstack/react-query';

import { customerPortalApi } from '../../api/customerPortal';

export function useMyCustomerProjectsQuery() {
  return useQuery({
    queryKey: ['customer-portal', 'projects'] as const,
    queryFn: () => customerPortalApi.myProjects(),
  });
}
