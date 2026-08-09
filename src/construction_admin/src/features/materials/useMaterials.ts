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

export function useMaterialsQuery(query: MaterialListQuery) {
  return useResourceList(materialKeys, materialsApi.list, query);
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
    [materialKeys.all],
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
