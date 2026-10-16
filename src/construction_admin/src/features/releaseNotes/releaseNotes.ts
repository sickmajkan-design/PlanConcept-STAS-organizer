import type { User } from '../../api/types';
import { canViewDirectory } from '../../auth/authHelpers';
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
export const RELEASE_ID = '2026-10-16.8';

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
    id: 'create1016',
    titleKey: 'releaseNotes.create1016.title',
    itemKeys: ['releaseNotes.create1016.forms', 'releaseNotes.create1016.book'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'due1016',
    titleKey: 'releaseNotes.due1016.title',
    itemKeys: ['releaseNotes.due1016.person', 'releaseNotes.due1016.office'],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'look1016',
    titleKey: 'releaseNotes.look1016.title',
    itemKeys: [
      'releaseNotes.look1016.home',
      'releaseNotes.look1016.phone',
      'releaseNotes.look1016.calm',
      'releaseNotes.look1016.app',
      'releaseNotes.look1016.dark',
    ],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'lang1016',
    titleKey: 'releaseNotes.lang1016.title',
    itemKeys: [
      'releaseNotes.lang1016.search',
      'releaseNotes.lang1016.guides',
      'releaseNotes.lang1016.language',
    ],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'drop1016',
    titleKey: 'releaseNotes.drop1016.title',
    itemKeys: ['releaseNotes.drop1016.anywhere', 'releaseNotes.drop1016.category', 'releaseNotes.drop1016.limits'],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
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
