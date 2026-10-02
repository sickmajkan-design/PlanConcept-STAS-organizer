import { request } from './client';
import type { Branch, BranchInput } from './types';

/** The operator's own business units (poslovne jedinice). Not a paged resource: there are a handful. */
export const branchesApi = {
  list: () => request<Branch[]>({ method: 'GET', url: '/api/v1/branches' }),

  create: (input: BranchInput) =>
    request<Branch>({ method: 'POST', url: '/api/v1/branches', data: input }),

  update: (id: string, input: BranchInput) =>
    request<Branch>({ method: 'PUT', url: `/api/v1/branches/${id}`, data: input }),

  /** Makes exactly these Main projects the unit's projects; their sub-projects follow. */
  setProjects: (id: string, projectIds: string[]) =>
    request<Branch>({ method: 'PUT', url: `/api/v1/branches/${id}/projects`, data: { projectIds } }),

  remove: (id: string) => request<void>({ method: 'DELETE', url: `/api/v1/branches/${id}` }),
};
