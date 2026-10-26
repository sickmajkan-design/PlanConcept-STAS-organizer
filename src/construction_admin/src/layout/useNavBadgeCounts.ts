
import type { AttentionGroup } from '../api/attention';
import type { User } from '../api/types';
import { useBulletinPostsQuery } from '../features/bulletin/useBulletin';
import { useDocumentRequestsQuery } from '../features/documentRequests/useDocumentRequests';
import { useVehicleExpensesQuery } from '../features/costs/useCosts';
import { useMaterialsQuery } from '../features/materials/useMaterials';
import { useAccommodationsQuery } from '../features/accommodations/useAccommodations';
import { useAbsencesQuery } from '../features/absences/useAbsences';
import { useExpiringDocumentsQuery } from '../features/attachments/useAttachments';
import { useAttentionQuery } from '../features/dashboard/useAttention';
import { useTimeEntriesQuery } from '../features/timeEntries/useTimeEntries';
import { useWorkItemsQuery } from '../features/workItems/useWorkItems';
import {
  canAdministerAccounts,
  canReviewAbsences,
  canReviewSpending,
  canReviewTimeEntries,
  canSeeSpending,
  canViewDirectory,
} from '../auth/authHelpers';
import { paths } from '../routes/paths';

const DOCUMENT_EXPIRY_WINDOW_DAYS = 30;
/** Just the count, not the rows — every one of these queries only reads `totalCount`/`length`. */
const COUNT_ONLY_PAGE = { pageNumber: 1, pageSize: 1 } as const;

/**
 * Every menu item that has a backlog behind it, by path. A menu change must keep this in step: `navBadges.test.ts`
 * fails when an item named here is no longer in the menu, so a badge cannot be left on a page that moved away, and
 * a group's total is computed from its items (`withGroupTotals`), so it cannot drift from them.
 */
export const BADGED_PATHS: readonly string[] = [
  paths.bulletin,
  paths.schedule,
  paths.absences,
  paths.timeEntries,
  paths.workItems,
  paths.articleOrders,
  paths.employees,
  paths.accommodations,
  paths.vehicles,
  paths.materials,
  paths.costRecords,
  paths.refunds,
  paths.bookReceivables,
  paths.expiringDocuments,
  paths.documentRequests,
  paths.dataQuality,
];

const countOf = (groups: readonly AttentionGroup[] | undefined, ...keys: string[]) =>
  (groups ?? []).filter((group) => keys.includes(group.key)).reduce((sum, group) => sum + group.count, 0);

/**
 * Small counts shown as a badge on the nav, keyed by the item's own `path` (its row inside the flyout/drawer).
 * A group's badge on the rail is the sum of its items' (`withGroupTotals`), so the number on the group always
 * follows through to the exact item, and from there the exact module, a notification was about.
 *
 * Whatever the work-queue endpoint (`/attention`) already counts is read from it rather than counted again:
 * unconfirmed postings, article orders and refunds waiting, vehicle dates and certificates about to pass,
 * overdue invoices and costs, records to tidy. It is the same list the dashboard shows, so the two agree.
 *
 * Each one mirrors a notification type this app sends: `absencesQuery` for
 * `AbsenceRequested`/`AbsenceEditProposed`, `documentsQuery` for
 * `DocumentExpiring`, `workItemsOpenQuery`/`workItemsInProgressQuery` for
 * `DefectReported` (an unassigned defect is exactly "nobody is assigned to
 * it yet"), and `timeEntriesQuery` for the review half of `ShiftAutoClosed`
 * and the clock-in notices. A few notification types have nothing to count
 * here on purpose: `ProjectAssigned`/`EmployeeAssigned`/`VehicleAssigned`/
 * `ToolAssigned` are one-off events with no resulting backlog, and
 * `GeneralAnnouncement`/`DirectMessage`/`BulletinPosted` are free text with
 * no queryable "how many are still open" — those stay visible only in the
 * notification bell itself, same as `DocumentRetentionEnded`, which the
 * expiring-documents endpoint deliberately doesn't return (see
 * `GetExpiringDocumentsQuery` — it only ever filters by `ExpiresAt`).
 *
 * Every query here goes through the same resource hooks (`useAbsencesQuery`,
 * `useWorkItemsQuery`, `useTimeEntriesQuery`, `useExpiringDocumentsQuery`)
 * every list page and dashboard widget already uses — not a separate
 * "nav-badge" cache someone has to remember to invalidate by hand. Their
 * resource's own mutations (`absenceKeys.all`, `workItemKeys.all`, …) already
 * invalidate every query keyed under them, this one included, so a badge
 * drops the instant its backlog is resolved from *anywhere* on the
 * platform — the inbox, the dashboard, or the module's own page — with no
 * bespoke wiring needed for the next module that gets one.
 */
