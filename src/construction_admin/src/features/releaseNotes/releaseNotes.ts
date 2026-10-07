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
export const RELEASE_ID = '2026-10-07.6';

export interface ReleaseSection {
  id: string;
  titleKey: MessageKey;
  itemKeys: MessageKey[];
  /** Who the section is for. A section nobody in the audience needs is not shown to them. */
  audience: (user: User) => boolean;
}

export const releaseSections: ReleaseSection[] = [
  {
    id: 'paper1007',
    titleKey: 'releaseNotes.paper1007.title',
    itemKeys: ['releaseNotes.paper1007.export', 'releaseNotes.paper1007.days'],
    audience: (user) =>
      user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager' || user.role === 'Foreman',
  },
  {
    id: 'fm1007',
    titleKey: 'releaseNotes.fm1007.title',
    itemKeys: ['releaseNotes.fm1007.view', 'releaseNotes.fm1007.grant'],
    audience: (user) =>
      user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager' || user.role === 'Foreman',
  },
  {
    id: 'cert1007',
    titleKey: 'releaseNotes.cert1007.title',
    itemKeys: ['releaseNotes.cert1007.attention', 'releaseNotes.cert1007.certificates'],
    audience: (user) =>
      user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager',
  },
  {
    id: 'tidy1007',
    titleKey: 'releaseNotes.tidy1007.title',
    itemKeys: ['releaseNotes.tidy1007.check', 'releaseNotes.tidy1007.warnings', 'releaseNotes.tidy1007.ack'],
    audience: (user) =>
      user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager',
  },
  {
    id: 'plan1007',
    titleKey: 'releaseNotes.plan1007.title',
    itemKeys: [
      'releaseNotes.plan1007.what',
      'releaseNotes.plan1007.move',
      'releaseNotes.plan1007.need',
      'releaseNotes.plan1007.cover',
      'releaseNotes.plan1007.guide',
      'releaseNotes.plan1007.limits',
    ],
    audience: (user) =>
      user.role === 'SuperAdmin' || user.role === 'Admin' || user.role === 'ProjectManager',
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
