import {
  AddOutlined,
  ApartmentOutlined,
  AssignmentReturnOutlined,
  CarRentalOutlined,
  CreditCard,
  DeleteOutlined,
  EditOutlined,
  LocalShippingOutlined,
  MyLocationOutlined,
  PaymentsOutlined,
  PersonOffOutlined,
  QrCode2Outlined,
} from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  FormControlLabel,
  Grid,
  IconButton,
  MenuItem,
  Select,
  Stack,
  Switch,
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
import type {
  FuelCard,
  VehicleExpense,
  VehicleRentalOut,
  VehicleRentalRate,
  VehicleToll,
  VehicleTollComputedState,
} from '../../api/types';
import { vehicleTollTypes } from '../../api/types';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { AttachmentList } from '../../components/AttachmentList';
import { QrLabelDialog } from '../../components/QrLabelDialog';
import { StatusChip } from '../../components/StatusChip';
import { useCoverPhoto } from '../../features/attachments/useAttachments';
import { useAllCustomersQuery } from '../../features/customers/useCustomers';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import {
  useAddFuelCard,
  useDeleteFuelCard,
  useFuelCardsQuery,
} from '../../features/fuelCards/useFuelCards';
import {
  useDeleteVehicleRentalOut,
  useDeleteVehicleRentalRate,
  useRecordVehicleRentalOut,
  useReturnVehicleRentalOut,
  useSetVehicleRentalRate,
  useUpdateVehicleRentalOut,
  useUpdateVehicleRentalRate,
  useVehicleExpensesQuery,
  useVehicleRentalRatesQuery,
  useVehicleRentalsOutQuery,
  useVehicleRentalsOutSummaryQuery,
} from '../../features/costs/useCosts';
import {
  useAssignVehicle,
  useAssignVehicleProject,
  useDeleteVehicle,
  useUnassignVehicle,
  useUnassignVehicleProject,
  useVehicleQuery,
} from '../../features/vehicles/useVehicles';
import {
  useAddVehicleToll,
  useDeleteVehicleToll,
  useMarkVehicleTollPaid,
  useUpdateVehicleToll,
  useVehicleTollsQuery,
} from '../../features/vehicles/useVehicleTolls';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { SiblingNavButtons } from '../../components/SiblingNavButtons';
import { useSiblingNavigation } from '../../hooks/useSiblingNavigation';
import { useRecordVisit } from '../../layout/useRecentRecords';
import { paths } from '../../routes/paths';
import { dateOnlyOffset, formatDate, formatMoney } from '../../utils/formatting';
import { VehicleExpenseDialog } from '../costs/VehicleExpensesPage';

