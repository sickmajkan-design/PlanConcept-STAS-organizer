import { AddOutlined, CheckOutlined, CloseOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  AlertTitle,
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
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef, GridSortModel } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { VehicleExpenseListQuery } from '../../api/costs';
import {
  vehicleExpenseKinds,
  vehicleExpenseStatuses,
  type VehicleExpense,
  type VehicleExpenseKind,
  type VehicleExpenseStatus,
} from '../../api/types';
import { canAdministerAccounts, canReviewSpending } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { BulkActionsBar } from '../../components/BulkActionsBar';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import {
  StatusBoard,
  ViewModeToggle,
  useViewMode,
  type BoardColumn,
} from '../../components/StatusBoard';
import { ReasonDialog } from '../../components/ReasonDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { StatusChip } from '../../components/StatusChip';
import {
  useDeleteVehicleExpense,
  useFuelConsumptionFlagsQuery,
  useRecordVehicleExpense,
  useReviewVehicleExpense,
  useUpdateVehicleExpense,
  useVehicleExpensesQuery,
  useVehicleExpensesSummaryQuery,
} from '../../features/costs/useCosts';
import { useAllVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useOpenOnParam } from '../../hooks/useOpenOnParam';
import { useBulkSelection } from '../../hooks/useBulkSelection';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney, formatQuantity } from '../../utils/formatting';

/** What a phone-width screen keeps: which vehicle, how much, and where it stands in review. */
const compactHiddenFields = [
  'occurredOn',
  'kind',
  'litres',
  'pricePerLitre',
  'odometerKm',
  'recordedByName',
] as const;

interface VehicleExpenseViewState {
  sortModel: GridSortModel;
  kind: VehicleExpenseKind | '';
}

