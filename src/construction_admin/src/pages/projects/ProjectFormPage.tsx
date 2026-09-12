import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Divider,
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
import { useEffect, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { ProjectInput, ProjectKind } from '../../api/types';
import { projectStatuses } from '../../api/types';
import { COUNTRIES, countryLabel, resolveCountryCode } from '../../data/countries';
import { ErrorState } from '../../components/ErrorState';
import { useAllCustomersQuery } from '../../features/customers/useCustomers';
import {
  useAllMainProjectsQuery,
  useCreateProject,
  useProjectQuery,
  useUpdateProject,
} from '../../features/projects/useProjects';
import { projectFormSchema, type ProjectFormValues } from '../../features/projects/validation';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: ProjectFormValues = {
  name: '',
  description: '',
  customerId: '',
  parentProjectId: '',
  address: '',
  countryCode: '',
  latitude: '',
  longitude: '',
  shiftStartTime: '',
  startDate: '',
  endDate: '',
  status: 'Planned',
  contractValue: '',
};

/**
 * The API stores the shift start as a UTC time-of-day with no date attached,
 * so converting to/from the local `time` input uses today as a stand-in
 * reference date — the same approach (and the same accepted DST-day edge
 * case) as `TimeEntryFormPage`'s local/UTC helpers.
 */
function utcTimeToLocalInput(value: string | null | undefined): string {
  if (!value) return '';

  const [hours = '0', minutes = '0'] = value.split(':');
  const reference = new Date();
  reference.setUTCHours(Number(hours), Number(minutes), 0, 0);
  if (Number.isNaN(reference.getTime())) return '';

  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(reference.getHours())}:${pad(reference.getMinutes())}`;
}

function localInputToUtcTime(value: string): string | null {
  if (!value) return null;

  const [hours = '0', minutes = '0'] = value.split(':');
  const reference = new Date();
  reference.setHours(Number(hours), Number(minutes), 0, 0);

  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(reference.getUTCHours())}:${pad(reference.getUTCMinutes())}:00`;
}

