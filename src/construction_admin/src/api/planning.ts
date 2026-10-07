import { request } from './client';

/** One site as the planning screen sees it. Dates are `YYYY-MM-DD`. */
export interface PlanningProject {
  id: string;
  name: string;
  status: string;
  customerName: string | null;
  startDate: string | null;
  endDate: string | null;
  worksSaturdays: boolean;
  worksSundays: boolean;
  needs: PlanningNeed[];
}

export interface PlanningNeed {
  position: string;
  count: number;
}

export interface PlanningPosting {
  projectId: string;
  startDate: string;
  endDate: string | null;
  /** When the worker confirmed it on their phone. Null while they have not. */
  acknowledgedAt?: string | null;
}

export interface PlanningAbsence {
  type: string;
  startDate: string;
  endDate: string;
}

export interface PlanningEmployee {
  id: string;
  fullName: string;
  position: string;
  postings: PlanningPosting[];
  absences: PlanningAbsence[];
}

export interface PlanningData {
  from: string;
  to: string;
  today: string;
  projects: PlanningProject[];
  employees: PlanningEmployee[];
  positions: string[];
}

export interface PlanningQuery {
  from: string;
  to: string;
  branchId?: string;
}

export interface AssignInput {
  employeeId: string;
  /** Null frees the person for the range. */
  projectId: string | null;
  from: string;
  to: string;
  /** Fill only the days the person has no posting on at all. */
  onlyFreeDays?: boolean;
}

export interface SwapInput {
  employeeAId: string;
  employeeBId: string;
  from: string;
  to: string;
}

/** Reads and actions behind the scheduling screen. Every action sets a range outright, so a retry is harmless. */
export const planningApi = {
  get: (query: PlanningQuery) =>
    request<PlanningData>({
      method: 'GET',
      url: '/api/v1/planning',
      params: { from: query.from, to: query.to, branchId: query.branchId },
    }),

  assign: (input: AssignInput) =>
    request<void>({ method: 'POST', url: '/api/v1/planning/assign', data: input }),

  swap: (input: SwapInput) =>
    request<void>({ method: 'POST', url: '/api/v1/planning/swap', data: input }),

  setNeeds: (projectId: string, needs: PlanningNeed[]) =>
    request<void>({
      method: 'PUT',
      url: `/api/v1/planning/projects/${projectId}/needs`,
      data: { needs },
    }),
};
