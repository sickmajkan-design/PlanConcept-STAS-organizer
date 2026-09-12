import { zodResolver } from '@hookform/resolvers/zod';
import { DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Grid,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';

import { LOGO_ACCEPTED_EXTENSIONS, MAX_LOGO_BYTES } from '../../api/companySettings';
import { toApiError } from '../../api/apiError';
import type { CompanySettingsInput } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { config } from '../../config';
import {
  companySettingsFormSchema,
  type CompanySettingsFormValues,
} from '../../features/companySettings/validation';
import {
  useCompanySettingsQuery,
  useDeleteCompanyLogo,
  useUpdateCompanySettings,
  useUploadCompanyLogo,
} from '../../features/companySettings/useCompanySettings';
import { useT } from '../../i18n/useI18n';

const emptyValues: CompanySettingsFormValues = {
  name: '',
  address: '',
  taxId: '',
  registrationNumber: '',
  vatNumber: '',
  phone: '',
  email: '',
  weeklyReportsForwardEmail: '',
};

export function CompanySettingsPage() {
  const t = useT();
  const { data: existing, isLoading, isError, error, refetch } = useCompanySettingsQuery();
  const updateSettings = useUpdateCompanySettings();
  const uploadLogo = useUploadCompanyLogo();
  const deleteLogo = useDeleteCompanyLogo();

  const [saved, setSaved] = useState(false);
  // Bumped on every logo change so the browser does not keep serving the
  // previous image from cache for a URL that never changes shape.
  const [logoCacheBust, setLogoCacheBust] = useState(0);
  const [logoError, setLogoError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<CompanySettingsFormValues>({
    resolver: zodResolver(companySettingsFormSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({
        name: existing.name ?? '',
        address: existing.address ?? '',
        taxId: existing.taxId ?? '',
        registrationNumber: existing.registrationNumber ?? '',
        vatNumber: existing.vatNumber ?? '',
        phone: existing.phone ?? '',
        email: existing.email ?? '',
        weeklyReportsForwardEmail: existing.weeklyReportsForwardEmail ?? '',
      });
    }
  }, [existing, reset]);

  if (isLoading) {
    return null;
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: CompanySettingsFormValues) => {
    setSaved(false);

    const input: CompanySettingsInput = {
      name: values.name.trim(),
      address: values.address || null,
      taxId: values.taxId || null,
      registrationNumber: values.registrationNumber || null,
      vatNumber: values.vatNumber || null,
      phone: values.phone || null,
      email: values.email || null,
      weeklyReportsForwardEmail: values.weeklyReportsForwardEmail || null,
    };

    try {
      await updateSettings.mutateAsync(input);
      setSaved(true);
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof CompanySettingsFormValues, {
            message: apiError.errorFor(field),
          });
        }
      }

      setError('root', { message: apiError.message });
    }
  };

  const pickLogo = async (chosen: FileList | null) => {
    setLogoError(null);

    const file = chosen?.[0];

    if (!file) {
      return;
    }

    if (file.size > MAX_LOGO_BYTES) {
      setLogoError(
        t('companySettings.logoTooLarge', {
          limit: Math.round(MAX_LOGO_BYTES / (1024 * 1024)),
        }),
      );
      return;
    }

    try {
      await uploadLogo.mutateAsync(file);
      setLogoCacheBust((n) => n + 1);
    } catch (err) {
      setLogoError(toApiError(err).message);
    }
  };

  const removeLogo = async () => {
    setLogoError(null);

    try {
      await deleteLogo.mutateAsync();
      setLogoCacheBust((n) => n + 1);
    } catch (err) {
      setLogoError(toApiError(err).message);
    }
  };

  const rootError = errors.root as { message?: string } | undefined;

  const logoUrl = existing?.hasLogo
    ? `${config.apiBaseUrl}/api/v1/company-settings/logo?v=${logoCacheBust}`
    : null;

  return (
    <Box sx={{ maxWidth: 720 }}>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 700 }}>
        {t('companySettings.title')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('companySettings.subtitle')}
      </Typography>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1.5 }}>
          {t('companySettings.logo')}
        </Typography>

        {logoError && (
          <Alert severity="error" sx={{ mb: 1.5 }}>
            {logoError}
          </Alert>
        )}

        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Avatar
            src={logoUrl ?? undefined}
            variant="rounded"
            sx={{ width: 72, height: 72, bgcolor: 'grey.100' }}
          >
            {!logoUrl && (existing?.name?.charAt(0) ?? '?')}
          </Avatar>

          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              component="label"
              disabled={uploadLogo.isPending}
            >
              {t('companySettings.uploadLogo')}
              <input
                hidden
                type="file"
                accept={LOGO_ACCEPTED_EXTENSIONS}
                onChange={(event) => {
                  void pickLogo(event.target.files);
                  event.target.value = '';
                }}
              />
            </Button>

            {logoUrl && (
              <Button
                variant="text"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => void removeLogo()}
                disabled={deleteLogo.isPending}
              >
                {t('companySettings.removeLogo')}
              </Button>
            )}
          </Stack>
        </Stack>
      </Paper>

      <Paper sx={{ p: 3 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {saved && <Alert severity="success">{t('companySettings.saved')}</Alert>}
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Grid container spacing={2}>
              <Grid size={12}>
                <Controller
                  name="name"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.name')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Controller
                  name="address"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.address')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="taxId"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.taxId')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="registrationNumber"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.registrationNumber')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="vatNumber"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.vatNumber')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="phone"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.phone')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="email"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.email')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Controller
                  name="weeklyReportsForwardEmail"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('companySettings.weeklyReportsForwardEmail')}
                      helperText={
                        fieldState.error?.message ?? t('companySettings.weeklyReportsForwardEmailHint')
                      }
                      error={!!fieldState.error}
                      fullWidth
                    />
                  )}
                />
              </Grid>
            </Grid>

            <Stack direction="row" spacing={2} sx={{ justifyContent: 'flex-end' }}>
              <Button type="submit" variant="contained" loading={isSubmitting}>
                {t('common.save')}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
