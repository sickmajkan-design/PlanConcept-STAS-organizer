import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi, listParams } from './resource';
import type {
  AnnualRealizationPlan,
  ListQuery,
  PagedList,
  Project,
  ProjectDetail,
  ProjectInput,
  ProjectRevenue,
  ProjectRevenueInput,
  ProjectStatus,
} from './types';

export interface ProjectListQuery extends ListQuery {
  status?: ProjectStatus | '';
  employeeId?: string;
}

export const projectsApi = createCrudApi<
  Project,
  ProjectDetail,
  ProjectInput,
  ProjectListQuery
>('/api/v1/projects');

export interface ProjectRevenueListQuery extends ListQuery {
  projectId?: string;
  from?: string;
  to?: string;
}

/**
 * The annual realization plan and the payments it is built from. Not part of
 * `projectsApi`'s CRUD shape: the plan is a report, not a resource, and a
 * revenue is booked and withdrawn rather than edited in place.
 */
export const realizationApi = {
  plan: (year: number) =>
    request<AnnualRealizationPlan>({
      method: 'GET',
      url: '/api/v1/projects/annual-realization',
      params: { year },
    }),

  revenues: {
    list: (query: ProjectRevenueListQuery) =>
      request<PagedList<ProjectRevenue>>({
        method: 'GET',
        url: '/api/v1/project-revenues',
        params: listParams(query),
      }),

    record: (input: ProjectRevenueInput, idempotencyKey?: string) =>
      request<ProjectRevenue>({
        method: 'POST',
        url: '/api/v1/project-revenues',
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/project-revenues/${id}` }),
  },
};
