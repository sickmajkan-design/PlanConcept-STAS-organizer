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
export const RELEASE_ID = '2026-10-17.4';

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
    id: 'menu1017',
    titleKey: 'releaseNotes.menu1017.title',
    itemKeys: ['releaseNotes.menu1017.rail', 'releaseNotes.menu1017.documents', 'releaseNotes.menu1017.finance', 'releaseNotes.menu1017.app'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'scanner1017',
    titleKey: 'releaseNotes.scanner1017.title',
    itemKeys: ['releaseNotes.scanner1017.warning', 'releaseNotes.scanner1017.probe'],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'calmdesk1017',
    titleKey: 'releaseNotes.calmdesk1017.title',
    itemKeys: ['releaseNotes.calmdesk1017.tiles', 'releaseNotes.calmdesk1017.buttons', 'releaseNotes.calmdesk1017.empty'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'today1017',
    titleKey: 'releaseNotes.today1017.title',
    itemKeys: ['releaseNotes.today1017.summary', 'releaseNotes.today1017.sheet'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'calm1016',
    titleKey: 'releaseNotes.calm1016.title',
    itemKeys: ['releaseNotes.calm1016.phone', 'releaseNotes.calm1016.calm', 'releaseNotes.calm1016.app'],
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
