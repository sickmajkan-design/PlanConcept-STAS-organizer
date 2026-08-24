/** Pagination envelope returned by every list endpoint. */
export interface PagedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface User {
  id: string;
  email: string;
  role: Role;
  employeeId: string | null;
  firstName: string | null;
  lastName: string | null;
  lastLoginAt: string | null;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: User;
}

export const roles = [
  'SuperAdmin',
  'Admin',
  'ProjectManager',
  'Foreman',
  'Worker',
] as const;

export type Role = (typeof roles)[number];

export const employeeStatuses = [
  'Active',
  'OnLeave',
  'Suspended',
  'Terminated',
] as const;

export type EmployeeStatus = (typeof employeeStatuses)[number];

export const projectStatuses = [
  'Planned',
  'Active',
  'OnHold',
  'Completed',
  'Cancelled',
] as const;

export type ProjectStatus = (typeof projectStatuses)[number];

export interface Employee {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  dateOfBirth: string | null;
  employmentDate: string;
  position: string;
  status: EmployeeStatus;
  createdAt: string;
  updatedAt: string | null;
}

export interface EmployeeProjectAssignment {
  projectId: string;
  projectName: string;
  projectStatus: ProjectStatus;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while the posting is still open. */
  endDate: string | null;
  assignedAt: string;
  /** Hours paid for on this posting, from hourly finance entries. */
  workedHours: number;
  /** Days paid for on this posting, from daily finance entries. */
  workedDays: number;
  /** Null for a role the API doesn't show pay to. */
  totalPay: number | null;
}

export interface EmployeeDetail extends Employee {
  hasUserAccount: boolean;
  projects: EmployeeProjectAssignment[];
  /** Postings that have ended, most recently closed first. */
  pastProjects: EmployeeProjectAssignment[];
}

export interface EmployeeInput {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  dateOfBirth?: string | null;
  employmentDate: string;
  position: string;
  status: EmployeeStatus;
}

