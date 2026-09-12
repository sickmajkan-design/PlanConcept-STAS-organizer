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
  /** Whether this account may see a customer's tax ID, registration number and VAT number. */
  canViewCustomerTaxDetails: boolean;
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

export const employeeTypes = ['Employee', 'Subcontractor'] as const;

export type EmployeeType = (typeof employeeTypes)[number];

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
  /** Direct employee or subcontractor. */
  type: EmployeeType;
  /** Every project this employee is currently posted to. Empty means unassigned. */
  currentProjectNames: string[];
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
  type: EmployeeType;
}

export type ProjectKind = 'Main' | 'Sub';

export interface Project {
  id: string;
  name: string;
  description: string | null;
  customerId: string | null;
  customerName: string | null;
  parentProjectId: string | null;
  parentProjectName: string | null;
  /** "Main" when this project has no parent, "Sub" otherwise. */
  kind: ProjectKind;
  /** How many sub-projects this project has. Always 0 for a Sub project. */
  subProjectCount: number;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  /** ISO 3166-1 alpha-2 (e.g. "BA") — which country's holiday calendar applies here. */
  countryCode: string | null;
  /** The site's expected daily clock-in time, in UTC (`HH:mm:ss`), if one is set. */
  shiftStartTime: string | null;
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
  customerId?: string | null;
  /** Set to make this a sub-project of another (Main) project. */
  parentProjectId?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  countryCode?: string | null;
  shiftStartTime?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  status: ProjectStatus;
  contractValue?: number | null;
}

