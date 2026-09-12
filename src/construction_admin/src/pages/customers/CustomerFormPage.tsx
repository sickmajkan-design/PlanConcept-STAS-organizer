import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  Divider,
  Grid,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { customersApi } from '../../api/customers';
import { toApiError } from '../../api/apiError';
import type { CustomerInput } from '../../api/types';
import { isSuperAdmin } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { DuplicateWarningAlert } from '../../components/DuplicateWarningAlert';
import { ErrorState } from '../../components/ErrorState';
import { useCreateCustomer, useCustomerQuery, useUpdateCustomer } from '../../features/customers/useCustomers';
import { customerFormSchema, type CustomerFormValues } from '../../features/customers/validation';
import { useDuplicateWarning } from '../../hooks/useDuplicateWarning';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

async function searchSimilarCustomers(term: string) {
  const result = await customersApi.list({ pageNumber: 1, pageSize: 5, search: term });
  return result.items.map((c) => ({
    id: c.id,
    label: c.name,
    path: paths.customerEdit(c.id),
  }));
}

const emptyValues: CustomerFormValues = {
  name: '',
  contactPerson: '',
  phone: '',
  email: '',
  note: '',
  taxId: '',
  registrationNumber: '',
  vatNumber: '',
};

export function CustomerFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();
  const { user } = useAuth();
  const canEditTaxDetails = isSuperAdmin(user);
  const canViewTaxDetails = canEditTaxDetails || !!user?.canViewCustomerTaxDetails;

  const { data: existing, isLoading, isError, error, refetch } = useCustomerQuery(id);
  const createCustomer = useCreateCustomer();
  const updateCustomer = useUpdateCustomer(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerFormSchema),
    defaultValues: emptyValues,
  });

  const duplicates = useDuplicateWarning(searchSimilarCustomers, watch('name'), id);

  useEffect(() => {
    if (existing) {
      reset({
        name: existing.name,
        contactPerson: existing.contactPerson ?? '',
        phone: existing.phone ?? '',
        email: existing.email ?? '',
        note: existing.note ?? '',
        taxId: existing.taxId ?? '',
        registrationNumber: existing.registrationNumber ?? '',
        vatNumber: existing.vatNumber ?? '',
      });
    }
  }, [existing, reset]);

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: CustomerFormValues) => {
    const input: CustomerInput = {
      name: values.name.trim(),
      contactPerson: values.contactPerson || null,
      phone: values.phone || null,
      email: values.email || null,
      note: values.note || null,
      ...(canEditTaxDetails
        ? {
            taxId: values.taxId || null,
            registrationNumber: values.registrationNumber || null,
            vatNumber: values.vatNumber || null,
          }
        : {}),
    };

    try {
      if (isEdit) {
        await updateCustomer.mutateAsync(input);
      } else {
        await createCustomer.mutateAsync(input);
      }

      navigate(paths.customers);
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof CustomerFormValues, {
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
        {isEdit ? t('customers.editTitle') : t('customers.newTitle')}
      </Typography>

      <Paper sx={{ p: 3, mt: 2 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}
            {!isEdit && <DuplicateWarningAlert candidates={duplicates.candidates} />}

            <Grid container spacing={2}>
              <Grid size={12}>
                <Controller
                  name="name"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('customers.name')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="contactPerson"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('customers.contactPerson')}
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
                      label={t('customers.phone')}
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
                      label={t('customers.email')}
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
                      label={t('customers.note')}
                      fullWidth
                      multiline
                      minRows={2}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>

              {canViewTaxDetails && (
                <>
                  <Grid size={12}>
                    <Divider sx={{ my: 0.5 }} />
                    <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
                      {t('customers.taxSection')}
                    </Typography>
                    {!canEditTaxDetails && (
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        {t('customers.taxSectionReadOnlyHint')}
                      </Typography>
                    )}
                  </Grid>
                  <Grid size={{ xs: 12, sm: 4 }}>
                    <Controller
                      name="taxId"
                      control={control}
                      render={({ field, fieldState }) => (
                        <TextField
                          {...field}
                          label={t('customers.taxId')}
                          fullWidth
                          disabled={!canEditTaxDetails}
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
                          label={t('customers.registrationNumber')}
                          fullWidth
                          disabled={!canEditTaxDetails}
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
                          label={t('customers.vatNumber')}
                          fullWidth
                          disabled={!canEditTaxDetails}
                          error={!!fieldState.error}
                          helperText={fieldState.error?.message}
                        />
                      )}
                    />
                  </Grid>
                </>
              )}
            </Grid>

            <Stack direction="row" spacing={2} sx={{ justifyContent: 'flex-end' }}>
              <Button onClick={() => navigate(-1)} disabled={isSubmitting}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" variant="contained" loading={isSubmitting}>
                {isEdit ? t('common.save') : t('customers.create')}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
