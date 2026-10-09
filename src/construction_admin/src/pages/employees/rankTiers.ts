import { organizationRanks, type OrganizationHierarchyNode, type OrganizationRank, type Role } from '../../api/types';

/**
 * Where somebody with no rank picked is placed, going by their login role.
 *
 * Only where that is unambiguous. A worker, a foreman and a project manager
 * each map to the one rank that carries their name; an administrator or a
 * super-admin could be anything from the owner to the office clerk, and
 * guessing "Director" for them would put a wrong title on the chart. Those
 * stay unplaced until somebody picks.
 */
export function rankFromRole(role: Role | null): OrganizationRank | null {
  switch (role) {
    case null:
    case 'Worker':
      return 'Worker';
    case 'Foreman':
      return 'Foreman';
    case 'ProjectManager':
      return 'ProjectManager';
    default:
      return null;
  }
}

/** People sorted into the rank tiers, highest first, with those who cannot be placed kept apart. */
export function groupByRank(people: readonly OrganizationHierarchyNode[]): {
  tiers: { rank: OrganizationRank; index: number; people: OrganizationHierarchyNode[] }[];
  unplaced: OrganizationHierarchyNode[];
} {
  const byRank = new Map<OrganizationRank, OrganizationHierarchyNode[]>();
  const unplaced: OrganizationHierarchyNode[] = [];

  for (const node of people) {
    const rank = node.rank ?? rankFromRole(node.role);

    if (!rank) {
      unplaced.push(node);
      continue;
    }

    byRank.set(rank, [...(byRank.get(rank) ?? []), node]);
  }

  return {
    tiers: organizationRanks
      .map((rank, index) => ({ rank, index, people: byRank.get(rank) ?? [] }))
      .filter((tier) => tier.people.length > 0),
    unplaced,
  };
}
