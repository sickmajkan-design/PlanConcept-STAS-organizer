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
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { VehicleExpenseListQuery } from '../../api/costs';
import {
  vehicleExpenseKinds,
  type VehicleExpense,
  type VehicleExpenseKind,
} from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useDeleteVehicleExpense,
  useRecordVehicleExpense,
  useUpdateVehicleExpense,
  useVehicleExpensesQuery,
  useVehicleExpensesSummaryQuery,
} from '../../features/costs/useCosts';
import { useAllVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime, formatMoney, formatQuantity } from '../../utils/formatting';

export function VehicleExpensesPage() {
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<VehicleExpenseKind | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<VehicleExpense | null>(null);

  const query: VehicleExpenseListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      kind: kind || undefined,
    }),
    [kind, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useVehicleExpensesQuery(query);
  const { data: summary } = useVehicleExpensesSummaryQuery(query);
  const remove = useDeleteWithConfirm<VehicleExpense>(useDeleteVehicleExpense());

  const columns: GridColDef<VehicleExpense>[] = useMemo(
    () => [
      {
        field: 'occurredOn',
        headerName: t('vehicleExpenses.occurredOn'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'vehicleName',
        headerName: t('vehicleExpenses.vehicle'),
        flex: 1,
        minWidth: 200,
      },
      {
        field: 'kind',
        headerName: t('vehicleExpenses.kind'),
        width: 130,
        valueGetter: (_value, row) => enumLabel('vehicleExpenseKind', row.kind),
      },
      {
        field: 'amount',
        headerName: t('vehicleExpenses.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'litres',
        headerName: t('vehicleExpenses.litres'),
        width: 100,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? '—' : formatQuantity(value as number, locale),
      },
      {
        field: 'pricePerLitre',
        headerName: t('vehicleExpenses.pricePerLitre'),
        width: 120,
        align: 'right',
        headerAlign: 'right',
        sortable: false,
        valueGetter: (value) => formatMoney(value as number | null, locale),
      },
      {
        field: 'odometerKm',
        headerName: t('vehicleExpenses.odometer'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? '—' : formatQuantity(value as number, locale),
      },
      {
        field: 'recordedByName',
        headerName: t('vehicleExpenses.recordedBy'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'createdAt',
        headerName: t('vehicleExpenses.createdAt'),
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
        title={t('vehicleExpenses.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('vehicleExpenses.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <TextField
          select
          size="small"
          label={t('vehicleExpenses.kind')}
          value={kind}
          onChange={(event) => {
            setKind(event.target.value as VehicleExpenseKind | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">{t('vehicleExpenses.allKinds')}</MenuItem>
          {vehicleExpenseKinds.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('vehicleExpenseKind', value)}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {summary && (
        <Paper variant="outlined" sx={{ px: 2, py: 1, mb: 2 }}>
          <Stack direction="row" spacing={3}>
            <Typography variant="body2" color="text.secondary">
              {t('vehicleExpenses.summaryTotal')}:{' '}
              <strong>{formatMoney(summary.totalAmount, locale)}</strong>
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('vehicleExpenses.summaryLitres')}:{' '}
              <strong>{formatQuantity(summary.totalLitres, locale)}</strong>
            </Typography>
          </Stack>
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

      <VehicleExpenseDialog open={recording} onClose={() => setRecording(false)} />
      <VehicleExpenseDialog
        open={!!editing}
        editingExpense={editing}
        onClose={() => setEditing(null)}
        canAdminister={canAdministerAccounts(user)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('vehicleExpenses.deleteTitle')}
        description={t('vehicleExpenses.deleteBody')}
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

export function VehicleExpenseDialog({
  open,
  editingExpense,
  onClose,
  canAdminister,
}: {
  open: boolean;
  editingExpense?: VehicleExpense | null;
  onClose: () => void;
  canAdminister?: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: vehicles } = useAllVehiclesQuery();
  const record = useRecordVehicleExpense();
  const update = useUpdateVehicleExpense();
  const isEditing = !!editingExpense;

  const [vehicleId, setVehicleId] = useState('');
  const [kind, setKind] = useState<VehicleExpenseKind>('Fuel');
  const [amount, setAmount] = useState('');
  const [litres, setLitres] = useState('');
  const [odometerKm, setOdometerKm] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [supplier, setSupplier] = useState('');
  const [note, setNote] = useState('');

  const resetRecord = record.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetRecord();
    resetUpdate();

    if (editingExpense) {
      setVehicleId(editingExpense.vehicleId);
      setKind(editingExpense.kind);
      setAmount(String(editingExpense.amount));
      setLitres(editingExpense.litres === null ? '' : String(editingExpense.litres));
      setOdometerKm(editingExpense.odometerKm === null ? '' : String(editingExpense.odometerKm));
      setOccurredOn(editingExpense.occurredOn);
      setSupplier(editingExpense.supplier ?? '');
      setNote(editingExpense.note ?? '');
    } else {
      setVehicleId('');
      setKind('Fuel');
      setAmount('');
      setLitres('');
      setOdometerKm('');
      setOccurredOn('');
      setSupplier('');
      setNote('');
    }
  }, [open, editingExpense, resetRecord, resetUpdate]);

  const isFuel = kind === 'Fuel';
  const parsedAmount = Number(amount);
  const parsedLitres = Number(litres);
  const amountIsValid =
    amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount >= 0;
  const litresAreValid =
    !isFuel || (litres.trim() !== '' && !Number.isNaN(parsedLitres) && parsedLitres > 0);

  const canSubmit =
    vehicleId !== '' && amountIsValid && litresAreValid && (!isEditing || occurredOn !== '');
  const mutation = isEditing ? update : record;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const input = {
      vehicleId,
      kind,
      amount: parsedAmount,
      litres: isFuel ? parsedLitres : null,
      odometerKm: odometerKm.trim() === '' ? null : Number(odometerKm),
      occurredOn: occurredOn || null,
      supplier: supplier.trim() || null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingExpense.id, input: { ...input, occurredOn } },
        { onSuccess: onClose },
      );
    } else {
      record.mutate(input, { onSuccess: onClose });
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {isEditing ? t('vehicleExpenses.editTitle') : t('vehicleExpenses.add')}
      </DialogTitle>
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
              label={t('vehicleExpenses.vehicle')}
              value={vehicleId}
              onChange={(event) => setVehicleId(event.target.value)}
            >
              {vehicles?.items.map((vehicle) => (
                <MenuItem key={vehicle.id} value={vehicle.id}>
                  {vehicle.brand} {vehicle.model} ({vehicle.registrationNumber})
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('vehicleExpenses.kind')}
              value={kind}
              onChange={(event) => setKind(event.target.value as VehicleExpenseKind)}
            >
              {vehicleExpenseKinds.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('vehicleExpenseKind', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('vehicleExpenses.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              error={amount.trim() !== '' && !amountIsValid}
            />
          </Grid>

          {isFuel && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                type="number"
                fullWidth
                required
                label={t('vehicleExpenses.litres')}
                value={litres}
                onChange={(event) => setLitres(event.target.value)}
                error={litres.trim() !== '' && !litresAreValid}
                helperText={
                  litres.trim() === '' ? t('vehicleExpenses.fuelNeedsLitres') : undefined
                }
              />
            </Grid>
          )}

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('vehicleExpenses.odometer')}
              value={odometerKm}
              onChange={(event) => setOdometerKm(event.target.value)}
              helperText={t('vehicleExpenses.odometerHint')}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              required={isEditing}
              label={t('vehicleExpenses.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              fullWidth
              label={t('vehicleExpenses.supplier')}
              value={supplier}
              onChange={(event) => setSupplier(event.target.value)}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('vehicleExpenses.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <>
              <Grid size={12}>
                <AttachmentList
                  ownerType="VehicleExpense"
                  ownerId={editingExpense.id}
                  categories={['Other']}
                  canUpload={canAdminister}
                  canDelete={canAdminister}
                />
              </Grid>
              {canAdminister && (
                <Grid size={12}>
                  <AuditHistoryCard entityName="VehicleExpense" entityId={editingExpense.id} />
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
