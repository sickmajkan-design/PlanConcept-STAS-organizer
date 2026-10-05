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
export const RELEASE_ID = '2026-10-05.3';

export interface ReleaseSection {
  id: string;
  titleKey: MessageKey;
  itemKeys: MessageKey[];
  /** Who the section is for. A section nobody in the audience needs is not shown to them. */
  audience: (user: User) => boolean;
}

/** Roles that could read the cost reports before finance became a right of its own. */
const COST_ROLES = new Set(['SuperAdmin', 'Admin', 'ProjectManager', 'Foreman']);

export const releaseSections: ReleaseSection[] = [
  {
    id: 'tolls1128',
    titleKey: 'releaseNotes.tolls1128.title',
    itemKeys: ['releaseNotes.tolls1128.what', 'releaseNotes.tolls1128.who'],
    audience: (user) => COST_ROLES.has(user.role),
  },
  {
    id: 'branches1001',
    titleKey: 'releaseNotes.branches1001.title',
    itemKeys: [
      'releaseNotes.branches1001.what',
      'releaseNotes.branches1001.filter',
      'releaseNotes.branches1001.assign',
      'releaseNotes.branches1001.money',
      'releaseNotes.branches1001.details',
      'releaseNotes.branches1001.documents',
      'releaseNotes.branches1001.employees',
      'releaseNotes.branches1001.payroll',
      'releaseNotes.branches1001.screens',
      'releaseNotes.branches1001.notices',
      'releaseNotes.branches1001.app',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'documents1005',
    titleKey: 'releaseNotes.documents1005.title',
    itemKeys: [
      'releaseNotes.documents1005.count',
      'releaseNotes.documents1005.reminders',
      'releaseNotes.documents1005.housing',
      'releaseNotes.documents1005.shift',
      'releaseNotes.documents1005.layout',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'security1005',
    titleKey: 'releaseNotes.security1005.title',
    itemKeys: [
      'releaseNotes.security1005.passwords',
      'releaseNotes.security1005.limit',
      'releaseNotes.security1005.panel',
      'releaseNotes.security1005.behindTheScenes',
    ],
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
