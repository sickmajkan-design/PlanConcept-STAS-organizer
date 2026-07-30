import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { projectsApi, type ProjectListQuery } from '../../api/projects';
import type { ProjectInput } from '../../api/types';

export const projectKeys = {
  all: ['projects'] as const,
  list: (query: ProjectListQuery) => [...projectKeys.all, 'list', query] as const,
  detail: (id: string) => [...projectKeys.all, 'detail', id] as const,
};

export function useProjectsQuery(query: ProjectListQuery) {
  return useQuery({
    queryKey: projectKeys.list(query),
    queryFn: () => projectsApi.list(query),
    placeholderData: keepPreviousData,
  });
}

export function useProjectQuery(id: string | undefined) {
  return useQuery({
    queryKey: projectKeys.detail(id ?? ''),
    queryFn: () => projectsApi.get(id!),
    enabled: !!id,
  });
}

/** All projects for pickers (assign-to-project). Large enough for typical fleets. */
export function useAllProjectsQuery() {
  return useQuery({
    queryKey: projectKeys.list({ pageNumber: 1, pageSize: 100, sortBy: 'name' }),
    queryFn: () => projectsApi.list({ pageNumber: 1, pageSize: 100, sortBy: 'name' }),
    staleTime: 60_000,
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ProjectInput) => projectsApi.create(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: projectKeys.all });
    },
  });
}

export function useUpdateProject(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ProjectInput) => projectsApi.update(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: projectKeys.all });
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => projectsApi.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: projectKeys.all });
    },
  });
}
