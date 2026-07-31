import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { materialsApi, type AdjustMaterialInput, type MaterialListQuery } from '../../api/materials';
import type { MaterialInput } from '../../api/types';

export const materialKeys = {
  all: ['materials'] as const,
  list: (query: MaterialListQuery) => [...materialKeys.all, 'list', query] as const,
  detail: (id: string) => [...materialKeys.all, 'detail', id] as const,
};

export function useMaterialsQuery(query: MaterialListQuery) {
  return useQuery({
    queryKey: materialKeys.list(query),
    queryFn: () => materialsApi.list(query),
    placeholderData: keepPreviousData,
  });
}

export function useMaterialQuery(id: string | undefined) {
  return useQuery({
    queryKey: materialKeys.detail(id ?? ''),
    queryFn: () => materialsApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateMaterial() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: MaterialInput) => materialsApi.create(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: materialKeys.all });
    },
  });
}

export function useUpdateMaterial(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: MaterialInput) => materialsApi.update(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: materialKeys.all });
    },
  });
}

export function useAdjustMaterial(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AdjustMaterialInput) => materialsApi.adjust(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: materialKeys.all });
    },
  });
}

export function useDeleteMaterial() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => materialsApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: materialKeys.all });
    },
  });
}