export function VehicleExpensesPage() {
  const t = useT();
  const { locale } = useI18n();
  const navigate = useNavigate();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<VehicleExpenseKind | ''>('');
  const [status, setStatus] = useState<VehicleExpenseStatus | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<VehicleExpense | null>(null);
  const [approving, setApproving] = useState<VehicleExpense | null>(null);
  const [rejecting, setRejecting] = useState<VehicleExpense | null>(null);
  const [bulkApproving, setBulkApproving] = useState(false);
  useOpenOnParam('new', () => setRecording(true));
  const selection = useBulkSelection();

  const savedViews = useSavedViews<VehicleExpenseViewState>('vehicle-expenses');

  const applyView = (state: VehicleExpenseViewState) => {
    list.setSortModel(state.sortModel);
    setKind(state.kind);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, { sortModel: list.sortModel, kind });
  };

  const query: VehicleExpenseListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      kind: kind || undefined,
      status: status || undefined,
    }),
    [kind, status, list.query],
  );

  const [viewMode, setViewMode] = useViewMode('vehicle-expenses');
  const isBoard = viewMode === 'board';

  // The board shows every status side by side, so it asks for one big page of
  // the newest costs rather than a page of whichever status is filtered.
  const boardQuery: VehicleExpenseListQuery = useMemo(
    () => ({
      ...query,
      pageNumber: 1,
      pageSize: 100,
      status: undefined,
      sortBy: 'occurredOn',
      sortDescending: true,
    }),
    [query],
  );

  const { data, isLoading, isError, error, refetch } = useVehicleExpensesQuery(
    isBoard ? boardQuery : query,
  );

  const boardColumns: BoardColumn[] = [
    { status: 'Pending', label: enumLabel('vehicleExpenseStatus', 'Pending'), color: 'warning' },
    { status: 'Approved', label: enumLabel('vehicleExpenseStatus', 'Approved'), color: 'success' },
    { status: 'Rejected', label: enumLabel('vehicleExpenseStatus', 'Rejected'), color: 'error' },
  ];
  const { data: summary } = useVehicleExpensesSummaryQuery(query);
  const { data: consumptionFlags } = useFuelConsumptionFlagsQuery({});
  const remove = useDeleteWithConfirm<VehicleExpense>(useDeleteVehicleExpense());
  const review = useReviewVehicleExpense();
  // A foreman records costs but does not review them; showing them buttons the
  // API will refuse is a trap, not a courtesy.
  const reviewer = canReviewSpending(user);

  // Bulk approval only takes what a person could approve one by one without a
  // second thought: pending, and not their own entry. A cost that was sent
  // back needs the deliberate single approval that overrides the rejection.
  const selectedRows = (data?.items ?? []).filter((row) => selection.selectedIds.includes(row.id));
  const bulkEligible = selectedRows.filter(
    (row) =>
      row.status === 'Pending' &&
      !(user && user.role !== 'SuperAdmin' && row.recordedByName === user.email),
  );
  const bulkSkipped = selectedRows.length - bulkEligible.length;

  // Order is the order of importance, and the total width is kept under what a
  // laptop screen has: the status and the approve/reject buttons are the point
  // of this page, and when they sat at the far right of eleven columns they
  // were off-screen until somebody scrolled sideways. The recorded-at time and
  // the fuel type used to have columns of their own; the type now sits with the
  // kind it qualifies and the timestamp is in the audit trail.
  const columns: GridColDef<VehicleExpense>[] = useMemo(
    () => [
      {
        field: 'occurredOn',
        headerName: t('vehicleExpenses.occurredOn'),
        width: 110,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'status',
        headerName: t('vehicleExpenses.status'),
        width: 140,
        renderCell: (params) => (
          <StatusChip status={params.value} kind="vehicleExpenseStatus" />
        ),
      },
      {
        field: 'vehicleName',
        headerName: t('vehicleExpenses.vehicle'),
        flex: 1,
        // Small on purpose: the name is truncated with an ellipsis anyway, and
        // on a phone every pixel here is one the amount column loses.
        minWidth: 105,
      },
      {
        field: 'kind',
        headerName: t('vehicleExpenses.kind'),
        width: 150,
        valueGetter: (_value, row) =>
          row.fuelProductType
            ? `${enumLabel('vehicleExpenseKind', row.kind)} · ${row.fuelProductType}`
            : enumLabel('vehicleExpenseKind', row.kind),
      },
      {
        field: 'amount',
        headerName: t('vehicleExpenses.amount'),
        width: 110,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'litres',
        headerName: t('vehicleExpenses.litres'),
        width: 90,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? '—' : formatQuantity(value as number, locale),
      },
      {
        field: 'pricePerLitre',
        headerName: t('vehicleExpenses.pricePerLitre'),
        width: 100,
        align: 'right',
        headerAlign: 'right',
        sortable: false,
        valueGetter: (value) => formatMoney(value as number | null, locale),
      },
      {
        field: 'odometerKm',
        headerName: t('vehicleExpenses.odometer'),
        width: 110,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) =>
          value === null ? '—' : formatQuantity(value as number, locale),
      },
      {
        field: 'recordedByName',
        headerName: t('vehicleExpenses.recordedBy'),
        flex: 1,
        minWidth: 130,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'actions',
        headerName: '',
        width: 112,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => {
          const isPending = params.row.status === 'Pending';
          // The recorder is shown by email, which is also what the account
          // signs in with. Never your own — the API refuses it, so the button
          // says why up front instead of failing after a click. The owner
          // (SuperAdmin) is the one exception, as on the API.
          const isOwn =
            !!user && user.role !== 'SuperAdmin' && params.row.recordedByName === user.email;
          const canAct = isPending && !isOwn;
          const reason = !isPending
            ? t('vehicleExpenses.answered')
            : isOwn
              ? t('vehicleExpenses.reviewOwnCost')
              : null;

          return (
            <Stack direction="row" spacing={0.5}>
              {reviewer && (
                <>
                  <Tooltip title={reason ?? t('vehicleExpenses.approve')}>
                    <span>
                      <IconButton
                        size="small"
                        color="success"
                        disabled={!canAct}
                        onClick={(event) => {
                          event.stopPropagation();
                          setApproving(params.row);
                        }}
                      >
                        <CheckOutlined fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                  <Tooltip title={reason ?? t('vehicleExpenses.reject')}>
                    <span>
                      <IconButton
                        size="small"
                        color="warning"
                        disabled={!canAct}
                        onClick={(event) => {
                          event.stopPropagation();
                          setRejecting(params.row);
                        }}
                      >
                        <CloseOutlined fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                </>
              )}
              <IconButton
                size="small"
                onClick={(event) => {
                  event.stopPropagation();
                  remove.request(params.row);
                }}
              >
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Stack>
          );
        },
      },
    ],
    [enumLabel, locale, remove, reviewer, t, user],
  );

  return (
    <Box>
      <PageHeader
        title={t('vehicleExpenses.title')}
        description={t('vehicleExpenses.description')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('vehicleExpenses.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
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
        {!isBoard && (
        <TextField
          select
          size="small"
          label={t('vehicleExpenses.status')}
          value={status}
          onChange={(event) => {
            setStatus(event.target.value as VehicleExpenseStatus | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">{t('vehicleExpenses.allStatuses')}</MenuItem>
          {vehicleExpenseStatuses.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('vehicleExpenseStatus', value)}
            </MenuItem>
          ))}
        </TextField>
        )}
        <Button variant="outlined" onClick={() => navigate(paths.fuelImport)}>
          {t('fuelImport.title')}
        </Button>
        <ViewModeToggle value={viewMode} onChange={setViewMode} />
      </Stack>

      {consumptionFlags && consumptionFlags.length > 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <AlertTitle>
            {t('vehicleExpenses.consumptionFlagsTitle', { count: consumptionFlags.length })}
          </AlertTitle>
          <Stack spacing={0.5}>
            {consumptionFlags.map((flag) => (
              <Typography key={flag.expenseId} variant="body2">
                {t('vehicleExpenses.consumptionFlagRow', {
                  vehicle: flag.vehicleName,
                  date: formatDate(flag.occurredOn),
                  litresPer100Km: formatQuantity(flag.litresPer100Km, locale),
                  average: formatQuantity(flag.vehicleAverageLitresPer100Km, locale),
                  sign: flag.deviationPercent > 0 ? '+' : '',
                  deviation: flag.deviationPercent,
                })}
              </Typography>
            ))}
          </Stack>
        </Alert>
      )}

      <Box sx={{ mb: 2 }}>
        <SavedViewsBar
          views={savedViews.views}
          onApply={applyView}
          onSave={saveCurrentView}
          onDelete={savedViews.deleteView}
        />
      </Box>

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

      {reviewer && !isBoard && (
        <BulkActionsBar
          count={selection.count}
          onApprove={() => setBulkApproving(true)}
          onClear={selection.clear}
        />
      )}

      {isBoard ? (
        <StatusBoard
          rows={data?.items ?? []}
          totalCount={data?.totalCount ?? 0}
          columns={columns}
          boardColumns={boardColumns}
          getStatus={(row) => row.status}
          hideFields={['status']}
          isLoading={isLoading}
          onCardClick={(row) => setEditing(row)}
        />
      ) : (
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
          compactHiddenFields={compactHiddenFields}
          rowSelectionModel={reviewer ? selection.model : undefined}
          onRowSelectionModelChange={reviewer ? selection.setModel : undefined}
        />
      )}

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

      <ConfirmDialog
        open={bulkApproving}
        title={t('bulk.approveTitle')}
        description={
          bulkEligible.length === 0
            ? t('bulk.approveNothing')
            : [
                t('bulk.approveBody', { count: bulkEligible.length }),
                bulkSkipped > 0 ? t('bulk.approveSkipped', { count: bulkSkipped }) : '',
              ]
                .filter(Boolean)
                .join(' ')
        }
        confirmLabel={t('vehicleExpenses.approve')}
        onConfirm={async () => {
          if (bulkEligible.length === 0) {
            setBulkApproving(false);
            return;
          }

          // One at a time: each is its own reviewed decision on the server.
          let failed = 0;
          for (const row of bulkEligible) {
            try {
              await review.mutateAsync({ id: row.id, input: { approve: true } });
            } catch {
              failed += 1;
            }
          }

          if (failed > 0) {
            void refetch();
            throw new Error(t('bulk.approvePartial', { count: failed }));
          }

          selection.clear();
          setBulkApproving(false);
        }}
        onCancel={() => setBulkApproving(false)}
      />

      <ConfirmDialog
        open={!!approving}
        title={t('vehicleExpenses.approveTitle')}
        description={t('vehicleExpenses.approveBody')}
        confirmLabel={t('vehicleExpenses.approve')}
        onConfirm={async () => {
          if (!approving) return;

          // Awaited, so a refusal is thrown to the dialog and shown there.
          // Left fire-and-forget, a refused approval (the API will not let you
          // approve a cost you recorded) left the dialog open and silent —
          // a button that seemed to do nothing.
          try {
            await review.mutateAsync({ id: approving.id, input: { approve: true } });
          } catch (err) {
            // A refusal usually means the row on screen is out of date — somebody
            // else already reviewed it — so bring the list up to date too,
            // instead of leaving it saying "pending" beside an error that says
            // otherwise.
            void refetch();
            throw err;
          }

          setApproving(null);
        }}
        onCancel={() => setApproving(null)}
      />

      <RejectVehicleExpenseDialog expense={rejecting} onClose={() => setRejecting(null)} />
    </Box>
  );
}

