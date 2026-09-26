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

/**
 * Whether the account may see the company's amounts — and so the pages and
 * dashboard widgets that show them. The server reads the grant from the
 * database on every request; this only decides what is worth offering.
 */
export function canViewFinance(user: User | null | undefined): boolean {
  return user?.financeAccess === 'Full';
}

/**
 * Whether the account may see statistics about the company's money — the
 * percentages, not the amounts. Anyone who may see the amounts may see these too.
 */
export function canViewFinanceStatistics(user: User | null | undefined): boolean {
  return user?.financeAccess === 'StatisticsOnly' || user?.financeAccess === 'Full';
}

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
 * Roles the API lets approve or send back a time entry (its
 * `ProjectManagerAndAbove` policy on `/time-entries/{id}/review`), and never
 * their own — that half is a same-set check the caller does against the
 * entry's `employeeId`, not this helper.
 */
export function canReviewTimeEntries(user: User | null | undefined): boolean {
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
 * Roles the API lets approve or send back a recorded cost
 * (`CostRules.CanReviewSpending`) — one tier above those who record one. The
 * other half of the rule, that nobody reviews a cost they recorded themselves,
 * is a comparison the caller makes against the cost's author.
 */
export function canReviewSpending(user: User | null | undefined): boolean {
  return !!user && ASSIGNMENT_ROLES.has(user.role);
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

/**
 * Roles that get the configurable home dashboard (the API's `AdminAndAbove`
 * policy on `/dashboard-layout`). Every other role keeps the plain static
 * home page — a foreman or worker has no fleet of widgets worth arranging.
 */
export function canConfigureDashboard(user: User | null | undefined): boolean {
  return canAdministerAccounts(user);
}

/**
 * Only the SuperAdmin role (the API's `SuperAdminOnly` policy) — used for the
 * personal ledger ("Evidencija"), which is deliberately not shared with any
 * other role, not even Admin.
 */
export function isSuperAdmin(user: User | null | undefined): boolean {
  return !!user && user.role === 'SuperAdmin';
}

/** Who runs the orders for articles: orders them, sends them, declines them. Mirrors ArticleOrderRules on the API. */
export function canManageArticleOrders(user: User | null | undefined): boolean {
  return !!user && (user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager');
}
