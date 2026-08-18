import type { User } from '../api/types';

/** Roles the API serves the employee/project directories to. */
const DIRECTORY_ROLES = new Set(['SuperAdmin', 'Admin', 'ProjectManager', 'Foreman']);

export function canViewDirectory(user: User | null | undefined): boolean {
  return !!user && DIRECTORY_ROLES.has(user.role);
}

export function displayName(user: User): string {
  const name = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
  return name.length > 0 ? name : user.email;
}

/** Roles the API lets administer accounts (its AdminAndAbove policy). */
const ACCOUNT_ADMIN_ROLES = new Set(['SuperAdmin', 'Admin']);

export function canAdministerAccounts(user: User | null | undefined): boolean {
  return !!user && ACCOUNT_ADMIN_ROLES.has(user.role);
}

/**
 * Roles the API lets move people between sites (its `ProjectManagerAndAbove`
 * policy on the assignment endpoints).
 *
 * Tighter than {@link canViewDirectory}: a foreman reads the roster, but
 * staffing a site is a call made above them.
 */
const ASSIGNMENT_ROLES = new Set(['SuperAdmin', 'Admin', 'ProjectManager']);

export function canManageAssignments(user: User | null | undefined): boolean {
  return !!user && ASSIGNMENT_ROLES.has(user.role);
}

/**
 * Roles the API shows pay rates to (its `CostRules.CanSeeLabourCost`).
 *
 * Deliberately tighter than {@link canViewDirectory}: a rate is effectively
 * somebody's pay, and a foreman running a site has no business with it. This
 * only hides the screens — the API refuses the calls regardless, and returns
 * the labour half of a cost report as zero rather than refusing the report.
 */
const LABOUR_COST_ROLES = new Set(['SuperAdmin', 'Admin', 'ProjectManager']);

export function canSeeLabourCost(user: User | null | undefined): boolean {
  return !!user && LABOUR_COST_ROLES.has(user.role);
}

/**
 * Roles that may record and read spending (`CostRules.CanRecordSpending`).
 *
 * Wide on purpose: the person who signed for the delivery is the one who knows
 * it arrived, and figures nobody records are worth nothing.
 */
export function canSeeSpending(user: User | null | undefined): boolean {
  return canViewDirectory(user);
}
