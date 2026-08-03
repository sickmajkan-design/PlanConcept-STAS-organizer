import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControlLabel,
  IconButton,
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
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import {
  useDeleteTimeEntry,
  useReviewTimeEntry,
  useTimeEntriesQuery,
} from '../../features/timeEntries/useTimeEntries';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatTimeOfDay, splitMinutes } from '../../utils/formatting';
import { ReviewButtons } from './ReviewButtons';

export function TimeEntriesListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('startedAt', 'desc');

  // Two switches rather than a status dropdown: these are the two questions a
  // supervisor actually opens this screen to answer — who is on site now, and
  // what is waiting on me.
  const [openOnly, setOpenOnly] = useState(false);
  const [pendingOnly, setPendingOnly] = useState(false);

  const query: TimeEntryListQuery = useMemo(
    () => ({
      ...list.query,
      // The API has no text search on this collection; sending one would be
      // ignored, and leaving it in the key would refetch on every keystroke.
      search: undefined,
      openOnly: openOnly || undefined,
      status: pendingOnly ? 'Submitted' : undefined,
    }),
    [list.query, openOnly, pendingOnly],
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
        field: 'startedAt',
        headerName: t('timeEntries.startedAt'),
        width: 170,
        valueGetter: (_value, row) =>
          `${formatDate(row.startedAt)} ${formatTimeOfDay(row.startedAt)}`,
      },
      {
        field: 'endedAt',
        headerName: t('timeEntries.endedAt'),
        width: 110,
        sortable: true,
        valueGetter: (_value, row) =>
          row.endedAt ? formatTimeOfDay(row.endedAt) : '—',
      },
      {
        field: 'workedMinutes',
        headerName: t('timeEntries.worked'),
        width: 120,
        sortable: false,
        valueGetter: (_value, row) =>
          row.workedMinutes === null
            ? t('timeEntries.running')
            : t('timeEntries.hoursShort', splitMinutes(row.workedMinutes)),
      },
      {
        field: 'projectName',
        headerName: t('timeEntries.project'),
        flex: 1,
        minWidth: 140,
        valueGetter: (v) => v || t('timeEntries.noProject'),
      },
      {
        field: 'workType',
        headerName: t('timeEntries.workType'),
        width: 130,
        valueGetter: (_value, row) => enumLabel('workType', row.workType),
      },
      {
        field: 'status',
        headerName: t('timeEntries.status'),
        width: 150,
        renderCell: (params) => (
          <StatusChip status={params.row.status} kind="timeEntryStatus" />
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
        <Button size="small" onClick={() => navigate(paths.timeEntrySummary)}>
          {t('timeEntries.summary')}
        </Button>
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
