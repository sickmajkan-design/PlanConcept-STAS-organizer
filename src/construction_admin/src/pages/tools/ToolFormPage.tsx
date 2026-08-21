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
import type { ToolInput } from '../../api/types';
import { toolStatuses } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { useCreateTool, useToolQuery, useUpdateTool } from '../../features/tools/useTools';
import { toolFormSchema, type ToolFormValues } from '../../features/tools/validation';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: ToolFormValues = {
  name: '',
  category: '',
  serialNumber: '',
  qrCode: '',
  status: 'Available',
};

export function ToolFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();

  const { data: existing, isLoading, isError, error, refetch } = useToolQuery(id);
  const createTool = useCreateTool();
  const updateTool = useUpdateTool(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<ToolFormValues>({
    resolver: zodResolver(toolFormSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({
        name: existing.name,
        category: existing.category ?? '',
        serialNumber: existing.serialNumber ?? '',
        qrCode: existing.qrCode ?? '',
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

  const onSubmit = async (values: ToolFormValues) => {
    const input: ToolInput = {
      name: values.name.trim(),
      category: values.category || null,
      serialNumber: values.serialNumber || null,
      qrCode: values.qrCode || null,
      status: values.status,
    };

    try {
      const saved = isEdit
        ? await updateTool.mutateAsync(input)
        : await createTool.mutateAsync(input);

      navigate(paths.toolDetail(saved.id));
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof ToolFormValues, {
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
        {isEdit ? t('tools.editTitle') : t('tools.newTitle')}
      </Typography>

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
                      label={t('tools.name')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="category"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('tools.category')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="serialNumber"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('tools.serialNumber')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="qrCode"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('tools.qrCode')}
                      placeholder={t('tools.qrCodeHint')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="status"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="tool-status-label">{t('tools.status')}</InputLabel>
                      <Select {...field} labelId="tool-status-label" label={t('tools.status')}>
                        {toolStatuses.map((value) => (
                          <MenuItem key={value} value={value}>
                            {enumLabel('toolStatus', value)}
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
                {isEdit ? t('common.save') : 'Create tool'}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
