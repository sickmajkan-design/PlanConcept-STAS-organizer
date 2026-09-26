import type { User } from '../../api/types';
import type { MessageKey } from '../../i18n/en';
import { canViewFinanceStatistics } from '../../auth/authHelpers';

/**
 * The id of the release the notes describe. Change it, and write the new notes
 * below, when there is something people using the platform should be told about;
 * everybody sees the dialog once more, and only once.
 */
export const RELEASE_ID = '2026-09-26.7';

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
    id: 'everyone',
    titleKey: 'releaseNotes.everyone.title',
    itemKeys: ['releaseNotes.everyone.phone', 'releaseNotes.everyone.buttons'],
    audience: () => true,
  },
  {
    id: 'access',
    titleKey: 'releaseNotes.access.title',
    itemKeys: [
      'releaseNotes.access.who',
      'releaseNotes.access.whatMoved',
      'releaseNotes.access.noDouble',
      'releaseNotes.access.housing',
    ],
    audience: (user) => COST_ROLES.has(user.role),
  },
  {
    id: 'finance',
    titleKey: 'releaseNotes.finance.title',
    itemKeys: [
      'releaseNotes.finance.widgets',
      'releaseNotes.finance.period',
      'releaseNotes.finance.settings',
      'releaseNotes.finance.income',
      'releaseNotes.finance.budget',
      'releaseNotes.finance.statistics',
    ],
    audience: (user) => canViewFinanceStatistics(user),
  },
  {
    id: 'fixes',
    titleKey: 'releaseNotes.fixes.title',
    itemKeys: ['releaseNotes.fixes.deletedProject', 'releaseNotes.fixes.demoData'],
    audience: (user) => canViewFinanceStatistics(user),
  },
  {
    id: 'reminders',
    titleKey: 'releaseNotes.reminders.title',
    itemKeys: ['releaseNotes.reminders.atLogin'],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'setup',
    titleKey: 'releaseNotes.setup.title',
    itemKeys: [
      'releaseNotes.setup.guide',
      'releaseNotes.setup.server',
      'releaseNotes.setup.location',
      'releaseNotes.setup.employee',
      'releaseNotes.setup.tracking',
      'releaseNotes.setup.app',
      'releaseNotes.setup.downloadLink',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'testing',
    titleKey: 'releaseNotes.testing.title',
    itemKeys: [
      'releaseNotes.testing.site',
      'releaseNotes.testing.mine',
      'releaseNotes.testing.app',
      'releaseNotes.testing.choose',
      'releaseNotes.testing.company',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'offline',
    titleKey: 'releaseNotes.offline.title',
    itemKeys: [
      'releaseNotes.offline.queue',
      'releaseNotes.offline.portal',
      'releaseNotes.offline.owner',
      'releaseNotes.offline.apk',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'phone119',
    titleKey: 'releaseNotes.phone119.title',
    itemKeys: [
      'releaseNotes.phone119.defectButton',
      'releaseNotes.phone119.defectHome',
      'releaseNotes.phone119.foreman',
      'releaseNotes.phone119.small',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'phone1110',
    titleKey: 'releaseNotes.phone1110.title',
    itemKeys: [
      'releaseNotes.phone1110.today',
      'releaseNotes.phone1110.crew',
      'releaseNotes.phone1110.more',
      'releaseNotes.phone1110.foremanWork',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'payroll1111',
    titleKey: 'releaseNotes.payroll1111.title',
    itemKeys: [
      'releaseNotes.payroll1111.widgets',
      'releaseNotes.payroll1111.widgetHelp',
      'releaseNotes.payroll1111.hours',
      'releaseNotes.payroll1111.contributions',
      'releaseNotes.payroll1111.checks',
      'releaseNotes.payroll1111.app',
    ],
    audience: (user) => user.role === 'SuperAdmin' || user.role === 'Admin',
  },
  {
    id: 'orders1112',
    titleKey: 'releaseNotes.orders1112.title',
    itemKeys: [
      'releaseNotes.orders1112.orders',
      'releaseNotes.orders1112.refunds',
      'releaseNotes.orders1112.payroll',
      'releaseNotes.orders1112.app',
      'releaseNotes.orders1112.design',
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
