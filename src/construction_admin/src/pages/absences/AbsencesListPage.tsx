import {
  AddOutlined,
  CheckOutlined,
  CloseOutlined,
  DeleteOutlined,
} from '@mui/icons-material';
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
  MenuItem,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import type { AbsenceListQuery } from '../../api/absences';
import { exportsApi } from '../../api/exports';
import { absenceStatuses, absenceTypes, type Absence, type AbsenceType } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { DateQuickFilters } from '../../components/DateQuickFilters';
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import {
  useAbsenceBalanceQuery,
  useAbsencesQuery,
  useDeleteAbsence,
  useReviewAbsence,
} from '../../features/absences/useAbsences';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate, lastYearRange } from '../../utils/formatting';
import { BookAbsenceDialog } from './BookAbsenceDialog';

export function AbsencesListPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('startDate', 'desc');

  // The one question a supervisor opens this screen to answer.
  const [pendingOnly, setPendingOnly] = useState(false);
  const [type, setType] = useState<AbsenceType | ''>('');
  const [booking, setBooking] = useState(false);
  const [quickDate, setQuickDate] = useState<string | null>(null);

  const query: AbsenceListQuery = useMemo(
    () => ({
      ...list.query,
      // The API has no text search on this collection; sending one would be
      // ignored, and leaving it in the key would refetch on every keystroke.
      search: undefined,
      status: pendingOnly ? 'Requested' : undefined,
      type: type || undefined,
      // Overlap, not containment: whoever is away on that day, not only leave
      // that starts on it.
      from: quickDate ?? undefined,
      to: quickDate ?? undefined,
    }),
    [list.query, pendingOnly, quickDate, type],
  );

  const { data, isLoading, isError, error, refetch } = useAbsencesQuery(query);
  const remove = useDeleteWithConfirm<Absence>(useDeleteAbsence());

  const [approving, setApproving] = useState<Absence | null>(null);
  const [refusing, setRefusing] = useState<Absence | null>(null);

  const columns: GridColDef<Absence>[] = useMemo(
    () => [
      {
        field: 'employeeName',
        headerName: t('absences.employee'),
        flex: 1,
        minWidth: 160,
      },
      {
        field: 'type',
        headerName: t('absences.type'),
        width: 150,
        valueGetter: (_value, row) => enumLabel('absenceType', row.type),
      },
      {
        field: 'startDate',
        headerName: t('absences.startDate'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'endDate',
        headerName: t('absences.endDate'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'dayCount',
        headerName: t('absences.dayCount'),
        width: 100,
        sortable: false,
        valueGetter: (_value, row) => t('absences.days', { count: row.dayCount }),
      },
      {
        field: 'status',
        headerName: t('absences.status'),
        width: 140,
        renderCell: (params) => (
          <StatusChip status={params.row.status} kind="absenceStatus" />
        ),
      },
      {
        field: 'actions',
        headerName: '',
        width: 140,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <RowActions
            absence={params.row}
            onApprove={() => setApproving(params.row)}
            onRefuse={() => setRefusing(params.row)}
            onWithdraw={() => remove.request(params.row)}
          />
        ),
      },
    ],
    [enumLabel, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('absences.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('absences.add'),
          icon: <AddOutlined />,
          onClick: () => setBooking(true),
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
          label={t('absences.pendingOnly')}
        />
        <TextField
          select
          size="small"
          label={t('absences.type')}
          value={type}
          onChange={(event) => {
            setType(event.target.value as AbsenceType | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">{t('absences.allTypes')}</MenuItem>
          {absenceTypes.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('absenceType', value)}
            </MenuItem>
          ))}
        </TextField>
        <DateQuickFilters
          value={quickDate}
          onChange={(date) => {
            setQuickDate(date);
            list.resetToFirstPage();
          }}
        />
        <StatusLegend kind="absenceStatus" values={absenceStatuses} />

        {/* Exports the last year rather than whatever's on screen: a
            spreadsheet of one day's leave is not what anyone opens this for. */}
        <ExportButton
          onExport={(language) => exportsApi.absences({ ...lastYearRange(), language })}
        />
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

      <BookAbsenceDialog open={booking} onClose={() => setBooking(false)} />
      <ApproveDialog absence={approving} onClose={() => setApproving(null)} />
      <RefuseDialog absence={refusing} onClose={() => setRefusing(null)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('absences.deleteTitle')}
        description={
          remove.pending
            ? t('absences.deleteBody', { name: remove.pending.employeeName })
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
 * Granting and refusing only make sense while a request is unanswered, and
 * granted leave is refused rather than withdrawn — withdrawing it would leave
 * work planned around days somebody is still away.
 */
function RowActions({
  absence,
  onApprove,
  onRefuse,
  onWithdraw,
}: {
  absence: Absence;
  onApprove: () => void;
  onRefuse: () => void;
  onWithdraw: () => void;
}) {
  const t = useT();
  const isPending = absence.status === 'Requested';

  return (
    <Stack direction="row" spacing={0.5}>
      <Tooltip title={isPending ? t('absences.approve') : t('absences.answered')}>
        {/* A disabled button swallows its own events, so the tooltip needs a
            wrapper that still receives them. */}
        <span>
          <IconButton
            size="small"
            color="success"
            disabled={!isPending}
            onClick={onApprove}
          >
            <CheckOutlined fontSize="small" />
          </IconButton>
        </span>
      </Tooltip>
      <Tooltip title={isPending ? t('absences.reject') : t('absences.answered')}>
        <span>
          <IconButton
            size="small"
            color="warning"
            disabled={!isPending}
            onClick={onRefuse}
          >
            <CloseOutlined fontSize="small" />
          </IconButton>
        </span>
      </Tooltip>
      <Tooltip
        title={
          absence.status === 'Approved'
            ? t('absences.grantedLocked')
            : t('common.delete')
        }
      >
        <span>
          <IconButton
            size="small"
            disabled={absence.status === 'Approved'}
            onClick={onWithdraw}
          >
            <DeleteOutlined fontSize="small" />
          </IconButton>
        </span>
      </Tooltip>
    </Stack>
  );
}

/** Only annual leave draws down a balance — the other kinds don't have one. */
function useAbsenceBalanceLine(absence: Absence | null): string | null {
  const t = useT();
  const year = absence ? Number(absence.startDate.slice(0, 4)) : undefined;
  const balance = useAbsenceBalanceQuery(
    absence?.type === 'AnnualLeave' ? absence.employeeId : undefined,
    year,
  );

  if (absence?.type !== 'AnnualLeave' || !balance.data) return null;

  const { allowanceDays, usedDays, remainingDays } = balance.data;

  return t(remainingDays < 0 ? 'absences.balanceOver' : 'absences.balance', {
    used: usedDays,
    allowance: allowanceDays,
    remaining: Math.abs(remainingDays),
  });
}

function ApproveDialog({
  absence,
  onClose,
}: {
  absence: Absence | null;
  onClose: () => void;
}) {
  const t = useT();
  const review = useReviewAbsence();
  const balanceLine = useAbsenceBalanceLine(absence);

  return (
    <ConfirmDialog
      open={!!absence}
      title={t('absences.approveTitle')}
      description={
        balanceLine ? `${t('absences.approveBody')} ${balanceLine}.` : t('absences.approveBody')
      }
      confirmLabel={t('absences.approve')}
      loading={review.isPending}
      onConfirm={() => {
        if (!absence) return;

        review.mutate(
          { id: absence.id, input: { approve: true } },
          { onSuccess: onClose },
        );
      }}
      onCancel={onClose}
    />
  );
}

/** Refusing needs a reason; the API refuses one without it. */
function RefuseDialog({
  absence,
  onClose,
}: {
  absence: Absence | null;
  onClose: () => void;
}) {
  const t = useT();
  const review = useReviewAbsence();
  const balanceLine = useAbsenceBalanceLine(absence);
  const [note, setNote] = useState('');

  const close = () => {
    setNote('');
    onClose();
  };

  return (
    <Dialog open={!!absence} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('absences.rejectTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: balanceLine ? 0.5 : 2 }}>
          {t('absences.rejectHint')}
        </DialogContentText>
        {balanceLine && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            {balanceLine}
          </Typography>
        )}
        <TextField
          autoFocus
          fullWidth
          multiline
          minRows={2}
          label={t('absences.rejectReason')}
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
            if (!absence) return;

            review.mutate(
              { id: absence.id, input: { approve: false, note: note.trim() } },
              { onSuccess: close },
            );
          }}
        >
          {t('absences.reject')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
