import { describe, expect, it } from 'vitest';

import { paths } from '../routes/paths';
import { signedIn } from '../test/renderScreen';
import { buildNavEntries, flattenNavEntries, isNavGroup } from './navConfig';
import { withGroupTotals } from './navBadgeTotals';
import { BADGED_PATHS } from './useNavBadgeCounts';

/**
 * Whenever the menu changes, its badges have to change with it. These are the checks that fail when they do not:
 * a badge left on a page that moved away, a group that does not add up to what is in it, a count counted twice.
 */
const t = ((key: string) => key) as Parameters<typeof buildNavEntries>[1];
const superAdmin = signedIn('SuperAdmin');
const entries = buildNavEntries(superAdmin, t);
const items = flattenNavEntries(entries);

describe('menu badges', () => {
  it('every item that has a count behind it is still in the menu', () => {
    const present = new Set(items.map((item) => item.path));

    expect(BADGED_PATHS.filter((path) => !present.has(path))).toEqual([]);
  });

  it('a group shows the sum of what is in it, whichever group it is in', () => {
    const counts = Object.fromEntries(BADGED_PATHS.map((path) => [path, 1]));
    const totals = withGroupTotals(entries, counts);

    for (const entry of entries.filter(isNavGroup)) {
      const expected = entry.items.filter((item) => !item.inTabs && counts[item.path]).length;

      expect(totals[entry.key], `group ${entry.key}`).toBe(expected);
    }
  });

  it('Documents, as a group of their own, carry the documents and the requests', () => {
    const counts = { [paths.expiringDocuments]: 2, [paths.documentRequests]: 3 };
    const totals = withGroupTotals(entries, counts);
    const group = entries.filter(isNavGroup).find((entry) => entry.key === 'documents');

    expect(group?.items.map((item) => item.path)).toEqual(
      expect.arrayContaining([paths.expiringDocuments, paths.documentRequests]),
    );
    expect(totals.documents).toBe(5);
    // And Administration, which they left, no longer carries them.
    expect(totals.admin).toBe(0);
  });

  it('a cost tab and its hub are not counted twice in Finance', () => {
    const hub = items.find((item) => item.alsoActiveOn?.length);
    expect(hub).toBeDefined();

    const tab = items.find((item) => item.inTabs);
    expect(tab).toBeDefined();

    const totals = withGroupTotals(entries, { [hub!.path]: 4, [tab!.path]: 4 });
    const costs = entries.filter(isNavGroup).find((entry) => entry.items.some((item) => item.path === hub!.path));

    expect(totals[costs!.key]).toBe(4);
  });
});
