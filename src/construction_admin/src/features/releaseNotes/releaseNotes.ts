import type { User } from '../../api/types';
import { canViewBook, canViewDirectory } from '../../auth/authHelpers';
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
export const RELEASE_ID = '2026-10-17.18';

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
    id: 'app1017',
    titleKey: 'releaseNotes.app1017.title',
    itemKeys: ['releaseNotes.app1017.bar', 'releaseNotes.app1017.swipe', 'releaseNotes.app1017.sheets', 'releaseNotes.app1017.words', 'releaseNotes.app1017.tidy'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'book1017',
    titleKey: 'releaseNotes.book1017.title',
    itemKeys: ['releaseNotes.book1017.quick', 'releaseNotes.book1017.settlement'],
    audience: (user) => canViewBook(user),
  },
  {
    id: 'grid1017',
    titleKey: 'releaseNotes.grid1017.title',
    itemKeys: ['releaseNotes.grid1017.empty', 'releaseNotes.grid1017.columns'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'plan1017',
    titleKey: 'releaseNotes.plan1017.title',
    itemKeys: ['releaseNotes.plan1017.views', 'releaseNotes.plan1017.filters'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'ui1017',
    titleKey: 'releaseNotes.ui1017.title',
    itemKeys: ['releaseNotes.ui1017.buttons', 'releaseNotes.ui1017.status', 'releaseNotes.ui1017.tables', 'releaseNotes.ui1017.oneline'],
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
