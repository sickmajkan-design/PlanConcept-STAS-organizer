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
  /** Whose public holidays the site keeps. */
  countryCode?: string | null;
  needs: PlanningNeed[];
  /** Certificates everybody posted here has to hold. */
  requiredCertificates?: string[];
}

export interface PlanningHoliday {
  date: string;
  name: string;
  countryCode: string;
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
  certificates?: PlanningCertificate[];
  /** Where the company houses the worker during the window. */
  stays?: PlanningStay[];
}

export interface PlanningStay {
  place: string;
  startDate: string;
  endDate: string | null;
}

export interface PlanningCertificate {
  name: string;
  /** The last valid day. Null for one that does not expire. */
  validUntil: string | null;
}

export interface PlanningData {
  from: string;
  to: string;
  today: string;
  projects: PlanningProject[];
  employees: PlanningEmployee[];
  positions: string[];
  certificateNames?: string[];
  holidays?: PlanningHoliday[];
  /** False for a foreman without the right to move people: the screen is then a view. */
  canEdit?: boolean;
  /** True when the caller sees only one business unit. */
  isScoped?: boolean;
  scopeBranchName?: string | null;
}

export interface PlanningQuery {
  from: string;
  to: string;
  branchId?: string;
}

export interface PlannedLabourCostMonth {
  month: string;
  plannedDays: number;
  plannedCost: number;
  actualCost: number;
}

export interface PlannedLabourCostProject {
  projectId: string;
  name: string;
  months: PlannedLabourCostMonth[];
  plannedCost: number;
  actualCost: number;
  plannedDays: number;
}

export interface PlannedLabourCost {
  from: string;
  to: string;
  hoursPerDay: number;
  months: string[];
  projects: PlannedLabourCostProject[];
  unpricedDays: number;
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

  labourCost: (query: { from: string; to: string; hoursPerDay: number; branchId?: string }) =>
    request<PlannedLabourCost>({
      method: 'GET',
      url: '/api/v1/planning/labour-cost',
      params: { from: query.from, to: query.to, hoursPerDay: query.hoursPerDay, branchId: query.branchId },
    }),

  assign: (input: AssignInput) =>
    request<void>({ method: 'POST', url: '/api/v1/planning/assign', data: input }),

  swap: (input: SwapInput) =>
    request<void>({ method: 'POST', url: '/api/v1/planning/swap', data: input }),

  setNeeds: (projectId: string, needs: PlanningNeed[], requiredCertificates?: string[]) =>
    request<void>({
      method: 'PUT',
      url: `/api/v1/planning/projects/${projectId}/needs`,
      data: { needs, requiredCertificates },
    }),
};
