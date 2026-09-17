import { useQuery } from '@tanstack/react-query';

import { absencesApi } from '../api/absences';
import { attachmentsApi } from '../api/attachments';
import { timeEntriesApi } from '../api/timeEntries';
import type { User } from '../api/types';
import { workItemsApi } from '../api/workItems';
import {
  canAdministerAccounts,
  canReviewTimeEntries,
  canViewDirectory,
} from '../auth/authHelpers';
import { paths } from '../routes/paths';

const DOCUMENT_EXPIRY_WINDOW_DAYS = 30;
/**
 * A minute's staleness is fine for a badge nobody is staring at — but the
 * whole point of a badge is that it goes away the moment its own backlog is
 * cleared, not up to a minute later. Every write that resolves one of these
 * (approving an absence, assigning a work item, reviewing a time entry,
 * deleting an expiring document) invalidates the matching key below
 * directly, so this staleness only ever matters for a *different* browser
 * tab or user, never the one that just acted.
 */
const STALE_TIME_MS = 60_000;
/** Just the count, not the rows — every one of these queries only reads `totalCount`/`length`. */
const COUNT_ONLY_PAGE = { pageNumber: 1, pageSize: 1 } as const;

/**
 * Exported so the mutation that resolves each backlog — approving an
 * absence, assigning a work item, reviewing a time entry, deleting or
 * replacing an expiring document — can invalidate its badge directly, and
 * the number updates the instant the action succeeds instead of waiting out
 * {@link STALE_TIME_MS} or a page reload.
 */
export const navBadgeKeys = {
  documentsExpiring: ['nav-badge', 'documents-expiring'] as const,
  absencesPending: ['nav-badge', 'absences-pending'] as const,
  workItemsUnassigned: ['nav-badge', 'work-items-unassigned'] as const,
  timeEntriesSubmitted: ['nav-badge', 'time-entries-submitted'] as const,
};

/**
 * Small counts shown as a badge on the nav — keyed both by {@link NavGroup.key}
 * (the rail's collapsed group icon, an aggregate of everything inside it) and
 * by the specific item's own `path` (its row inside the flyout/drawer), so
 * the same number that shows on the group also follows through to the exact
 * item — and, from there, the exact module — a notification was actually
 * about, rather than stopping at the group icon.
 *
 * Each one mirrors a notification type this app sends: {@link absencesQuery}
 * for `AbsenceRequested`/`AbsenceEditProposed`, {@link documentsQuery} for
 * `DocumentExpiring`, {@link workItemsQuery} for `DefectReported` (an
 * unassigned defect is exactly "nobody is assigned to it yet"), and
 * {@link timeEntriesQuery} for the review half of `ShiftAutoClosed` and the
 * clock-in notices. A few notification types have nothing to count here on
 * purpose: `ProjectAssigned`/`EmployeeAssigned`/`VehicleAssigned`/
 * `ToolAssigned` are one-off events with no resulting backlog, and
 * `GeneralAnnouncement`/`DirectMessage`/`BulletinPosted` are free text with
 * no queryable "how many are still open" — those stay visible only in the
 * notification bell itself, same as `DocumentRetentionEnded`, which the
 * expiring-documents endpoint deliberately doesn't return (see
 * `GetExpiringDocumentsQuery` — it only ever filters by `ExpiresAt`).
 */
export function useNavBadgeCounts(user: User | null | undefined): Record<string, number> {
  const showDocuments = canAdministerAccounts(user);
  const showAbsences = canViewDirectory(user);
  const showWorkItems = canViewDirectory(user);
  const showTimeEntries = canReviewTimeEntries(user);

  const documentsQuery = useQuery({
    queryKey: navBadgeKeys.documentsExpiring,
    queryFn: () => attachmentsApi.expiring(DOCUMENT_EXPIRY_WINDOW_DAYS),
    enabled: showDocuments,
    staleTime: STALE_TIME_MS,
  });

  const absencesQuery = useQuery({
    queryKey: navBadgeKeys.absencesPending,
    queryFn: () => absencesApi.list({ ...COUNT_ONLY_PAGE, status: 'Requested' }),
    enabled: showAbsences,
    staleTime: STALE_TIME_MS,
  });

  const workItemsQuery = useQuery({
    queryKey: navBadgeKeys.workItemsUnassigned,
    queryFn: () =>
      workItemsApi.list({ ...COUNT_ONLY_PAGE, unassignedOnly: true, openOnly: true }),
    enabled: showWorkItems,
    staleTime: STALE_TIME_MS,
  });

  const timeEntriesQuery = useQuery({
    queryKey: navBadgeKeys.timeEntriesSubmitted,
    queryFn: () => timeEntriesApi.list({ ...COUNT_ONLY_PAGE, status: 'Submitted' }),
    enabled: showTimeEntries,
    staleTime: STALE_TIME_MS,
  });

  const documentsCount = showDocuments ? (documentsQuery.data?.length ?? 0) : 0;
  const absencesCount = showAbsences ? (absencesQuery.data?.totalCount ?? 0) : 0;
  const workItemsCount = showWorkItems ? (workItemsQuery.data?.totalCount ?? 0) : 0;
  const timeEntriesCount = showTimeEntries ? (timeEntriesQuery.data?.totalCount ?? 0) : 0;

  return {
    admin: documentsCount,
    work: absencesCount + workItemsCount + timeEntriesCount,
    [paths.expiringDocuments]: documentsCount,
    [paths.absences]: absencesCount,
    [paths.workItems]: workItemsCount,
    [paths.timeEntries]: timeEntriesCount,
  };
}
