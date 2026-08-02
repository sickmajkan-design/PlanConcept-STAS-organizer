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
import type { MaterialInput } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useCreateMaterial, useMaterialQuery, useUpdateMaterial } from '../../features/materials/useMaterials';
import { materialFormSchema, type MaterialFormValues } from '../../features/materials/validation';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: MaterialFormValues = {
  name: '',
  unit: '',
  quantity: '0',
  warehouse: '',
  projectId: '',
};

export function MaterialFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();

  const { data: existing, isLoading, isError, error, refetch } = useMaterialQuery(id);
  const { data: allProjects } = useAllProjectsQuery();
  const createMaterial = useCreateMaterial();
  const updateMaterial = useUpdateMaterial(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<MaterialFormValues>({
    resolver: zodResolver(materialFormSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({
        name: existing.name,
        unit: existing.unit,
        quantity: String(existing.quantity),
        warehouse: existing.warehouse ?? '',
        projectId: existing.projectId ?? '',
      });
    }
  }, [existing, reset]);

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: MaterialFormValues) => {
    const input: MaterialInput = {
      name: values.name.trim(),
      unit: values.unit.trim(),
      quantity: Number(values.quantity),
      warehouse: values.warehouse || null,
      projectId: values.projectId || null,
    };

    try {
      const saved = isEdit
        ? await updateMaterial.mutateAsync(input)
        : await createMaterial.mutateAsync(input);

      navigate(paths.materialDetail(saved.id));
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof MaterialFormValues, {
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
        {isEdit ? t('materials.editTitle') : t('materials.newTitle')}
      </Typography>

      {isEdit && (
        <Alert severity="info" sx={{ mt: 1 }}>
          This form sets an absolute quantity. For day-to-day stock movements, use "Adjust stock" on the
          material's detail page instead.
        </Alert>
      )}

      <Paper sx={{ p: 3, mt: 2 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Grid container spacing={2}>
              <Grid size={12}>
                <Controller
                  name="name"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('materials.name')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="unit"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('materials.unitHint')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="quantity"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('materials.quantity')}
                      type="number"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="warehouse"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('materials.warehouse')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="projectId"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="material-project-label">{t('materials.project')}</InputLabel>
                      <Select {...field} labelId="material-project-label" label={t('materials.project')}>
                        <MenuItem value="">
                          <em>Warehouse stock (no project)</em>
                        </MenuItem>
                        {(allProjects?.items ?? []).map((project) => (
                          <MenuItem key={project.id} value={project.id}>
                            {project.name}
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
                {t('common.cancel')}
              </Button>
              <Button type="submit" variant="contained" loading={isSubmitting}>
                {isEdit ? t('common.save') : 'Create material'}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
