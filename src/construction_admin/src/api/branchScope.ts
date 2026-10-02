/**
 * The business unit (poslovna jedinica) the header's switcher has chosen, for code that is not a
 * React hook — a download started from a click reads it here when it builds its request.
 *
 * Written only by `BranchFilterProvider`; nothing else should set it.
 */
let current: string | undefined;
let currentBasis: 'Site' | 'Employer' = 'Site';

export const branchScope = {
  get id(): string | undefined {
    return current;
  },
  /** Whose hours and pay count for the unit: those worked on its sites, or those of the people it employs. */
  get basis(): 'Site' | 'Employer' {
    return currentBasis;
  },
  set(id: string | undefined, basis: 'Site' | 'Employer' = 'Site'): void {
    current = id;
    currentBasis = basis;
  },
};
