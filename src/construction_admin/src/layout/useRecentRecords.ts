import { useEffect } from 'react';

import { useAuth } from '../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../hooks/userScopedStorage';

const RECENT_KEY = 'nav.recentRecords';
const MAX_RECENT = 6;

export interface RecentRecord {
  path: string;
  label: string;
}

export function readRecentRecords(scope: string | null): RecentRecord[] {
  return readScoped<RecentRecord[]>(scope, RECENT_KEY, []);
}

/**
 * Only the records under a page the current role can reach.
 *
 * The nav is the one place that already says what a role may open, so it is
 * the test: a record belongs to a page when the page's path is a prefix of the
 * record's. Anything else — an employee the account viewed as an admin and may
 * not open as a foreman — is left out of what is shown, not deleted, so a later
 * promotion brings it back.
 */
export function filterRecentForRole(
  records: RecentRecord[],
  allowedPagePaths: string[],
): RecentRecord[] {
  return records.filter((record) =>
    allowedPagePaths.some(
      (page) => page !== '/' && (record.path === page || record.path.startsWith(`${page}/`)),
    ),
  );
}

/**
 * Call from a detail page once its record has loaded, to add it to the
 * global "recently viewed" list shown in the command palette.
 *
 * Stored per signed-in account and role — see `userScopedStorage` for why.
 *
 * Reads/writes localStorage directly rather than shared React state: the
 * palette lives in `AppLayout`, an entirely different part of the tree, and
 * re-reads the list itself on every route change — there is no render this
 * hook needs to trigger, only a value that needs to be there by the time the
 * palette next opens.
 */
export function useRecordVisit(path: string, label: string | undefined): void {
  const { user } = useAuth();
  const scope = storageScope(user);

  useEffect(() => {
    if (!label || !scope) return;
    const existing = readRecentRecords(scope).filter((r) => r.path !== path);
    writeScoped(scope, RECENT_KEY, [{ path, label }, ...existing].slice(0, MAX_RECENT));
  }, [path, label, scope]);
}
