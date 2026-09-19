import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi } from './resource';
import type { ListQuery, Material, MaterialInput, MaterialPricing } from './types';

export interface MaterialListQuery extends ListQuery {
  projectId?: string;
  warehouse?: string;
  unassignedOnly?: boolean;
  /** `0` is a meaningful filter here — "out of stock" — and is sent as such. */
  maxQuantity?: number;
  incompleteOnly?: boolean;
  /** Only materials below the minimum set for them. */
  lowStockOnly?: boolean;
}

export interface AdjustMaterialInput {
  change: number;
  reason?: string | null;
}

export const materialsApi = {
  ...createCrudApi<Material, Material, MaterialInput, MaterialListQuery>(
    '/api/v1/materials',
  ),

  pricing: (id: string) =>
    request<MaterialPricing>({ method: 'GET', url: `/api/v1/materials/${id}/pricing` }),

  /**
   * A relative movement, and the reason the idempotency key exists at all: run
   * twice, it takes the stock down twice.
   */
  adjust: (id: string, input: AdjustMaterialInput, idempotencyKey?: string) =>
    request<Material>({
      method: 'POST',
      url: `/api/v1/materials/${id}/adjust`,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),
};
