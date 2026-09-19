import {
  AddOutlined,
  ChevronLeftOutlined,
  ChevronRightOutlined,
  InfoOutlined,
  PlaylistAddCheckOutlined,
} from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Popover,
  Select,
  Snackbar,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';

import type { TimeEntryListQuery } from '../../api/timeEntries';
import type { TimeEntry } from '../../api/types';
import { timeEntryStatuses } from '../../api/types';
import { canAdministerAccounts, canReviewTimeEntries, canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ReasonDialog } from '../../components/ReasonDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { StatusLegend } from '../../components/StatusLegend';
import {
  useDeleteTimeEntry,
  useReviewTimeEntry,
  useTimeEntriesQuery,
} from '../../features/timeEntries/useTimeEntries';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import {
  dateOnlyOffset,
  formatDate,
  toLocalDateOnly,
} from '../../utils/formatting';
import { PendingReviewDialog } from './PendingReviewDialog';
import { CheckInIcon, CHECK_IN_PRESENTATION, TimeEntryCard, type CheckInState } from './TimeEntryCard';

/** One page-worth of an entire day's entries — a crew this size never needs real pagination. */
const DAY_PAGE: { pageNumber: number; pageSize: number } = { pageNumber: 1, pageSize: 100 };

/** How many cards a column shows before it collapses the rest behind "show more". */
const COLUMN_CARD_CAP = 5;

/** Key used for the pseudo-column holding entries with no project. */
const NO_PROJECT_KEY = '__none__';

type CardSort = 'startedAt' | 'employeeName' | 'workedMinutes';

/** The instant just after the given local day ends. */
function endOfLocalDay(date: string): string {
  const next = new Date(`${date}T00:00`);
  next.setDate(next.getDate() + 1);

  return next.toISOString();
}

/** Adds (or subtracts) whole days from a `YYYY-MM-DD` value, in local time. */
function shiftDate(date: string, days: number): string {
  const next = new Date(`${date}T00:00`);
  next.setDate(next.getDate() + days);

  const pad = (value: number) => String(value).padStart(2, '0');
  return `${next.getFullYear()}-${pad(next.getMonth() + 1)}-${pad(next.getDate())}`;
}

/** One project's entries, keyed for grouping — `projectId` is null for the "no project" column. */
interface ProjectGroup {
  key: string;
  projectId: string | null;
  projectName: string;
  entries: TimeEntry[];
}

