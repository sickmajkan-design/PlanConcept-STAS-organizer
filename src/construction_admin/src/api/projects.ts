import { request } from './client';
import type {
  ListQuery,
  PagedList,
  Project,
  ProjectDetail,
  ProjectInput,
  ProjectStatus,
} from './types';

export interface ProjectListQuery extends ListQuery {
  status?: ProjectStatus | '';
  employeeId?: string;
}

export const projectsApi = {
  list: (query: ProjectListQuery) =>
    request<PagedList<Project>>({
      method: 'GET',
      url: '/api/projects',
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
        status: query.status || undefined,
        employeeId: query.employeeId || undefined,
        sortBy: query.sortBy || undefined,
        sortDescending: query.sortDescending || undefined,
      },
    }),

  get: (id: string) =>
    request<ProjectDetail>({ method: 'GET', url: `/api/projects/${id}` }),

  create: (input: ProjectInput) =>
    request<Project>({ method: 'POST', url: '/api/projects', data: input }),

  update: (id: string, input: ProjectInput) =>
    request<Project>({
      method: 'PUT',
      url: `/api/projects/${id}`,
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/projects/${id}` }),
};
