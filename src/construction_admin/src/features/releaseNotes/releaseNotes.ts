import type { User } from '../../api/types';
import { canManageAssignments, canReviewAbsences } from '../../auth/authHelpers';
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
export const RELEASE_ID = '2026-11-26.90';

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
    id: 'webshift1125',
    titleKey: 'releaseNotes.webshift1125.title',
    itemKeys: [
      'releaseNotes.webshift1125.shift',
      'releaseNotes.webshift1125.more',
      'releaseNotes.webshift1125.offline',
    ],
    audience: (user) => !!user.employeeId,
  },
  {
    id: 'absence1126',
    titleKey: 'releaseNotes.absence1126.title',
    itemKeys: ['releaseNotes.absence1126.preview', 'releaseNotes.absence1126.propose', 'releaseNotes.absence1126.keyboard'],
    audience: (user) => canReviewAbsences(user),
  },
  {
    id: 'contractors1126',
    titleKey: 'releaseNotes.contractors1126.title',
    itemKeys: [
      'releaseNotes.contractors1126.fields',
      'releaseNotes.contractors1126.purple',
      'releaseNotes.contractors1126.end',
    ],
    audience: (user) => canManageAssignments(user) || user.role === 'Foreman',
  },
  {
    id: 'equipment1126',
    titleKey: 'releaseNotes.equipment1126.title',
    itemKeys: [
      'releaseNotes.equipment1126.sites',
      'releaseNotes.equipment1126.people',
      'releaseNotes.equipment1126.alerts',
    ],
    audience: (user) => canManageAssignments(user) || user.role === 'Foreman',
  },
  {
    id: 'decision1126',
    titleKey: 'releaseNotes.decision1126.title',
    itemKeys: ['releaseNotes.decision1126.ask'],
    audience: (user) => canManageAssignments(user) || user.role === 'Foreman',
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
