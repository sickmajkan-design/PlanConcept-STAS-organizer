import type { User } from '../api/types';
import { useVehicleExpensesQuery } from '../features/costs/useCosts';
import { useMaterialsQuery } from '../features/materials/useMaterials';
import { useAbsencesQuery } from '../features/absences/useAbsences';
import { useExpiringDocumentsQuery } from '../features/attachments/useAttachments';
import { useTimeEntriesQuery } from '../features/timeEntries/useTimeEntries';
import { useWorkItemsQuery } from '../features/workItems/useWorkItems';
import {
  canAdministerAccounts,
  canReviewSpending,
  canReviewTimeEntries,
  canViewDirectory,
} from '../auth/authHelpers';
import { paths } from '../routes/paths';

const DOCUMENT_EXPIRY_WINDOW_DAYS = 30;
/** Just the count, not the rows — every one of these queries only reads `totalCount`/`length`. */
const COUNT_ONLY_PAGE = { pageNumber: 1, pageSize: 1 } as const;

/**
 * Small counts shown as a badge on the nav — keyed both by {@link NavGroup.key}
 * (the rail's collapsed group icon, an aggregate of everything inside it) and
 * by the specific item's own `path` (its row inside the flyout/drawer), so
 * the same number that shows on the group also follows through to the exact
 * item — and, from there, the exact module — a notification was actually
 * about, rather than stopping at the group icon.
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
  const showAbsences = canViewDirectory(user);
  const showWorkItems = canViewDirectory(user);
  const showTimeEntries = canReviewTimeEntries(user);
  const showVehicleExpenses = canReviewSpending(user);

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
    admin: documentsCount,
    work: absencesCount + workItemsCount + timeEntriesCount,
    directory: lowStockCount,
    [paths.materials]: lowStockCount,
    costs: vehicleExpensesCount,
    [paths.costRecords]: vehicleExpensesCount,
    [paths.vehicleExpenses]: vehicleExpensesCount,
    [paths.expiringDocuments]: documentsCount,
    [paths.absences]: absencesCount,
    [paths.workItems]: workItemsCount,
    [paths.timeEntries]: timeEntriesCount,
  };
}
