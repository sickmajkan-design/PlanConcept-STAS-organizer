import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Divider,
  FormControl,
  Grid,
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState, type ReactNode } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { MaterialInput } from '../../api/types';
import { useAuth } from '../../auth/useAuth';
import { canSeeSpending } from '../../auth/authHelpers';
import { ErrorState } from '../../components/ErrorState';
import { InvoiceFilePicker } from '../../components/InvoiceFilePicker';
import { costsApi } from '../../api/costs';
import { attachmentsApi } from '../../api/attachments';
import { useMovementSuppliersQuery } from '../../features/costs/useCosts';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useCreateMaterial, useMaterialQuery, useUpdateMaterial } from '../../features/materials/useMaterials';
import { materialFormSchema, type MaterialFormValues } from '../../features/materials/validation';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatMoney } from '../../utils/formatting';

const emptyValues: MaterialFormValues = {
  name: '',
  unit: '',
  quantity: '0',
  warehouse: '',
  unitPrice: '',
  minimumQuantity: '',
  projectId: '',
  receivedOn: '',
  supplier: '',
  invoiceNumber: '',
  purchaseUnitPrice: '',
  receiptNote: '',
};

/** One titled block of the form: a heading, what it is for, and its fields. */
function Section({
  title,
  hint,
  children,
}: {
  title: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
        {title}
      </Typography>
      {hint && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, maxWidth: 560 }}>
          {hint}
        </Typography>
      )}
      <Divider sx={{ my: 2 }} />
      {children}
    </Paper>
  );
}

