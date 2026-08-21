import {
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
  Divider,
  FormControl,
  Grid,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { AttachmentList } from '../../components/AttachmentList';
import { QrLabelDialog } from '../../components/QrLabelDialog';
import { StatusChip } from '../../components/StatusChip';
import { useCoverPhoto } from '../../features/attachments/useAttachments';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useAssignVehicle,
  useDeleteVehicle,
  useUnassignVehicle,
  useVehicleQuery,
} from '../../features/vehicles/useVehicles';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { paths } from '../../routes/paths';

export function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();

  const { data: vehicle, isLoading, isError, error, refetch } = useVehicleQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();
  const coverPhoto = useCoverPhoto('Vehicle', id ?? '');

  const assign = useAssignVehicle(id ?? '');
  const unassign = useUnassignVehicle(id ?? '');
  const deleteVehicle = useDeleteVehicle();

  const [selectedEmployeeId, setSelectedEmployeeId] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [confirmUnassign, setConfirmUnassign] = useState(false);
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
                <InfoRow label={t('vehicles.vin')} value={vehicle.vin} />
                <InfoRow label={t('vehicles.qrCode')} value={vehicle.qrCode} />
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
      />
    </Box>
  );
}

function InfoRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value || '—'}</Typography>
    </Box>
  );
}