export function TimeEntriesListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const canReview = canReviewTimeEntries(user);
  const canEdit = canViewDirectory(user);
  const canDelete = canAdministerAccounts(user);

  // A notification deep-link (a clock-in/out, e.g.) carries the day, project
  // and employee it happened on — read once on arrival so the page opens
  // already on the right day with that project's column expanded, rather
  // than always defaulting to today.
  const [searchParams] = useSearchParams();
  const { targetId: highlightEmployeeId, isHighlighted } = useHighlightTarget('employeeId');

  const [date, setDate] = useState(() => searchParams.get('date') || dateOnlyOffset(0));
  const [pendingOnly, setPendingOnly] = useState(() => searchParams.get('pendingOnly') === 'true');
  // The nav badge counts entries across every day, so its click opens this
  // instead of the single-day board — a detailed, cross-day view of
  // everything waiting on a decision, grouped and tagged by what's actually
  // wrong with each one, rather than a day that may well come up empty.
  const [reviewQueueOpen, setReviewQueueOpen] = useState(
    () => searchParams.get('reviewQueue') === 'true',
  );
  const [openOnly, setOpenOnly] = useState(false);
  const [search, setSearch] = useState('');
  const [cardSort, setCardSort] = useState<CardSort>('startedAt');
  const [expandedColumns, setExpandedColumns] = useState<Set<string>>(() => {
    const projectId = searchParams.get('projectId');
    return projectId ? new Set([projectId]) : new Set();
  });

  const query: TimeEntryListQuery = useMemo(
    () => ({
      ...DAY_PAGE,
      sortBy: 'startedAt',
      sortDescending: false,
      openOnly: openOnly || undefined,
      status: pendingOnly ? 'Submitted' : undefined,
      from: new Date(`${date}T00:00`).toISOString(),
      to: endOfLocalDay(date),
    }),
    [date, openOnly, pendingOnly],
  );

  const { data, isLoading, isError, error, refetch } = useTimeEntriesQuery(query);
  const remove = useDeleteWithConfirm<TimeEntry>(useDeleteTimeEntry());

  const [reviewing, setReviewing] = useState<TimeEntry | null>(null);
  const [approving, setApproving] = useState<TimeEntry | null>(null);

  // "Čeka pregled" filters to the current day only, same as every other
  // filter here — but a pending entry can sit on any day, so a reviewer
  // arriving with nothing to show on today's board (from the nav badge, most
  // often) would otherwise see an empty board with no clue where the pending
  // count actually lives. One lookup, once per time this filter turns on:
  // find the earliest Submitted entry anywhere and jump straight to its day.
  const [jumpMessage, setJumpMessage] = useState<string | null>(null);
  const autoJumpAttempted = useRef(false);
  const wasPendingOnly = useRef(pendingOnly);

  useEffect(() => {
    if (pendingOnly && !wasPendingOnly.current) {
      autoJumpAttempted.current = false;
    }
    wasPendingOnly.current = pendingOnly;
  }, [pendingOnly]);

  const needsEarliestPendingLookup =
    pendingOnly && !isLoading && (data?.totalCount ?? 0) === 0 && !autoJumpAttempted.current;

  const earliestPendingQuery = useTimeEntriesQuery(
    {
      pageNumber: 1,
      pageSize: 1,
      sortBy: 'startedAt',
      sortDescending: false,
      status: 'Submitted',
    },
    needsEarliestPendingLookup,
  );

  useEffect(() => {
    if (!needsEarliestPendingLookup || !earliestPendingQuery.isSuccess) return;

    autoJumpAttempted.current = true;

    const entry = earliestPendingQuery.data.items[0];
    if (!entry) {
      setJumpMessage(t('timeEntries.noPendingAnywhere'));
      return;
    }

    const entryDate = toLocalDateOnly(entry.startedAt);
    if (entryDate !== date) {
      setDate(entryDate);
      setJumpMessage(t('timeEntries.jumpedToPendingDate', { date: formatDate(entryDate) }));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [needsEarliestPendingLookup, earliestPendingQuery.isSuccess, earliestPendingQuery.data]);

  // Grouped by project rather than paged through as a flat list — a
  // supervisor opening this screen is asking "who's where today", and a
  // project's own column answers that without them hunting through pages.
  const groups = useMemo<ProjectGroup[]>(() => {
    const byProject = new Map<string, ProjectGroup>();

    for (const entry of data?.items ?? []) {
      const key = entry.projectId ?? NO_PROJECT_KEY;
      const existing = byProject.get(key);
      if (existing) {
        existing.entries.push(entry);
      } else {
        byProject.set(key, {
          key,
          projectId: entry.projectId,
          projectName: entry.projectName ?? t('timeEntries.noProject'),
          entries: [entry],
        });
      }
    }

    const sortEntries = (entries: TimeEntry[]) => {
      const sorted = [...entries];
      switch (cardSort) {
        case 'employeeName':
          sorted.sort((a, b) => a.employeeName.localeCompare(b.employeeName));
          break;
        case 'workedMinutes':
          sorted.sort((a, b) => (b.workedMinutes ?? 0) - (a.workedMinutes ?? 0));
          break;
        default:
          sorted.sort((a, b) => a.startedAt.localeCompare(b.startedAt));
      }
      return sorted;
    };

    const result = Array.from(byProject.values()).map((group) => ({
      ...group,
      entries: sortEntries(group.entries),
    }));

    // Real projects first, alphabetically; "no project" always trails since
    // it is a catch-all rather than a site anyone is actually staffing.
    result.sort((a, b) => {
      if (a.key === NO_PROJECT_KEY) return 1;
      if (b.key === NO_PROJECT_KEY) return -1;
      return a.projectName.localeCompare(b.projectName);
    });

    const term = search.trim().toLowerCase();
    return term ? result.filter((group) => group.projectName.toLowerCase().includes(term)) : result;
  }, [data, search, cardSort, t]);

  const toggleExpanded = (key: string) => {
    setExpandedColumns((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  return (
    <Box>
      <PageHeader
        title={t('timeEntries.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('timeEntries.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.timeEntryNew),
        }}
      />

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { sm: 'center' }, flexWrap: 'wrap', rowGap: 1.5 }}
      >
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <IconButton size="small" onClick={() => setDate((d) => shiftDate(d, -1))}>
            <ChevronLeftOutlined fontSize="small" />
          </IconButton>
          <TextField
            type="date"
            size="small"
            value={date}
            onChange={(event) => event.target.value && setDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <IconButton size="small" onClick={() => setDate((d) => shiftDate(d, 1))}>
            <ChevronRightOutlined fontSize="small" />
          </IconButton>
          <Button size="small" onClick={() => setDate(dateOnlyOffset(0))}>
            {t('dateFilter.today')}
          </Button>
        </Stack>

        <FormControlLabel
          control={
            <Switch checked={pendingOnly} onChange={(event) => setPendingOnly(event.target.checked)} />
          }
          label={t('timeEntries.pendingOnly')}
        />
        <FormControlLabel
          control={
            <Switch checked={openOnly} onChange={(event) => setOpenOnly(event.target.checked)} />
          }
          label={t('timeEntries.openOnly')}
        />

        <SearchField
          value={search}
          onChange={setSearch}
          placeholder={t('timeEntries.searchProjects')}
        />

        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="te-card-sort-label">{t('timeEntries.sortCards')}</InputLabel>
          <Select
            labelId="te-card-sort-label"
            label={t('timeEntries.sortCards')}
            value={cardSort}
            onChange={(event) => setCardSort(event.target.value as CardSort)}
          >
            <MenuItem value="startedAt">{t('timeEntries.sortByStart')}</MenuItem>
            <MenuItem value="employeeName">{t('timeEntries.sortByEmployee')}</MenuItem>
            <MenuItem value="workedMinutes">{t('timeEntries.sortByWorked')}</MenuItem>
          </Select>
        </FormControl>

        <Button size="small" onClick={() => navigate(paths.timeEntrySummary)}>
          {t('timeEntries.summary')}
        </Button>
        {canReview && (
          <Button
            size="small"
            variant="outlined"
            startIcon={<PlaylistAddCheckOutlined fontSize="small" />}
            onClick={() => setReviewQueueOpen(true)}
          >
            {t('timeEntries.reviewQueue')}
          </Button>
        )}
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <StatusLegend kind="timeEntryStatus" values={timeEntryStatuses} />
          <CheckInLegend />
        </Stack>
      </Stack>

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {isLoading && !isError && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {!isLoading && !isError && groups.length === 0 && (
        <EmptyState
          message={
            pendingOnly ? t('timeEntries.noPendingForDay') : t('timeEntries.noEntriesForDay')
          }
        />
      )}

      {!isLoading && !isError && groups.length > 0 && (
        // Everything for the day is on screen at once, so browsing more
        // projects is a horizontal scroll rather than a page-number control
        // at the bottom of the screen.
        <Stack direction="row" spacing={2} sx={{ overflowX: 'auto', pb: 2 }}>
          {groups.map((group) => (
            <ProjectColumn
              key={group.key}
              group={group}
              expanded={expandedColumns.has(group.key)}
              onToggleExpanded={() => toggleExpanded(group.key)}
              enumLabel={enumLabel}
              currentEmployeeId={user?.employeeId ?? null}
              canReview={canReview}
              canEdit={canEdit}
              canDelete={canDelete}
              onEdit={(entry) => navigate(paths.timeEntryEdit(entry.id))}
              onDelete={(entry) => remove.request(entry)}
              onApprove={(entry) => setApproving(entry)}
              onReject={(entry) => setReviewing(entry)}
              highlightedEmployeeId={highlightEmployeeId && isHighlighted(highlightEmployeeId) ? highlightEmployeeId : null}
            />
          ))}
        </Stack>
      )}

      <PendingReviewDialog
        open={reviewQueueOpen}
        onClose={() => setReviewQueueOpen(false)}
        enumLabel={enumLabel}
        currentEmployeeId={user?.employeeId ?? null}
        canReview={canReview}
        canEdit={canEdit}
        canDelete={canDelete}
        onEdit={(entry) => navigate(paths.timeEntryEdit(entry.id))}
        onDelete={(entry) => remove.request(entry)}
        onApprove={(entry) => setApproving(entry)}
        onReject={(entry) => setReviewing(entry)}
      />

      <ApproveDialog entry={approving} onClose={() => setApproving(null)} />
      <RejectDialog entry={reviewing} onClose={() => setReviewing(null)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('timeEntries.deleteTitle')}
        description={
          remove.pending
            ? t('timeEntries.deleteBody', { name: remove.pending.employeeName })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {remove.error.message}
          </Typography>
        </Box>
      )}

      <Snackbar
        open={!!jumpMessage}
        autoHideDuration={6000}
        onClose={() => setJumpMessage(null)}
        message={jumpMessage}
      />
    </Box>
  );
}

/** One project's column: a header with the day's headcount, and its crew's cards underneath. */
function ProjectColumn({
  group,
  expanded,
  onToggleExpanded,
  enumLabel,
  currentEmployeeId,
  canReview,
  canEdit,
  canDelete,
  onEdit,
  onDelete,
  onApprove,
  onReject,
  highlightedEmployeeId,
}: {
  group: ProjectGroup;
  expanded: boolean;
  onToggleExpanded: () => void;
  enumLabel: ReturnType<typeof useEnumLabel>;
  currentEmployeeId: string | null;
  canReview: boolean;
  canEdit: boolean;
  canDelete: boolean;
  onEdit: (entry: TimeEntry) => void;
  onDelete: (entry: TimeEntry) => void;
  onApprove: (entry: TimeEntry) => void;
  onReject: (entry: TimeEntry) => void;
  highlightedEmployeeId?: string | null;
}) {
  const t = useT();
  const shown = expanded ? group.entries : group.entries.slice(0, COLUMN_CARD_CAP);
  const hiddenCount = group.entries.length - shown.length;

  return (
    <Paper
      variant="outlined"
      sx={{ width: 300, flexShrink: 0, display: 'flex', flexDirection: 'column', maxHeight: '78vh' }}
    >
      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: 'center', p: 1.5, borderBottom: '1px solid', borderColor: 'divider' }}
      >
        <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }} noWrap title={group.projectName}>
          {group.projectName}
        </Typography>
        <Chip size="small" label={group.entries.length} />
      </Stack>

      <Stack spacing={1} sx={{ p: 1.5, overflowY: 'auto' }}>
        {shown.map((entry) => (
          <TimeEntryCard
            key={entry.id}
            entry={entry}
            workTypeLabel={enumLabel('workType', entry.workType)}
            canReview={canReview}
            isOwn={currentEmployeeId != null && entry.employeeId === currentEmployeeId}
            canEdit={canEdit}
            canDelete={canDelete}
            onEdit={() => onEdit(entry)}
            onDelete={() => onDelete(entry)}
            onApprove={() => onApprove(entry)}
            onReject={() => onReject(entry)}
            highlighted={!!highlightedEmployeeId && entry.employeeId === highlightedEmployeeId}
          />
        ))}

        {hiddenCount > 0 && (
          <Button size="small" onClick={onToggleExpanded}>
            {t('timeEntries.showMore', { count: hiddenCount })}
          </Button>
        )}
        {expanded && group.entries.length > COLUMN_CARD_CAP && (
          <Button size="small" onClick={onToggleExpanded}>
            {t('timeEntries.showLess')}
          </Button>
        )}
      </Stack>
    </Paper>
  );
}

