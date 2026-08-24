import { useQuery } from '@tanstack/react-query';

import { auditApi } from '../../api/audit';

/**
 * One record's history. Admin and above only — mirrors the API's own policy,
 * so a Foreman never sees a 403 from a "History" card they should not have
 * been shown in the first place.
 */
export function useAuditTrailQuery(entityName: string, entityId: string | undefined) {
  return useQuery({
    queryKey: ['audit', entityName, entityId],
    queryFn: () =>
      auditApi.list({ entityName, entityId: entityId!, pageNumber: 1, pageSize: 50 }),
    enabled: !!entityId,
  });
}
