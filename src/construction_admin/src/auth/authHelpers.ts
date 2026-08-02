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
