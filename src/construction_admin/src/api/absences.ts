import { request } from './client';
import { listParams } from './resource';
import type {
  Absence,
  AbsenceBalance,
  AbsenceInput,
  AbsenceStatus,
  AbsenceType,
  LeaveAdjustment,
  LeaveAdjustmentInput,
  LeaveSettings,
  ListQuery,
  PagedList,
  Schedule,
} from './types';

export interface AbsenceListQuery extends ListQuery {
  employeeId?: string;
  /** The unit that employed the person on the day the leave began. */
  branchId?: string;
  status?: AbsenceStatus;
  type?: AbsenceType;
  /** Undecided requests plus employee-proposed changes awaiting confirmation. */
  waitingOnReviewer?: boolean;
  /** `YYYY-MM-DD`. Matches leave overlapping the window, not only inside it. */
  from?: string;
  to?: string;
}

export interface ScheduleQuery {
  from: string;
  to: string;
  /** The people this unit employed at some point in the window. */
  branchId?: string;
  projectId?: string;
  /** Leaves out employees with nothing on them in this window. */
  assignedOnly?: boolean;
}

export interface ReviewAbsenceInput {
  approve: boolean;
  /** Required when refusing, so the person knows why. */
  note?: string | null;
  /** Take the person off their accommodation for the leave. Left out: annual leave does, others do not. */
  releaseAccommodation?: boolean;
  /** Book them back into the same place the day after. Left out: yes. */
  returnToAccommodation?: boolean;
}

/** Where a person lives on the first day of a leave, so the office can decide what granting it does. */
export interface AbsenceHousingImpact {
  hasStay: boolean;
  stayId: string | null;
  accommodationId: string | null;
  accommodationName: string | null;
  stayStartDate: string | null;
  stayEndDate: string | null;
}

export interface ProposeAbsenceEditInput {
  startDate: string;
  endDate: string;
  reason?: string | null;
}

export interface ConfirmAbsenceEditInput {
  approve: boolean;
}

/**
 * Absences are not a CRUD collection: leave is booked, then granted or
 * refused, and withdrawing it is a status change rather than an edit. There is
 * no update endpoint to wrap, so this is written out rather than built from
 * `createCrudApi`. The one exception is an already-approved annual leave's
 * dates: `proposeEdit`/`confirmEdit` are a two-step, two-sided change rather
 * than a plain edit, so they stay separate from a generic `update` too.
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

  housingImpact: (employeeId: string, startDate: string, endDate: string) =>
    request<AbsenceHousingImpact>({
      method: 'GET',
      url: '/api/v1/absences/housing-impact',
      params: { employeeId, startDate, endDate },
    }),

  review: (id: string, input: ReviewAbsenceInput) =>
    request<Absence>({
      method: 'POST',
      url: `/api/v1/absences/${id}/review`,
      data: input,
    }),

  proposeEdit: (id: string, input: ProposeAbsenceEditInput) =>
    request<Absence>({
      method: 'POST',
      url: `/api/v1/absences/${id}/propose-edit`,
      data: input,
    }),

  confirmEdit: (id: string, input: ConfirmAbsenceEditInput) =>
    request<Absence>({
      method: 'POST',
      url: `/api/v1/absences/${id}/confirm-edit`,
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

  adjustments: (employeeId: string, year?: number) =>
    request<LeaveAdjustment[]>({
      method: 'GET',
      url: '/api/v1/absences/adjustments',
      params: { employeeId, year },
    }),

  createAdjustment: (input: LeaveAdjustmentInput) =>
    request<LeaveAdjustment>({ method: 'POST', url: '/api/v1/absences/adjustments', data: input }),

  leaveSettings: () =>
    request<LeaveSettings>({ method: 'GET', url: '/api/v1/leave-settings' }),

  updateLeaveSettings: (input: LeaveSettings) =>
    request<LeaveSettings>({ method: 'PUT', url: '/api/v1/leave-settings', data: input }),
};
