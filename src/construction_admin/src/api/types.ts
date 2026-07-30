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