function RejectVehicleExpenseDialog({
  expense,
  onClose,
}: {
  expense: VehicleExpense | null;
  onClose: () => void;
}) {
  const t = useT();
  const review = useReviewVehicleExpense();

  return (
    <ReasonDialog
      open={!!expense}
      title={t('vehicleExpenses.rejectTitle')}
      hint={t('vehicleExpenses.rejectHint')}
      label={t('vehicleExpenses.rejectReason')}
      submitLabel={t('vehicleExpenses.reject')}
      onClose={onClose}
      onSubmit={async (note) => {
        if (!expense) return;

        await review.mutateAsync({ id: expense.id, input: { approve: false, note } });
        onClose();
      }}
    />
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
  const [fuelProductType, setFuelProductType] = useState('');
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
      setFuelProductType(editingExpense.fuelProductType ?? '');
      setOccurredOn(editingExpense.occurredOn);
      setSupplier(editingExpense.supplier ?? '');
      setNote(editingExpense.note ?? '');
    } else {
      setVehicleId('');
      setKind('Fuel');
      setAmount('');
      setLitres('');
      setOdometerKm('');
      setFuelProductType('');
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
      fuelProductType: isFuel ? fuelProductType.trim() || null : null,
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

        {editingExpense?.status === 'Rejected' && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {editingExpense.reviewNote}
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

          {isFuel && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                fullWidth
                label={t('vehicleExpenses.fuelProductType')}
                value={fuelProductType}
                onChange={(event) => setFuelProductType(event.target.value)}
              />
            </Grid>
          )}

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