export function ProjectFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const t = useT();
  const enumLabel = useEnumLabel();

  const { data: existing, isLoading, isError, error, refetch } = useProjectQuery(id);
  const { data: customers } = useAllCustomersQuery();
  const { data: mainProjects } = useAllMainProjectsQuery();
  const createProject = useCreateProject();
  const updateProject = useUpdateProject(id ?? '');

  // Not part of the submitted payload — the API always derives CustomerId
  // from the chosen parent for a Sub project, so this only steers which
  // fields the form shows. Defaults from the `?parentProjectId=` query param
  // set by "Add sub-project" on a Main project's detail page.
  const preselectedParentId = searchParams.get('parentProjectId');
  const [kind, setKind] = useState<ProjectKind>(preselectedParentId ? 'Sub' : 'Main');

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<ProjectFormValues>({
    resolver: zodResolver(projectFormSchema),
    defaultValues: preselectedParentId
      ? { ...emptyValues, parentProjectId: preselectedParentId }
      : emptyValues,
  });

  useEffect(() => {
    if (existing) {
      setKind(existing.kind);
      reset({
        name: existing.name,
        description: existing.description ?? '',
        customerId: existing.customerId ?? '',
        parentProjectId: existing.parentProjectId ?? '',
        address: existing.address ?? '',
        countryCode: countryLabel(existing.countryCode),
        latitude: existing.latitude?.toString() ?? '',
        longitude: existing.longitude?.toString() ?? '',
        shiftStartTime: utcTimeToLocalInput(existing.shiftStartTime),
        startDate: existing.startDate?.slice(0, 10) ?? '',
        endDate: existing.endDate?.slice(0, 10) ?? '',
        status: existing.status,
        contractValue: existing.contractValue?.toString() ?? '',
      });
    }
  }, [existing, reset]);

  const selectedParentId = watch('parentProjectId');
  const selectedParent = useMemo(
    () => (mainProjects?.items ?? []).find((project) => project.id === selectedParentId),
    [mainProjects, selectedParentId],
  );

  // A Main project already carrying sub-projects would strand them one
  // level too deep if it became a Sub itself — the API refuses this, the
  // toggle just keeps the form from offering it in the first place.
  const canBeSub = !isEdit || (existing?.subProjectCount ?? 0) === 0;

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: ProjectFormValues) => {
    const input: ProjectInput = {
      name: values.name.trim(),
      description: values.description || null,
      customerId: kind === 'Sub' ? null : values.customerId || null,
      parentProjectId: kind === 'Sub' ? values.parentProjectId || null : null,
      address: values.address || null,
      countryCode: kind === 'Sub' ? null : resolveCountryCode(values.countryCode ?? ''),
      latitude: values.latitude ? Number(values.latitude) : null,
      longitude: values.longitude ? Number(values.longitude) : null,
      shiftStartTime: localInputToUtcTime(values.shiftStartTime ?? ''),
      startDate: values.startDate || null,
      endDate: values.endDate || null,
      status: values.status,
      contractValue: values.contractValue ? Number(values.contractValue) : null,
    };

    try {
      const saved = isEdit
        ? await updateProject.mutateAsync(input)
        : await createProject.mutateAsync(input);

      // A fresh project has no crew or equipment yet — that's the very next
      // thing anyone does with it, so the detail page briefly highlights
      // where to add them instead of landing on a page that looks finished.
      navigate(paths.projectDetail(saved.id), {
        state: isEdit ? undefined : { justCreated: true },
      });
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof ProjectFormValues, { message: apiError.errorFor(field) });
        }
      }

      setError('root', { message: apiError.message });
    }
  };

  const rootError = errors.root as { message?: string } | undefined;

  return (
    <Box sx={{ maxWidth: 720 }}>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 700 }}>
        {isEdit ? t('projects.editTitle') : t('projects.newTitle')}
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
                      label={t('projects.projectName')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Controller
                  name="description"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('projects.description')}
                      fullWidth
                      multiline
                      minRows={2}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <FormControl fullWidth>
                  <InputLabel id="project-kind-label">{t('projects.kind')}</InputLabel>
                  <Select
                    labelId="project-kind-label"
                    label={t('projects.kind')}
                    value={kind}
                    onChange={(event) => setKind(event.target.value as ProjectKind)}
                    disabled={!canBeSub}
                  >
                    <MenuItem value="Main">{enumLabel('projectKind', 'Main')}</MenuItem>
                    <MenuItem value="Sub">{enumLabel('projectKind', 'Sub')}</MenuItem>
                  </Select>
                  {!canBeSub && (
                    <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, ml: 1.5 }}>
                      {t('projects.kindLockedHint')}
                    </Typography>
                  )}
                </FormControl>
              </Grid>
              {kind === 'Sub' ? (
                <>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Controller
                      name="parentProjectId"
                      control={control}
                      render={({ field, fieldState }) => (
                        <FormControl fullWidth error={!!fieldState.error}>
                          <InputLabel id="project-parent-label">{t('projects.parentProject')}</InputLabel>
                          <Select {...field} labelId="project-parent-label" label={t('projects.parentProject')}>
                            {(mainProjects?.items ?? [])
                              .filter((project) => project.id !== existing?.id)
                              .map((project) => (
                                <MenuItem key={project.id} value={project.id}>
                                  {project.name}
                                </MenuItem>
                              ))}
                          </Select>
                        </FormControl>
                      )}
                    />
                  </Grid>
                  <Grid size={12}>
                    <Typography variant="body2" color="text.secondary">
                      {selectedParent?.customerName
                        ? t('projects.followsCustomerHint', { name: selectedParent.customerName })
                        : t('projects.followsCustomerNoneHint')}
                    </Typography>
                  </Grid>
                </>
              ) : (
                <Grid size={{ xs: 12, sm: 6 }}>
                  <Controller
                    name="customerId"
                    control={control}
                    render={({ field }) => (
                      <FormControl fullWidth>
                        <InputLabel id="project-customer-label">{t('projects.customer')}</InputLabel>
                        <Select {...field} labelId="project-customer-label" label={t('projects.customer')}>
                          <MenuItem value="">
                            <em>{t('common.none')}</em>
                          </MenuItem>
                          {(customers?.items ?? []).map((customer) => (
                            <MenuItem key={customer.id} value={customer.id}>
                              {customer.name}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    )}
                  />
                </Grid>
              )}
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="status"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="project-status-label">{t('projects.status')}</InputLabel>
                      <Select {...field} labelId="project-status-label" label={t('projects.status')}>
                        {projectStatuses.map((value) => (
                          <MenuItem key={value} value={value}>
                            {enumLabel('projectStatus', value)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
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
                      label={t('projects.address')}
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Controller
                  name="countryCode"
                  control={control}
                  render={({ field, fieldState }) => (
                    <Autocomplete
                      freeSolo
                      fullWidth
                      disabled={kind === 'Sub'}
                      options={COUNTRIES.map((c) => c.label)}
                      inputValue={field.value ?? ''}
                      onInputChange={(_event, value) => field.onChange(value)}
                      renderInput={(params) => (
                        <TextField
                          {...params}
                          label={t('projects.country')}
                          helperText={
                            kind === 'Sub'
                              ? t('projects.countryInheritedHint')
                              : fieldState.error?.message ?? t('projects.countryHint')
                          }
                          error={!!fieldState.error}
                        />
                      )}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Divider sx={{ my: 0.5 }} />
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
                  {t('projects.checkInSection')}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  {t('projects.checkInSectionHint')}
                </Typography>
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="latitude"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('projects.latitude')}
                      fullWidth
                      placeholder="45.8150"
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="longitude"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('projects.longitude')}
                      fullWidth
                      placeholder="15.9819"
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="shiftStartTime"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('projects.shiftStartTime')}
                      type="time"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={12}>
                <Divider sx={{ my: 0.5 }} />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="startDate"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('projects.startDate')}
                      type="date"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="endDate"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('projects.endDate')}
                      type="date"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="contractValue"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      type="number"
                      label={t('projects.contractValue')}
                      fullWidth
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
                {isEdit ? t('common.save') : 'Create project'}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
