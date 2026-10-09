import type { Branch } from '../../api/types';

/** Levels a unit may be nested to, as on the server (`Branch.MaxDepth`). */
export const MAX_BRANCH_DEPTH = 3;

export interface BranchNode {
  branch: Branch;
  /** 1 for a unit directly under the company. */
  depth: number;
  children: BranchNode[];
  /** Everything under this unit, at any depth. */
  descendantIds: string[];
  /** The unit's own figures plus those of everything under it. */
  totals: { employees: number; projects: number };
}

const byOrder = (a: Branch, b: Branch) =>
  Number(b.isActive) - Number(a.isActive) || a.name.localeCompare(b.name);

/** The units as a tree, active ones first and then by name. A unit whose parent is missing is shown at the top. */
export function buildBranchTree(branches: readonly Branch[]): BranchNode[] {
  const ids = new Set(branches.map((b) => b.id));
  const kids = new Map<string | null, Branch[]>();

  for (const branch of branches) {
    const parent = branch.parentBranchId && ids.has(branch.parentBranchId) ? branch.parentBranchId : null;
    kids.set(parent, [...(kids.get(parent) ?? []), branch]);
  }

  const build = (branch: Branch, depth: number, seen: Set<string>): BranchNode => {
    const next = new Set(seen).add(branch.id);
    const children = (kids.get(branch.id) ?? [])
      .filter((child) => !next.has(child.id))
      .sort(byOrder)
      .map((child) => build(child, depth + 1, next));

    return {
      branch,
      depth,
      children,
      descendantIds: children.flatMap((c) => [c.branch.id, ...c.descendantIds]),
      totals: {
        employees: branch.employeeCount + children.reduce((sum, c) => sum + c.totals.employees, 0),
        projects: branch.projectCount + children.reduce((sum, c) => sum + c.totals.projects, 0),
      },
    };
  };

  return (kids.get(null) ?? []).sort(byOrder).map((branch) => build(branch, 1, new Set()));
}

/** The tree in reading order, parents before their units, for a list that shows depth by indentation. */
export function flattenBranchTree(nodes: readonly BranchNode[]): BranchNode[] {
  return nodes.flatMap((node) => [node, ...flattenBranchTree(node.children)]);
}

/** How many levels lie under a node (0 for a unit with none). */
function heightBelow(node: BranchNode): number {
  return node.children.length === 0 ? 0 : 1 + Math.max(...node.children.map(heightBelow));
}

/**
 * The units a unit may be placed under: not itself, not anything already under it, and not so deep
 * that it (with what it carries) would pass the nesting limit. Pass no id for a unit being added.
 */
export function allowedParents(branches: readonly Branch[], branchId: string | null): Branch[] {
  const flat = flattenBranchTree(buildBranchTree(branches));
  const self = branchId ? flat.find((n) => n.branch.id === branchId) : undefined;
  const blocked = new Set<string>(self ? [self.branch.id, ...self.descendantIds] : []);
  const carried = self ? heightBelow(self) : 0;

  return flat
    .filter((n) => !blocked.has(n.branch.id) && n.depth + 1 + carried <= MAX_BRANCH_DEPTH)
    .map((n) => n.branch);
}
