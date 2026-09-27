import { describe, expect, it } from 'vitest';

import type { Role, User } from '../api/types';
import { roles } from '../api/types';
import {
  canAdministerAccounts,
  canBeGrantedFinance,
  canReviewAbsences,
  canSeeLabourCost,
  canSeeSpending,
  canViewDirectory,
  displayName,
} from './authHelpers';

const userWith = (role: Role, rest: Partial<User> = {}): User => ({
  id: '1',
  email: 'operator@example.test',
  role,
  employeeId: null,
  firstName: null,
  lastName: null,
  lastLoginAt: null,
  canViewCustomerTaxDetails: false,
  financeAccess: 'None',
  ...rest,
});

/**
 * These only hide screens — the API refuses the calls regardless. What they
 * must not do is drift from it: a gate that is too tight hides a screen from
 * somebody entitled to it, which reads as a broken deployment, and one that is
 * too loose sends them to a page that answers 403.
 */
describe('role gates', () => {
  it('serve the directory to everyone but a worker', () => {
    expect(canViewDirectory(userWith('SuperAdmin'))).toBe(true);
    expect(canViewDirectory(userWith('Admin'))).toBe(true);
    expect(canViewDirectory(userWith('ProjectManager'))).toBe(true);
    expect(canViewDirectory(userWith('Foreman'))).toBe(true);
    expect(canViewDirectory(userWith('Worker'))).toBe(false);
  });

  it('keep account administration to Admin and above', () => {
    expect(canAdministerAccounts(userWith('SuperAdmin'))).toBe(true);
    expect(canAdministerAccounts(userWith('Admin'))).toBe(true);
    expect(canAdministerAccounts(userWith('ProjectManager'))).toBe(false);
    expect(canAdministerAccounts(userWith('Foreman'))).toBe(false);
    expect(canAdministerAccounts(userWith('Worker'))).toBe(false);
  });

  it('keep pay rates from a foreman', () => {
    // Tighter than the directory on purpose: a rate is effectively somebody's
    // pay, and a foreman running a site has no business with it.
    expect(canSeeLabourCost(userWith('ProjectManager', { financeAccess: 'Full' }))).toBe(true);
    expect(canSeeLabourCost(userWith('Foreman', { financeAccess: 'Full' }))).toBe(false);
  });

  it('keep pay from anyone the Super Admin has not granted the figures', () => {
    // The customer's rule: nothing in euro except for the Super Admin and
    // whoever they choose, however senior the role.
    expect(canSeeLabourCost(userWith('Admin'))).toBe(false);
    expect(canSeeLabourCost(userWith('ProjectManager', { financeAccess: 'StatisticsOnly' }))).toBe(false);
    expect(canSeeLabourCost(userWith('SuperAdmin', { financeAccess: 'Full' }))).toBe(true);
  });

  it('let management, and only management, answer leave requests', () => {
    expect(canReviewAbsences(userWith('SuperAdmin'))).toBe(true);
    expect(canReviewAbsences(userWith('Admin'))).toBe(true);
    expect(canReviewAbsences(userWith('ProjectManager'))).toBe(false);
    expect(canReviewAbsences(userWith('Foreman'))).toBe(false);
  });

  it('never offer the figures grant for a worker or customer', () => {
    expect(canBeGrantedFinance('Foreman')).toBe(true);
    expect(canBeGrantedFinance('Worker')).toBe(false);
    expect(canBeGrantedFinance('Customer')).toBe(false);
  });

  it('keep what was spent from a foreman until the Super Admin grants the figures', () => {
    // Recording stays wide (the person who signed for the delivery knows it
    // arrived); reading the euro back needs the grant.
    expect(canSeeSpending(userWith('Foreman'))).toBe(false);
    expect(canSeeSpending(userWith('Foreman', { financeAccess: 'Full' }))).toBe(true);
    expect(canSeeSpending(userWith('Worker', { financeAccess: 'Full' }))).toBe(false);
  });

  it('refuse everything when nobody is signed in', () => {
    for (const gate of [
      canViewDirectory,
      canAdministerAccounts,
      canSeeLabourCost,
      canSeeSpending,
    ]) {
      expect(gate(null)).toBe(false);
      expect(gate(undefined)).toBe(false);
    }
  });

  it('answer for every role the API can send, not only the ones listed here', () => {
    // A role added to the API and not to a gate would otherwise reach
    // production as `undefined`, which is falsy — a screen quietly missing
    // rather than an error anybody notices.
    for (const role of roles) {
      expect(typeof canViewDirectory(userWith(role))).toBe('boolean');
      expect(typeof canAdministerAccounts(userWith(role))).toBe('boolean');
      expect(typeof canSeeLabourCost(userWith(role))).toBe('boolean');
    }
  });
});

describe('displayName', () => {
  it('prefers the person s name', () => {
    expect(
      displayName(userWith('Admin', { firstName: 'Ivan', lastName: 'Horvat' })),
    ).toBe('Ivan Horvat');
  });

  it('uses whichever half of the name it has', () => {
    expect(displayName(userWith('Admin', { firstName: 'Ivan' }))).toBe('Ivan');
    expect(displayName(userWith('Admin', { lastName: 'Horvat' }))).toBe('Horvat');
  });

  it('falls back to the email rather than showing a blank', () => {
    expect(displayName(userWith('Admin'))).toBe('operator@example.test');
    expect(
      displayName(userWith('Admin', { firstName: '  ', lastName: '  ' })),
    ).toBe('operator@example.test');
  });
});
