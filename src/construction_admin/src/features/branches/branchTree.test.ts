import { describe, expect, it } from 'vitest';

import type { Branch } from '../../api/types';
import { allowedParents, buildBranchTree, flattenBranchTree } from './branchTree';

function unit(id: string, parent: string | null = null, extra: Partial<Branch> = {}): Branch {
  return {
    id,
    name: id,
    color: '#3457D5',
    isActive: true,
    kind: 'LegalEntity',
    projectCount: 1,
    employeeCount: 10,
    parentBranchId: parent,
    headEmployeeId: null,
    headEmployeeName: null,
    legalName: null,
    address: null,
    city: null,
    postalCode: null,
    countryCode: null,
    taxId: null,
    registrationNumber: null,
    vatNumber: null,
    ownerName: null,
    contactPerson: null,
    phone: null,
    email: null,
    note: null,
    ...extra,
  };
}

describe('branchTree', () => {
  const units = [unit('office', 'branch'), unit('branch', 'region'), unit('region'), unit('other')];

  it('puts each unit under its parent and adds up what lies under it', () => {
    const tree = buildBranchTree(units);
    const region = tree.find((n) => n.branch.id === 'region')!;

    expect(tree.map((n) => n.branch.id)).toEqual(['other', 'region']);
    expect(region.children[0].children[0].branch.id).toBe('office');
    expect(region.descendantIds).toEqual(['branch', 'office']);
    expect(region.totals).toEqual({ employees: 30, projects: 3 });
    expect(region.children[0].children[0].depth).toBe(3);
  });

  it('lists parents before their units', () => {
    expect(flattenBranchTree(buildBranchTree(units)).map((n) => n.branch.id)).toEqual([
      'other',
      'region',
      'branch',
      'office',
    ]);
  });

  it('shows a unit whose parent is missing at the top instead of losing it', () => {
    expect(buildBranchTree([unit('lost', 'gone')]).map((n) => n.branch.id)).toEqual(['lost']);
  });

  it('offers no parent that would make a loop or pass the depth limit', () => {
    // The region carries two levels, so it can only go to the top; "other" may take a lone unit.
    expect(allowedParents(units, 'region').map((b) => b.id)).toEqual([]);
    expect(allowedParents(units, 'other').map((b) => b.id)).toEqual(['region', 'branch']);
    // A new unit may go under the region or the branch, but not under the office (that would be level 4).
    expect(allowedParents(units, null).map((b) => b.id)).toEqual(['other', 'region', 'branch']);
  });
});
