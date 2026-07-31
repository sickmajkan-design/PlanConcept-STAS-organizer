import { request } from './client';
import type { ListQuery, Material, MaterialInput, PagedList } from './types';

export interface MaterialListQuery extends ListQuery {
  projectId?: string;
  warehouse?: string;
  unassignedOnly?: boolean;
  maxQuantity?: number;
}

export interface AdjustMaterialInput {
  change: number;
  reason?: string | null;
}

export const materialsApi = {
  list: (query: MaterialListQuery) =>
    request<PagedList<Material>>({
      method: 'GET',
      url: '/api/materials',
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
        projectId: query.projectId || undefined,
        warehouse: query.warehouse || undefined,
        unassignedOnly: query.unassignedOnly || undefined,
        maxQuantity: query.maxQuantity ?? undefined,
        sortBy: query.sortBy || undefined,
        sortDescending: query.sortDescending || undefined,
      },
    }),

  get: (id: string) =>
    request<Material>({ method: 'GET', url: `/api/materials/${id}` }),

  create: (input: MaterialInput) =>
    request<Material>({ method: 'POST', url: '/api/materials', data: input }),

  update: (id: string, input: MaterialInput) =>
    request<Material>({
      method: 'PUT',
      url: `/api/materials/${id}`,
      data: input,
    }),

  adjust: (id: string, input: AdjustMaterialInput) =>
    request<Material>({
      method: 'POST',
      url: `/api/materials/${id}/adjust`,
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/materials/${id}` }),
};