export function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();

  const { data: vehicle, isLoading, isError, error, refetch } = useVehicleQuery(id);
  useRecordVisit(
    paths.vehicleDetail(id ?? ''),
    vehicle ? `${vehicle.brand} ${vehicle.model}` : undefined,
  );
  const { prevId, nextId, siblingIds } = useSiblingNavigation(id);
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
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
              <SiblingNavButtons
                prevId={prevId}
                nextId={nextId}
                siblingIds={siblingIds}
                buildPath={paths.vehicleDetail}
              />
              {vehicle.gpsTrackingUrl && (
                <Tooltip
                  title={
                    vehicle.gpsProvider
                      ? t('vehicles.gpsTrackHint', { provider: vehicle.gpsProvider })
                      : ''
                  }
                >
                  <Button
                    variant="outlined"
                    startIcon={<MyLocationOutlined />}
                    component="a"
                    href={vehicle.gpsTrackingUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {t('vehicles.gpsTrack')}
                  </Button>
                </Tooltip>
              )}
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
                {t('common.edit')}
              </Button>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => setConfirmDelete(true)}
              >
                {t('common.delete')}
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
                {t('detail.vehicle')}
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
                {t('detail.assignment')}
              </Typography>

              {vehicle.assignedEmployeeId ? (
                <Stack spacing={1.5} sx={{ mt: 1 }}>
                  <Typography>
                    {t('detail.assignedTo')} <strong>{vehicle.assignedEmployeeName}</strong> ({vehicle.assignedEmployeeNumber})
                  </Typography>
                  <Box>
                    <Button
                      size="small"
                      color="error"
                      variant="outlined"
                      startIcon={<PersonOffOutlined />}
                      onClick={() => setConfirmUnassign(true)}
                    >
                      {t('detail.unassign')}
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
                          <em>{t('detail.selectEmployee')}</em>
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
                      {t('detail.assign')}
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
                {t('detail.project')}
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
                    {t('detail.removeFromProject')}
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
                      {t('detail.placeOnProject')}
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
          <VehicleRentalOutCard vehicleId={vehicle.id} vehicleStatus={vehicle.status} />
        </Grid>

        <Grid size={12}>
          <VehicleTollsCard vehicleId={vehicle.id} canEdit={canAdministerAccounts(user)} />
        </Grid>

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

        <Grid size={{ xs: 12, sm: 6 }}>
          <FuelCardsCard vehicleId={vehicle.id} />
        </Grid>

      </Grid>

      <Divider sx={{ my: 3 }} />

      <ConfirmDialog
        open={confirmUnassign}
        title={t('vehicles.unassignTitle')}
        description={t('detail.vehicleUnassignBody', { name: `${vehicle.brand} ${vehicle.model}`, person: vehicle.assignedEmployeeName ?? t('detail.thisEmployee') })}
        confirmLabel={t('detail.unassign')}
        destructive
        loading={unassign.isPending}
        onConfirm={handleUnassign}
        onCancel={() => setConfirmUnassign(false)}
      />

      <ConfirmDialog
        open={confirmUnassignProject}
        title={t('vehicles.unassignProject')}
        description={t('detail.unplaceBody', { name: `${vehicle.brand} ${vehicle.model}`, project: vehicle.assignedProjectName ?? t('detail.thisProject') })}
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

/**
 * Vignettes, tunnels and road passages carried by this vehicle. Every role
 * reads the list — a driver needs to know what is paid before setting off —
 * while adding, editing, renewing and deleting are Admin/SuperAdmin only.
 * Rows stay in the order the API returns them (expired and expiring first).
 */
function VehicleTollsCard({ vehicleId, canEdit }: { vehicleId: string; canEdit: boolean }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<VehicleToll | null>(null);
  const [paying, setPaying] = useState<VehicleToll | null>(null);

  const { data } = useVehicleTollsQuery(vehicleId);
  const remove = useDeleteWithConfirm<VehicleToll>(useDeleteVehicleToll());
  const rows = data ?? [];

  return (
    <Card>
      <CardContent>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('vehicleTolls.title')}
          </Typography>
          {canEdit && (
            <Button size="small" startIcon={<AddOutlined />} onClick={() => setAdding(true)}>
              {t('vehicleTolls.add')}
            </Button>
          )}
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('vehicleTolls.empty')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('vehicleTolls.type')}</TableCell>
                  <TableCell>{t('vehicleTolls.country')}</TableCell>
                  <TableCell>{t('vehicleTolls.routeSegment')}</TableCell>
                  <TableCell>{t('vehicleTolls.status')}</TableCell>
                  <TableCell>{t('vehicleTolls.validUntil')}</TableCell>
                  {canEdit && <TableCell align="right" />}
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.id} hover>
                    <TableCell>{enumLabel('vehicleTollType', row.type)}</TableCell>
                    <TableCell>{row.country}</TableCell>
                    <TableCell>{row.routeSegment || '—'}</TableCell>
                    <TableCell>
                      <VehicleTollStateChip toll={row} />
                    </TableCell>
                    <TableCell>{formatDate(row.validUntil)}</TableCell>
                    {canEdit && (
                      <TableCell align="right">
                        <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end' }}>
                          <Tooltip title={t('vehicleTolls.markPaid')}>
                            <IconButton size="small" onClick={() => setPaying(row)}>
                              <PaymentsOutlined fontSize="small" />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title={t('common.edit')}>
                            <IconButton size="small" onClick={() => setEditing(row)}>
                              <EditOutlined fontSize="small" />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title={t('common.delete')}>
                            <IconButton size="small" onClick={() => remove.request(row)}>
                              <DeleteOutlined fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        </Stack>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      {canEdit && (
        <>
          <VehicleTollDialog open={adding} vehicleId={vehicleId} onClose={() => setAdding(false)} />
          <VehicleTollDialog
            open={!!editing}
            vehicleId={vehicleId}
            editingToll={editing}
            onClose={() => setEditing(null)}
          />
          <VehicleTollPayDialog toll={paying} onClose={() => setPaying(null)} />

          <ConfirmDialog
            open={!!remove.pending}
            title={t('vehicleTolls.deleteTitle')}
            description={
              remove.pending
                ? t('vehicleTolls.deleteBody', {
                    name: `${enumLabel('vehicleTollType', remove.pending.type)} — ${remove.pending.country}`,
                  })
                : ''
            }
            confirmLabel={t('common.delete')}
            destructive
            loading={remove.isDeleting}
            onConfirm={remove.confirm}
            onCancel={remove.cancel}
          />
        </>
      )}
    </Card>
  );
}

/** Green when paid, amber when about to lapse, red once lapsed, outlined grey when unpaid — driven by the API's `computedState`. */
function VehicleTollStateChip({ toll }: { toll: VehicleToll }) {
  const t = useT();
  const enumLabel = useEnumLabel();

  const colors: Record<VehicleTollComputedState, 'success' | 'warning' | 'error' | 'default'> = {
    Paid: 'success',
    ExpiringSoon: 'warning',
    Expired: 'error',
    Unpaid: 'default',
  };
  const color = colors[toll.computedState] ?? 'default';

  const chip = (
    <Chip
      size="small"
      color={color}
      variant={color === 'default' ? 'outlined' : 'filled'}
      label={enumLabel('vehicleTollState', toll.computedState)}
    />
  );

  if (!toll.paidByUserName || !toll.paidAt) {
    return chip;
  }

  return (
    <Tooltip
      title={t('vehicleTolls.paidBy', {
        name: toll.paidByUserName,
        date: formatDate(toll.paidAt),
      })}
    >
      <span>{chip}</span>
    </Tooltip>
  );
}

/** A valid-until date has to be today or later for a payment to be recorded — the API enforces the same rule. */
function useTollValidUntilError(validUntil: string, required: boolean): string | null {
  const t = useT();

  if (!validUntil) {
    return required ? t('vehicleTolls.validUntilRequired') : null;
  }

  return validUntil < dateOnlyOffset(0) ? t('vehicleTolls.validUntilTodayOrLater') : null;
}

function VehicleTollDialog({
  open,
  vehicleId,
  editingToll,
  onClose,
}: {
  open: boolean;
  vehicleId: string;
  editingToll?: VehicleToll | null;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const add = useAddVehicleToll();
  const update = useUpdateVehicleToll();
  const isEditing = !!editingToll;

  const [type, setType] = useState<VehicleToll['type']>('Vignette');
  const [country, setCountry] = useState('');
  const [routeSegment, setRouteSegment] = useState('');
  const [paid, setPaid] = useState(false);
  const [validUntil, setValidUntil] = useState('');

  const resetAdd = add.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetAdd();
    resetUpdate();

    if (editingToll) {
      setType(editingToll.type);
      setCountry(editingToll.country);
      setRouteSegment(editingToll.routeSegment ?? '');
    } else {
      setType('Vignette');
      setCountry('');
      setRouteSegment('');
    }

    setPaid(false);
    setValidUntil('');
  }, [open, editingToll, resetAdd, resetUpdate]);

  // The date only matters, and is only checked, when a new entry is recorded already paid.
  const dateRequired = !isEditing && paid;
  const dateError = useTollValidUntilError(dateRequired ? validUntil : '', dateRequired);
  const canSubmit = country.trim() !== '' && !dateError;

  const mutation = isEditing ? update : add;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const shared = {
      type,
      country: country.trim(),
      routeSegment: routeSegment.trim() || null,
    };

    if (editingToll) {
      update.mutate({ id: editingToll.id, input: shared }, { onSuccess: onClose });
    } else {
      add.mutate(
        { vehicleId, ...shared, markPaid: paid, validUntil: paid ? validUntil : null },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isEditing ? t('vehicleTolls.editTitle') : t('vehicleTolls.add')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('vehicleTolls.type')}
              value={type}
              onChange={(event) => setType(event.target.value as VehicleToll['type'])}
            >
              {vehicleTollTypes.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('vehicleTollType', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              fullWidth
              required
              label={t('vehicleTolls.country')}
              value={country}
              onChange={(event) => setCountry(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 100 } }}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('vehicleTolls.routeSegment')}
              value={routeSegment}
              onChange={(event) => setRouteSegment(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 200 } }}
            />
          </Grid>

          {isEditing ? (
            <Grid size={12}>
              <Typography variant="caption" color="text.secondary">
                {t('vehicleTolls.paymentStateHint')}
              </Typography>
            </Grid>
          ) : (
            <>
              <Grid size={{ xs: 12, sm: 6 }}>
                <FormControlLabel
                  control={
                    <Switch checked={paid} onChange={(event) => setPaid(event.target.checked)} />
                  }
                  label={t('vehicleTolls.paid')}
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  type="date"
                  fullWidth
                  required={paid}
                  disabled={!paid}
                  label={t('vehicleTolls.validUntil')}
                  value={validUntil}
                  onChange={(event) => setValidUntil(event.target.value)}
                  slotProps={{ inputLabel: { shrink: true } }}
                  error={paid && !!dateError && validUntil !== ''}
                  helperText={paid && validUntil !== '' ? (dateError ?? undefined) : undefined}
                />
              </Grid>
            </>
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

/** Marks a toll paid or renews it — deliberately narrow: only the new valid-until date. */
function VehicleTollPayDialog({
  toll,
  onClose,
}: {
  toll: VehicleToll | null;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const markPaid = useMarkVehicleTollPaid();
  const [validUntil, setValidUntil] = useState('');

  const resetMarkPaid = markPaid.reset;

  useEffect(() => {
    if (!toll) return;
    resetMarkPaid();
    setValidUntil('');
  }, [toll, resetMarkPaid]);

  const dateError = useTollValidUntilError(validUntil, true);

  if (!toll) return null;

  const error = markPaid.isError ? toApiError(markPaid.error) : null;

  const submit = () => {
    markPaid.mutate({ id: toll.id, input: { validUntil } }, { onSuccess: onClose });
  };

  return (
    <Dialog open={!!toll} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('vehicleTolls.markPaidTitle')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          {t('vehicleTolls.markPaidBody')}
        </Typography>
        <Typography variant="body2" sx={{ mb: 2, fontWeight: 600 }}>
          {enumLabel('vehicleTollType', toll.type)} — {toll.country}
          {toll.routeSegment ? ` (${toll.routeSegment})` : ''}
        </Typography>
        <TextField
          type="date"
          fullWidth
          required
          autoFocus
          label={t('vehicleTolls.validUntil')}
          value={validUntil}
          onChange={(event) => setValidUntil(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          error={validUntil !== '' && !!dateError}
          helperText={validUntil !== '' ? (dateError ?? undefined) : undefined}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!!dateError}
          loading={markPaid.isPending}
          onClick={submit}
        >
          {t('vehicleTolls.markPaid')}
        </Button>
      </DialogActions>
    </Dialog>
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
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}>
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

/** Every fuel card ever issued against this vehicle, and the retire action that frees its number back up for a replacement card. */
function FuelCardsCard({ vehicleId }: { vehicleId: string }) {
  const t = useT();
  const [adding, setAdding] = useState(false);
  const [deleting, setDeleting] = useState<FuelCard | null>(null);
  const deleteCard = useDeleteFuelCard();

  const query = useMemo(
    () => ({ vehicleId, pageNumber: 1, pageSize: 20, sortBy: 'createdAt', sortDescending: true }),
    [vehicleId],
  );

  const { data } = useFuelCardsQuery(query);
  const rows = data?.items ?? [];

  const handleDelete = async () => {
    if (!deleting) return;
    // Awaited so a failure reaches the confirm dialog instead of vanishing.
    await deleteCard.mutateAsync(deleting.id);
    setDeleting(null);
  };

  return (
    <Card>
      <CardContent>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('fuelCards.title')}
          </Typography>
          <Button size="small" startIcon={<AddOutlined />} onClick={() => setAdding(true)}>
            {t('fuelCards.add')}
          </Button>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('fuelCards.empty')}
          </Typography>
        ) : (
          <Stack spacing={1} sx={{ mt: 1 }}>
            {rows.map((card) => (
              <Stack
                key={card.id}
                direction="row"
                spacing={1}
                sx={{ alignItems: 'center', justifyContent: 'space-between' }}
              >
                <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
                  <CreditCard fontSize="small" color="action" />
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {card.provider} — {card.cardNumber}
                    </Typography>
                    {card.issuedOn && (
                      <Typography variant="caption" color="text.secondary">
                        {t('fuelCards.issuedOn')}: {formatDate(card.issuedOn)}
                      </Typography>
                    )}
                  </Box>
                </Stack>
                <IconButton size="small" onClick={() => setDeleting(card)}>
                  <DeleteOutlined fontSize="small" />
                </IconButton>
              </Stack>
            ))}
          </Stack>
        )}
      </CardContent>

      <AddFuelCardDialog vehicleId={vehicleId} open={adding} onClose={() => setAdding(false)} />

      <ConfirmDialog
        open={!!deleting}
        title={t('fuelCards.deleteTitle')}
        description={deleting ? t('fuelCards.deleteBody', { cardNumber: deleting.cardNumber }) : ''}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteCard.isPending}
        onConfirm={handleDelete}
        onCancel={() => setDeleting(null)}
      />
    </Card>
  );
}

function AddFuelCardDialog({
  vehicleId,
  open,
  onClose,
}: {
  vehicleId: string;
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const addCard = useAddFuelCard();
  const [provider, setProvider] = useState('DKV');
  const [cardNumber, setCardNumber] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setProvider('DKV');
      setCardNumber('');
      setError(null);
    }
  }, [open]);

  const handleSave = () => {
    setError(null);
    addCard.mutate(
      { vehicleId, provider: provider.trim(), cardNumber: cardNumber.trim() },
      {
        onSuccess: () => onClose(),
        onError: (err) => setError(toApiError(err).message),
      },
    );
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('fuelCards.add')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label={t('fuelCards.provider')}
            value={provider}
            onChange={(event) => setProvider(event.target.value)}
            fullWidth
          />
          <TextField
            label={t('fuelCards.cardNumber')}
            value={cardNumber}
            onChange={(event) => setCardNumber(event.target.value)}
            fullWidth
            autoFocus
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!provider.trim() || !cardNumber.trim()}
          loading={addCard.isPending}
          onClick={handleSave}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
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
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}>
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

