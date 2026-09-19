import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi } from './resource';
import type {
  Employee,
  EmployeeDetail,
  EmployeeInput,
  EmployeeStatus,
  EmployeeType,
  ListQuery,
  OrganizationHierarchyNode,
  OrganizationRank,
} from './types';

export interface EmployeeListQuery extends ListQuery {
  status?: EmployeeStatus | '';
  type?: EmployeeType | '';
  projectId?: string;
}

export const employeesApi = {
  ...createCrudApi<Employee, EmployeeDetail, EmployeeInput, EmployeeListQuery>(
    '/api/v1/employees',
  ),

  assignToProject: (
    employeeId: string,
    projectId: string,
    idempotencyKey?: string,
    dates?: { startDate?: string | null; endDate?: string | null },
  ) =>
    request<void>({
      method: 'POST',
      url: `/api/v1/employees/${employeeId}/projects/${projectId}`,
      data: dates,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  removeFromProject: (employeeId: string, projectId: string) =>
    request<void>({
      method: 'DELETE',
      url: `/api/v1/employees/${employeeId}/projects/${projectId}`,
    }),

  setRank: (id: string, rank: OrganizationRank | null) =>
    request<Employee>({
      method: 'PUT',
      url: `/api/v1/employees/${id}/rank`,
      data: { rank },
    }),

  hierarchy: () =>
    request<OrganizationHierarchyNode[]>({
      method: 'GET',
      url: '/api/v1/employees/hierarchy',
    }),
};
