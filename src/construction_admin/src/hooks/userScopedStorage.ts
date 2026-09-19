import type { User } from '../api/types';

/**
 * Per-person browser storage for the things the panel remembers on screen —
 * pinned pages, recently viewed records, saved list filters, which nav groups
 * are open.
 *
 * These used to live under one fixed key per browser. On a shared office
 * machine that meant the next person to sign in inherited the last one's
 * recent records — real employee and project names, possibly ones their own
 * role is not allowed to open — and their pinned pages.
 *
 * The scope is the account *and* its role. Role is in there so that a demotion
 * does not leave behind a list of records the account could see yesterday and
 * may not today; changing it simply starts an empty list.
 *
 * Nothing is read or written without a signed-in user, and there is no
 * fallback to an unscoped key: a fallback is exactly the leak this exists to
 * close.
 */
export function storageScope(user: Pick<User, 'id' | 'role'> | null | undefined): string | null {
  return user ? `${user.id}.${user.role}` : null;
}

const PREFIX = 'u.';

function fullKey(scope: string, base: string): string {
  return `${PREFIX}${scope}.${base}`;
}

export function readScoped<T>(scope: string | null, base: string, fallback: T): T {
  if (!scope) return fallback;

  try {
    const raw = window.localStorage.getItem(fullKey(scope, base));
    return raw ? (JSON.parse(raw) as T) : fallback;
  } catch {
    return fallback;
  }
}

export function writeScoped(scope: string | null, base: string, value: unknown): void {
  if (!scope) return;

  try {
    window.localStorage.setItem(fullKey(scope, base), JSON.stringify(value));
  } catch {
    // best-effort persistence only
  }
}

/** Keys written before storage was scoped. Their owner is unknown, so they are deleted, never adopted. */
const LEGACY_KEYS = ['nav.recentRecords', 'nav.favorites', 'nav.expandedGroups'];
const LEGACY_PREFIXES = ['savedViews.'];

/**
 * Removes what an older version stored under shared keys.
 *
 * Deleted rather than migrated: whoever signs in first would be handed a list
 * that belonged to somebody else, which is the exact leak being closed.
 */
export function purgeLegacyUiStorage(): void {
  try {
    for (const key of LEGACY_KEYS) {
      window.localStorage.removeItem(key);
    }

    for (const key of Object.keys(window.localStorage)) {
      if (LEGACY_PREFIXES.some((prefix) => key.startsWith(prefix))) {
        window.localStorage.removeItem(key);
      }
    }
  } catch {
    // storage unavailable — nothing to clean
  }
}
