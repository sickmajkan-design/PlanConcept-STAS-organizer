import { request } from './client';
import type { ListQuery, PagedList, Tool, ToolInput, ToolStatus } from './types';

export interface ToolListQuery extends ListQuery {
  status?: ToolStatus | '';
  category?: string;
  assignedEmployeeId?: string;
  assignedProjectId?: string;
  unassigned?: boolean;
}

export const toolsApi = {
  list: (query: ToolListQuery) =>
    request<PagedList<Tool>>({
      method: 'GET',
      url: '/api/tools',
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
        status: query.status || undefined,
        category: query.category || undefined,
        assignedEmployeeId: query.assignedEmployeeId || undefined,
        assignedProjectId: query.assignedProjectId || undefined,
        unassigned: query.unassigned || undefined,
        sortBy: query.sortBy || undefined,
        sortDescending: query.sortDescending || undefined,
      },
    }),

  get: (id: string) => request<Tool>({ method: 'GET', url: `/api/tools/${id}` }),

  getByQrCode: (qrCode: string) =>
    request<Tool>({
      method: 'GET',
      url: `/api/tools/by-qr/${encodeURIComponent(qrCode)}`,
    }),

  create: (input: ToolInput) =>
    request<Tool>({ method: 'POST', url: '/api/tools', data: input }),

  update: (id: string, input: ToolInput) =>
    request<Tool>({ method: 'PUT', url: `/api/tools/${id}`, data: input }),

  remove: (id: string) => request<void>({ method: 'DELETE', url: `/api/tools/${id}` }),

  assignEmployee: (id: string, employeeId: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/tools/${id}/assign-employee/${employeeId}`,
    }),

  unassignEmployee: (id: string) =>
    request<Tool>({ method: 'POST', url: `/api/tools/${id}/unassign-employee` }),

  assignProject: (id: string, projectId: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/tools/${id}/assign-project/${projectId}`,
    }),

  unassignProject: (id: string) =>
    request<Tool>({ method: 'POST', url: `/api/tools/${id}/unassign-project` }),
};
