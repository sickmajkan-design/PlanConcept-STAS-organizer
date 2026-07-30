import { request } from './client';
import type {
  Employee,
  EmployeeDetail,
  EmployeeInput,
  EmployeeStatus,
  ListQuery,
  PagedList,
} from './types';

export interface EmployeeListQuery extends ListQuery {
  status?: EmployeeStatus | '';
  projectId?: string;
}

export const employeesApi = {
  list: (query: EmployeeListQuery) =>
    request<PagedList<Employee>>({
      method: 'GET',
      url: '/api/employees',
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
        status: query.status || undefined,
        projectId: query.projectId || undefined,
        sortBy: query.sortBy || undefined,
        sortDescending: query.sortDescending || undefined,
      },
    }),

  get: (id: string) =>
    request<EmployeeDetail>({ method: 'GET', url: `/api/employees/${id}` }),

  create: (input: EmployeeInput) =>
    request<Employee>({ method: 'POST', url: '/api/employees', data: input }),

  update: (id: string, input: EmployeeInput) =>
    request<Employee>({
      method: 'PUT',
      url: `/api/employees/${id}`,
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/employees/${id}` }),

  assignToProject: (employeeId: string, projectId: string) =>
    request<void>({
      method: 'POST',
      url: `/api/employees/${employeeId}/projects/${projectId}`,
    }),

  removeFromProject: (employeeId: string, projectId: string) =>
    request<void>({
      method: 'DELETE',
      url: `/api/employees/${employeeId}/projects/${projectId}`,
    }),
};
