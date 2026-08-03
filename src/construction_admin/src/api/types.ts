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
  photoUrl: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface EmployeeProjectAssignment {
  projectId: string;
  projectName: string;
  projectStatus: ProjectStatus;
  assignedAt: string;
}

export interface EmployeeDetail extends Employee {
  hasUserAccount: boolean;
  projects: EmployeeProjectAssignment[];
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
  photoUrl?: string | null;
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
  assignedAt: string;
}

export interface ProjectDetail extends Project {
  employees: ProjectEmployee[];
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
  fuelType: FuelType;
  status: VehicleStatus;
  assignedEmployeeId: string | null;
  assignedEmployeeName: string | null;
  assignedEmployeeNumber: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface VehicleInput {
  brand: string;
  model: string;
  registrationNumber: string;
  vin?: string | null;
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
 * `/api/auth/me`. Keeping them apart stops "the current user" and "a row in
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
