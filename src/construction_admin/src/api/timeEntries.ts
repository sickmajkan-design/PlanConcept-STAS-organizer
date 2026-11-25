import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi, listParams } from './resource';
import type {
  ListQuery,
  TimeEntry,
  TimeEntryInput,
  TimeEntryStatus,
  TimeEntrySummary,
  WorkType,
} from './types';

export interface TimeEntryListQuery extends ListQuery {
  branchId?: string;
  /** With a unit: count hours by the unit that employed the person instead of the site. */
  basis?: 'Employer';
  employeeId?: string;
  projectId?: string;
  status?: TimeEntryStatus;
  workType?: WorkType;
  /** ISO instants. Matches entries overlapping the window, not only ones inside it. */
  from?: string;
  to?: string;
  openOnly?: boolean;
}

export interface TimeEntrySummaryQuery {
  from: string;
  to: string;
  branchId?: string;
  basis?: 'Employer';
  employeeId?: string;
  projectId?: string;
  approvedOnly?: boolean;
}

export interface ReviewTimeEntryInput {
  approve: boolean;
  /** Required when sending an entry back. */
  note?: string | null;
  /** Required to approve an entry that was previously sent back. */
  confirm?: boolean;
}

export interface ClockInInput {
  projectId?: string | null;
  latitude?: number;
  longitude?: number;
  /** Only for a press that waited for signal: when it really happened. */
  occurredAt?: string;
}

export interface ClockOutInput {
  breakMinutes: number;
  latitude?: number;
  longitude?: number;
  occurredAt?: string;
}

export const timeEntriesApi = {
  /** The caller's running shift; the API answers 204 (empty body) when there is none. */
  current: async () => (await request<TimeEntry | ''>({ method: 'GET', url: '/api/v1/timeentries/current' })) || null,

  clockInSites: () => request<{ id: string; name: string }[]>({ method: 'GET', url: '/api/v1/timeentries/clock-in-sites' }),

  /** A shift started here is marked as coming from the browser. */
  clockIn: (input: ClockInInput, key: string) =>
    request<TimeEntry>({ method: 'POST', url: '/api/v1/timeentries/clock-in', data: { ...input, viaWeb: true }, headers: idempotencyHeaders(key) }),

  clockOut: (input: ClockOutInput, key: string) =>
    request<TimeEntry>({ method: 'POST', url: '/api/v1/timeentries/clock-out', data: input, headers: idempotencyHeaders(key) }),

  ...createCrudApi<TimeEntry, TimeEntry, TimeEntryInput, TimeEntryListQuery>(
    '/api/v1/timeentries',
  ),

  review: (id: string, input: ReviewTimeEntryInput) =>
    request<TimeEntry>({
      method: 'POST',
      url: `/api/v1/timeentries/${id}/review`,
      data: input,
    }),

  summary: (query: TimeEntrySummaryQuery) =>
    request<TimeEntrySummary>({
      method: 'GET',
      url: '/api/v1/timeentries/summary',
      params: listParams(query),
    }),
};
