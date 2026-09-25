import { anonymousRequest, request } from './client';
import { idempotencyHeaders } from './idempotency';

/** One line of an employee spreadsheet, already normalised by the client. */
export interface ImportEmployeeRow {
  /** 1-based line in the source file, so an error can say where. */
  line: number;
  employeeNumber?: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  email?: string;
  address?: string;
  position?: string;
  /** `YYYY-MM-DD`. */
  employmentDate?: string;
  /** `YYYY-MM-DD`. */
  dateOfBirth?: string;
  type?: 'Employee' | 'Subcontractor';
}

export type ImportDuplicateHandling = 'Skip' | 'Update';
export type ImportRowOutcome = 'Create' | 'Update' | 'Skip' | 'Error';

export interface ImportRowResult {
  line: number;
  outcome: ImportRowOutcome;
  fullName: string;
  employeeNumber: string | null;
  message: string | null;
}

export interface ImportEmployeesResult {
  dryRun: boolean;
  created: number;
  updated: number;
  skipped: number;
  errors: number;
  rows: ImportRowResult[];
}

export interface EmployeeInvitation {
  employeeId: string;
  employeeName: string;
  /** Shown once. Never stored in readable form on the server. */
  token: string;
  expiresAt: string;
  suggestedEmail: string | null;
}

export interface InvitationPreview {
  firstName: string;
  suggestedEmail: string | null;
  expiresAt: string;
}

/** What is still missing before the system does its job. */
export type SetupChecklistKey =
  | 'companyProfile'
  | 'noEmployees'
  | 'noProjects'
  | 'employeesWithoutAccount'
  | 'employeesWithoutProject'
  | 'projectsWithoutLocation'
  | 'holidaysMissing'
  | 'emailNotConfigured'
  | 'pushNotConfigured';

export interface SetupChecklistItem {
  key: SetupChecklistKey;
  count: number;
}

export const onboardingApi = {
  importEmployees: (
    input: { rows: ImportEmployeeRow[]; dryRun: boolean; onDuplicate: ImportDuplicateHandling },
    idempotencyKey?: string,
  ) =>
    request<ImportEmployeesResult>({
      method: 'POST',
      url: '/api/v1/employees/import',
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  createInvitation: (employeeId: string, idempotencyKey?: string) =>
    request<EmployeeInvitation>({
      method: 'POST',
      url: '/api/v1/invitations',
      data: { employeeId },
      headers: idempotencyHeaders(idempotencyKey),
    }),

  // The next two are opened by someone who has no account yet.
  getInvitation: (token: string) =>
    anonymousRequest<InvitationPreview>({
      method: 'GET',
      url: `/api/v1/invitations/${encodeURIComponent(token)}`,
    }),

  acceptInvitation: (token: string, email: string, password: string) =>
    anonymousRequest<{ email: string }>({
      method: 'POST',
      url: `/api/v1/invitations/${encodeURIComponent(token)}/accept`,
      data: { email: email.trim(), password },
    }),

  setupChecklist: () =>
    request<{ items: SetupChecklistItem[] }>({ method: 'GET', url: '/api/v1/setup/checklist' }),
};
