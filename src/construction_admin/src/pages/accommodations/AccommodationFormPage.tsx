import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Button, Grid, Paper, Stack, TextField, Typography } from '@mui/material';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { AccommodationInput } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import {
  useAccommodationQuery,
  useCreateAccommodation,
  useUpdateAccommodation,
} from '../../features/accommodations/useAccommodations';
import {
  accommodationFormSchema,
  type AccommodationFormValues,
} from '../../features/accommodations/validation';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: AccommodationFormValues = {
  address: '',
  note: '',
};

export function AccommodationFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();

  const { data: existing, isLoading, isError, error, refetch } = useAccommodationQuery(id);
  const createAccommodation = useCreateAccommodation();
  const updateAccommodation = useUpdateAccommodation(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<AccommodationFormValues>({
    resolver: zodResolver(accommodationFormSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({
        address: existing.address,
        note: existing.note ?? '',
      });
    }
  }, [existing, reset]);

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: AccommodationFormValues) => {
    const input: AccommodationInput = {
      address: values.address.trim(),
      note: values.note || null,
    };

    try {
      const saved = isEdit
        ? await updateAccommodation.mutateAsync(input)
        : await createAccommodation.mutateAsync(input);

      navigate(paths.accommodationDetail(saved.id));
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof AccommodationFormValues, {
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
        {isEdit ? t('accommodations.editTitle') : t('accommodations.newTitle')}
      </Typography>

      <Paper sx={{ p: 3, mt: 2 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Grid container spacing={2}>
              <Grid size={12}>
                <Controller
                  name="address"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('accommodations.address')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Controller
                  name="note"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('accommodations.note')}
                      fullWidth
                      multiline
                      minRows={2}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
            </Grid>

            <Stack direction="row" spacing={2} sx={{ justifyContent: 'flex-end' }}>
              <Button onClick={() => navigate(-1)} disabled={isSubmitting}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" variant="contained" loading={isSubmitting}>
                {isEdit ? t('common.save') : t('accommodations.create')}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