export interface Project {
  id: string;
  name: string;
  description: string | null;
  client: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  startDate: string | null;
  endDate: string | null;
  status: ProjectStatus;
  contractValue: number | null;
  employeeCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface ProjectEmployee {
  employeeId: string;
  employeeNumber: string;
  fullName: string;
  position: string;
  status: EmployeeStatus;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while the posting is still open. */
  endDate: string | null;
  assignedAt: string;
  /** Hours paid for on this posting, from hourly finance entries. */
  workedHours: number;
  /** Days paid for on this posting, from daily finance entries. */
  workedDays: number;
  /** Null for a role the API doesn't show pay to. */
  totalPay: number | null;
}

export interface ProjectDetail extends Project {
  employees: ProjectEmployee[];
  /** Crew whose posting here has ended, most recently closed first. */
  pastEmployees: ProjectEmployee[];
}

export interface ProjectInput {
  name: string;
  description?: string | null;
  client?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  status: ProjectStatus;
  contractValue?: number | null;
}

export interface ProjectRevenue {
  id: string;
  projectId: string;
  projectName: string;
  amount: number;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
  note: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface ProjectRevenueInput {
  projectId: string;
  amount: number;
  occurredOn?: string | null;
  note?: string | null;
}

export interface AnnualRealizationRow {
  projectId: string;
  projectName: string;
  status: ProjectStatus;
  contractValue: number;
  realizedThisYear: number;
  realizedToDate: number;
  remaining: number;
  percentOfContract: number | null;
}

export interface AnnualRealizationPlan {
  year: number;
  rows: AnnualRealizationRow[];
  totalContractValue: number;
  totalRealizedThisYear: number;
  totalRealizedToDate: number;
  totalRemaining: number;
  percentRealized: number | null;
}

export interface AssignmentBoardPosting {
  projectId: string;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while the posting is open-ended. */
  endDate: string | null;
}

export interface AssignmentBoardEquipment {
  id: string;
  name: string;
}

export interface AssignmentBoardEmployee {
  id: string;
  fullName: string;
  employeeNumber: string;
  position: string;
  /** Every posting this employee currently holds — never just one. */
  postings: AssignmentBoardPosting[];
  assignedTools: AssignmentBoardEquipment[];
  assignedVehicles: AssignmentBoardEquipment[];
}

export interface AssignmentBoardProject {
  id: string;
  name: string;
  status: ProjectStatus;
  toolCount: number;
  vehicleCount: number;
}

export interface AssignmentBoard {
  employees: AssignmentBoardEmployee[];
  projects: AssignmentBoardProject[];
}

export interface EmployeeLocation {
  employeeId: string;
  employeeNumber: string;
  fullName: string;
  position: string;
  latitude: number;
  longitude: number;
  accuracy: number | null;
  timestamp: string;
}

/** Query shared by the paged list endpoints. */
export interface ListQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export const vehicleStatuses = [
  'Available',
  'Assigned',
  'InService',
  'OutOfService',
] as const;

export type VehicleStatus = (typeof vehicleStatuses)[number];

export const fuelTypes = ['Petrol', 'Diesel', 'Electric', 'Hybrid', 'Lpg'] as const;

export type FuelType = (typeof fuelTypes)[number];

export interface Vehicle {
  id: string;
  brand: string;
  model: string;
  registrationNumber: string;
  vin: string | null;
  qrCode: string | null;
  fuelType: FuelType;
  status: VehicleStatus;
  assignedEmployeeId: string | null;
  assignedEmployeeName: string | null;
  assignedEmployeeNumber: string | null;
  assignedProjectId: string | null;
  assignedProjectName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface VehicleInput {
  brand: string;
  model: string;
  registrationNumber: string;
  vin?: string | null;
  qrCode?: string | null;
  fuelType: FuelType;
  status: VehicleStatus;
}

export const toolStatuses = [
  'Available',
  'Assigned',
  'UnderRepair',
  'Lost',
  'Retired',
] as const;

export type ToolStatus = (typeof toolStatuses)[number];

export interface Tool {
  id: string;
  name: string;
  category: string | null;
  serialNumber: string | null;
  qrCode: string | null;
  status: ToolStatus;
  assignedEmployeeId: string | null;
  assignedEmployeeName: string | null;
  assignedEmployeeNumber: string | null;
  assignedProjectId: string | null;
  assignedProjectName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ToolInput {
  name: string;
  category?: string | null;
  serialNumber?: string | null;
  qrCode?: string | null;
  status: ToolStatus;
}

export interface Material {
  id: string;
  name: string;
  unit: string;
  quantity: number;
  warehouse: string | null;
  projectId: string | null;
  projectName: string | null;
  lastUpdated: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface MaterialInput {
  name: string;
  unit: string;
  quantity: number;
  warehouse?: string | null;
  projectId?: string | null;
}

/**
 * An account as the user-administration screens see it.
 *
 * Distinct from `User`, which is the signed-in operator's own profile from
 * `/api/v1/auth/me`. Keeping them apart stops "the current user" and "a row in
 * the accounts table" drifting into one type that is right for neither.
 */
export interface UserAccount {
  id: string;
  email: string;
  role: Role;
  isActive: boolean;
  lastLoginAt: string | null;
  lockoutEndsAt: string | null;
  employeeId: string | null;
  employeeName: string | null;
  createdAt: string;
}

export interface UserAccountInput {
  email: string;
  role: Role;
  employeeId?: string | null;
}

export interface CreateUserAccountInput extends UserAccountInput {
  password: string;
}

export const timeEntryStatuses = [
  'InProgress',
  'Submitted',
  'Approved',
  'Rejected',
] as const;

export type TimeEntryStatus = (typeof timeEntryStatuses)[number];

export const workTypes = [
  'Regular',
  'Overtime',
  'Weekend',
  'PublicHoliday',
  'Travel',
] as const;

export type WorkType = (typeof workTypes)[number];

export interface TimeEntry {
  id: string;
  employeeId: string;
  employeeName: string;
  projectId: string | null;
  projectName: string | null;
  startedAt: string;
  endedAt: string | null;
  breakMinutes: number;
  /** Null while the shift is still running. */
  workedMinutes: number | null;
  workType: WorkType;
  status: TimeEntryStatus;
  note: string | null;
  startLatitude: number | null;
  startLongitude: number | null;
  endLatitude: number | null;
  endLongitude: number | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface TimeEntryInput {
  employeeId: string;
  projectId?: string | null;
  startedAt: string;
  endedAt?: string | null;
  breakMinutes: number;
  workType: WorkType;
  note?: string | null;
}

export interface TimeEntrySummaryRow {
  employeeId: string;
  employeeName: string;
  entryCount: number;
  totalMinutes: number;
  approvedMinutes: number;
  pendingCount: number;
}

export interface TimeEntrySummary {
  from: string;
  to: string;
  rows: TimeEntrySummaryRow[];
  totalMinutes: number;
  approvedMinutes: number;
  pendingCount: number;
}

export const attachmentOwnerTypes = [
  'Employee',
  'Project',
  'Vehicle',
  'Tool',
  'WorkItem',
] as const;

export type AttachmentOwnerType = (typeof attachmentOwnerTypes)[number];

export const attachmentCategories = [
  'Contract',
  'Certificate',
  'MedicalCheck',
  'Licence',
  'Insurance',
  'SiteDocument',
  'Photo',
  'Other',
] as const;

export type AttachmentCategory = (typeof attachmentCategories)[number];

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  category: AttachmentCategory;
  description: string | null;
  /** `YYYY-MM-DD`, or null for anything that does not lapse. */
  expiresAt: string | null;
  ownerType: AttachmentOwnerType;
  ownerId: string;
  ownerName: string | null;
  uploadedByName: string | null;
  createdAt: string;
}

export const workItemKinds = ['Task', 'Defect'] as const;

export type WorkItemKind = (typeof workItemKinds)[number];

export const workItemStatuses = [
  'Open',
  'InProgress',
  'Resolved',
  'Closed',
  'Cancelled',
] as const;

export type WorkItemStatus = (typeof workItemStatuses)[number];

export const workItemPriorities = ['Low', 'Normal', 'High', 'Urgent'] as const;

export type WorkItemPriority = (typeof workItemPriorities)[number];

export interface WorkItem {
  id: string;
  kind: WorkItemKind;
  title: string;
  description: string | null;
  projectId: string | null;
  projectName: string | null;
  assignedEmployeeId: string | null;
  assignedEmployeeName: string | null;
  priority: WorkItemPriority;
  status: WorkItemStatus;
  /** `YYYY-MM-DD`. */
  dueDate: string | null;
  latitude: number | null;
  longitude: number | null;
  requiresAcknowledgment: boolean;
  createdByName: string | null;
  resolvedByName: string | null;
  resolvedAt: string | null;
  attachmentCount: number;
  isFinished: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface WorkItemInput {
  kind: WorkItemKind;
  title: string;
  description?: string | null;
  projectId?: string | null;
  assignedEmployeeId?: string | null;
  priority: WorkItemPriority;
  dueDate?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  requiresAcknowledgment?: boolean;
}

export const absenceTypes = [
  'AnnualLeave',
  'SickLeave',
  'UnpaidLeave',
  'PaidSpecialLeave',
  'Training',
  'Other',
] as const;

export type AbsenceType = (typeof absenceTypes)[number];

export const absenceStatuses = [
  'Requested',
  'Approved',
  'Rejected',
  'Cancelled',
] as const;

export type AbsenceStatus = (typeof absenceStatuses)[number];

export interface Absence {
  id: string;
  employeeId: string;
  employeeName: string;
  type: AbsenceType;
  status: AbsenceStatus;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, inclusive. */
  endDate: string;
  /** Calendar days covered, both ends included. */
  dayCount: number;
  reason: string | null;
  requestedByName: string | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  createdAt: string;
}

export interface AbsenceBalance {
  employeeId: string;
  year: number;
  allowanceDays: number;
  usedDays: number;
  remainingDays: number;
}

export interface AbsenceInput {
  /** Omitted books the caller's own leave. */
  employeeId?: string | null;
  type: AbsenceType;
  startDate: string;
  endDate: string;
  reason?: string | null;
  /** Records it as already granted. Supervisors only. */
  approve?: boolean;
}

/**
 * A posting on the board, already clipped to the window being shown, so the
 * bar can be drawn without re-deriving where it starts.
 */
export interface ScheduleAssignment {
  id: string;
  projectId: string;
  projectName: string;
  from: string;
  to: string;
  /** True when the posting runs on past the end of the window. */
  continuesAfter: boolean;
}

export interface ScheduleAbsence {
  id: string;
  type: AbsenceType;
  from: string;
  to: string;
}

export interface ScheduleRow {
  employeeId: string;
  employeeName: string;
  position: string;
  assignments: ScheduleAssignment[];
  /** Only granted leave. A request nobody has answered is not on the board. */
  absences: ScheduleAbsence[];
}

export interface Schedule {
  from: string;
  to: string;
  rows: ScheduleRow[];
}

export const materialMovementKinds = ['In', 'Out', 'Adjustment'] as const;

export type MaterialMovementKind = (typeof materialMovementKinds)[number];

export const vehicleExpenseKinds = [
  'Fuel',
  'Service',
  'Repair',
  'Insurance',
  'Registration',
  'Other',
] as const;

export type VehicleExpenseKind = (typeof vehicleExpenseKinds)[number];

export interface EmployeeRate {
  id: string;
  employeeId: string;
  employeeName: string;
  hourlyRate: number;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while it is the rate in force. */
  endDate: string | null;
  note: string | null;
  setByName: string | null;
  createdAt: string;
}

export interface EmployeeRateInput {
  employeeId: string;
  hourlyRate: number;
  startDate?: string | null;
  endDate?: string | null;
  note?: string | null;
}

export interface MaterialMovement {
  id: string;
  materialId: string;
  materialName: string;
  unit: string;
  kind: MaterialMovementKind;
  /** Positive for a delivery or an issue; signed for a correction. */
  quantity: number;
  unitPrice: number | null;
  totalCost: number | null;
  projectId: string | null;
  projectName: string | null;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
  note: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface MaterialMovementInput {
  materialId: string;
  kind: MaterialMovementKind;
  quantity: number;
  unitPrice?: number | null;
  projectId?: string | null;
  occurredOn?: string | null;
  note?: string | null;
}

export interface VehicleExpense {
  id: string;
  vehicleId: string;
  vehicleName: string;
  kind: VehicleExpenseKind;
  amount: number;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
  /** Only ever set on a fill-up. */
  litres: number | null;
  pricePerLitre: number | null;
  odometerKm: number | null;
  supplier: string | null;
  note: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface VehicleExpenseInput {
  vehicleId: string;
  kind: VehicleExpenseKind;
  amount: number;
  occurredOn?: string | null;
  litres?: number | null;
  odometerKm?: number | null;
  supplier?: string | null;
  note?: string | null;
}

export const financeEntryKinds = [
  'WorkerPaymentHourly',
  'WorkerPaymentFixed',
  'WorkerPaymentDaily',
] as const;

export type FinanceEntryKind = (typeof financeEntryKinds)[number];

export interface FinanceEntry {
  id: string;
  employeeId: string;
  employeeName: string;
  kind: FinanceEntryKind;
  amount: number;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
  projectId: string | null;
  projectName: string | null;
  /** Only ever set for `WorkerPaymentHourly`. */
  hoursWorked: number | null;
  note: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface FinanceEntryInput {
  employeeId: string;
  kind: FinanceEntryKind;
  amount: number;
  occurredOn?: string | null;
  projectId?: string | null;
  hoursWorked?: number | null;
  note?: string | null;
}

export interface ProjectCostRow {
  projectId: string;
  projectName: string;
  /** Approved hours only. */
  labourMinutes: number;
  labourCost: number;
  /** Hours no rate covered — reported rather than treated as free. */
  unpricedMinutes: number;
  materialCost: number;
  total: number;
}

export interface ProjectCostReport {
  from: string;
  to: string;
  /** False when the caller may not see pay rates; every labour figure is zero. */
  includesLabour: boolean;
  rows: ProjectCostRow[];
  totalLabourCost: number;
  totalMaterialCost: number;
  total: number;
}

export interface VehicleCostRow {
  vehicleId: string;
  vehicleName: string;
  fuelCost: number;
  litres: number;
  serviceCost: number;
  otherCost: number;
  total: number;
  distanceKm: number | null;
  litresPer100Km: number | null;
}

export interface VehicleCostReport {
  from: string;
  to: string;
  rows: VehicleCostRow[];
  total: number;
  totalFuelCost: number;
  totalLitres: number;
}

export const notificationTypes = [
  'ProjectAssigned',
  'EmployeeAssigned',
  'VehicleAssigned',
  'ToolAssigned',
  'GeneralAnnouncement',
  'DocumentExpiring',
  'TaskAssigned',
  'DefectAssigned',
  'WorkItemDue',
] as const;

export type NotificationType = (typeof notificationTypes)[number];

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  body: string;
  /** Deep-link payload the mobile app uses; the panel only shows the text. */
  dataJson: string | null;
  isRead: boolean;
  readAt: string | null;
  requiresAcknowledgment: boolean;
  acknowledgedAt: string | null;
  createdAt: string;
}

export interface AnnouncementInput {
  title: string;
  body: string;
  /** Narrows the audience to one role. */
  role?: Role | null;
  /** Narrows it to the crew of one site. */
  projectId?: string | null;
  /** Narrows it to the members of one notification group. */
  groupId?: string | null;
  /** Recipients must confirm they saw it before doing anything else in the app. */
  requiresAcknowledgment?: boolean;
}

export interface NotificationGroup {
  id: string;
  name: string;
  memberCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface NotificationGroupDetail extends NotificationGroup {
  memberEmployeeIds: string[];
}

export interface NotificationGroupInput {
  name: string;
  employeeIds: string[];
}

export interface AuditChange {
  from: string | null;
  to: string | null;
}

/** One recorded change to a record — who, when, and which fields moved. */
export interface AuditEntry {
  id: number;
  occurredAt: string;
  action: string;
  entityName: string;
  entityId: string;
  userId: string | null;
  userEmail: string | null;
  userRole: string | null;
  ipAddress: string | null;
  changes: Record<string, AuditChange>;
}
