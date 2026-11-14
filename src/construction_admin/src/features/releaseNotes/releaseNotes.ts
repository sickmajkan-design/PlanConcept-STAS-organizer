import type { User } from '../../api/types';
import { canAdministerAccounts, isSuperAdmin } from '../../auth/authHelpers';
import type { MessageKey } from '../../i18n/en';

/**
 * The id of the release the notes describe. Change it, and write the new notes
 * below, when there is something people using the platform should be told about;
 * everybody sees the dialog once more, and only once.
 *
 * Keep the sections of the last five releases only. The dialog is a summary of
 * what changed recently, not a changelog: when a release is added, drop the
 * sections older than the fifth (and their `releaseNotes.*` keys in i18n).
 */
export const RELEASE_ID = '2026-11-14.60';

/** The day of the release, `YYYY-MM-DD`, read from {@link RELEASE_ID}. */
export const RELEASE_DATE = RELEASE_ID.slice(0, 10);

export interface ReleaseSection {
  id: string;
  titleKey: MessageKey;
  itemKeys: MessageKey[];
  /** Who the section is for. A section nobody in the audience needs is not shown to them. */
  audience: (user: User) => boolean;
}

export const releaseSections: ReleaseSection[] = [
  {
    id: 'bulkdelete1113',
    titleKey: 'releaseNotes.bulkdelete1113.title',
    itemKeys: ['releaseNotes.bulkdelete1113.pick'],
    audience: (user) => canAdministerAccounts(user),
  },
  {
    id: 'backups1114',
    titleKey: 'releaseNotes.backups1114.title',
    itemKeys: ['releaseNotes.backups1114.card'],
    audience: (user) => isSuperAdmin(user),
  },
  {
    id: 'uptime1114',
    titleKey: 'releaseNotes.uptime1114.title',
    itemKeys: ['releaseNotes.uptime1114.card'],
    audience: (user) => isSuperAdmin(user),
  },
  {
    id: 'notices1114',
    titleKey: 'releaseNotes.notices1114.title',
    itemKeys: ['releaseNotes.notices1114.card'],
    audience: (user) => isSuperAdmin(user),
  },
  {
    id: 'docker1114',
    titleKey: 'releaseNotes.docker1114.title',
    itemKeys: ['releaseNotes.docker1114.card', 'releaseNotes.docker1114.cert', 'releaseNotes.docker1114.errors', 'releaseNotes.docker1114.releases', 'releaseNotes.docker1114.metrics', 'releaseNotes.docker1114.security'],
    audience: (user) => isSuperAdmin(user),
  },
];

/** The sections this account should be told about, in order. */
export function sectionsFor(user: User): ReleaseSection[] {
  return releaseSections.filter((section) => section.audience(user));
}

function storageKey(userId: string): string {
  return `releaseNotes.seen.${userId}`;
}

/** Whether this account has already dismissed these notes. Storage that cannot be read counts as "not seen". */
export function hasSeen(userId: string): boolean {
  try {
    return window.localStorage.getItem(storageKey(userId)) === RELEASE_ID;
  } catch {
    return false;
  }
}

export function markSeen(userId: string): void {
  try {
    window.localStorage.setItem(storageKey(userId), RELEASE_ID);
  } catch {
    // Storage blocked: the notes will show again next time, which is harmless.
  }
}
