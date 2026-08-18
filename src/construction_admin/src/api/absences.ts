import { request } from './client';
import { listParams } from './resource';
import type {
  Absence,
  AbsenceBalance,
  AbsenceInput,
  AbsenceStatus,
  AbsenceType,
  ListQuery,
  PagedList,
  Schedule,
} from './types';

export interface AbsenceListQuery extends ListQuery {
  employeeId?: string;
  status?: AbsenceStatus;
  type?: AbsenceType;
  /** `YYYY-MM-DD`. Matches leave overlapping the window, not only inside it. */
  from?: string;
  to?: string;
}

export interface ScheduleQuery {
  from: string;
  to: string;
  projectId?: string;
  /** Leaves out employees with nothing on them in this window. */
  assignedOnly?: boolean;
}

export interface ReviewAbsenceInput {
  approve: boolean;
  /** Required when refusing, so the person knows why. */
  note?: string | null;
}

/**
 * Absences are not a CRUD collection: leave is booked, then granted or
 * refused, and withdrawing it is a status change rather than an edit. There is
 * no update endpoint to wrap, so this is written out rather than built from
 * `createCrudApi`.
 */
export const absencesApi = {
  list: (query: AbsenceListQuery) =>
    request<PagedList<Absence>>({
      method: 'GET',
      url: '/api/v1/absences',
      params: listParams(query),
    }),

  book: (input: AbsenceInput) =>
    request<Absence>({ method: 'POST', url: '/api/v1/absences', data: input }),

  review: (id: string, input: ReviewAbsenceInput) =>
    request<Absence>({
      method: 'POST',
      url: `/api/v1/absences/${id}/review`,
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/absences/${id}` }),

  schedule: (query: ScheduleQuery) =>
    request<Schedule>({
      method: 'GET',
      url: '/api/v1/schedule',
      params: listParams(query),
    }),

  balance: (employeeId: string, year?: number) =>
    request<AbsenceBalance>({
      method: 'GET',
      url: '/api/v1/absences/balance',
      params: { employeeId, year },
    }),
};
