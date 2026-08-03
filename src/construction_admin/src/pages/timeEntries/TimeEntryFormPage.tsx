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
import { workTypes, type TimeEntryInput } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import {
  useCreateTimeEntry,
  useTimeEntryQuery,
  useUpdateTimeEntry,
} from '../../features/timeEntries/useTimeEntries';
import {
  timeEntryFormSchema,
  type TimeEntryFormValues,
} from '../../features/timeEntries/validation';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: TimeEntryFormValues = {
  employeeId: '',
  projectId: '',
  startedAt: '',
  endedAt: '',
  breakMinutes: '0',
  workType: 'Regular',
  note: '',
};

/**
 * `datetime-local` speaks local wall-clock time with no zone, so both
 * directions convert explicitly. Doing it implicitly is how an entry ends up
 * an hour out twice a year.
 */
function toLocalInput(iso: string | null | undefined): string {
  if (!iso) return '';

  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';

  const pad = (value: number) => String(value).padStart(2, '0');

  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}`
  );
}

function toIso(localValue: string): string {
  return new Date(localValue).toISOString();
}

export function TimeEntryFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();

  const { data: existing, isLoading, isError, error, refetch } = useTimeEntryQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();
  const { data: allProjects } = useAllProjectsQuery();
  const createEntry = useCreateTimeEntry();
  const updateEntry = useUpdateTimeEntry(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<TimeEntryFormValues>({
    resolver: zodResolver(timeEntryFormSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({
        employeeId: existing.employeeId,
        projectId: existing.projectId ?? '',
        startedAt: toLocalInput(existing.startedAt),
        endedAt: toLocalInput(existing.endedAt),
        breakMinutes: String(existing.breakMinutes),
        workType: existing.workType,
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

  const onSubmit = async (values: TimeEntryFormValues) => {
    const input: TimeEntryInput = {
      employeeId: values.employeeId,
      projectId: values.projectId || null,
      startedAt: toIso(values.startedAt),
      endedAt: values.endedAt ? toIso(values.endedAt) : null,
      breakMinutes: Number(values.breakMinutes || 0),
      workType: values.workType,
      note: values.note || null,
    };

    try {
      if (isEdit) {
        await updateEntry.mutateAsync(input);
      } else {
        await createEntry.mutateAsync(input);
      }

      navigate(paths.timeEntries);
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof TimeEntryFormValues, {
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
        {isEdit ? t('timeEntries.editTitle') : t('timeEntries.newTitle')}
      </Typography>

      <Paper sx={{ p: 3, mt: 2 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="employeeId"
                  control={control}
                  render={({ field, fieldState }) => (
                    <FormControl fullWidth error={!!fieldState.error}>
                      <InputLabel id="entry-employee-label">
                        {t('timeEntries.employee')}
                      </InputLabel>
                      <Select
                        {...field}
                        labelId="entry-employee-label"
                        label={t('timeEntries.employee')}
                      >
                        {(allEmployees?.items ?? []).map((employee) => (
                          <MenuItem key={employee.id} value={employee.id}>
                            {employee.firstName} {employee.lastName}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="projectId"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="entry-project-label">
                        {t('timeEntries.project')}
                      </InputLabel>
                      <Select
                        {...field}
                        labelId="entry-project-label"
                        label={t('timeEntries.project')}
                      >
                        <MenuItem value="">{t('timeEntries.noProject')}</MenuItem>
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

              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="startedAt"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('timeEntries.startedAt')}
                      type="datetime-local"
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
                  name="endedAt"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('timeEntries.endedAt')}
                      type="datetime-local"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                      error={!!fieldState.error}
                      // Left blank deliberately records a shift that is still
                      // running, which is how a forgotten clock-in is opened.
                      helperText={fieldState.error?.message ?? t('timeEntries.running')}
                    />
                  )}
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="breakMinutes"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('timeEntries.breakMinutes')}
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
                  name="workType"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="entry-worktype-label">
                        {t('timeEntries.workType')}
                      </InputLabel>
                      <Select
                        {...field}
                        labelId="entry-worktype-label"
                        label={t('timeEntries.workType')}
                      >
                        {workTypes.map((type) => (
                          <MenuItem key={type} value={type}>
                            {enumLabel('workType', type)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
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
                      label={t('timeEntries.note')}
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

            <Stack direction="row" spacing={1.5} sx={{ pt: 1 }}>
              <Button type="submit" variant="contained" disabled={isSubmitting}>
                {isEdit ? t('common.save') : t('timeEntries.create')}
              </Button>
              <Button onClick={() => navigate(paths.timeEntries)}>
                {t('common.cancel')}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