/** On-demand explanation of the check-in badge colours, in the same style as `StatusLegend`. */
function CheckInLegend() {
  const t = useT();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const states: CheckInState[] = ['bothCorrect', 'wrongLocation', 'wrongTime', 'bothWrong', 'unknown'];

  return (
    <>
      <Tooltip title={t('timeEntries.checkInLegendTitle')}>
        <IconButton size="small" onClick={(event) => setAnchor(event.currentTarget)}>
          <InfoOutlined fontSize="small" />
        </IconButton>
      </Tooltip>
      <Popover
        open={!!anchor}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Stack spacing={1.25} sx={{ p: 2, minWidth: 260 }}>
          <Typography variant="subtitle2">{t('timeEntries.checkInLegendTitle')}</Typography>
          {states.map((state) => (
            <Stack key={state} direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <CheckInIcon
                locationCorrect={state === 'wrongLocation' || state === 'bothWrong' ? false : state === 'unknown' ? null : true}
                timeCorrect={state === 'wrongTime' || state === 'bothWrong' ? false : state === 'unknown' ? null : true}
              />
              <Typography variant="body2">{t(CHECK_IN_PRESENTATION[state].labelKey)}</Typography>
            </Stack>
          ))}
        </Stack>
      </Popover>
    </>
  );
}

