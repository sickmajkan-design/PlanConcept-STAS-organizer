import { request } from './client';
import { listParams } from './resource';
import type { AuditEntry, PagedList } from './types';

export interface AuditTrailQuery {
  entityName: string;
  entityId: string;
  pageNumber: number;
  pageSize: number;
}

export const auditApi = {
  list: (query: AuditTrailQuery) =>
    request<PagedList<AuditEntry>>({
      method: 'GET',
      url: '/api/v1/audit',
      params: listParams(query),
    }),
};
