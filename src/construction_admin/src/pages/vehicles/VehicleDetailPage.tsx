import { DeleteOutlined, EditOutlined, LocalShippingOutlined, PersonOffOutlined } from '@mui/icons-material';
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
import { StatusChip } from '../../components/StatusChip';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useAssignVehicle,
  useDeleteVehicle,
  useUnassignVehicle,
  useVehicleQuery,
} from '../../features/vehicles/useVehicles';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

export function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: vehicle, isLoading, isError, error, refetch } = useVehicleQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();

  const assign = useAssignVehicle(id ?? '');
  const unassign = useUnassignVehicle(id ?? '');
  const deleteVehicle = useDeleteVehicle();

  const [selectedEmployeeId, setSelectedEmployeeId] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [confirmUnassign, setConfirmUnassign] = useState(false);

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
            <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}>
              <LocalShippingOutlined />
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {vehicle.brand} {vehicle.model}
              </Typography>
              <Typography color="text.secondary">{vehicle.registrationNumber}</Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: 'center' }}>
                <StatusChip status={vehicle.status} />
              </Stack>
            </Box>
            <Stack direction="row" spacing={1}>
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
                <InfoRow label="VIN" value={vehicle.vin} />
                <InfoRow label="Fuel type" value={humanizeEnum(vehicle.fuelType)} />
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
                  <Typography color="text.secondary">Not assigned to any employee.</Typography>
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
      </Grid>

      <Divider sx={{ my: 3 }} />

      <ConfirmDialog
        open={confirmUnassign}
        title="Unassign vehicle?"
        description={`${vehicle.brand} ${vehicle.model} will no longer be assigned to ${vehicle.assignedEmployeeName ?? 'this employee'}.`}
        confirmLabel="Unassign"
        destructive
        loading={unassign.isPending}
        onConfirm={handleUnassign}
        onCancel={() => setConfirmUnassign(false)}
      />

      <ConfirmDialog
        open={confirmDelete}
        title="Delete vehicle?"
        description={`${vehicle.brand} ${vehicle.model} (${vehicle.registrationNumber}) will be removed from active records.`}
        confirmLabel="Delete"
        destructive
        loading={deleteVehicle.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
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
