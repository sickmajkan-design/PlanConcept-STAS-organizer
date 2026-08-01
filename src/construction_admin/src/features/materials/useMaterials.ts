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

export function useMaterialsQuery(query: MaterialListQuery) {
  return useResourceList(materialKeys, materialsApi.list, query);
}

export function useMaterialQuery(id: string | undefined) {
  return useResourceDetail(materialKeys, materialsApi.get, id);
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
    (input: AdjustMaterialInput) => materialsApi.adjust(id, input),
    [materialKeys.all],
  );
}

export function useDeleteMaterial() {
  return useResourceMutation((id: string) => materialsApi.remove(id), [
    materialKeys.all,
  ]);
}
