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
export const RELEASE_ID = '2026-10-24.28';

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
    id: 'sheet1024',
    titleKey: 'releaseNotes.sheet1024.title',
    itemKeys: ['releaseNotes.sheet1024.width', 'releaseNotes.sheet1024.weeks'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'dnd1020',
    titleKey: 'releaseNotes.dnd1020.title',
    itemKeys: ['releaseNotes.dnd1020.drag', 'releaseNotes.dnd1020.window', 'releaseNotes.dnd1020.timeline'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'dark1018',
    titleKey: 'releaseNotes.dark1018.title',
    itemKeys: ['releaseNotes.dark1018.logo'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'home1018',
    titleKey: 'releaseNotes.home1018.title',
    itemKeys: ['releaseNotes.home1018.fewer', 'releaseNotes.home1018.strips', 'releaseNotes.home1018.finance', 'releaseNotes.home1018.small', 'releaseNotes.home1018.exact', 'releaseNotes.home1018.zone', 'releaseNotes.home1018.app', 'releaseNotes.home1018.docs'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'since1018',
    titleKey: 'releaseNotes.since1018.title',
    itemKeys: ['releaseNotes.since1018.notes', 'releaseNotes.since1018.rules'],
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
