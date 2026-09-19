import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  Divider,
  FormControlLabel,
  Grid,
  MenuItem,
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, type ReactNode } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { accommodationTypes, type AccommodationInput } from '../../api/types';
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
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: AccommodationFormValues = {
  address: '',
  name: '',
  type: 'Apartment',
  city: '',
  floor: '',
  rooms: '',
  beds: '',
  areaSquareMeters: '',
  landlordName: '',
  landlordPhone: '',
  landlordEmail: '',
  contractNumber: '',
  contractStart: '',
  contractEnd: '',
  depositAmount: '',
  utilitiesIncluded: false,
  isActive: true,
  note: '',
};

const numberOrNull = (value: string) => (value === '' ? null : Number(value));

/** One titled block of the form, the same shape the material form uses. */
function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
        {title}
      </Typography>
      <Divider sx={{ my: 2 }} />
      {children}
    </Paper>
  );
}

export function AccommodationFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();

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
        name: existing.name ?? '',
        type: existing.type,
        city: existing.city ?? '',
        floor: existing.floor ?? '',
        rooms: existing.rooms === null ? '' : String(existing.rooms),
        beds: existing.beds === null ? '' : String(existing.beds),
        areaSquareMeters: existing.areaSquareMeters === null ? '' : String(existing.areaSquareMeters),
        landlordName: existing.landlordName ?? '',
        landlordPhone: existing.landlordPhone ?? '',
        landlordEmail: existing.landlordEmail ?? '',
        contractNumber: existing.contractNumber ?? '',
        contractStart: existing.contractStart ?? '',
        contractEnd: existing.contractEnd ?? '',
        depositAmount: existing.depositAmount === null ? '' : String(existing.depositAmount),
        utilitiesIncluded: existing.utilitiesIncluded,
        isActive: existing.isActive,
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
      name: values.name || null,
      type: values.type,
      city: values.city || null,
      floor: values.floor || null,
      rooms: numberOrNull(values.rooms),
      beds: numberOrNull(values.beds),
      areaSquareMeters: numberOrNull(values.areaSquareMeters),
      landlordName: values.landlordName || null,
      landlordPhone: values.landlordPhone || null,
      landlordEmail: values.landlordEmail || null,
      contractNumber: values.contractNumber || null,
      contractStart: values.contractStart || null,
      contractEnd: values.contractEnd || null,
      depositAmount: numberOrNull(values.depositAmount),
      utilitiesIncluded: values.utilitiesIncluded,
      isActive: values.isActive,
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

  const text = (name: keyof AccommodationFormValues, label: string, extra: object = {}) => (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          value={(field.value as string | undefined) ?? ''}
          label={label}
          fullWidth
          error={!!fieldState.error}
          helperText={fieldState.error?.message}
          {...extra}
        />
      )}
    />
  );

  const date = (name: keyof AccommodationFormValues, label: string) =>
    text(name, label, { type: 'date', slotProps: { inputLabel: { shrink: true } } });

  return (
    <Box sx={{ maxWidth: 840 }}>
      <Typography variant="h5" sx={{ fontWeight: 700 }}>
        {isEdit ? t('accommodations.editTitle') : t('accommodations.newTitle')}
      </Typography>

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Stack spacing={2.5} sx={{ mt: 3 }}>
          {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

          <Section title={t('accommodations.sectionPlace')}>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 8 }}>
                {text('name', t('accommodations.name'), { helperText: t('accommodations.nameHint') })}
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="type"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} select fullWidth label={t('accommodations.type')}>
                      {accommodationTypes.map((value) => (
                        <MenuItem key={value} value={value}>
                          {enumLabel('accommodationType', value)}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 8 }}>{text('address', t('accommodations.address'))}</Grid>
              <Grid size={{ xs: 12, sm: 4 }}>{text('city', t('accommodations.city'))}</Grid>
              <Grid size={{ xs: 6, sm: 3 }}>{text('floor', t('accommodations.floor'))}</Grid>
              <Grid size={{ xs: 6, sm: 3 }}>
                {text('rooms', t('accommodations.rooms'), { type: 'number' })}
              </Grid>
              <Grid size={{ xs: 6, sm: 3 }}>
                {text('beds', t('accommodations.beds'), {
                  type: 'number',
                  helperText: errors.beds?.message ?? t('accommodations.bedsHint'),
                })}
              </Grid>
              <Grid size={{ xs: 6, sm: 3 }}>
                {text('areaSquareMeters', t('accommodations.area'), { type: 'number' })}
              </Grid>
              <Grid size={12}>
                <Controller
                  name="utilitiesIncluded"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Switch checked={field.value} onChange={field.onChange} />}
                      label={t('accommodations.utilitiesIncluded')}
                    />
                  )}
                />
                <Controller
                  name="isActive"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Switch checked={field.value} onChange={field.onChange} />}
                      label={t('accommodations.isActive')}
                    />
                  )}
                />
              </Grid>
            </Grid>
          </Section>

          <Section title={t('accommodations.sectionLandlord')}>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                {text('landlordName', t('accommodations.landlordName'))}
              </Grid>
              <Grid size={{ xs: 12, sm: 3 }}>
                {text('landlordPhone', t('accommodations.landlordPhone'))}
              </Grid>
              <Grid size={{ xs: 12, sm: 3 }}>
                {text('landlordEmail', t('accommodations.landlordEmail'))}
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                {text('contractNumber', t('accommodations.contractNumber'))}
              </Grid>
              <Grid size={{ xs: 6, sm: 4 }}>{date('contractStart', t('accommodations.contractStart'))}</Grid>
              <Grid size={{ xs: 6, sm: 4 }}>{date('contractEnd', t('accommodations.contractEnd'))}</Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                {text('depositAmount', t('accommodations.deposit'), {
                  type: 'number',
                  helperText: errors.depositAmount?.message ?? t('accommodations.depositHint'),
                })}
              </Grid>
            </Grid>
          </Section>

          <Section title={t('accommodations.sectionNotes')}>
            {text('note', t('accommodations.note'), {
              multiline: true,
              minRows: 3,
              helperText: errors.note?.message ?? t('accommodations.noteHint'),
            })}
          </Section>

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
    </Box>
  );
}