export function MaterialFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  // Price, supplier and invoice are spending records; without the right to
  // record spending only the quantity is asked for.
  const showReceiptDetails = canSeeSpending(user);

  const { data: existing, isLoading, isError, error, refetch } = useMaterialQuery(id);
  const { data: allProjects } = useAllProjectsQuery();
  const { data: knownSuppliers } = useMovementSuppliersQuery(!isEdit && showReceiptDetails);
  const createMaterial = useCreateMaterial();
  const [invoiceFile, setInvoiceFile] = useState<File | null>(null);
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

  const [unit, quantity, purchasePrice, invoiceNumber] = useWatch({
    control,
    name: ['unit', 'quantity', 'purchaseUnitPrice', 'invoiceNumber'],
  });
  const receivedQuantity = Number(quantity);
  const receiptTotal =
    receivedQuantity > 0 && purchasePrice !== '' && !Number.isNaN(Number(purchasePrice))
      ? receivedQuantity * Number(purchasePrice)
      : null;

  useEffect(() => {
    if (existing) {
      reset({
        ...emptyValues,
        name: existing.name,
        unit: existing.unit,
        quantity: String(existing.quantity),
        warehouse: existing.warehouse ?? '',
        unitPrice: existing.unitPrice === null ? '' : String(existing.unitPrice),
        minimumQuantity: existing.minimumQuantity === null ? '' : String(existing.minimumQuantity),
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
    const purchase = values.purchaseUnitPrice === '' ? null : Number(values.purchaseUnitPrice);
    const hasStock = Number(values.quantity) > 0;

    const input: MaterialInput = {
      name: values.name.trim(),
      unit: values.unit.trim(),
      quantity: Number(values.quantity),
      warehouse: values.warehouse || null,
      // Left empty, the reference price follows what the first delivery cost.
      unitPrice: values.unitPrice === '' ? purchase : Number(values.unitPrice),
      minimumQuantity: values.minimumQuantity === '' ? null : Number(values.minimumQuantity),
      projectId: values.projectId || null,
      ...(isEdit || !hasStock
        ? {}
        : {
            invoiceNumber: values.invoiceNumber || null,
            supplier: values.supplier || null,
            purchaseUnitPrice: purchase,
            receivedOn: values.receivedOn || null,
            receiptNote: values.receiptNote || null,
          }),
    };

    try {
      const saved = isEdit
        ? await updateMaterial.mutateAsync(input)
        : await createMaterial.mutateAsync(input);

      let invoiceProblem: string | null = null;

      // The starting delivery exists now; the invoice file hangs off it.
      if (!isEdit && invoiceFile && hasStock) {
        try {
          const created = await costsApi.movements.list({
            materialId: saved.id,
            pageNumber: 1,
            pageSize: 1,
            sortBy: 'createdAt',
            sortDescending: true,
          });
          const movement = created.items[0];

          if (movement) {
            await attachmentsApi.upload({
              ownerType: 'MaterialMovement',
              ownerId: movement.id,
              category: 'Other',
              file: invoiceFile,
            });
          }
        } catch (uploadErr) {
          invoiceProblem = toApiError(uploadErr).message;
        }
      }

      navigate(paths.materialDetail(saved.id), {
        state: invoiceProblem ? { invoiceUploadFailed: invoiceProblem } : undefined,
      });
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
  const text = (name: keyof MaterialFormValues, label: string, extra: object = {}) => (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          value={field.value ?? ''}
          label={label}
          fullWidth
          error={!!fieldState.error}
          helperText={fieldState.error?.message}
          {...extra}
        />
      )}
    />
  );

  return (
    <Box sx={{ maxWidth: 840 }}>
      <Typography variant="h5" sx={{ fontWeight: 700 }}>
        {isEdit ? t('materials.editTitle') : t('materials.newTitle')}
      </Typography>

      {isEdit && (
        <Alert severity="info" sx={{ mt: 2 }}>
          {t('materials.absoluteQuantityNotice')}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Stack spacing={2.5} sx={{ mt: 3 }}>
          {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

          <Section title={t('materials.sectionBasics')}>
            <Grid container spacing={2}>
              <Grid size={12}>{text('name', t('materials.name'))}</Grid>
              <Grid size={{ xs: 12, sm: 4 }}>{text('unit', t('materials.unitHint'))}</Grid>
              <Grid size={{ xs: 12, sm: 4 }}>{text('warehouse', t('materials.warehouse'))}</Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="projectId"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="material-project-label">{t('materials.project')}</InputLabel>
                      <Select {...field} labelId="material-project-label" label={t('materials.project')}>
                        <MenuItem value="">
                          <em>{t('materials.warehouseStock')}</em>
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
              {isEdit && (
                <Grid size={{ xs: 12, sm: 4 }}>
                  {text('quantity', t('materials.quantity'), { type: 'number' })}
                </Grid>
              )}
              <Grid size={{ xs: 12, sm: 4 }}>
                {text('minimumQuantity', t('materials.minimumQuantity'), {
                  type: 'number',
                  helperText: errors.minimumQuantity?.message ?? t('materials.minimumHint'),
                })}
              </Grid>
            </Grid>
          </Section>

          {!isEdit && (
            <Section title={t('materials.sectionReceipt')} hint={t('materials.receiptHint')}>
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 4 }}>
                  {text('quantity', t('materials.receivedQuantity'), {
                    type: 'number',
                    slotProps: {
                      input: {
                        endAdornment: unit ? (
                          <InputAdornment position="end">{unit}</InputAdornment>
                        ) : undefined,
                      },
                    },
                  })}
                </Grid>
                {showReceiptDetails && (
                  <>
                    <Grid size={{ xs: 12, sm: 4 }}>
                      {text('receivedOn', t('materials.receivedOn'), {
                        type: 'date',
                        slotProps: { inputLabel: { shrink: true } },
                      })}
                    </Grid>
                    <Grid size={{ xs: 12, sm: 4 }}>
                      {text('purchaseUnitPrice', t('materials.purchasePrice'), {
                        type: 'number',
                        slotProps: {
                          input: {
                            endAdornment: unit ? (
                              <InputAdornment position="end">/ {unit}</InputAdornment>
                            ) : undefined,
                          },
                        },
                      })}
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                      <Controller
                        name="supplier"
                        control={control}
                        render={({ field, fieldState }) => (
                          <Autocomplete
                            freeSolo
                            options={knownSuppliers ?? []}
                            inputValue={field.value ?? ''}
                            onInputChange={(_event, value) => field.onChange(value)}
                            renderInput={(params) => (
                              <TextField
                                {...params}
                                label={t('materials.supplier')}
                                error={!!fieldState.error}
                                helperText={fieldState.error?.message}
                              />
                            )}
                          />
                        )}
                      />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                      {text('invoiceNumber', t('materials.invoiceNumber'))}
                    </Grid>
                    <Grid size={12}>
                      <InvoiceFilePicker file={invoiceFile} onChange={setInvoiceFile} />
                    </Grid>
                    <Grid size={12}>
                      {text('receiptNote', t('materials.receiptNote'), { multiline: true, minRows: 2 })}
                    </Grid>
                  </>
                )}
              </Grid>

              {receivedQuantity > 0 && (
                <Box sx={{ mt: 2 }}>
                  {receiptTotal !== null && (
                    <Typography variant="body2">
                      {t('materials.receiptValue')}:{' '}
                      <strong>{formatMoney(receiptTotal, locale)}</strong>
                    </Typography>
                  )}
                  {!invoiceNumber && (
                    <Typography variant="body2" color="text.secondary">
                      {t('materials.openingStockHint')}
                    </Typography>
                  )}
                </Box>
              )}
            </Section>
          )}

          <Section title={t('materials.sectionPricing')} hint={t('materials.unitPriceHint')}>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                {text('unitPrice', t('materials.unitPrice'), {
                  type: 'number',
                  helperText: isEdit ? undefined : t('materials.priceFromPurchase'),
                })}
              </Grid>
            </Grid>
          </Section>

          <Stack direction="row" spacing={2} sx={{ justifyContent: 'flex-end' }}>
            <Button onClick={() => navigate(-1)} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="contained" loading={isSubmitting}>
              {isEdit ? t('common.save') : t('materials.create')}
            </Button>
          </Stack>
        </Stack>
      </form>
    </Box>
  );
}
