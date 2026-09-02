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
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { EmployeeRateListQuery } from '../../api/costs';
import type { EmployeeRate, RateType } from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useDeleteEmployeeRate,
  useEmployeeRatesQuery,
  useEmployeeRatesSummaryQuery,
  useSetEmployeeRate,
  useUpdateEmployeeRate,
} from '../../features/costs/useCosts';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime, formatMoney } from '../../utils/formatting';

export function RatesPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { locale } = useI18n();
  const { user } = useAuth();
  const list = useListQueryState('startDate', 'desc');

  const [currentOnly, setCurrentOnly] = useState(true);
  const [setting, setSetting] = useState(false);
  const [editing, setEditing] = useState<EmployeeRate | null>(null);

  const query: EmployeeRateListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      currentOnly: currentOnly || undefined,
    }),
    [currentOnly, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useEmployeeRatesQuery(query);
  const { data: summary } = useEmployeeRatesSummaryQuery(query);
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
        field: 'rateType',
        headerName: t('rates.rateType'),
        width: 110,
        valueGetter: (value) => enumLabel('rateType', value as string),
      },
      {
        field: 'hourlyRate',
        headerName: t('rates.hourlyRate'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? '—' : formatMoney(value as number, locale),
      },
      {
        field: 'dailyRate',
        headerName: t('rates.dailyRate'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? '—' : formatMoney(value as number, locale),
      },
      {
        field: 'weekendHourlyRate',
        headerName: t('rates.weekendHourlyRate'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? t('rates.noPremium') : formatMoney(value as number, locale),
      },
      {
        field: 'holidayHourlyRate',
        headerName: t('rates.holidayHourlyRate'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? t('rates.noPremium') : formatMoney(value as number, locale),
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
        field: 'createdAt',
        headerName: t('rates.createdAt'),
        width: 160,
        valueGetter: (value) => formatDateTime(value as string),
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
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              remove.request(params.row);
            }}
          >
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
        title={t('rates.title')}
        description={t('rates.description')}
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

      {summary && (
        <Paper variant="outlined" sx={{ px: 2, py: 1, mb: 2 }}>
          <Typography variant="body2" color="text.secondary">
            {t('rates.summaryAverage')}:{' '}
            <strong>
              {summary.averageHourlyRate === null
                ? '—'
                : formatMoney(summary.averageHourlyRate, locale)}
            </strong>
          </Typography>
        </Paper>
      )}

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
        onRowDoubleClick={(row) => setEditing(row)}
      />

      <RateDialog open={setting} onClose={() => setSetting(false)} />
      <RateDialog
        open={!!editing}
        editingRate={editing}
        onClose={() => setEditing(null)}
        canAdminister={canAdministerAccounts(user)}
      />

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

function RateDialog({
  open,
  editingRate,
  onClose,
  canAdminister,
}: {
  open: boolean;
  editingRate?: EmployeeRate | null;
  onClose: () => void;
  canAdminister?: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: employees } = useAllEmployeesQuery();
  const set = useSetEmployeeRate();
  const update = useUpdateEmployeeRate();
  const isEditing = !!editingRate;

  const [employeeId, setEmployeeId] = useState('');
  const [rateType, setRateType] = useState<RateType>('Hourly');
  const [hourlyRate, setHourlyRate] = useState('');
  const [weekendHourlyRate, setWeekendHourlyRate] = useState('');
  const [holidayHourlyRate, setHolidayHourlyRate] = useState('');
  const [dailyRate, setDailyRate] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [note, setNote] = useState('');

  const resetSet = set.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetSet();
    resetUpdate();

    if (editingRate) {
      setEmployeeId(editingRate.employeeId);
      setRateType(editingRate.rateType);
      setHourlyRate(editingRate.hourlyRate === null ? '' : String(editingRate.hourlyRate));
      setWeekendHourlyRate(
        editingRate.weekendHourlyRate === null ? '' : String(editingRate.weekendHourlyRate),
      );
      setHolidayHourlyRate(
        editingRate.holidayHourlyRate === null ? '' : String(editingRate.holidayHourlyRate),
      );
      setDailyRate(editingRate.dailyRate === null ? '' : String(editingRate.dailyRate));
      setStartDate(editingRate.startDate);
      setEndDate(editingRate.endDate ?? '');
      setNote(editingRate.note ?? '');
    } else {
      setEmployeeId('');
      setRateType('Hourly');
      setHourlyRate('');
      setWeekendHourlyRate('');
      setHolidayHourlyRate('');
      setDailyRate('');
      setStartDate('');
      setEndDate('');
      setNote('');
    }
  }, [open, editingRate, resetSet, resetUpdate]);

  const isHourly = rateType === 'Hourly';

  const parsedRate = Number(hourlyRate);
  const rateIsValid =
    !isHourly || (hourlyRate.trim() !== '' && !Number.isNaN(parsedRate) && parsedRate > 0);

  const parsedWeekendRate = Number(weekendHourlyRate);
  const weekendRateIsValid =
    weekendHourlyRate.trim() === ''
    || (!Number.isNaN(parsedWeekendRate) && parsedWeekendRate > 0);

  const parsedHolidayRate = Number(holidayHourlyRate);
  const holidayRateIsValid =
    holidayHourlyRate.trim() === ''
    || (!Number.isNaN(parsedHolidayRate) && parsedHolidayRate > 0);

  const parsedDailyRate = Number(dailyRate);
  const dailyRateIsValid =
    isHourly || (dailyRate.trim() !== '' && !Number.isNaN(parsedDailyRate) && parsedDailyRate > 0);

  const datesAreValid = !startDate || !endDate || endDate >= startDate;

  const canSubmit =
    employeeId !== ''
    && rateIsValid
    && weekendRateIsValid
    && holidayRateIsValid
    && dailyRateIsValid
    && datesAreValid;
  const mutation = isEditing ? update : set;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const weekendRate = weekendHourlyRate.trim() === '' ? null : parsedWeekendRate;
    const holidayRate = holidayHourlyRate.trim() === '' ? null : parsedHolidayRate;

    const shared = {
      employeeId,
      rateType,
      hourlyRate: isHourly ? parsedRate : null,
      weekendHourlyRate: isHourly ? weekendRate : null,
      holidayHourlyRate: isHourly ? holidayRate : null,
      dailyRate: isHourly ? null : parsedDailyRate,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        {
          id: editingRate.id,
          input: { ...shared, startDate, endDate: endDate || null },
        },
        { onSuccess: onClose },
      );
    } else {
      set.mutate(
        { ...shared, startDate: startDate || null, endDate: endDate || null },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isEditing ? t('rates.editTitle') : t('rates.add')}</DialogTitle>
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
              // Editing a rate is a narrow correction — it deliberately does
              // not re-run the chain that closes/reopens neighbouring rates
              // when an employee changes. Locking this field is what keeps
              // that safe: without it, "fixing a typo" could silently move a
              // historical rate onto a different employee's pay record.
              disabled={isEditing}
              helperText={isEditing ? t('rates.employeeLockedHint') : undefined}
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
              select
              fullWidth
              label={t('rates.rateType')}
              value={rateType}
              onChange={(event) => setRateType(event.target.value as RateType)}
              helperText={t('rates.rateTypeHint')}
            >
              <MenuItem value="Hourly">{enumLabel('rateType', 'Hourly')}</MenuItem>
              <MenuItem value="Daily">{enumLabel('rateType', 'Daily')}</MenuItem>
            </TextField>
          </Grid>

          {isHourly ? (
            <>
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
                  type="number"
                  fullWidth
                  label={t('rates.weekendHourlyRate')}
                  value={weekendHourlyRate}
                  onChange={(event) => setWeekendHourlyRate(event.target.value)}
                  error={weekendHourlyRate.trim() !== '' && !weekendRateIsValid}
                  helperText={
                    weekendHourlyRate.trim() !== '' && !weekendRateIsValid
                      ? t('rates.mustBePositive')
                      : undefined
                  }
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  type="number"
                  fullWidth
                  label={t('rates.holidayHourlyRate')}
                  value={holidayHourlyRate}
                  onChange={(event) => setHolidayHourlyRate(event.target.value)}
                  error={holidayHourlyRate.trim() !== '' && !holidayRateIsValid}
                  helperText={
                    holidayHourlyRate.trim() !== '' && !holidayRateIsValid
                      ? t('rates.mustBePositive')
                      : undefined
                  }
                />
              </Grid>
            </>
          ) : (
            <Grid size={12}>
              <TextField
                type="number"
                fullWidth
                label={t('rates.dailyRate')}
                value={dailyRate}
                onChange={(event) => setDailyRate(event.target.value)}
                error={dailyRate.trim() !== '' && !dailyRateIsValid}
                helperText={
                  dailyRate.trim() !== '' && !dailyRateIsValid
                    ? t('rates.mustBePositive')
                    : t('rates.dailyRateHint')
                }
              />
            </Grid>
          )}

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

          <Grid size={12}>
            <Alert severity="info">
              {isEditing ? t('rates.editHint') : t('rates.supersedeHint')}
            </Alert>
          </Grid>

          {isEditing && (
            <>
              <Grid size={12}>
                <AttachmentList
                  ownerType="EmployeeRate"
                  ownerId={editingRate.id}
                  categories={['Contract', 'Other']}
                  canUpload={canAdminister}
                  canDelete={canAdminister}
                />
              </Grid>
              {canAdminister && (
                <Grid size={12}>
                  <AuditHistoryCard entityName="EmployeeRate" entityId={editingRate.id} />
                </Grid>
              )}
            </>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || mutation.isPending}
          onClick={submit}
        >
          {isEditing ? t('common.save') : t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
