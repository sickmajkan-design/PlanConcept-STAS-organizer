import {
  AddOutlined,
  ChevronLeftOutlined,
  ChevronRightOutlined,
  DeleteOutlined,
  EditOutlined,
  HelpOutlineOutlined,
  InfoOutlined,
  LocationOffOutlined,
  ReportOutlined,
  ScheduleOutlined,
  TaskAltOutlined,
} from '@mui/icons-material';
import {
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Popover,
  Select,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { TimeEntryListQuery } from '../../api/timeEntries';
import type { TimeEntry } from '../../api/types';
import { timeEntryStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import {
  useDeleteTimeEntry,
  useReviewTimeEntry,
  useTimeEntriesQuery,
} from '../../features/timeEntries/useTimeEntries';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { dateOnlyOffset, formatTimeOfDay, splitMinutes } from '../../utils/formatting';
import { ReviewButtons } from './ReviewButtons';

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

/** Two-letter initials from a full "First Last" name, for the card avatar. */
function employeeInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? '' : '';
  return (first + last).toUpperCase() || '?';
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

  const [date, setDate] = useState(() => dateOnlyOffset(0));
  const [pendingOnly, setPendingOnly] = useState(false);
  const [openOnly, setOpenOnly] = useState(false);
  const [search, setSearch] = useState('');
  const [cardSort, setCardSort] = useState<CardSort>('startedAt');
  const [expandedColumns, setExpandedColumns] = useState<Set<string>>(new Set());

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
        <EmptyState message={t('timeEntries.noEntriesForDay')} />
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
              onEdit={(entry) => navigate(paths.timeEntryEdit(entry.id))}
              onDelete={(entry) => remove.request(entry)}
              onApprove={(entry) => setApproving(entry)}
              onReject={(entry) => setReviewing(entry)}
            />
          ))}
        </Stack>
      )}

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
    </Box>
  );
}

