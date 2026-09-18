import { useQuery } from '@tanstack/react-query';

import { auditApi, type AuditTrailQuery } from '../../api/audit';
import { createResourceKeys, useResourceList } from '../resourceQueries';

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

export const auditTrailKeys = createResourceKeys<AuditTrailQuery>('auditTrail');

/**
 * The whole trail, filtered however the audit page is asked to — a separate
 * cache from {@link useAuditTrailQuery} because that one is keyed for a single
 * record's history and this one pages through everything.
 */
export function useAuditListQuery(query: AuditTrailQuery) {
  return useResourceList(auditTrailKeys, auditApi.list, query);
}
