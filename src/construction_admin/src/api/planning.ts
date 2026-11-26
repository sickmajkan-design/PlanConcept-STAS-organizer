import { request } from './client';

/** One site as the planning screen sees it. Dates are `YYYY-MM-DD`. */
export interface PlanningProject {
  id: string;
  name: string;
  status: string;
  customerName: string | null;
  /** The customer's country (two letters). The site's own is `countryCode`. */
  customerCountryCode?: string | null;
  startDate: string | null;
  endDate: string | null;
  worksSaturdays: boolean;
  worksSundays: boolean;
  /** Where the site is. Null: no coordinates. */
  latitude?: number | null;
  longitude?: number | null;
  /** When the shift starts there, `HH:mm:ss`. Null: not entered. */
  shiftStartTime?: string | null;
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
  firstName?: string;
  lastName?: string;
  position: string;
  isContractor?: boolean;
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

/** One vehicle or tool as it is held now. */
export interface PlanningEquipment {
  id: string;
  kind: 'Vehicle' | 'Tool' | string;
  /** "Vehicle", "Kit" (a kofer), "BatterySet", "MeasuringSet", "CableSet" or "Other". */
  group: string;
  label: string;
  status: string;
  employeeId: string | null;
  projectId: string | null;
}

/** One person's shift and position today, as the server saw them a moment ago. */
export interface PlanningAttendanceRow {
  employeeId: string;
  /** The site they are posted to today. */
  siteId: string | null;
  /** `HH:mm` in company time. */
  clockedInAt: string | null;
  clockedOutAt: string | null;
  /** Metres from the site when they clocked in. */
  clockInMeters: number | null;
  lastPingMinutesAgo: number | null;
  lastPingMeters: number | null;
  lastPingAccuracy: number | null;
}

export interface PlanningAttendance {
  today: string;
  /** `HH:mm` in company time. */
  nowLocal: string;
  rows: PlanningAttendanceRow[];
}

export interface PlanningData {
  from: string;
  to: string;
  today: string;
  projects: PlanningProject[];
  employees: PlanningEmployee[];
  /** Vehicles and tools as held now, for the unit in view. */
  equipment?: PlanningEquipment[];
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
export interface PlanningHistoryEntry {
  id: number;
  occurredAt: string;
  action: 'Created' | 'Updated' | 'Deleted';
  employeeId: string | null;
  employeeName: string | null;
  projectId: string | null;
  projectName: string | null;
  startDate: string | null;
  endDate: string | null;
  byEmail: string | null;
  changes: Record<string, { from: string | null; to: string | null }>;
}

/** One decision about a vehicle or tool: stays, goes to another person, or back to the store. */
export interface EquipmentChoiceInput {
  kind: 'Vehicle' | 'Tool';
  id: string;
  choice: 'Stay' | 'HandOver' | 'ToStore';
  toEmployeeId: string | null;
}

/** Who held one vehicle or tool on a day. */
export interface PlanningHolder {
  kind: string;
  id: string;
  employeeId: string | null;
  projectId: string | null;
}

/** A change of holder decided for a later day, still waiting for it. */
export interface PlannedEquipmentChange {
  id: string;
  kind: string;
  itemId: string;
  employeeId: string | null;
  projectId: string | null;
  effectiveFrom: string;
}

export interface PlanningEquipmentOn {
  date: string;
  items: PlanningHolder[];
  planned: PlannedEquipmentChange[];
}

export const planningApi = {
  history: (query: { employeeId?: string; projectId?: string; take?: number } = {}) =>
    request<PlanningHistoryEntry[]>({ method: 'GET', url: '/api/v1/planning/history', params: query }),

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

  attendance: (query: { branchId?: string } = {}) =>
    request<PlanningAttendance>({ method: 'GET', url: '/api/v1/planning/attendance', params: { branchId: query.branchId } }),

  /** Returns how many items were changed. */
  decideEquipment: (items: EquipmentChoiceInput[], effectiveFrom?: string) =>
    request<number>({ method: 'POST', url: '/api/v1/planning/equipment', data: { items, effectiveFrom } }),

  /** Who held what on one day, and the changes still waiting for their day. */
  equipmentOn: (date: string, query: { branchId?: string } = {}) =>
    request<PlanningEquipmentOn>({ method: 'GET', url: '/api/v1/planning/equipment', params: { date, branchId: query.branchId } }),

  cancelPlannedEquipment: (id: string) => request<void>({ method: 'DELETE', url: `/api/v1/planning/equipment/planned/${id}` }),

  swap: (input: SwapInput) =>
    request<void>({ method: 'POST', url: '/api/v1/planning/swap', data: input }),

  setNeeds: (projectId: string, needs: PlanningNeed[], requiredCertificates?: string[]) =>
    request<void>({
      method: 'PUT',
      url: `/api/v1/planning/projects/${projectId}/needs`,
      data: { needs, requiredCertificates },
    }),
};
