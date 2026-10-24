import type { User } from '../../api/types';
import { canAdministerAccounts, canViewDirectory } from '../../auth/authHelpers';
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
export const RELEASE_ID = '2026-10-24.33';

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
    id: 'notices1024',
    titleKey: 'releaseNotes.notices1024.title',
    itemKeys: ['releaseNotes.notices1024.audience', 'releaseNotes.notices1024.sent', 'releaseNotes.notices1024.read', 'releaseNotes.notices1024.remind'],
    audience: (user) => canAdministerAccounts(user),
  },
  {
    id: 'android1024',
    titleKey: 'releaseNotes.android1024.title',
    itemKeys: ['releaseNotes.android1024.form', 'releaseNotes.android1024.again', 'releaseNotes.android1024.notices'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'office1024',
    titleKey: 'releaseNotes.office1024.title',
    itemKeys: ['releaseNotes.office1024.enter', 'releaseNotes.office1024.review', 'releaseNotes.office1024.notice'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'lines1024',
    titleKey: 'releaseNotes.lines1024.title',
    itemKeys: ['releaseNotes.lines1024.form', 'releaseNotes.lines1024.review', 'releaseNotes.lines1024.ledger', 'releaseNotes.lines1024.month', 'releaseNotes.lines1024.merge'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'weekly1024',
    titleKey: 'releaseNotes.weekly1024.title',
    itemKeys: ['releaseNotes.weekly1024.review', 'releaseNotes.weekly1024.approve', 'releaseNotes.weekly1024.late', 'releaseNotes.weekly1024.notice', 'releaseNotes.weekly1024.ledger'],
    audience: (user) => canViewDirectory(user),
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
