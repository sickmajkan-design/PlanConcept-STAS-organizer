import { toApiError } from './apiError';
import { apiClient, request } from './client';
import { listParams } from './resource';
import type {
  PagedList,
  ReportableProject,
  WeeklyReportType,
  WeeklySiteReport,
  WeeklySiteReportListQuery,
} from './types';

export interface CreateWeeklySiteReportInput {
  projectId: string;
  isoYear: number;
  isoWeek: number;
  type: WeeklyReportType;
  submittedByEmployeeId?: string | null;
  quantity?: number | null;
  note?: string | null;
  file: File;
}

export const weeklySiteReportsApi = {
  list: (query: WeeklySiteReportListQuery) =>
    request<PagedList<WeeklySiteReport>>({
      method: 'GET',
      url: '/api/v1/weeklysitereports',
      params: listParams(query),
    }),

  reportableProjects: () =>
    request<ReportableProject[]>({
      method: 'GET',
      url: '/api/v1/weeklysitereports/reportable-projects',
    }),

  create: (input: CreateWeeklySiteReportInput) => {
    const form = new FormData();

    form.append('projectId', input.projectId);
    form.append('isoYear', String(input.isoYear));
    form.append('isoWeek', String(input.isoWeek));
    form.append('type', input.type);
    if (input.submittedByEmployeeId) form.append('submittedByEmployeeId', input.submittedByEmployeeId);
    if (input.quantity != null) form.append('quantity', String(input.quantity));
    if (input.note) form.append('note', input.note);
    form.append('file', input.file);

    // No explicit Content-Type: the browser has to set it, because only it
    // knows the multipart boundary it generated.
    return request<WeeklySiteReport>({
      method: 'POST',
      url: '/api/v1/weeklysitereports',
      data: form,
    });
  },

  markProcessed: (id: string) =>
    request<void>({ method: 'POST', url: `/api/v1/weeklysitereports/${id}/mark-processed` }),

  /** The proof document's bytes, for opening/downloading — the endpoint needs a bearer token, so a plain link cannot reach it. */
  blob: async (id: string): Promise<Blob> => {
    try {
      const response = await apiClient.request<Blob>({
        method: 'GET',
        url: `/api/v1/weeklysitereports/${id}/content`,
        responseType: 'blob',
      });

      return response.data;
    } catch (error) {
      throw toApiError(error);
    }
  },
};
