import { useQuery } from '@tanstack/react-query';

import {
  projectsApi,
  realizationApi,
  type ProjectListQuery,
  type ProjectRevenueListQuery,
} from '../../api/projects';
import type { ProjectInput, ProjectRevenueInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const projectKeys = createResourceKeys<ProjectListQuery>('projects');
export const projectRevenueKeys = createResourceKeys<ProjectRevenueListQuery>('projectRevenues');

export const realizationKeys = {
  all: ['realization'] as const,
  plan: (year: number) => ['realization', 'plan', year] as const,
};

/** The largest page the API will serve, used by the picker queries below. */
const PICKER_QUERY: ProjectListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'name',
};

export function useProjectsQuery(query: ProjectListQuery) {
  return useResourceList(projectKeys, projectsApi.list, query);
}

export function useProjectQuery(id: string | undefined) {
  return useResourceDetail(projectKeys, projectsApi.get, id);
}

/**
 * All projects for the assign-to-project pickers. Kept separate from
 * `useProjectsQuery` because it is cached for a minute rather than paged: a
 * picker is opened repeatedly and its contents rarely change mid-session.
 */
export function useAllProjectsQuery() {
  return useQuery({
    queryKey: projectKeys.list(PICKER_QUERY),
    queryFn: () => projectsApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateProject() {
  return useResourceMutation(
    (input: ProjectInput) => projectsApi.create(input),
    [projectKeys.all],
  );
}

export function useUpdateProject(id: string) {
  return useResourceMutation(
    (input: ProjectInput) => projectsApi.update(id, input),
    [projectKeys.all],
  );
}

export function useDeleteProject() {
  return useResourceMutation((id: string) => projectsApi.remove(id), [
    projectKeys.all,
  ]);
}

// ---- annual realization -----------------------------------------------

export function useAnnualRealizationPlanQuery(year: number) {
  return useQuery({
    queryKey: realizationKeys.plan(year),
    queryFn: () => realizationApi.plan(year),
  });
}

export function useProjectRevenuesQuery(query: ProjectRevenueListQuery) {
  return useResourceList(projectRevenueKeys, realizationApi.revenues.list, query);
}

const revenueCaches = [projectRevenueKeys.all, realizationKeys.all];

export function useRecordProjectRevenue() {
  return useResourceMutation(
    (input: ProjectRevenueInput, key: string) => realizationApi.revenues.record(input, key),
    revenueCaches,
  );
}

export function useDeleteProjectRevenue() {
  return useResourceMutation(
    (id: string) => realizationApi.revenues.remove(id),
    revenueCaches,
  );
}
