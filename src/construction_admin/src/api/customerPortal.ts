import { request } from './client';

export interface CustomerProjectStatus {
  projectId: string;
  projectName: string;
  status: 'Planned' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled';
  address: string | null;
  startDate: string | null;
  endDate: string | null;
  percentComplete: number | null;
}

export const customerPortalApi = {
  myProjects: () =>
    request<CustomerProjectStatus[]>({
      method: 'GET',
      url: '/api/v1/customer-portal/projects',
    }),
};