/**
 * Approval is a confirmation rather than a one-click action, because it locks
 * the entry: undoing it means sending the hours back to the worker, which is
 * visible to them.
 */
function ApproveDialog({
  entry,
  onClose,
}: {
  entry: TimeEntry | null;
  onClose: () => void;
}) {
  const t = useT();
  const review = useReviewFor(entry);
  const wasRejected = entry?.status === 'Rejected';

  return (
    <ConfirmDialog
      open={!!entry}
      title={t('timeEntries.approveTitle')}
      description={
        wasRejected
          ? t('timeEntries.reapproveBody', { reason: entry?.reviewNote ?? '' })
          : t('timeEntries.approveBody')
      }
      confirmLabel={t('timeEntries.approve')}
      loading={review.isPending}
      error={review.error?.message}
      onConfirm={() => {
        // Going through this dialog is itself the confirmation the API asks
        // for when reversing an earlier rejection — an ordinary approval
        // ignores the flag.
        review.mutate({ approve: true, confirm: wasRejected }, { onSuccess: onClose });
      }}
      onCancel={onClose}
    />
  );
}

/** Sending an entry back needs a reason; the API refuses one without it. */
function RejectDialog({
  entry,
  onClose,
}: {
  entry: TimeEntry | null;
  onClose: () => void;
}) {
  const t = useT();
  const review = useReviewFor(entry);

  return (
    <ReasonDialog
      open={!!entry}
      title={t('timeEntries.rejectTitle')}
      hint={t('timeEntries.rejectHint')}
      label={t('timeEntries.rejectReason')}
      submitLabel={t('timeEntries.reject')}
      onClose={onClose}
      onSubmit={async (note) => {
        await review.mutateAsync({ approve: false, note });
        onClose();
      }}
    />
  );
}

/**
 * The review mutation for whichever entry a dialog is showing.
 *
 * Hooks cannot be called conditionally, and the dialogs render before an entry
 * is picked, so this binds to a placeholder id until one is. The dialog is
 * closed at that point, so the mutation is never fired against it.
 */
function useReviewFor(entry: TimeEntry | null) {
  return useReviewTimeEntry(entry?.id ?? '');
}
