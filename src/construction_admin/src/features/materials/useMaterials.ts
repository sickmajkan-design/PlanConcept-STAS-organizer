import { useQuery } from '@tanstack/react-query';

import {
  materialsApi,
  type AdjustMaterialInput,
  type MaterialListQuery,
} from '../../api/materials';
import type { MaterialInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const materialKeys = createResourceKeys<MaterialListQuery>('materials');

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: MaterialListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'name',
};

export function useMaterialsQuery(query: MaterialListQuery, enabled = true) {
  return useResourceList(materialKeys, materialsApi.list, query, { enabled });
}

/** A fresh read of whatever file is on screen; nothing to cache. */
export function usePreviewMaterialDeliveryImport() {
  return useResourceMutation((file: File) => materialsApi.deliveryImport.preview(file), []);
}

/** Importing deliveries changes stock, the movement history and the cost reports. */
export function useImportMaterialDeliveries() {
  return useResourceMutation(
    (file: File) => materialsApi.deliveryImport.commit(file),
    [materialKeys.all, ['materialMovements'], ['costReports']],
  );
}

/** Last and average purchase price. Only for those who may see spending. */
export function useMaterialPricingQuery(id: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ['materialMovements', 'pricing', id],
    queryFn: () => materialsApi.pricing(id!),
    enabled: !!id && enabled,
  });
}

export function useMaterialQuery(id: string | undefined) {
  return useResourceDetail(materialKeys, materialsApi.get, id);
}

/**
 * All materials for the movement picker. Cached for a minute rather than
 * paged, like the other pickers: it is opened repeatedly and its contents
 * rarely change mid-session.
 */
export function useAllMaterialsQuery() {
  return useQuery({
    queryKey: materialKeys.list(PICKER_QUERY),
    queryFn: () => materialsApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateMaterial() {
  return useResourceMutation(
    (input: MaterialInput) => materialsApi.create(input),
    // A starting delivery is also a movement, so the stock history follows.
    [materialKeys.all, ['materialMovements']],
  );
}

export function useUpdateMaterial(id: string) {
  return useResourceMutation(
    (input: MaterialInput) => materialsApi.update(id, input),
    [materialKeys.all],
  );
}

export function useAdjustMaterial(id: string) {
  return useResourceMutation(
    (input: AdjustMaterialInput, key: string) => materialsApi.adjust(id, input, key),
    [materialKeys.all],
  );
}

export function useDeleteMaterial() {
  return useResourceMutation((id: string) => materialsApi.remove(id), [
    materialKeys.all,
  ]);
}
