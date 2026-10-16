import type { User } from '../../api/types';
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
export const RELEASE_ID = '2026-10-16.3';

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
    id: 'drop1016',
    titleKey: 'releaseNotes.drop1016.title',
    itemKeys: ['releaseNotes.drop1016.anywhere', 'releaseNotes.drop1016.category', 'releaseNotes.drop1016.limits'],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'sec1016',
    titleKey: 'releaseNotes.sec1016.title',
    itemKeys: ['releaseNotes.sec1016.audit', 'releaseNotes.sec1016.phone'],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'pdf1015',
    titleKey: 'releaseNotes.pdf1015.title',
    itemKeys: ['releaseNotes.pdf1015.pdf', 'releaseNotes.pdf1015.scan'],
    audience: (user) =>
      user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager' || user.role === 'Foreman',
  },
  {
    id: 'share1015',
    titleKey: 'releaseNotes.share1015.title',
    itemKeys: [
      'releaseNotes.share1015.share',
      'releaseNotes.share1015.phone',
      'releaseNotes.share1015.requests',
      'releaseNotes.share1015.licence',
      'releaseNotes.share1015.delete',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'docs1015',
    titleKey: 'releaseNotes.docs1015.title',
    itemKeys: ['releaseNotes.docs1015.search', 'releaseNotes.docs1015.tag', 'releaseNotes.docs1015.nodocs'],
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
