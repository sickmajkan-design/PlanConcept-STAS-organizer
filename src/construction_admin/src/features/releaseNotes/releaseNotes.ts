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
export const RELEASE_ID = '2026-10-26.38';

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
    id: 'vehicles1026',
    titleKey: 'releaseNotes.vehicles1026.title',
    itemKeys: ['releaseNotes.vehicles1026.state', 'releaseNotes.vehicles1026.tolls', 'releaseNotes.vehicles1026.fines', 'releaseNotes.vehicles1026.notices'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'people1026',
    titleKey: 'releaseNotes.people1026.title',
    itemKeys: ['releaseNotes.people1026.rates', 'releaseNotes.people1026.basics', 'releaseNotes.people1026.dated'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'stays1026',
    titleKey: 'releaseNotes.stays1026.title',
    itemKeys: ['releaseNotes.stays1026.many', 'releaseNotes.stays1026.check', 'releaseNotes.stays1026.own', 'releaseNotes.stays1026.customer'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'kits1026',
    titleKey: 'releaseNotes.kits1026.title',
    itemKeys: ['releaseNotes.kits1026.kit', 'releaseNotes.kits1026.problems', 'releaseNotes.kits1026.copy', 'releaseNotes.kits1026.quick', 'releaseNotes.kits1026.customer', 'releaseNotes.kits1026.units'],
    audience: (user) => canViewDirectory(user),
  },
  {
    id: 'logos1025',
    titleKey: 'releaseNotes.logos1025.title',
    itemKeys: ['releaseNotes.logos1025.bar', 'releaseNotes.logos1025.upload', 'releaseNotes.logos1025.main', 'releaseNotes.logos1025.exports', 'releaseNotes.logos1025.open', 'releaseNotes.logos1025.missing', 'releaseNotes.logos1025.android'],
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