export function useNavBadgeCounts(user: User | null | undefined): Record<string, number> {
  const showDocuments = canAdministerAccounts(user);
  // Only management answers leave requests, so only they get a "waiting" count.
  const showAbsences = canReviewAbsences(user);
  const showWorkItems = canViewDirectory(user);
  const showTimeEntries = canReviewTimeEntries(user);
  // Reading the amounts back needs the finance grant, so a reviewer without it
  // would only get a 403 for this count.
  const showVehicleExpenses = canReviewSpending(user) && canSeeSpending(user);

  // The same query, with the same key, as the dashboard's "Needs attention": one request serves both.
  const attentionQuery = useAttentionQuery(showWorkItems);
  const attention = attentionQuery.data?.groups;

  // Answers waiting for the office's check, and requests whose deadline passed with nothing sent.
  const submittedRequestsQuery = useDocumentRequestsQuery({ ...COUNT_ONLY_PAGE, status: 'Submitted' }, showDocuments);
  const overdueRequestsQuery = useDocumentRequestsQuery({ ...COUNT_ONLY_PAGE, status: 'Open', overdue: true }, showDocuments);
  const documentRequestsCount = showDocuments
    ? (submittedRequestsQuery.data?.totalCount ?? 0) + (overdueRequestsQuery.data?.totalCount ?? 0)
    : 0;

  // Notices this person has not opened yet. Opening the board is what marks them seen, so the number goes
  // down by itself.
  const bulletinQuery = useBulletinPostsQuery();
  const unseenNoticesCount = Array.isArray(bulletinQuery.data) ? bulletinQuery.data.filter((post) => !post.viewed).length : 0;

  const documentsQuery = useExpiringDocumentsQuery(
    DOCUMENT_EXPIRY_WINDOW_DAYS,
    false,
    showDocuments,
  );

  const absencesQuery = useAbsencesQuery(
    { ...COUNT_ONLY_PAGE, waitingOnReviewer: true },
    showAbsences,
  );

  // Not `openOnly`: the API's own definition of "open" only excludes Closed
  // and Cancelled, and deliberately still counts Resolved as open (a
  // Resolved item can still be reopened). A defect resolved without ever
  // being assigned — a common shortcut — would stay Resolved-and-unassigned
  // forever and never leave this badge if it used that flag. Open and
  // InProgress are the only two states where "nobody is assigned to it yet"
  // is still actually a problem.
  const workItemsOpenQuery = useWorkItemsQuery(
    { ...COUNT_ONLY_PAGE, unassignedOnly: true, status: 'Open' },
    showWorkItems,
  );

  const workItemsInProgressQuery = useWorkItemsQuery(
    { ...COUNT_ONLY_PAGE, unassignedOnly: true, status: 'InProgress' },
    showWorkItems,
  );

  const timeEntriesQuery = useTimeEntriesQuery(
    { ...COUNT_ONLY_PAGE, status: 'Submitted' },
    showTimeEntries,
  );

  // Costs waiting for a decision - the same "waiting on me" number the
  // notification of the same name announces.
  const vehicleExpensesQuery = useVehicleExpensesQuery(
    { ...COUNT_ONLY_PAGE, status: 'Pending' },
    showVehicleExpenses,
  );

  // Materials under their reorder level: only the people who order stock.
  const lowStockQuery = useMaterialsQuery({ ...COUNT_ONLY_PAGE, lowStockOnly: true }, showVehicleExpenses);

  // Housing contracts ending within a month (or already over): the same
  // number the notification of the same name announces.
  const contractsQuery = useAccommodationsQuery({ ...COUNT_ONLY_PAGE, contractEndsWithinDays: 30 }, showDocuments);
  const contractsCount = showDocuments ? (contractsQuery.data?.totalCount ?? 0) : 0;

  const documentsCount = showDocuments ? (documentsQuery.data?.length ?? 0) : 0;
  const absencesCount = showAbsences ? (absencesQuery.data?.totalCount ?? 0) : 0;
  const workItemsCount = showWorkItems
    ? (workItemsOpenQuery.data?.totalCount ?? 0) + (workItemsInProgressQuery.data?.totalCount ?? 0)
    : 0;
  const timeEntriesCount = showTimeEntries ? (timeEntriesQuery.data?.totalCount ?? 0) : 0;
  const lowStockCount = showVehicleExpenses ? (lowStockQuery.data?.totalCount ?? 0) : 0;
  const vehicleExpensesCount = showVehicleExpenses
    ? (vehicleExpensesQuery.data?.totalCount ?? 0)
    : 0;

  return {
    [paths.bulletin]: unseenNoticesCount,
    [paths.schedule]: countOf(attention, 'unconfirmedPostings'),
    [paths.absences]: absencesCount,
    [paths.timeEntries]: timeEntriesCount,
    [paths.workItems]: workItemsCount,
    [paths.articleOrders]: countOf(attention, 'articleOrders'),
    [paths.employees]: countOf(attention, 'certificatesExpiring'),
    [paths.accommodations]: contractsCount,
    [paths.vehicles]: countOf(attention, 'vehicleDates') + countOf(attention, 'vehicleFines'),
    [paths.materials]: lowStockCount,
    // The hub of the cost tabs carries the number of the tab that has one; the tab itself is left out of group totals.
    [paths.costRecords]: vehicleExpensesCount + countOf(attention, 'dkvRows'),
    [paths.vehicleExpenses]: vehicleExpensesCount,
    [paths.refunds]: countOf(attention, 'refunds'),
    [paths.bookReceivables]: countOf(attention, 'lateInvoices', 'lateCosts'),
    [paths.expiringDocuments]: documentsCount,
    [paths.documentRequests]: documentRequestsCount,
    [paths.dataQuality]: countOf(attention, 'dataQuality'),
  };
}
