import { request } from './client';
import type { AssignmentBoard } from './types';

export type ScheduleDayStatus =
  | 'Free'
  | 'Expected'
  | 'OnSite'
  | 'Late'
  | 'NoShow'
  | 'Leave'
  | 'Sick';

export interface SchedulePosting {
  projectId: string;
  startDate: string;
  endDate: string | null;
}

export interface ScheduleAbsence {
  type: string;
  startDate: string;
  endDate: string;
}

export interface ScheduleProject {
  id: string;
  name: string;
  status: string;
  /** Expected clock-in, `HH:mm:ss` in UTC; null when the site has none. */
  shiftStartTime: string | null;
  endDate: string | null;
  worksSaturdays: boolean;
  worksSundays: boolean;
}

export interface ScheduleEmployee {
  id: string;
  fullName: string;
  position: string;
  status: ScheduleDayStatus;
  clockedInAt: string | null;
  todayProjectId: string | null;
  postings: SchedulePosting[];
  absences: ScheduleAbsence[];
  vehicles: string[];
  tools: string[];
}

export interface AssignmentSchedule {
  from: string;
  days: number;
  today: string;
  lateToleranceMinutes: number;
  projects: ScheduleProject[];
  employees: ScheduleEmployee[];
}

export interface ScheduleQuery {
  from?: string;
  days: number;
  branchId?: string;
}

/**
 * The reads behind the assignment boards. Assigning and removing reuse
 * `employeesApi.assignToProject` / `removeFromProject` — this module only
 * reads the combined picture those writes change.
 */
export const assignmentsApi = {
  board: (query: { branchId?: string } = {}) =>
    request<AssignmentBoard>({ method: 'GET', url: '/api/v1/assignment-board', params: query.branchId ? { branchId: query.branchId } : undefined }),

  schedule: (query: ScheduleQuery) =>
    request<AssignmentSchedule>({
      method: 'GET',
      url: '/api/v1/assignment-board/schedule',
      params: { from: query.from, days: query.days, branchId: query.branchId },
    }),
};
