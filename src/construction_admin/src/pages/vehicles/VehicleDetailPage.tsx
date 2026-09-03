import {
  ApartmentOutlined,
  CarRentalOutlined,
  DeleteOutlined,
  EditOutlined,
  LocalShippingOutlined,
  PersonOffOutlined,
  QrCode2Outlined,
} from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  Grid,
  IconButton,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { VehicleExpense, VehicleRentalRate } from '../../api/types';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { AttachmentList } from '../../components/AttachmentList';
import { QrLabelDialog } from '../../components/QrLabelDialog';
import { StatusChip } from '../../components/StatusChip';
import { useCoverPhoto } from '../../features/attachments/useAttachments';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import {
  useDeleteVehicleRentalRate,
  useSetVehicleRentalRate,
  useUpdateVehicleRentalRate,
  useVehicleExpensesQuery,
  useVehicleRentalRatesQuery,
} from '../../features/costs/useCosts';
import {
  useAssignVehicle,
  useAssignVehicleProject,
  useDeleteVehicle,
  useUnassignVehicle,
  useUnassignVehicleProject,
  useVehicleQuery,
} from '../../features/vehicles/useVehicles';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';
import { VehicleExpenseDialog } from '../costs/VehicleExpensesPage';

export function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();

  const { data: vehicle, isLoading, isError, error, refetch } = useVehicleQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();
  const { data: allProjects } = useAllProjectsQuery();
  const coverPhoto = useCoverPhoto('Vehicle', id ?? '');

  const assign = useAssignVehicle(id ?? '');
  const unassign = useUnassignVehicle(id ?? '');
  const assignProject = useAssignVehicleProject(id ?? '');
  const unassignProject = useUnassignVehicleProject(id ?? '');
  const deleteVehicle = useDeleteVehicle();

  const [selectedEmployeeId, setSelectedEmployeeId] = useState('');
  const [selectedProjectId, setSelectedProjectId] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [confirmUnassign, setConfirmUnassign] = useState(false);
  const [confirmUnassignProject, setConfirmUnassignProject] = useState(false);
  const [qrLabelOpen, setQrLabelOpen] = useState(false);

  if (isLoading) return null;
  if (isError || !vehicle) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleAssign = async () => {
    if (!selectedEmployeeId) return;
    await assign.mutateAsync(selectedEmployeeId);
    setSelectedEmployeeId('');
  };

  const handleUnassign = async () => {
    await unassign.mutateAsync();
    setConfirmUnassign(false);
  };

  const handleAssignProject = async () => {
    if (!selectedProjectId) return;
    await assignProject.mutateAsync(selectedProjectId);
    setSelectedProjectId('');
  };

  const handleUnassignProject = async () => {
    await unassignProject.mutateAsync();
    setConfirmUnassignProject(false);
  };

  const handleDelete = async () => {
    await deleteVehicle.mutateAsync(vehicle.id);
    navigate(paths.vehicles, { replace: true });
  };

  return (
    <Box sx={{ maxWidth: 900 }}>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={3}
            sx={{ alignItems: 'flex-start' }}
          >
            <Avatar
              src={coverPhoto ?? undefined}
              sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}
            >
              <LocalShippingOutlined />
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {vehicle.brand} {vehicle.model}
              </Typography>
              <Typography color="text.secondary">{vehicle.registrationNumber}</Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: 'center' }}>
                <StatusChip status={vehicle.status} kind="vehicleStatus" />
                <StatusChip status={vehicle.ownershipType} kind="vehicleOwnershipType" />
              </Stack>
            </Box>
            <Stack direction="row" spacing={1}>
              <Button
                variant="outlined"
                startIcon={<QrCode2Outlined />}
                onClick={() => setQrLabelOpen(true)}
              >
                {t('common.printLabel')}
              </Button>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.vehicleEdit(vehicle.id))}
              >
                Edit
              </Button>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => setConfirmDelete(true)}
              >
                Delete
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Vehicle
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('vehicles.vin')} value={vehicle.vin} flagMissing />
                <InfoRow label={t('vehicles.qrCode')} value={vehicle.qrCode} flagMissing />
                <InfoRow label={t('vehicles.fuelType')} value={enumLabel('fuelType', vehicle.fuelType)} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Assignment
              </Typography>

              {vehicle.assignedEmployeeId ? (
                <Stack spacing={1.5} sx={{ mt: 1 }}>
                  <Typography>
                    Assigned to <strong>{vehicle.assignedEmployeeName}</strong> (
                    {vehicle.assignedEmployeeNumber})
                  </Typography>
                  <Box>
                    <Button
                      size="small"
                      color="error"
                      variant="outlined"
                      startIcon={<PersonOffOutlined />}
                      onClick={() => setConfirmUnassign(true)}
                    >
                      Unassign
                    </Button>
                  </Box>
                </Stack>
              ) : (
                <Stack spacing={2} sx={{ mt: 1 }}>
                  <Typography color="text.secondary">{t('vehicles.notAssignedSentence')}</Typography>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <FormControl size="small" sx={{ minWidth: 220 }}>
                      <Select
                        displayEmpty
                        value={selectedEmployeeId}
                        onChange={(event) => setSelectedEmployeeId(event.target.value)}
                      >
                        <MenuItem value="">
                          <em>Select an employee…</em>
                        </MenuItem>
                        {(allEmployees?.items ?? []).map((employee) => (
                          <MenuItem key={employee.id} value={employee.id}>
                            {employee.fullName}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    <Button
                      variant="outlined"
                      disabled={!selectedEmployeeId}
                      loading={assign.isPending}
                      onClick={handleAssign}
                    >
                      Assign
                    </Button>
                  </Stack>
                </Stack>
              )}

              {(assign.isError || unassign.isError) && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {toApiError(assign.error ?? unassign.error).message}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid size={12}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Project
              </Typography>
              {vehicle.assignedEmployeeId && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  {t('vehicles.followsEmployeeHint', { name: vehicle.assignedEmployeeName ?? '' })}
                </Typography>
              )}

              {vehicle.assignedProjectId ? (
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1.5}
                  sx={{ mt: 1, alignItems: { sm: 'center' } }}
                >
                  <Avatar sx={{ bgcolor: 'action.selected' }}>
                    <ApartmentOutlined />
                  </Avatar>
                  <Typography
                    sx={{ flex: 1, cursor: 'pointer' }}
                    onClick={() => navigate(paths.projectDetail(vehicle.assignedProjectId!))}
                  >
                    {vehicle.assignedProjectName}
                  </Typography>
                  <Button
                    size="small"
                    color="error"
                    variant="outlined"
                    onClick={() => setConfirmUnassignProject(true)}
                  >
                    Remove from project
                  </Button>
                </Stack>
              ) : (
                <Stack spacing={2} sx={{ mt: 1 }}>
                  <Typography color="text.secondary">{t('vehicles.notPlacedSentence')}</Typography>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <FormControl size="small" sx={{ minWidth: 260 }}>
                      <Select
                        displayEmpty
                        value={selectedProjectId}
                        onChange={(event) => setSelectedProjectId(event.target.value)}
                      >
                        <MenuItem value="">
                          <em>{t('common.selectProject')}</em>
                        </MenuItem>
                        {(allProjects?.items ?? []).map((project) => (
                          <MenuItem key={project.id} value={project.id}>
                            {project.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    <Button
                      variant="outlined"
                      disabled={!selectedProjectId}
                      loading={assignProject.isPending}
                      onClick={handleAssignProject}
                    >
                      Place on project
                    </Button>
                  </Stack>
                </Stack>
              )}

              {(assignProject.isError || unassignProject.isError) && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {toApiError(assignProject.error ?? unassignProject.error).message}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        {vehicle.ownershipType !== 'Owned' && (
          <Grid size={12}>
            <VehicleRentalCard vehicleId={vehicle.id} />
          </Grid>
        )}

        <Grid size={12}>
          <VehicleCostsCard vehicleId={vehicle.id} />
        </Grid>

        {canAdministerAccounts(user) && (
          <Grid size={12}>
            <AuditHistoryCard entityName="Vehicle" entityId={vehicle.id} />
          </Grid>
        )}

        <Grid size={12}>
          <Card>
            <CardContent>
              <AttachmentList
                ownerType="Vehicle"
                ownerId={vehicle.id}
                categories={['Insurance', 'Licence', 'Certificate', 'Photo', 'Other']}
                canDelete={canAdministerAccounts(user)}
              />
            </CardContent>
          </Card>
        </Grid>

      </Grid>

      <Divider sx={{ my: 3 }} />

      <ConfirmDialog
        open={confirmUnassign}
        title={t('vehicles.unassignTitle')}
        description={`${vehicle.brand} ${vehicle.model} will no longer be assigned to ${vehicle.assignedEmployeeName ?? 'this employee'}.`}
        confirmLabel="Unassign"
        destructive
        loading={unassign.isPending}
        onConfirm={handleUnassign}
        onCancel={() => setConfirmUnassign(false)}
      />

      <ConfirmDialog
        open={confirmUnassignProject}
        title={t('vehicles.unassignProject')}
        description={`${vehicle.brand} ${vehicle.model} will no longer be placed at ${vehicle.assignedProjectName ?? 'this project'}.`}
        confirmLabel={t('employees.removeFromProject')}
        destructive
        loading={unassignProject.isPending}
        onConfirm={handleUnassignProject}
        onCancel={() => setConfirmUnassignProject(false)}
      />

      <ConfirmDialog
        open={confirmDelete}
        title={t('vehicles.deleteTitle')}
        description={t('vehicles.deleteBody', {
          name: `${vehicle.brand} ${vehicle.model} (${vehicle.registrationNumber})`,
        })}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteVehicle.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />

      <QrLabelDialog
        open={qrLabelOpen}
        onClose={() => setQrLabelOpen(false)}
        title={`${vehicle.brand} ${vehicle.model}`}
        qrCode={vehicle.qrCode}
        onEdit={() => navigate(paths.vehicleEdit(vehicle.id))}
      />
    </Box>
  );
}

/** The last few costs recorded against this vehicle, double-click to correct one. */
function VehicleCostsCard({ vehicleId }: { vehicleId: string }) {
  const t = useT();
  const navigate = useNavigate();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const [editing, setEditing] = useState<VehicleExpense | null>(null);

  const query = useMemo(
    () => ({
      vehicleId,
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'occurredOn',
      sortDescending: true,
    }),
    [vehicleId],
  );

  const { data } = useVehicleExpensesQuery(query);
  const rows = data?.items ?? [];

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('vehicleExpenses.title')}
          </Typography>
          <Button size="small" onClick={() => navigate(paths.vehicleExpenses)}>
            {t('common.viewAll')}
          </Button>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('vehicleExpenses.empty')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('vehicleExpenses.occurredOn')}</TableCell>
                  <TableCell>{t('vehicleExpenses.kind')}</TableCell>
                  <TableCell align="right">{t('vehicleExpenses.amount')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow
                    key={row.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onDoubleClick={() => setEditing(row)}
                  >
                    <TableCell>{formatDate(row.occurredOn)}</TableCell>
                    <TableCell>{enumLabel('vehicleExpenseKind', row.kind)}</TableCell>
                    <TableCell align="right">{formatMoney(row.amount, locale)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      <VehicleExpenseDialog
        open={!!editing}
        editingExpense={editing}
        onClose={() => setEditing(null)}
      />
    </Card>
  );
}

/** The rent/lease history for this vehicle, add a new rate to close off the one in force. */
function VehicleRentalCard({ vehicleId }: { vehicleId: string }) {
  const t = useT();
  const { locale } = useI18n();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<VehicleRentalRate | null>(null);

  const query = useMemo(
    () => ({
      vehicleId,
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'startDate',
      sortDescending: true,
    }),
    [vehicleId],
  );

  const { data } = useVehicleRentalRatesQuery(query);
  const remove = useDeleteWithConfirm<VehicleRentalRate>(useDeleteVehicleRentalRate());
  const rows = data?.items ?? [];

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('vehicleRentalRates.title')}
          </Typography>
          <Button size="small" startIcon={<CarRentalOutlined />} onClick={() => setAdding(true)}>
            {t('vehicleRentalRates.add')}
          </Button>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('vehicleRentalRates.empty')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('vehicleRentalRates.provider')}</TableCell>
                  <TableCell align="right">{t('vehicleRentalRates.monthlyAmount')}</TableCell>
                  <TableCell>{t('vehicleRentalRates.startDate')}</TableCell>
                  <TableCell>{t('vehicleRentalRates.endDate')}</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow
                    key={row.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onDoubleClick={() => setEditing(row)}
                  >
                    <TableCell>{row.provider || '—'}</TableCell>
                    <TableCell align="right">{formatMoney(row.monthlyAmount, locale)}</TableCell>
                    <TableCell>{formatDate(row.startDate)}</TableCell>
                    <TableCell>
                      {row.endDate ? formatDate(row.endDate) : t('rates.open')}
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title={t('common.delete')}>
                        <IconButton
                          size="small"
                          onClick={(event) => {
                            event.stopPropagation();
                            remove.request(row);
                          }}
                        >
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      <VehicleRentalRateDialog
        open={adding}
        vehicleId={vehicleId}
        onClose={() => setAdding(false)}
      />
      <VehicleRentalRateDialog
        open={!!editing}
        vehicleId={vehicleId}
        editingRate={editing}
        onClose={() => setEditing(null)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('vehicleRentalRates.deleteTitle')}
        description={t('vehicleRentalRates.deleteBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Card>
  );
}

function VehicleRentalRateDialog({
  open,
  vehicleId,
  editingRate,
  onClose,
}: {
  open: boolean;
  vehicleId: string;
  editingRate?: VehicleRentalRate | null;
  onClose: () => void;
}) {
  const t = useT();
  const set = useSetVehicleRentalRate();
  const update = useUpdateVehicleRentalRate();
  const isEditing = !!editingRate;

  const [monthlyAmount, setMonthlyAmount] = useState('');
  const [provider, setProvider] = useState('');
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
      setMonthlyAmount(String(editingRate.monthlyAmount));
      setProvider(editingRate.provider ?? '');
      setStartDate(editingRate.startDate);
      setEndDate(editingRate.endDate ?? '');
      setNote(editingRate.note ?? '');
    } else {
      setMonthlyAmount('');
      setProvider('');
      setStartDate('');
      setEndDate('');
      setNote('');
    }
  }, [open, editingRate, resetSet, resetUpdate]);

  const parsedAmount = Number(monthlyAmount);
  const amountIsValid = monthlyAmount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount > 0;
  const datesAreValid = !startDate || !endDate || endDate >= startDate;
  const canSubmit = amountIsValid && datesAreValid;

  const mutation = isEditing ? update : set;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const shared = {
      vehicleId,
      monthlyAmount: parsedAmount,
      provider: provider.trim() || null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingRate.id, input: { ...shared, startDate, endDate: endDate || null } },
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
      <DialogTitle>
        {isEditing ? t('vehicleRentalRates.editTitle') : t('vehicleRentalRates.add')}
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
              type="number"
              fullWidth
              label={t('vehicleRentalRates.monthlyAmount')}
              value={monthlyAmount}
              onChange={(event) => setMonthlyAmount(event.target.value)}
              error={monthlyAmount.trim() !== '' && !amountIsValid}
              helperText={
                monthlyAmount.trim() !== '' && !amountIsValid
                  ? t('vehicleRentalRates.mustBePositive')
                  : undefined
              }
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('vehicleRentalRates.provider')}
              value={provider}
              onChange={(event) => setProvider(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('vehicleRentalRates.startDate')}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('vehicleRentalRates.endDate')}
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
              multiline
              minRows={2}
              label={t('rates.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <Grid size={12}>
              <AttachmentList
                ownerType="VehicleRentalRate"
                ownerId={editingRate.id}
                categories={['Contract', 'Other']}
              />
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit}
          loading={mutation.isPending}
          onClick={submit}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function InfoRow({
  label,
  value,
  flagMissing,
}: {
  label: string;
  value: string | null | undefined;
  /** Shows a warning-colored prompt instead of a plain dash when unset. */
  flagMissing?: boolean;
}) {
  const t = useT();
  const missing = !value && flagMissing;

  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1" color={missing ? 'warning.main' : undefined}>
        {value || (missing ? t('common.incomplete') : '—')}
      </Typography>
    </Box>
  );
}
