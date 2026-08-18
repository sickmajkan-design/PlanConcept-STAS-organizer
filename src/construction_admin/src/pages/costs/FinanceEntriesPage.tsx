import { AddOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { FinanceEntryListQuery } from '../../api/costs';
import { financeEntryKinds, type FinanceEntry, type FinanceEntryKind } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useDeleteFinanceEntry,
  useFinanceEntriesQuery,
  useRecordFinanceEntry,
} from '../../features/costs/useCosts';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

export function FinanceEntriesPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { locale } = useI18n();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<FinanceEntryKind | ''>('');
  const [recording, setRecording] = useState(false);

  const query: FinanceEntryListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      kind: kind || undefined,
    }),
    [kind, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useFinanceEntriesQuery(query);
  const remove = useDeleteWithConfirm<FinanceEntry>(useDeleteFinanceEntry());

  const columns: GridColDef<FinanceEntry>[] = useMemo(
    () => [
      {
        field: 'employeeName',
        headerName: t('financeEntries.employee'),
        flex: 1,
        minWidth: 180,
      },
      {
        field: 'kind',
        headerName: t('financeEntries.kind'),
        width: 120,
        valueGetter: (_value, row) => enumLabel('financeEntryKind', row.kind),
      },
      {
        field: 'amount',
        headerName: t('financeEntries.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'hoursWorked',
        headerName: t('financeEntries.hoursWorked'),
        width: 90,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => (value === null ? '—' : value),
      },
      {
        field: 'occurredOn',
        headerName: t('financeEntries.occurredOn'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'projectName',
        headerName: t('financeEntries.project'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || t('financeEntries.noProject'),
      },
      {
        field: 'actions',
        headerName: '',
        width: 60,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <IconButton size="small" onClick={() => remove.request(params.row)}>
            <DeleteOutlined fontSize="small" />
          </IconButton>
        ),
      },
    ],
    [enumLabel, locale, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('financeEntries.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('financeEntries.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center' }}>
        <TextField
          select
          size="small"
          label={t('financeEntries.kind')}
          value={kind}
          onChange={(event) => {
            setKind(event.target.value as FinanceEntryKind | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">{t('financeEntries.allKinds')}</MenuItem>
          {financeEntryKinds.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('financeEntryKind', value)}
            </MenuItem>
          ))}
        </TextField>
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

      <RecordFinanceEntryDialog open={recording} onClose={() => setRecording(false)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('financeEntries.deleteTitle')}
        description={remove.pending ? t('financeEntries.deleteBody') : ''}
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

function RecordFinanceEntryDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: employees } = useAllEmployeesQuery();
  const { data: projects } = useAllProjectsQuery();
  const record = useRecordFinanceEntry();

  const [employeeId, setEmployeeId] = useState('');
  const [kind, setKind] = useState<FinanceEntryKind>('WorkerPaymentHourly');
  const [amount, setAmount] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [projectId, setProjectId] = useState('');
  const [hoursWorked, setHoursWorked] = useState('');
  const [note, setNote] = useState('');

  const reset = record.reset;

  useEffect(() => {
    if (open) {
      setEmployeeId('');
      setKind('WorkerPaymentHourly');
      setAmount('');
      setOccurredOn('');
      setProjectId('');
      setHoursWorked('');
      setNote('');
      reset();
    }
  }, [open, reset]);

  const isHourly = kind === 'WorkerPaymentHourly';

  const parsedAmount = Number(amount);
  const amountIsValid = amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount >= 0;

  const parsedHours = Number(hoursWorked);
  const hoursAreValid =
    !isHourly || (hoursWorked.trim() !== '' && !Number.isNaN(parsedHours) && parsedHours >= 0);

  const canSubmit = employeeId !== '' && amountIsValid && hoursAreValid;
  const error = record.isError ? toApiError(record.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('financeEntries.add')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('financeEntries.employee')}
              value={employeeId}
              onChange={(event) => setEmployeeId(event.target.value)}
            >
              {employees?.items.map((employee) => (
                <MenuItem key={employee.id} value={employee.id}>
                  {employee.firstName} {employee.lastName}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('financeEntries.kind')}
              value={kind}
              onChange={(event) => setKind(event.target.value as FinanceEntryKind)}
            >
              {financeEntryKinds.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('financeEntryKind', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('financeEntries.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              error={amount.trim() !== '' && !amountIsValid}
            />
          </Grid>

          {isHourly && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                type="number"
                fullWidth
                label={t('financeEntries.hoursWorked')}
                value={hoursWorked}
                onChange={(event) => setHoursWorked(event.target.value)}
                error={hoursWorked.trim() !== '' && !hoursAreValid}
                helperText={
                  hoursWorked.trim() === '' ? t('financeEntries.hourlyNeedsHours') : undefined
                }
              />
            </Grid>
          )}

          <Grid size={{ xs: 12, sm: isHourly ? 6 : 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('financeEntries.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('financeEntries.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
            >
              <MenuItem value="">{t('financeEntries.noProject')}</MenuItem>
              {projects?.items.map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('financeEntries.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || record.isPending}
          onClick={() =>
            record.mutate(
              {
                employeeId,
                kind,
                amount: parsedAmount,
                occurredOn: occurredOn || null,
                projectId: projectId || null,
                hoursWorked: isHourly ? parsedHours : null,
                note: note.trim() || null,
              },
              { onSuccess: onClose },
            )
          }
        >
          {t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
