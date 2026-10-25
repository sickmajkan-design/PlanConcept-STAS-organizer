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

  /** Moves these employees into the unit from a date; everyone else keeps their unit. */
  assignEmployees: (id: string, input: { employeeIds: string[]; from?: string; backdateNewcomers?: boolean }) =>
    request<Branch>({ method: 'POST', url: `/api/v1/branches/${id}/employees`, data: input }),

  /** `raster` is a png copy of an SVG logo: spreadsheets and the phone draw pictures, not SVG. */
  uploadLogo: (id: string, file: File, dark = false, raster?: Blob | null) => {
    const form = new FormData();
    form.append('file', file);
    if (raster) form.append('raster', raster, 'logo.png');

    return request<Branch>({ method: 'POST', url: `/api/v1/branches/${id}/logo${dark ? '?dark=true' : ''}`, data: form });
  },

  removeLogo: (id: string, dark = false) =>
    request<Branch>({ method: 'DELETE', url: `/api/v1/branches/${id}/logo${dark ? '?dark=true' : ''}` }),

  /** Makes the unit the one whose logo stands for "all business units"; answers with the whole list. */
  setMain: (id: string) => request<Branch[]>({ method: 'PUT', url: `/api/v1/branches/${id}/main` }),

  remove: (id: string) => request<void>({ method: 'DELETE', url: `/api/v1/branches/${id}` }),
};
