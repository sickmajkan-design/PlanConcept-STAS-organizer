import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { VehicleInput } from '../../api/types';
import { fuelTypes, vehicleStatuses } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { useCreateVehicle, useUpdateVehicle, useVehicleQuery } from '../../features/vehicles/useVehicles';
import { vehicleFormSchema, type VehicleFormValues } from '../../features/vehicles/validation';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

const emptyValues: VehicleFormValues = {
  brand: '',
  model: '',
  registrationNumber: '',
  vin: '',
  fuelType: 'Diesel',
  status: 'Available',
};

export function VehicleFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();

  const { data: existing, isLoading, isError, error, refetch } = useVehicleQuery(id);
  const createVehicle = useCreateVehicle();
  const updateVehicle = useUpdateVehicle(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<VehicleFormValues>({
    resolver: zodResolver(vehicleFormSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({
        brand: existing.brand,
        model: existing.model,
        registrationNumber: existing.registrationNumber,
        vin: existing.vin ?? '',
        fuelType: existing.fuelType,
        status: existing.status,
      });
    }
  }, [existing, reset]);

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: VehicleFormValues) => {
    const input: VehicleInput = {
      brand: values.brand.trim(),
      model: values.model.trim(),
      registrationNumber: values.registrationNumber.trim(),
      vin: values.vin || null,
      fuelType: values.fuelType,
      status: values.status,
    };

    try {
      const saved = isEdit
        ? await updateVehicle.mutateAsync(input)
        : await createVehicle.mutateAsync(input);

      navigate(paths.vehicleDetail(saved.id));
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof VehicleFormValues, {
            message: apiError.errorFor(field),
          });
        }
      }

      setError('root', { message: apiError.message });
    }
  };

  const rootError = errors.root as { message?: string } | undefined;

  return (
    <Box sx={{ maxWidth: 720 }}>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 700 }}>
        {isEdit ? 'Edit vehicle' : 'Add vehicle'}
      </Typography>

      <Paper sx={{ p: 3, mt: 2 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="brand"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label="Brand"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="model"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label="Model"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="registrationNumber"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label="Registration number"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="vin"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label="VIN"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="fuelType"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="vehicle-fuel-type-label">Fuel type</InputLabel>
                      <Select {...field} labelId="vehicle-fuel-type-label" label="Fuel type">
                        {fuelTypes.map((value) => (
                          <MenuItem key={value} value={value}>
                            {humanizeEnum(value)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="status"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="vehicle-status-label">Status</InputLabel>
                      <Select {...field} labelId="vehicle-status-label" label="Status">
                        {vehicleStatuses.map((value) => (
                          <MenuItem key={value} value={value}>
                            {humanizeEnum(value)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
            </Grid>

            <Stack direction="row" spacing={2} sx={{ justifyContent: 'flex-end' }}>
              <Button onClick={() => navigate(-1)} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" variant="contained" loading={isSubmitting}>
                {isEdit ? 'Save changes' : 'Create vehicle'}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