export interface Customer {
  id: string;
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  note: string | null;
  /**
   * Null both when it was never set and when the signed-in user may not see
   * it — the two look the same on purpose, since the API never sends the
   * real value to someone without the grant in the first place.
   */
  taxId: string | null;
  registrationNumber: string | null;
  vatNumber: string | null;
  /** How many projects (Main and Sub together) currently belong to this customer. */
  projectCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CustomerInput {
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  note?: string | null;
  /** Only ever applied by the API when the caller is a SuperAdmin — sent by anyone else, silently ignored. */
  taxId?: string | null;
  registrationNumber?: string | null;
  vatNumber?: string | null;
}

/**
 * The platform's own company profile — a singleton, not a per-record
 * resource. Readable by any signed-in role; only a SuperAdmin may write it.
 */
export interface CompanySettings {
  name: string | null;
  address: string | null;
  taxId: string | null;
  registrationNumber: string | null;
  vatNumber: string | null;
  phone: string | null;
  email: string | null;
  weeklyReportsForwardEmail: string | null;
  hasLogo: boolean;
  updatedAt: string | null;
}

export interface CompanySettingsInput {
  name: string;
  address?: string | null;
  taxId?: string | null;
  registrationNumber?: string | null;
  vatNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  weeklyReportsForwardEmail?: string | null;
}

/**
 * What the pre-login screen and the app sidebar need — reached with no auth
 * token at all, so this carries nothing beyond name and whether a logo
 * exists.
 */
export interface PublicCompanyBranding {
  name: string | null;
  hasLogo: boolean;
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
  customerId: string | null;
  /** Null when the project has no customer set. */
  customerName: string | null;
  parentProjectId: string | null;
  parentProjectName: string | null;
  /** "Main" when this project has no parent, "Sub" otherwise. */
  kind: ProjectKind;
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
  'RentedOut',
] as const;

export type VehicleStatus = (typeof vehicleStatuses)[number];

export const fuelTypes = ['Petrol', 'Diesel', 'Electric', 'Hybrid', 'Lpg'] as const;

export type FuelType = (typeof fuelTypes)[number];

export const vehicleOwnershipTypes = ['Owned', 'Rented', 'Leased'] as const;

export type VehicleOwnershipType = (typeof vehicleOwnershipTypes)[number];

export interface Vehicle {
  id: string;
  brand: string;
  model: string;
  registrationNumber: string;
  vin: string | null;
  qrCode: string | null;
  /** Name of whatever GPS tracking platform this vehicle's tracker reports to. Free text. */
  gpsProvider: string | null;
  /** Deep link to this vehicle on its GPS provider's own site. Opened in a new tab. */
  gpsTrackingUrl: string | null;
  fuelType: FuelType;
  status: VehicleStatus;
  ownershipType: VehicleOwnershipType;
  /** The rate currently in force, when `ownershipType` is Rented or Leased. */
  currentRentalMonthlyAmount: number | null;
  currentRentalProvider: string | null;
  /** Set when this vehicle is currently loaned out to another company. */
  currentRentalOutRenterName: string | null;
  currentRentalOutDailyRate: number | null;
  currentRentalOutStartDate: string | null;
  /** Renter on the most recently closed rental-out loan. Null if never loaned out. */
  lastRentalOutRenterName: string | null;
  lastRentalOutEndDate: string | null;
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
  gpsProvider?: string | null;
  gpsTrackingUrl?: string | null;
  fuelType: FuelType;
  status: VehicleStatus;
  ownershipType: VehicleOwnershipType;
}

export const toolStatuses = [
  'Available',
  'Assigned',
  'UnderRepair',
  'Lost',
  'Retired',
  'RentedOut',
] as const;

export type ToolStatus = (typeof toolStatuses)[number];

export const toolOwnershipTypes = ['Owned', 'Rented', 'Leased'] as const;

export type ToolOwnershipType = (typeof toolOwnershipTypes)[number];

export interface Tool {
  id: string;
  name: string;
  category: string | null;
  serialNumber: string | null;
  qrCode: string | null;
  status: ToolStatus;
  ownershipType: ToolOwnershipType;
  /** The rate currently in force, when `ownershipType` is Rented or Leased. */
  currentRentalMonthlyAmount: number | null;
  currentRentalProvider: string | null;
  /** Set when this tool is currently loaned out to another company. */
  currentRentalOutRenterName: string | null;
  currentRentalOutDailyRate: number | null;
  currentRentalOutStartDate: string | null;
  /** Renter on the most recently closed rental-out loan. Null if never loaned out. */
  lastRentalOutRenterName: string | null;
  lastRentalOutEndDate: string | null;
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
  ownershipType: ToolOwnershipType;
}

export interface Material {
  id: string;
  name: string;
  unit: string;
  quantity: number;
  warehouse: string | null;
  /** Reference price per unit — a planning figure, not what any delivery actually cost. */
  unitPrice: number | null;
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
  unitPrice?: number | null;
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
  /** Days of warning before a document lapses. Null means the system default. Admin/SuperAdmin only. */
  documentExpiryReminderDays: number | null;
  /** Whether this account may see a customer's tax ID, registration number and VAT number. */
  canViewCustomerTaxDetails: boolean;
  createdAt: string;
}

export interface UserAccountInput {
  email: string;
  role: Role;
  employeeId?: string | null;
  documentExpiryReminderDays?: number | null;
  /** Only a SuperAdmin caller may actually change this — sent by anyone else, the API leaves it as it was. */
  canViewCustomerTaxDetails?: boolean;
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
  /** Null when the entry or the project has no coordinates to compare. */
  locationCorrect: boolean | null;
  /** Null when the project has no expected shift start time set. */
  timeCorrect: boolean | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  autoClosed: boolean;
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
  projectId: string | null;
  projectName: string | null;
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
  'VehicleExpense',
  'MaterialMovement',
  'EmployeeRate',
  'FinanceEntry',
  'ToolExpense',
  'VehicleRentalRate',
  'GeneralExpense',
  'Accommodation',
  'AccommodationRate',
  'ToolRentalRate',
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
  /** `YYYY-MM-DD` — a legal retention requirement, null if none applies. */
  retainUntil: string | null;
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
  /** `YYYY-MM-DD`. Set together with `proposedEndDate` while a change awaits confirmation. */
  proposedStartDate: string | null;
  /** `YYYY-MM-DD`, inclusive. */
  proposedEndDate: string | null;
  proposedReason: string | null;
  proposedByName: string | null;
  /** True when the employee proposed the change (so management must confirm it), false when management did (so the employee must). */
  proposedByEmployee: boolean;
  proposedAt: string | null;
  /** Derived from `proposedStartDate` — a convenience the DTO also computes. */
  hasPendingEdit: boolean;
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

export const toolExpenseKinds = ['Repair', 'Maintenance', 'Calibration', 'Other'] as const;

export type ToolExpenseKind = (typeof toolExpenseKinds)[number];

export interface ToolExpense {
  id: string;
  toolId: string;
  toolName: string;
  kind: ToolExpenseKind;
  amount: number;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
  supplier: string | null;
  note: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface ToolExpenseInput {
  toolId: string;
  kind: ToolExpenseKind;
  amount: number;
  occurredOn?: string | null;
  supplier?: string | null;
  note?: string | null;
}

export const vehicleExpenseKinds = [
  'Fuel',
  'Service',
  'Repair',
  'Insurance',
  'Registration',
  'Other',
] as const;

export type VehicleExpenseKind = (typeof vehicleExpenseKinds)[number];

export const rateTypes = ['Hourly', 'Daily'] as const;

export type RateType = (typeof rateTypes)[number];

export interface EmployeeRate {
  id: string;
  employeeId: string;
  employeeName: string;
  rateType: RateType;
  /** Set when `rateType` is Hourly; null otherwise. */
  hourlyRate: number | null;
  /** Cost per hour on a Saturday or Sunday. Null means no premium. Hourly only. */
  weekendHourlyRate: number | null;
  /** Cost per hour on a listed public holiday. Null means no premium. Hourly only. */
  holidayHourlyRate: number | null;
  /** Cost per hour for a shift tagged Overtime. Null means no premium. Hourly only. */
  overtimeHourlyRate: number | null;
  /** Cost per hour for a shift tagged Travel. Null means no premium. Hourly only. */
  travelHourlyRate: number | null;
  /** Set when `rateType` is Daily; null otherwise. */
  dailyRate: number | null;
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
  rateType: RateType;
  /** Required when `rateType` is Hourly. */
  hourlyRate?: number | null;
  weekendHourlyRate?: number | null;
  holidayHourlyRate?: number | null;
  overtimeHourlyRate?: number | null;
  travelHourlyRate?: number | null;
  /** Required when `rateType` is Daily. */
  dailyRate?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  note?: string | null;
}

export interface VehicleRentalRate {
  id: string;
  vehicleId: string;
  vehicleName: string;
  monthlyAmount: number;
  provider: string | null;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while it is the rate in force. */
  endDate: string | null;
  note: string | null;
  setByName: string | null;
  createdAt: string;
}

export interface VehicleRentalRateInput {
  vehicleId: string;
  monthlyAmount: number;
  provider?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  note?: string | null;
}

export interface ToolRentalRate {
  id: string;
  toolId: string;
  toolName: string;
  monthlyAmount: number;
  provider: string | null;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while it is the rate in force. */
  endDate: string | null;
  note: string | null;
  setByName: string | null;
  createdAt: string;
}

export interface ToolRentalRateInput {
  toolId: string;
  monthlyAmount: number;
  provider?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  note?: string | null;
}

/** The company's own vehicle loaned out to another company — the revenue direction, opposite `VehicleRentalRate`. */
export interface VehicleRentalOut {
  id: string;
  vehicleId: string;
  vehicleName: string;
  customerId: string | null;
  /** The linked customer's name when set, the free-text `renterName` otherwise. */
  renterDisplayName: string;
  renterName: string;
  dailyRate: number;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while the vehicle has not come back. */
  endDate: string | null;
  isOpen: boolean;
  note: string | null;
  setByName: string | null;
  createdAt: string;
}

export interface VehicleRentalOutInput {
  vehicleId: string;
  customerId?: string | null;
  renterName: string;
  dailyRate: number;
  startDate?: string | null;
  note?: string | null;
}

/** Narrow correction only — never touches `endDate` or the vehicle's status. Returning is a separate action. */
export interface UpdateVehicleRentalOutInput {
  customerId?: string | null;
  renterName: string;
  dailyRate: number;
  startDate: string;
  note?: string | null;
}

/** Fields on a `PUT .../return`. */
export interface ReturnRentalOutInput {
  endDate?: string | null;
}

export interface VehicleRentalOutSummary {
  count: number;
  openCount: number;
  totalValue: number;
}

/** The company's own tool loaned out to another company. See `VehicleRentalOut` for the full shape. */
export interface ToolRentalOut {
  id: string;
  toolId: string;
  toolName: string;
  customerId: string | null;
  renterDisplayName: string;
  renterName: string;
  dailyRate: number;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while the tool has not come back. */
  endDate: string | null;
  isOpen: boolean;
  note: string | null;
  setByName: string | null;
  createdAt: string;
}

export interface ToolRentalOutInput {
  toolId: string;
  customerId?: string | null;
  renterName: string;
  dailyRate: number;
  startDate?: string | null;
  note?: string | null;
}

/** Narrow correction only — never touches `endDate` or the tool's status. Returning is a separate action. */
export interface UpdateToolRentalOutInput {
  customerId?: string | null;
  renterName: string;
  dailyRate: number;
  startDate: string;
  note?: string | null;
}

export interface ToolRentalOutSummary {
  count: number;
  openCount: number;
  totalValue: number;
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
  invoiceNumber: string | null;
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
  invoiceNumber?: string | null;
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
  /** What was pumped (diesel, AdBlue, ...). Only ever set on a fill-up. */
  fuelProductType: string | null;
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
  fuelProductType?: string | null;
  supplier?: string | null;
  note?: string | null;
}

export interface FuelCard {
  id: string;
  vehicleId: string;
  vehicleName: string;
  provider: string;
  cardNumber: string;
  /** `YYYY-MM-DD`. */
  issuedOn: string | null;
  note: string | null;
  createdAt: string;
}

export interface FuelCardInput {
  vehicleId: string;
  provider: string;
  cardNumber: string;
  issuedOn?: string | null;
  note?: string | null;
}

/** Which 0-based column of the uploaded statement holds which field. */
export interface FuelImportColumnMapping {
  cardNumberColumn: number;
  occurredOnColumn: number;
  amountColumn: number;
  litresColumn: number;
  supplierColumn?: number | null;
  noteColumn?: number | null;
  odometerColumn?: number | null;
  fuelProductTypeColumn?: number | null;
}

export const fuelImportRowStatuses = [
  'Ready',
  'MissingCardNumber',
  'NoMatchingCard',
  'InvalidDate',
  'InvalidAmount',
  'InvalidLitres',
  'AlreadyImported',
] as const;

export type FuelImportRowStatus = (typeof fuelImportRowStatuses)[number];

export interface FuelImportPreviewRow {
  rowNumber: number;
  cardNumber: string | null;
  vehicleId: string | null;
  vehicleName: string | null;
  occurredOn: string | null;
  amount: number | null;
  litres: number | null;
  odometerKm: number | null;
  fuelProductType: string | null;
  supplier: string | null;
  note: string | null;
  status: FuelImportRowStatus;
  reason: string | null;
}

export interface FuelImportPreviewResult {
  totalRows: number;
  readyCount: number;
  problemCount: number;
  rows: FuelImportPreviewRow[];
}

export interface FuelImportSkippedRow {
  rowNumber: number;
  cardNumber: string | null;
  reason: string;
}

export interface FuelImportResult {
  totalRows: number;
  createdCount: number;
  skippedCount: number;
  skipped: FuelImportSkippedRow[];
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

export const generalExpenseCategories = [
  'Housing',
  'Bookkeeping',
  'Damage',
  'Complaint',
  'WorkerOther',
  'Other',
] as const;

export type GeneralExpenseCategory = (typeof generalExpenseCategories)[number];

export interface GeneralExpense {
  id: string;
  category: GeneralExpenseCategory;
  amount: number;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
  projectId: string | null;
  projectName: string | null;
  employeeId: string | null;
  employeeName: string | null;
  supplier: string | null;
  note: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface GeneralExpenseInput {
  category: GeneralExpenseCategory;
  amount: number;
  occurredOn?: string | null;
  projectId?: string | null;
  employeeId?: string | null;
  supplier?: string | null;
  note?: string | null;
}

export interface GeneralExpenseSummary {
  count: number;
  totalAmount: number;
}

export interface Accommodation {
  id: string;
  address: string;
  note: string | null;
  /** Set when a rate is currently in force. Null for one with no rate on file. */
  currentMonthlyAmount: number | null;
  currentProvider: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface AccommodationInput {
  address: string;
  note?: string | null;
}

export interface AccommodationRate {
  id: string;
  accommodationId: string;
  accommodationAddress: string;
  monthlyAmount: number;
  provider: string | null;
  /** `YYYY-MM-DD`. */
  startDate: string;
  /** `YYYY-MM-DD`, or null while it is the rate in force. */
  endDate: string | null;
  note: string | null;
  setByName: string | null;
  createdAt: string;
}

export interface AccommodationRateInput {
  accommodationId: string;
  monthlyAmount: number;
  provider?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  note?: string | null;
}

export interface AccommodationRateSummary {
  count: number;
  totalMonthlyAmount: number;
}

// ---- SuperAdmin ledger ("Evidencija") -------------------------------------

export const ledgerColumnDataTypes = ['Number', 'Currency', 'Text', 'Date'] as const;

export type LedgerColumnDataType = (typeof ledgerColumnDataTypes)[number];

export const ledgerColumnSourceMetrics = [
  'VehicleTotalCost',
  'ToolTotalCost',
  'MaterialCost',
] as const;

export type LedgerColumnSourceMetric = (typeof ledgerColumnSourceMetrics)[number];

export interface LedgerSummary {
  id: string;
  name: string;
  year: number;
  month: number;
  note: string | null;
  createdByName: string | null;
  createdAt: string;
}

export interface LedgerColumn {
  id: string;
  name: string;
  dataType: LedgerColumnDataType;
  /** Null for a manual/free-typed column (default); otherwise the metric its cells are computed from. */
  sourceMetric: LedgerColumnSourceMetric | null;
  sortOrder: number;
}

export interface LedgerCell {
  /** The real cell record's id — null for a computed (sourced) cell, which has no edit history. */
  id: string | null;
  columnId: string;
  value: string | null;
  /** Hex background color, or null for none. */
  colorTag: string | null;
  /** True when this value was computed from real platform data — never editable. */
  isComputed: boolean;
}

export interface LedgerRow {
  id: string;
  label: string;
  employeeId: string | null;
  employeeName: string | null;
  vehicleId: string | null;
  vehicleName: string | null;
  toolId: string | null;
  toolName: string | null;
  materialId: string | null;
  materialName: string | null;
  /** Set once this row was pushed through the real General Expense form. */
  promotedGeneralExpenseId: string | null;
  /** Set once this row was pushed through the real Accommodation-rate form. */
  promotedAccommodationRateId: string | null;
  sortOrder: number;
  /** Hex background color for the whole row, or null for none. */
  colorTag: string | null;
  cells: LedgerCell[];
}

export interface LedgerSection {
  id: string;
  name: string;
  projectId: string | null;
  projectName: string | null;
  sortOrder: number;
  /** Known even before this section's rows are loaded. */
  rowCount: number;
  /** Empty on the ledger shell — populated only once this section is opened and its rows are fetched. */
  rows: LedgerRow[];
}

export interface LedgerDetail {
  id: string;
  name: string;
  year: number;
  month: number;
  note: string | null;
  createdByName: string | null;
  createdAt: string;
  updatedAt: string | null;
  columns: LedgerColumn[];
  sections: LedgerSection[];
}

export interface CreateLedgerInput {
  name: string;
  year: number;
  month: number;
  note?: string | null;
  /** When set, duplicates that ledger's columns/sections/rows structure (blank cells) into the new one. */
  copyFromLedgerId?: string | null;
}

export interface UpdateLedgerInput {
  name: string;
  year: number;
  month: number;
  note?: string | null;
}

export interface LedgerColumnInput {
  name: string;
  dataType: LedgerColumnDataType;
  sourceMetric?: LedgerColumnSourceMetric | null;
}

/** One box of a ledger's month-summary panel, with its current computed value. */
export interface LedgerSummaryBox {
  id: string;
  label: string;
  sourceColumnId: string | null;
  sourceColumnName: string | null;
  manualValue: number | null;
  /** +1 adds this box to the net total, -1 subtracts it. */
  sign: 1 | -1;
  color: string | null;
  sortOrder: number;
  /** The live sum of `sourceColumnId` across the ledger, or `manualValue` when there's no source column. */
  value: number;
}

export interface LedgerSummaryPanel {
  boxes: LedgerSummaryBox[];
  /** Sum of every box's `value * sign`. */
  netTotal: number;
}

/** One row with no Employee/Vehicle/Tool/Material link. */
export interface LedgerUnlinkedRow {
  rowId: string;
  rowLabel: string;
  sectionId: string;
  sectionName: string;
}

/** One row already pushed through to a real General Expense or Accommodation rate. */
export interface LedgerPromotion {
  rowId: string;
  rowLabel: string;
  sectionName: string;
  target: 'GeneralExpense' | 'AccommodationRate';
  targetId: string;
  amount: number;
  /** `YYYY-MM-DD`. */
  occurredOn: string;
}

export interface LedgerSummaryBoxInput {
  label: string;
  sourceColumnId?: string | null;
  manualValue?: number | null;
  sign: 1 | -1;
  color?: string | null;
}

export interface LedgerSectionInput {
  name: string;
  projectId?: string | null;
}

export interface LedgerRowInput {
  label: string;
  employeeId?: string | null;
  vehicleId?: string | null;
  toolId?: string | null;
  materialId?: string | null;
}

export interface PromoteLedgerRowToGeneralExpenseInput {
  category: GeneralExpenseCategory;
  amount: number;
  occurredOn?: string | null;
  projectId?: string | null;
  employeeId?: string | null;
  supplier?: string | null;
  note?: string | null;
}

export interface PromoteLedgerRowToAccommodationRateInput {
  accommodationId: string;
  monthlyAmount: number;
  provider?: string | null;
  startDate?: string | null;
  endDate?: string | null;
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
  /**
   * Value of material currently assigned to this site (quantity × reference
   * price), whether or not it has been used yet. Not part of `total` — see
   * the field of the same name on `ProjectCostReport`.
   */
  materialsOnSiteValue: number;
  /**
   * Manually entered pay (FinanceEntry) attributed to this site over the
   * period. Not part of `total` — a manual entry is often a correction to
   * hours already clocked and priced there, so adding both would risk
   * counting the same work twice.
   */
  manualPayAmount: number;
  /**
   * General expenses (housing, bookkeeping, damage, and the like) tied to
   * this project over the period. Part of `total` — unlike `manualPayAmount`,
   * there's no other source this could double-count against.
   */
  generalExpenseCost: number;
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
  totalMaterialsOnSiteValue: number;
  totalManualPayAmount: number;
  totalGeneralExpenseCost: number;
  total: number;
}

export interface VehicleCostRow {
  vehicleId: string;
  vehicleName: string;
  fuelCost: number;
  litres: number;
  serviceCost: number;
  otherCost: number;
  rentalCost: number;
  total: number;
  revenue: number;
  profit: number;
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
  totalRentalCost: number;
  totalRevenue: number;
  totalProfit: number;
}

export interface ToolCostRow {
  toolId: string;
  toolName: string;
  repairCost: number;
  maintenanceCost: number;
  otherCost: number;
  rentalCost: number;
  total: number;
  revenue: number;
  profit: number;
}

export interface ToolCostReport {
  from: string;
  to: string;
  rows: ToolCostRow[];
  total: number;
  totalRentalCost: number;
  totalRevenue: number;
  totalProfit: number;
}

/** The totals for whatever filter is currently applied to the list, not just the page on screen. */
export interface EmployeeRateSummary {
  count: number;
  averageHourlyRate: number | null;
}

export interface VehicleRentalRateSummary {
  count: number;
  totalMonthlyAmount: number;
}

export interface ToolRentalRateSummary {
  count: number;
  totalMonthlyAmount: number;
}

export interface MaterialMovementSummary {
  count: number;
  totalCost: number;
}

export interface VehicleExpenseSummary {
  count: number;
  totalAmount: number;
  totalLitres: number;
}

export interface FinanceEntrySummary {
  count: number;
  totalAmount: number;
  totalHoursWorked: number;
}

export interface ToolExpenseSummary {
  count: number;
  totalAmount: number;
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
  'ShiftAutoClosed',
  'BulletinPosted',
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

/**
 * A date priced like a holiday, wherever a pay rate sets a holiday premium —
 * for a project whose own country matches this one.
 */
export interface PublicHoliday {
  id: string;
  /** `YYYY-MM-DD`. */
  date: string;
  name: string;
  /** ISO 3166-1 alpha-2, e.g. "BA". */
  countryCode: string;
}

export interface PublicHolidayInput {
  date: string;
  name: string;
  countryCode: string;
}

/** One holiday fetched from the internet for a chosen country/year, offered up for review before importing. */
export interface PublicHolidayCandidate {
  /** `YYYY-MM-DD`. */
  date: string;
  name: string;
  /** Already on the calendar — importing it again is a silent no-op. */
  alreadyOnCalendar: boolean;
}

export interface BulletinPost {
  id: string;
  title: string;
  body: string;
  createdByName: string;
  createdAt: string;
  viewCount: number;
  /** Whether the current user has already viewed this post. */
  viewed: boolean;
}

export interface BulletinPostInput {
  title: string;
  body: string;
}

export interface BulletinViewer {
  userId: string;
  userEmail: string;
  viewedAt: string;
}

export const scheduledReportTypes = [
  'TimeEntries',
  'ProjectCosts',
  'VehicleCosts',
  'MaterialMovements',
  'Absences',
  'FinanceEntries',
] as const;

export type ScheduledReportType = (typeof scheduledReportTypes)[number];

export const scheduledReportCadences = ['Weekly', 'Monthly'] as const;

export type ScheduledReportCadence = (typeof scheduledReportCadences)[number];

export const weekDays = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
] as const;

export type WeekDay = (typeof weekDays)[number];

export interface ScheduledReportSubscription {
  id: string;
  recipientEmail: string;
  reportType: ScheduledReportType;
  cadence: ScheduledReportCadence;
  dayOfWeek: WeekDay | null;
  dayOfMonth: number | null;
  language: string;
  nextRunAtUtc: string;
  createdByEmail: string;
}

export interface ScheduledReportSubscriptionInput {
  recipientEmail?: string | null;
  reportType: ScheduledReportType;
  cadence: ScheduledReportCadence;
  dayOfWeek?: WeekDay | null;
  dayOfMonth?: number | null;
  language?: string | null;
}

export const weeklyReportTypes = ['SignedHours', 'Aufmass', 'Other'] as const;

export type WeeklyReportType = (typeof weeklyReportTypes)[number];

export const weeklyReportStatuses = ['Submitted', 'Processed'] as const;

export type WeeklyReportStatus = (typeof weeklyReportStatuses)[number];

export interface WeeklySiteReport {
  id: string;
  projectId: string;
  projectName: string;
  submittedByEmployeeId: string;
  submittedByEmployeeName: string;
  isoYear: number;
  isoWeek: number;
  type: WeeklyReportType;
  quantity: number | null;
  note: string | null;
  fileName: string;
  status: WeeklyReportStatus;
  processedAt: string | null;
  processedByEmail: string | null;
  createdAt: string;
}

export interface WeeklySiteReportListQuery extends ListQuery {
  projectId?: string;
  isoYear?: number;
  isoWeek?: number;
  status?: WeeklyReportStatus | '';
}

export interface ReportableProject {
  id: string;
  name: string;
}
