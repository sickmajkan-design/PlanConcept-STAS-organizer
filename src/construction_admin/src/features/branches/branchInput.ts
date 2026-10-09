import type { Branch, BranchInput } from '../../api/types';

/**
 * A unit as the full payload the API expects when it is saved. A save replaces every field, so a
 * quick change (a new head, a new parent) sends everything else back as it is.
 */
export function branchToInput(branch: Branch, changes: Partial<BranchInput> = {}): BranchInput {
  return {
    name: branch.name,
    color: branch.color,
    isActive: branch.isActive,
    kind: branch.kind,
    legalName: branch.legalName,
    address: branch.address,
    city: branch.city,
    postalCode: branch.postalCode,
    countryCode: branch.countryCode,
    taxId: branch.taxId,
    registrationNumber: branch.registrationNumber,
    vatNumber: branch.vatNumber,
    ownerName: branch.ownerName,
    contactPerson: branch.contactPerson,
    phone: branch.phone,
    email: branch.email,
    note: branch.note,
    parentBranchId: branch.parentBranchId,
    headEmployeeId: branch.headEmployeeId,
    ...changes,
  };
}
