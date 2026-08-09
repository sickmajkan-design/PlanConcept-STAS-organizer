import { request } from './client';
import { idempotencyHeaders } from './idempotency';
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
  ...createCrudApi<Tool, Tool, ToolInput, ToolListQuery>('/api/v1/tools'),

  getByQrCode: (qrCode: string) =>
    request<Tool>({
      method: 'GET',
      url: `/api/v1/tools/by-qr/${encodeURIComponent(qrCode)}`,
    }),

  assignEmployee: (id: string, employeeId: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/assign-employee/${employeeId}`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  unassignEmployee: (id: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/unassign-employee`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  assignProject: (id: string, projectId: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/assign-project/${projectId}`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  unassignProject: (id: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/unassign-project`,
      headers: idempotencyHeaders(idempotencyKey),
    }),
};
