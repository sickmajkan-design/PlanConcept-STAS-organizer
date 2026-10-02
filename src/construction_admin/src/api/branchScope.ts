/**
 * The business unit (poslovna jedinica) the header's switcher has chosen, for code that is not a
 * React hook — a download started from a click reads it here when it builds its request.
 *
 * Written only by `BranchFilterProvider`; nothing else should set it.
 */
let current: string | undefined;

export const branchScope = {
  get id(): string | undefined {
    return current;
  },
  set(id: string | undefined): void {
    current = id;
  },
};
