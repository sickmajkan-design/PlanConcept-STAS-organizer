import { isNavGroup, type NavEntry } from './navConfig';

/**
 * The badge of a rail group is the sum of its items' badges, worked out from the menu itself.
 *
 * It used to be a number kept by hand under the group's key, which is how moving Documents into a group of
 * their own left that group with no badge while its first item still had one. Now there is nothing to keep in
 * step: whatever is in a group, and whatever has a count of its own, adds up.
 *
 * Items reached through a tab strip (`inTabs`) are left out of the sum: their hub item carries the number,
 * so counting both would count the same thing twice.
 */
export function withGroupTotals(
  entries: readonly NavEntry[],
  itemCounts: Readonly<Record<string, number>>,
): Record<string, number> {
  const totals: Record<string, number> = { ...itemCounts };

  for (const entry of entries) {
    if (!isNavGroup(entry)) continue;

    totals[entry.key] = entry.items
      .filter((item) => !item.inTabs)
      .reduce((sum, item) => sum + (itemCounts[item.path] ?? 0), 0);
  }

  return totals;
}
