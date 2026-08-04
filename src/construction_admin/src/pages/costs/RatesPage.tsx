import { AddOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { EmployeeRateListQuery } from '../../api/costs';
import type { EmployeeRate } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useDeleteEmployeeRate,
  useEmployeeRatesQuery,
  useSetEmployeeRate,
} from '../../features/costs/useCosts';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

export function RatesPage() {
  const t = useT();
  const { locale } = useI18n();
  const list = useListQueryState('startDate', 'desc');

  const [currentOnly, setCurrentOnly] = useState(true);
  const [setting, setSetting] = useState(false);

  const query: EmployeeRateListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      currentOnly: currentOnly || undefined,
    }),
    [currentOnly, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useEmployeeRatesQuery(query);
  const remove = useDeleteWithConfirm<EmployeeRate>(useDeleteEmployeeRate());

  const columns: GridColDef<EmployeeRate>[] = useMemo(
    () => [
      {
        field: 'employeeName',
        headerName: t('rates.employee'),
        flex: 1,
        minWidth: 180,
      },
      {
        field: 'hourlyRate',
        headerName: t('rates.hourlyRate'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'startDate',
        headerName: t('rates.startDate'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'endDate',
        headerName: t('rates.endDate'),
        width: 140,
        renderCell: (params) =>
          params.row.endDate === null ? (
            <Chip label={t('rates.open')} color="success" size="small" />
          ) : (
            formatDate(params.row.endDate)
          ),
      },
      {
        field: 'setByName',
        headerName: t('rates.setBy'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
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
    [locale, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('rates.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('rates.add'),
          icon: <AddOutlined />,
          onClick: () => setSetting(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center' }}>
        <FormControlLabel
          control={
            <Switch
              checked={currentOnly}
              onChange={(event) => {
                setCurrentOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('rates.currentOnly')}
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

      <SetRateDialog open={setting} onClose={() => setSetting(false)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('rates.deleteTitle')}
        description={t('rates.deleteBody')}
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

function SetRateDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const { data: employees } = useAllEmployeesQuery();
  const set = useSetEmployeeRate();

  const [employeeId, setEmployeeId] = useState('');
  const [hourlyRate, setHourlyRate] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [note, setNote] = useState('');

  const reset = set.reset;

  useEffect(() => {
    if (open) {
      setEmployeeId('');
      setHourlyRate('');
      setStartDate('');
      setEndDate('');
      setNote('');
      reset();
    }
  }, [open, reset]);

  const parsedRate = Number(hourlyRate);
  const rateIsValid =
    hourlyRate.trim() !== '' && !Number.isNaN(parsedRate) && parsedRate > 0;
  const datesAreValid = !startDate || !endDate || endDate >= startDate;

  const canSubmit = employeeId !== '' && rateIsValid && datesAreValid;
  const error = set.isError ? toApiError(set.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('rates.add')}</DialogTitle>
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
              label={t('rates.employee')}
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

          <Grid size={12}>
            <TextField
              type="number"
              fullWidth
              label={t('rates.hourlyRate')}
              value={hourlyRate}
              onChange={(event) => setHourlyRate(event.target.value)}
              error={hourlyRate.trim() !== '' && !rateIsValid}
              helperText={
                hourlyRate.trim() !== '' && !rateIsValid
                  ? t('rates.mustBePositive')
                  : undefined
              }
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('rates.startDate')}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('rates.endDate')}
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              error={!datesAreValid}
              helperText={!datesAreValid ? t('rates.endsBeforeStart') : undefined}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('rates.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {/* Says what setting a rate actually does, because the answer is not
              obvious and it is not undoable by editing. */}
          <Grid size={12}>
            <Alert severity="info">{t('rates.supersedeHint')}</Alert>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || set.isPending}
          onClick={() =>
            set.mutate(
              {
                employeeId,
                hourlyRate: parsedRate,
                startDate: startDate || null,
                endDate: endDate || null,
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
