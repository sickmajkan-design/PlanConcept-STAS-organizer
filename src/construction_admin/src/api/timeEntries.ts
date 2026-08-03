import { request } from './client';
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
  employeeId?: string;
  projectId?: string;
  approvedOnly?: boolean;
}

export interface ReviewTimeEntryInput {
  approve: boolean;
  /** Required when sending an entry back. */
  note?: string | null;
}

export const timeEntriesApi = {
  ...createCrudApi<TimeEntry, TimeEntry, TimeEntryInput, TimeEntryListQuery>(
    '/api/timeentries',
  ),

  review: (id: string, input: ReviewTimeEntryInput) =>
    request<TimeEntry>({
      method: 'POST',
      url: `/api/timeentries/${id}/review`,
      data: input,
    }),

  summary: (query: TimeEntrySummaryQuery) =>
    request<TimeEntrySummary>({
      method: 'GET',
      url: '/api/timeentries/summary',
      params: listParams(query),
    }),
};
