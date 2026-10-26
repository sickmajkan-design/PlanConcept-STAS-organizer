import { request } from './client';
import { createCrudApi, listParams } from './resource';
import type {
  Accommodation,
  AccommodationImportPreview,
  AccommodationImportResult,
  AccommodationCostSummary,
  AccommodationInput,
  AccommodationStay,
  AccommodationStayInput,
  AccommodationType,
  ListQuery,
  PagedList,
} from './types';
import { idempotencyHeaders } from './idempotency';

export interface AccommodationListQuery extends ListQuery {
  type?: AccommodationType;
  branchId?: string;
  /** True: only the ones still rented. False: only the ones given up. Omit for both. */
  isActive?: boolean;
  /** Only rented places whose contract ends within this many days (or already has). */
  contractEndsWithinDays?: number;
}

export interface AccommodationStayListQuery extends ListQuery {
  accommodationId?: string;
  employeeId?: string;
  /** Only the stays that cover today. */
  currentOnly?: boolean;
}

const crud = createCrudApi<Accommodation, Accommodation, AccommodationInput, AccommodationListQuery>(
  '/api/v1/accommodations',
);

function fileForm(file: File): FormData {
  const form = new FormData();
  form.append('file', file);
  return form;
}

export const accommodationsApi = {
  ...crud,

  import: {
    preview: (file: File) =>
      request<AccommodationImportPreview>({
        method: 'POST',
        url: '/api/v1/accommodations/import/preview',
        data: fileForm(file),
      }),

    commit: (file: File) =>
      request<AccommodationImportResult>({
        method: 'POST',
        url: '/api/v1/accommodations/import',
        data: fileForm(file),
      }),
  },

  stays: {
    list: (query: AccommodationStayListQuery) =>
      request<PagedList<AccommodationStay>>({
        method: 'GET',
        url: '/api/v1/accommodations/stays',
        params: listParams(query),
      }),

    add: (accommodationId: string, input: AccommodationStayInput) =>
      request<AccommodationStay>({
        method: 'POST',
        url: `/api/v1/accommodations/${accommodationId}/stays`,
        data: input,
      }),

    addMany: (accommodationId: string, input: AccommodationStaysInput, idempotencyKey?: string) =>
      request<AccommodationStaysResult>({
        method: 'POST',
        url: `/api/v1/accommodations/${accommodationId}/stays/bulk`,
        data: input,
        headers: idempotencyHeaders(idempotencyKey),
      }),

    update: (id: string, input: AccommodationStayInput) =>
      request<AccommodationStay>({
        method: 'PUT',
        url: `/api/v1/accommodations/stays/${id}`,
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/accommodations/stays/${id}` }),
  },

  costs: (id: string, period: { from: string; to: string }) =>
    request<AccommodationCostSummary>({
      method: 'GET',
      url: `/api/v1/accommodations/${id}/costs`,
      params: period,
    }),
};

export interface AccommodationStayRowInput {
  employeeId: string;
  startDate?: string | null;
  endDate?: string | null;
  projectId?: string | null;
  note?: string | null;
}

export interface AccommodationStaysInput {
  /** True: nothing is written, the answer says what would happen to each person. */
  preview: boolean;
  stays: AccommodationStayRowInput[];
}

export type AccommodationStayRowStatus = 'Ok' | 'OverCapacity' | 'Conflict' | 'Invalid';

export interface AccommodationStayRowResult {
  employeeId: string;
  employeeName: string | null;
  startDate: string;
  endDate: string | null;
  projectId: string | null;
  projectName: string | null;
  status: AccommodationStayRowStatus;
  /** EmployeeNotFound, ProjectNotFound, EndBeforeStart, ListedTwice or AlreadyHoused. */
  reason: string | null;
  place: string | null;
  placeUntil: string | null;
  /** The stay that was written. Null in a preview and for a person who was not written. */
  stayId: string | null;
}

export interface AccommodationStaysResult {
  preview: boolean;
  beds: number | null;
  peakOccupancy: number;
  peakDate: string | null;
  saved: number;
  rows: AccommodationStayRowResult[];
}
