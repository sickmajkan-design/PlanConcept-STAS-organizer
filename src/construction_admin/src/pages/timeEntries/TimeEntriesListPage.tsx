import {
  AddOutlined,
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
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControlLabel,
  IconButton,
  Popover,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { TimeEntryListQuery } from '../../api/timeEntries';
import type { TimeEntry } from '../../api/types';
import { timeEntryStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { DateQuickFilters } from '../../components/DateQuickFilters';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import {
  useDeleteTimeEntry,
  useReviewTimeEntry,
  useTimeEntriesQuery,
} from '../../features/timeEntries/useTimeEntries';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatTimeOfDay, splitMinutes } from '../../utils/formatting';
import { ReviewButtons } from './ReviewButtons';

/** The instant just after the given local day ends. */
function endOfLocalDay(date: string): string {
  const next = new Date(`${date}T00:00`);
  next.setDate(next.getDate() + 1);

  return next.toISOString();
}

export function TimeEntriesListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  // Chronological by default — whoever clocked in first is listed first.
  const list = useListQueryState('startedAt', 'asc');

  // Two switches rather than a status dropdown: these are the two questions a
  // supervisor actually opens this screen to answer — who is on site now, and
  // what is waiting on me.
  const [openOnly, setOpenOnly] = useState(false);
  const [pendingOnly, setPendingOnly] = useState(false);
  const [quickDate, setQuickDate] = useState<string | null>(null);

  const query: TimeEntryListQuery = useMemo(
    () => ({
      ...list.query,
      // The API has no text search on this collection; sending one would be
      // ignored, and leaving it in the key would refetch on every keystroke.
      search: undefined,
      openOnly: openOnly || undefined,
      status: pendingOnly ? 'Submitted' : undefined,
      from: quickDate ? new Date(`${quickDate}T00:00`).toISOString() : undefined,
      to: quickDate ? endOfLocalDay(quickDate) : undefined,
    }),
    [list.query, openOnly, pendingOnly, quickDate],
  );

  const { data, isLoading, isError, error, refetch } = useTimeEntriesQuery(query);
  const remove = useDeleteWithConfirm<TimeEntry>(useDeleteTimeEntry());

  const [reviewing, setReviewing] = useState<TimeEntry | null>(null);
  const [approving, setApproving] = useState<TimeEntry | null>(null);

  const columns: GridColDef<TimeEntry>[] = useMemo(
    () => [
      {
        field: 'employeeName',
        headerName: t('timeEntries.employee'),
        flex: 1,
        minWidth: 160,
      },
      {
        // Sorts by the same field as before (startedAt); start, end, and the
        // worked duration are one fact to a supervisor's eye, so they render
        // as one two-line cell instead of three separate columns.
        field: 'startedAt',
        headerName: t('timeEntries.shift'),
        width: 175,
        renderCell: (params) => {
          const row = params.row;
          const range = row.endedAt
            ? `${formatTimeOfDay(row.startedAt)}–${formatTimeOfDay(row.endedAt)}`
            : `${formatTimeOfDay(row.startedAt)}–…`;
          const worked =
            row.workedMinutes === null
              ? t('timeEntries.running')
              : t('timeEntries.hoursShort', splitMinutes(row.workedMinutes));

          return (
            <Stack sx={{ py: 0.5, lineHeight: 1.2 }}>
              <Typography variant="body2">
                {formatDate(row.startedAt)} {range}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {worked}
              </Typography>
            </Stack>
          );
        },
      },
      {
        // Work type has too few distinct values to earn its own column, so
        // it rides along as a small chip under the project it was worked on.
        field: 'projectName',
        headerName: t('timeEntries.project'),
        flex: 1,
        minWidth: 170,
        renderCell: (params) => (
          <Stack sx={{ py: 0.5, lineHeight: 1.2 }}>
            <Typography variant="body2">
              {params.row.projectName || t('timeEntries.noProject')}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {enumLabel('workType', params.row.workType)}
            </Typography>
          </Stack>
        ),
      },
      {
        field: 'checkIn',
        headerName: t('timeEntries.checkIn'),
        width: 80,
        sortable: false,
        align: 'center',
        headerAlign: 'center',
        renderCell: (params) => (
          <CheckInIcon
            locationCorrect={params.row.locationCorrect}
            timeCorrect={params.row.timeCorrect}
          />
        ),
      },
      {
        field: 'status',
        headerName: t('timeEntries.status'),
        width: 190,
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <StatusChip status={params.row.status} kind="timeEntryStatus" />
            {params.row.autoClosed && (
              <Tooltip title={t('timeEntries.autoClosedHint')}>
                <Chip
                  size="small"
                  color="warning"
                  variant="outlined"
                  label={t('timeEntries.autoClosed')}
                />
              </Tooltip>
            )}
          </Stack>
        ),
      },
      {
        field: 'actions',
        headerName: '',
        width: 175,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5}>
            <ReviewButtons
              entry={params.row}
              onApprove={() => setApproving(params.row)}
              onReject={() => setReviewing(params.row)}
            />
            <Tooltip
              title={
                params.row.status === 'Approved'
                  ? t('timeEntries.locked')
                  : t('common.edit')
              }
            >
              {/* A disabled button swallows its own events, so the tooltip
                  needs a wrapper that still receives them — otherwise the
                  reason it is disabled is invisible. */}
              <span>
                <IconButton
                  size="small"
                  disabled={params.row.status === 'Approved'}
                  onClick={() => navigate(paths.timeEntryEdit(params.row.id))}
                >
                  <EditOutlined fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
            <Tooltip
              title={
                params.row.status === 'Approved'
                  ? t('timeEntries.locked')
                  : t('common.delete')
              }
            >
              <span>
                <IconButton
                  size="small"
                  disabled={params.row.status === 'Approved'}
                  onClick={() => remove.request(params.row)}
                >
                  <DeleteOutlined fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [enumLabel, navigate, remove, t],
  );

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
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <FormControlLabel
          control={
            <Switch
              checked={pendingOnly}
              onChange={(event) => {
                setPendingOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('timeEntries.pendingOnly')}
        />
        <FormControlLabel
          control={
            <Switch
              checked={openOnly}
              onChange={(event) => {
                setOpenOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('timeEntries.openOnly')}
        />
        <DateQuickFilters
          value={quickDate}
          onChange={(date) => {
            setQuickDate(date);
            list.resetToFirstPage();
          }}
        />
        <Button size="small" onClick={() => navigate(paths.timeEntrySummary)}>
          {t('timeEntries.summary')}
        </Button>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <StatusLegend kind="timeEntryStatus" values={timeEntryStatuses} />
          <CheckInLegend />
        </Stack>
      </Stack>

      <ResourceDataGrid
        data={data}
        columns={columns}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        paginationModel={list.paginationModel}
        onPaginationModelChange={list.setPaginationModel}
        sortModel={list.sortModel}
        onSortModelChange={list.setSortModel}
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
    </Box>
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