/** One project's column: a header with the day's headcount, and its crew's cards underneath. */
function ProjectColumn({
  group,
  expanded,
  onToggleExpanded,
  enumLabel,
  onEdit,
  onDelete,
  onApprove,
  onReject,
}: {
  group: ProjectGroup;
  expanded: boolean;
  onToggleExpanded: () => void;
  enumLabel: ReturnType<typeof useEnumLabel>;
  onEdit: (entry: TimeEntry) => void;
  onDelete: (entry: TimeEntry) => void;
  onApprove: (entry: TimeEntry) => void;
  onReject: (entry: TimeEntry) => void;
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
            onEdit={() => onEdit(entry)}
            onDelete={() => onDelete(entry)}
            onApprove={() => onApprove(entry)}
            onReject={() => onReject(entry)}
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

/** One worker's entry for the day, styled like a small roster card. */
function TimeEntryCard({
  entry,
  workTypeLabel,
  onEdit,
  onDelete,
  onApprove,
  onReject,
}: {
  entry: TimeEntry;
  workTypeLabel: string;
  onEdit: () => void;
  onDelete: () => void;
  onApprove: () => void;
  onReject: () => void;
}) {
  const t = useT();
  const locked = entry.status === 'Approved';

  const range = entry.endedAt
    ? `${formatTimeOfDay(entry.startedAt)}–${formatTimeOfDay(entry.endedAt)}`
    : `${formatTimeOfDay(entry.startedAt)}–…`;
  const worked =
    entry.workedMinutes === null
      ? t('timeEntries.running')
      : t('timeEntries.hoursShort', splitMinutes(entry.workedMinutes));

  return (
    <Paper variant="outlined" sx={{ p: 1.25 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
        <Avatar sx={{ width: 30, height: 30, fontSize: '0.8rem' }}>
          {employeeInitials(entry.employeeName)}
        </Avatar>
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
            {entry.employeeName}
          </Typography>
          <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5, mt: 0.25 }}>
            <Chip size="small" variant="outlined" label={workTypeLabel} />
            <StatusChip status={entry.status} kind="timeEntryStatus" size="small" />
            {entry.autoClosed && (
              <Tooltip title={t('timeEntries.autoClosedHint')}>
                <Chip size="small" color="warning" variant="outlined" label={t('timeEntries.autoClosed')} />
              </Tooltip>
            )}
          </Stack>
        </Box>
        <CheckInIcon locationCorrect={entry.locationCorrect} timeCorrect={entry.timeCorrect} />
      </Stack>

      <Stack sx={{ mt: 1, pl: 4.75 }}>
        <Typography variant="caption" color="text.secondary">
          {range} · {worked}
        </Typography>
      </Stack>

      <Stack direction="row" spacing={0.25} sx={{ mt: 0.5, justifyContent: 'flex-end' }}>
        <ReviewButtons entry={entry} onApprove={onApprove} onReject={onReject} />
        <Tooltip title={locked ? t('timeEntries.locked') : t('common.edit')}>
          {/* A disabled button swallows its own events, so the tooltip
              needs a wrapper that still receives them. */}
          <span>
            <IconButton size="small" disabled={locked} onClick={onEdit}>
              <EditOutlined fontSize="small" />
            </IconButton>
          </span>
        </Tooltip>
        <Tooltip title={locked ? t('timeEntries.locked') : t('common.delete')}>
          <span>
            <IconButton size="small" disabled={locked} onClick={onDelete}>
              <DeleteOutlined fontSize="small" />
            </IconButton>
          </span>
        </Tooltip>
      </Stack>
    </Paper>
  );
}

/** One of the four check-in states, or "unknown" — resolved once so the icon and the legend never drift apart. */
type CheckInState = 'unknown' | 'bothWrong' | 'wrongLocation' | 'wrongTime' | 'bothCorrect';

function checkInState(locationCorrect: boolean | null, timeCorrect: boolean | null): CheckInState {
  if (locationCorrect === null && timeCorrect === null) return 'unknown';

  const locationWrong = locationCorrect === false;
  const timeWrong = timeCorrect === false;

  if (locationWrong && timeWrong) return 'bothWrong';
  if (locationWrong) return 'wrongLocation';
  if (timeWrong) return 'wrongTime';
  return 'bothCorrect';
}

const CHECK_IN_PRESENTATION: Record<
  CheckInState,
  { Icon: typeof TaskAltOutlined; color: string; bgcolor: string; labelKey: MessageKey }
> = {
  bothCorrect: {
    Icon: TaskAltOutlined,
    color: 'success.dark',
    bgcolor: 'success.light',
    labelKey: 'timeEntries.checkInBothCorrect',
  },
  wrongLocation: {
    Icon: LocationOffOutlined,
    color: 'warning.dark',
    bgcolor: 'warning.light',
    labelKey: 'timeEntries.checkInWrongLocation',
  },
  wrongTime: {
    Icon: ScheduleOutlined,
    color: 'warning.dark',
    bgcolor: 'warning.light',
    labelKey: 'timeEntries.checkInWrongTime',
  },
  bothWrong: {
    Icon: ReportOutlined,
    color: 'error.dark',
    bgcolor: 'error.light',
    labelKey: 'timeEntries.checkInBothWrong',
  },
  unknown: {
    Icon: HelpOutlineOutlined,
    color: 'text.disabled',
    bgcolor: 'action.hover',
    labelKey: 'timeEntries.checkInUnknown',
  },
};

/**
 * Whether the clock-in was at the right place and time, as a small coloured
 * badge — a bare icon reads as decoration, a filled circle reads as status.
 */
function CheckInIcon({
  locationCorrect,
  timeCorrect,
}: {
  locationCorrect: boolean | null;
  timeCorrect: boolean | null;
}) {
  const t = useT();
  const { Icon, color, bgcolor, labelKey } = CHECK_IN_PRESENTATION[
    checkInState(locationCorrect, timeCorrect)
  ];

  return (
    <Tooltip title={t(labelKey)}>
      <Box
        sx={{
          width: 28,
          height: 28,
          borderRadius: '50%',
          bgcolor,
          color,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexShrink: 0,
        }}
      >
        <Icon fontSize="small" sx={{ color: 'inherit' }} />
      </Box>
    </Tooltip>
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

  return (
    <ConfirmDialog
      open={!!entry}
      title={t('timeEntries.approveTitle')}
      description={t('timeEntries.approveBody')}
      confirmLabel={t('timeEntries.approve')}
      loading={review.isPending}
      onConfirm={() => {
        review.mutate({ approve: true }, { onSuccess: onClose });
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
  const [note, setNote] = useState('');

  const close = () => {
    setNote('');
    onClose();
  };

  return (
    <Dialog open={!!entry} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('timeEntries.rejectTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          {t('timeEntries.rejectHint')}
        </DialogContentText>
        <TextField
          autoFocus
          fullWidth
          multiline
          minRows={2}
          label={t('timeEntries.rejectReason')}
          value={note}
          onChange={(event) => setNote(event.target.value)}
          error={!!review.error}
          helperText={review.error?.message}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          color="warning"
          disabled={!note.trim() || review.isPending}
          onClick={() => {
            review.mutate({ approve: false, note: note.trim() }, { onSuccess: close });
          }}
        >
          {t('timeEntries.reject')}
        </Button>
      </DialogActions>
    </Dialog>
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
