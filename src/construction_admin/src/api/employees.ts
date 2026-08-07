import { request } from './client';
import { createCrudApi } from './resource';
import type {
  Employee,
  EmployeeDetail,
  EmployeeInput,
  EmployeeStatus,
  ListQuery,
} from './types';

export interface EmployeeListQuery extends ListQuery {
  status?: EmployeeStatus | '';
  projectId?: string;
}

export const employeesApi = {
  ...createCrudApi<Employee, EmployeeDetail, EmployeeInput, EmployeeListQuery>(
    '/api/v1/employees',
  ),

  assignToProject: (employeeId: string, projectId: string) =>
    request<void>({
      method: 'POST',
      url: `/api/v1/employees/${employeeId}/projects/${projectId}`,
    }),

  removeFromProject: (employeeId: string, projectId: string) =>
    request<void>({
      method: 'DELETE',
      url: `/api/v1/employees/${employeeId}/projects/${projectId}`,
    }),
};