/** Loan-out history for this vehicle — the revenue direction. Record a new loan, return the open one. */
function VehicleRentalOutCard({
  vehicleId,
  vehicleStatus,
}: {
  vehicleId: string;
  vehicleStatus: string;
}) {
  const t = useT();
  const { locale } = useI18n();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<VehicleRentalOut | null>(null);
  const [returning, setReturning] = useState<VehicleRentalOut | null>(null);

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

  const { data } = useVehicleRentalsOutQuery(query);
  const { data: summary } = useVehicleRentalsOutSummaryQuery({ vehicleId });
  const remove = useDeleteWithConfirm<VehicleRentalOut>(useDeleteVehicleRentalOut());
  const rows = data?.items ?? [];
  const canAdd = vehicleStatus === 'Available';

  return (
    <Card>
      <CardContent>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('vehicleRentalsOut.title')}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {t('vehicleRentalsOut.description')}
            </Typography>
          </Box>
          <Tooltip title={canAdd ? '' : t('vehicleRentalsOut.notAvailableHint')}>
            <span>
              <Button
                size="small"
                startIcon={<CarRentalOutlined />}
                disabled={!canAdd}
                onClick={() => setAdding(true)}
              >
                {t('vehicleRentalsOut.add')}
              </Button>
            </span>
          </Tooltip>
        </Stack>

        {summary && (
          <Typography variant="body2" sx={{ mt: 1, fontWeight: 600 }}>
            {t('vehicleRentalsOut.totalRevenue')}: {formatMoney(summary.totalValue, locale)}
          </Typography>
        )}

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('vehicleRentalsOut.empty')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('vehicleRentalsOut.renter')}</TableCell>
                  <TableCell align="right">{t('vehicleRentalsOut.dailyRate')}</TableCell>
                  <TableCell>{t('vehicleRentalsOut.startDate')}</TableCell>
                  <TableCell>{t('vehicleRentalsOut.endDate')}</TableCell>
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
                    <TableCell>{row.renterDisplayName}</TableCell>
                    <TableCell align="right">{formatMoney(row.dailyRate, locale)}</TableCell>
                    <TableCell>{formatDate(row.startDate)}</TableCell>
                    <TableCell>
                      {row.endDate ? (
                        formatDate(row.endDate)
                      ) : (
                        <Chip
                          label={t('vehicleRentalsOut.stillOut')}
                          color="warning"
                          size="small"
                        />
                      )}
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end' }}>
                        {row.isOpen && (
                          <Tooltip title={t('vehicleRentalsOut.return')}>
                            <IconButton
                              size="small"
                              onClick={(event) => {
                                event.stopPropagation();
                                setReturning(row);
                              }}
                            >
                              <AssignmentReturnOutlined fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
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
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      <VehicleRentalOutDialog
        open={adding}
        vehicleId={vehicleId}
        onClose={() => setAdding(false)}
      />
      <VehicleRentalOutDialog
        open={!!editing}
        vehicleId={vehicleId}
        editingRental={editing}
        onClose={() => setEditing(null)}
      />

      <VehicleRentalOutReturnDialog rental={returning} onClose={() => setReturning(null)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('vehicleRentalsOut.deleteTitle')}
        description={t('vehicleRentalsOut.deleteBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Card>
  );
}

function VehicleRentalOutDialog({
  open,
  vehicleId,
  editingRental,
  onClose,
}: {
  open: boolean;
  vehicleId: string;
  editingRental?: VehicleRentalOut | null;
  onClose: () => void;
}) {
  const t = useT();
  const { data: customers } = useAllCustomersQuery();
  const record = useRecordVehicleRentalOut();
  const update = useUpdateVehicleRentalOut();
  const isEditing = !!editingRental;

  const [customerId, setCustomerId] = useState<string | null>(null);
  const [renterName, setRenterName] = useState('');
  const [dailyRate, setDailyRate] = useState('');
  const [startDate, setStartDate] = useState('');
  const [note, setNote] = useState('');

  const resetRecord = record.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetRecord();
    resetUpdate();

    if (editingRental) {
      setCustomerId(editingRental.customerId);
      setRenterName(editingRental.renterName);
      setDailyRate(String(editingRental.dailyRate));
      setStartDate(editingRental.startDate);
      setNote(editingRental.note ?? '');
    } else {
      setCustomerId(null);
      setRenterName('');
      setDailyRate('');
      setStartDate('');
      setNote('');
    }
  }, [open, editingRental, resetRecord, resetUpdate]);

  const parsedRate = Number(dailyRate);
  const rateIsValid = dailyRate.trim() !== '' && !Number.isNaN(parsedRate) && parsedRate > 0;
  const canSubmit = rateIsValid && renterName.trim() !== '';

  const mutation = isEditing ? update : record;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const shared = {
      customerId,
      renterName: renterName.trim(),
      dailyRate: parsedRate,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingRental.id, input: { ...shared, startDate } },
        { onSuccess: onClose },
      );
    } else {
      record.mutate(
        { vehicleId, ...shared, startDate: startDate || null },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {isEditing ? t('vehicleRentalsOut.editTitle') : t('vehicleRentalsOut.add')}
      </DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <Autocomplete
              options={customers?.items ?? []}
              getOptionLabel={(option) => option.name}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={customers?.items.find((c) => c.id === customerId) ?? null}
              onChange={(_, value) => setCustomerId(value?.id ?? null)}
              renderInput={(params) => (
                <TextField {...params} label={t('vehicleRentalsOut.customer')} />
              )}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              required
              label={t('vehicleRentalsOut.renterName')}
              value={renterName}
              onChange={(event) => setRenterName(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('vehicleRentalsOut.dailyRate')}
              value={dailyRate}
              onChange={(event) => setDailyRate(event.target.value)}
              error={dailyRate.trim() !== '' && !rateIsValid}
              helperText={
                dailyRate.trim() !== '' && !rateIsValid
                  ? t('vehicleRentalsOut.mustBePositive')
                  : undefined
              }
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('vehicleRentalsOut.startDate')}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
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

/** Closes an open loan — a separate, deliberately narrow action from correcting one. */
function VehicleRentalOutReturnDialog({
  rental,
  onClose,
}: {
  rental: VehicleRentalOut | null;
  onClose: () => void;
}) {
  const t = useT();
  const { locale } = useI18n();
  const returnRental = useReturnVehicleRentalOut();
  const [endDate, setEndDate] = useState('');

  const resetReturn = returnRental.reset;

  useEffect(() => {
    if (!rental) return;
    resetReturn();
    setEndDate('');
  }, [rental, resetReturn]);

  if (!rental) return null;

  const error = returnRental.isError ? toApiError(returnRental.error) : null;

  const submit = () => {
    returnRental.mutate(
      { id: rental.id, input: { endDate: endDate || null } },
      { onSuccess: onClose },
    );
  };

  return (
    <Dialog open={!!rental} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('vehicleRentalsOut.returnTitle')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          {t('vehicleRentalsOut.returnBody')}
        </Typography>
        <Typography variant="body2" sx={{ mb: 2 }}>
          {rental.renterDisplayName} — {formatMoney(rental.dailyRate, locale)}/
          {t('vehicleRentalsOut.dailyRate').toLowerCase()}
        </Typography>
        <TextField
          type="date"
          fullWidth
          label={t('vehicleRentalsOut.endDate')}
          value={endDate}
          onChange={(event) => setEndDate(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          helperText={t('vehicleRentalsOut.defaultsToday')}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" loading={returnRental.isPending} onClick={submit}>
          {t('vehicleRentalsOut.return')}
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
