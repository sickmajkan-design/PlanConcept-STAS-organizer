import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { toolsApi, type ToolListQuery } from '../../api/tools';
import type { ToolInput } from '../../api/types';

export const toolKeys = {
  all: ['tools'] as const,
  list: (query: ToolListQuery) => [...toolKeys.all, 'list', query] as const,
  detail: (id: string) => [...toolKeys.all, 'detail', id] as const,
};

export function useToolsQuery(query: ToolListQuery) {
  return useQuery({
    queryKey: toolKeys.list(query),
    queryFn: () => toolsApi.list(query),
    placeholderData: keepPreviousData,
  });
}

export function useToolQuery(id: string | undefined) {
  return useQuery({
    queryKey: toolKeys.detail(id ?? ''),
    queryFn: () => toolsApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateTool() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ToolInput) => toolsApi.create(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}

export function useUpdateTool(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ToolInput) => toolsApi.update(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}

export function useDeleteTool() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => toolsApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}

export function useAssignToolEmployee(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (employeeId: string) => toolsApi.assignEmployee(id, employeeId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}

export function useUnassignToolEmployee(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => toolsApi.unassignEmployee(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}

export function useAssignToolProject(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) => toolsApi.assignProject(id, projectId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}

export function useUnassignToolProject(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => toolsApi.unassignProject(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: toolKeys.all });
    },
  });
}
