import { request } from './client';
import { createCrudApi } from './resource';
import type { ListQuery, Tool, ToolInput, ToolStatus } from './types';

export interface ToolListQuery extends ListQuery {
  status?: ToolStatus | '';
  category?: string;
  assignedEmployeeId?: string;
  assignedProjectId?: string;
  unassigned?: boolean;
}

export const toolsApi = {
  ...createCrudApi<Tool, Tool, ToolInput, ToolListQuery>('/api/tools'),

  getByQrCode: (qrCode: string) =>
    request<Tool>({
      method: 'GET',
      url: `/api/tools/by-qr/${encodeURIComponent(qrCode)}`,
    }),

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
