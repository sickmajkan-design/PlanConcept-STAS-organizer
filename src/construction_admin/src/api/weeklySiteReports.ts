import { toApiError } from './apiError';
import { apiClient, request } from './client';
import { listParams } from './resource';
import type {
  ApproveWeeklySiteReportResult,
  PagedList,
  ReportableProject,
  WeeklyReportType,
  WeeklyReportLineInput,
  WeeklySiteReport,
  WeeklySiteReportDetail,
  WeeklySiteReportListQuery,
  WeeklySiteReportsSummary,
} from './types';

export interface CreateWeeklySiteReportInput {
  projectId: string;
  isoYear: number;
  isoWeek: number;
  type: WeeklyReportType;
  submittedByEmployeeId?: string | null;
  quantity?: number | null;
  note?: string | null;
  /** For an office entry: where the report came from ("mail od kupca 20.10."). */
  sourceNote?: string | null;
  /** The pictures of the signed sheet: a week can be written on several. */
  files: File[];
  /** The workers and their hours; the report's total is their sum. */
  lines?: WeeklyReportLineInput[];
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
    if (input.sourceNote) form.append('sourceNote', input.sourceNote);
    for (const file of input.files) form.append('files', file);
    if (input.lines && input.lines.length > 0) form.append('lines', JSON.stringify(input.lines));

    // No explicit Content-Type: the browser has to set it, because only it
    // knows the multipart boundary it generated.
    return request<WeeklySiteReport>({
      method: 'POST',
      url: '/api/v1/weeklysitereports',
      data: form,
    });
  },

  summary: () =>
    request<WeeklySiteReportsSummary>({ method: 'GET', url: '/api/v1/weeklysitereports/summary' }),

  detail: (id: string) =>
    request<WeeklySiteReportDetail>({ method: 'GET', url: `/api/v1/weeklysitereports/${id}` }),

  /** Approves the report and enters its hours into the month's sheet; `lines` are the workers as just checked. */
  approve: (id: string, body?: { lines?: WeeklyReportLineInput[]; keepExisting?: string[] }) =>
    request<ApproveWeeklySiteReportResult>({
      method: 'POST',
      url: `/api/v1/weeklysitereports/${id}/approve`,
      data: body ?? {},
    }),

  updateLines: (id: string, lines: WeeklyReportLineInput[]) =>
    request<void>({ method: 'PUT', url: `/api/v1/weeklysitereports/${id}/lines`, data: { lines } }),

  requestCorrection: (id: string, note: string) =>
    request<void>({
      method: 'POST',
      url: `/api/v1/weeklysitereports/${id}/request-correction`,
      data: { note },
    }),

  /** One picture of the report. */
  pageBlob: async (id: string, pageId: string): Promise<Blob> => {
    try {
      const response = await apiClient.request<Blob>({
        method: 'GET',
        url: `/api/v1/weeklysitereports/${id}/pages/${pageId}/content`,
        responseType: 'blob',
      });

      return response.data;
    } catch (error) {
      throw toApiError(error);
    }
  },

  /** The first picture's bytes, for opening/downloading — the endpoint needs a bearer token, so a plain link cannot reach it. */
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
