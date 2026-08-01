import { request } from './client';
import { createCrudApi } from './resource';
import type { ListQuery, Material, MaterialInput } from './types';

export interface MaterialListQuery extends ListQuery {
  projectId?: string;
  warehouse?: string;
  unassignedOnly?: boolean;
  /** `0` is a meaningful filter here — "out of stock" — and is sent as such. */
  maxQuantity?: number;
}

export interface AdjustMaterialInput {
  change: number;
  reason?: string | null;
}

export const materialsApi = {
  ...createCrudApi<Material, Material, MaterialInput, MaterialListQuery>(
    '/api/materials',
  ),

  adjust: (id: string, input: AdjustMaterialInput) =>
    request<Material>({
      method: 'POST',
      url: `/api/materials/${id}/adjust`,
      data: input,
    }),
};
