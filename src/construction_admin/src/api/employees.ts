import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi } from './resource';
import type {
  Employee,
  EmployeeBranchPeriod,
  EmployeeDetail,
  EmployeeInput,
  EmployeeStatus,
  EmployeeType,
  ListQuery,
  OrganizationHierarchy,
  OrganizationRank,
} from './types';

export interface EmployeeListQuery extends ListQuery {
  status?: EmployeeStatus | '';
  type?: EmployeeType | '';
  projectId?: string;
  /** The unit that employs them now. */
  branchId?: string;
}

export const employeesApi = {
  ...createCrudApi<Employee, EmployeeDetail, EmployeeInput, EmployeeListQuery>(
    '/api/v1/employees',
  ),

  /** Puts the employee in a unit from a date (or in none); returns their history. */
  setBranch: (employeeId: string, input: { branchId: string | null; from?: string }) =>
    request<EmployeeBranchPeriod[]>({
      method: 'PUT',
      url: `/api/v1/employees/${employeeId}/branch`,
      data: input,
    }),

  removeBranchPeriod: (employeeId: string, periodId: string) =>
    request<EmployeeBranchPeriod[]>({
      method: 'DELETE',
      url: `/api/v1/employees/${employeeId}/branch-periods/${periodId}`,
    }),

  assignToProject: (
    employeeId: string,
    projectId: string,
    idempotencyKey?: string,
    dates?: { startDate?: string | null; endDate?: string | null; customerCompanyId?: string | null },
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

  /** Changes which of the client's companies an existing posting is worked for. Null clears it. */
  setProjectCompany: (employeeId: string, projectId: string, customerCompanyId: string | null) =>
    request<void>({
      method: 'PUT',
      url: `/api/v1/employees/${employeeId}/projects/${projectId}/company`,
      data: { customerCompanyId },
    }),

  setRank: (id: string, rank: OrganizationRank | null) =>
    request<Employee>({
      method: 'PUT',
      url: `/api/v1/employees/${id}/rank`,
      data: { rank },
    }),

  hierarchy: () =>
    request<OrganizationHierarchy>({
      method: 'GET',
      url: '/api/v1/employees/hierarchy',
    }),
};
