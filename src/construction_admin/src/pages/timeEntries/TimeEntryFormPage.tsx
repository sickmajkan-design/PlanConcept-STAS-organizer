import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  Chip,
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
import { workTypes, type TimeEntry, type TimeEntryInput } from '../../api/types';
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

/** `45.81234, 15.98123`, matching the format the project detail screen uses. */
function formatCoordinates(latitude: number | null, longitude: number | null): string | null {
  if (latitude === null || longitude === null) return null;
  return `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
}

/** Splits a `YYYY-MM-DDTHH:mm` value into its date and time halves. */
function splitLocal(value: string): { date: string; time: string } {
  const [date = '', time = ''] = value.split('T');
  return { date, time };
}

/** Rebuilds the combined `YYYY-MM-DDTHH:mm` value from its two halves. */
function combineLocal(date: string, time: string): string {
  if (!date && !time) return '';
  return `${date}T${time}`;
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
            {existing?.autoClosed && (
              <Alert severity="warning">{t('timeEntries.autoClosedHint')}</Alert>
            )}
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            {isEdit && existing && <LocationSection entry={existing} />}

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
                  render={({ field, fieldState }) => {
                    const { date, time } = splitLocal(field.value);

                    return (
                      <Stack direction="row" spacing={1}>
                        <TextField
                          label={t('timeEntries.startedAt')}
                          type="date"
                          value={date}
                          onChange={(event) =>
                            field.onChange(combineLocal(event.target.value, time))
                          }
                          fullWidth
                          slotProps={{ inputLabel: { shrink: true } }}
                          error={!!fieldState.error}
                        />
                        <TextField
                          label={t('timeEntries.time')}
                          value={time}
                          onChange={(event) =>
                            field.onChange(combineLocal(date, event.target.value))
                          }
                          placeholder="HH:mm"
                          sx={{ width: 110 }}
                          slotProps={{
                            htmlInput: {
                              inputMode: 'numeric',
                              pattern: '^([01][0-9]|2[0-3]):[0-5][0-9]$',
                              maxLength: 5,
                            },
                          }}
                          error={!!fieldState.error}
                          helperText={fieldState.error?.message}
                        />
                      </Stack>
                    );
                  }}
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="endedAt"
                  control={control}
                  render={({ field, fieldState }) => {
                    const { date, time } = splitLocal(field.value ?? '');

                    return (
                      <Stack direction="row" spacing={1}>
                        <TextField
                          label={t('timeEntries.endedAt')}
                          type="date"
                          value={date}
                          onChange={(event) =>
                            field.onChange(combineLocal(event.target.value, time))
                          }
                          fullWidth
                          slotProps={{ inputLabel: { shrink: true } }}
                          error={!!fieldState.error}
                        />
                        <TextField
                          label={t('timeEntries.time')}
                          value={time}
                          onChange={(event) =>
                            field.onChange(combineLocal(date, event.target.value))
                          }
                          placeholder="HH:mm"
                          sx={{ width: 110 }}
                          slotProps={{
                            htmlInput: {
                              inputMode: 'numeric',
                              pattern: '^([01][0-9]|2[0-3]):[0-5][0-9]$',
                              maxLength: 5,
                            },
                          }}
                          error={!!fieldState.error}
                          // Left blank deliberately records a shift that is
                          // still running, which is how a forgotten clock-in
                          // is opened.
                          helperText={fieldState.error?.message ?? t('timeEntries.running')}
                        />
                      </Stack>
                    );
                  }}
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

/**
 * The coordinates a phone reported at clock-in/out, read-only — this is
 * device-captured evidence, not something a reviewer corrects by hand. Shown
 * only when at least one side has a fix; a shift recorded from the office
 * (via this same form) never sets these.
 */
function LocationSection({ entry }: { entry: TimeEntry }) {
  const t = useT();

  const start = formatCoordinates(entry.startLatitude, entry.startLongitude);
  const end = formatCoordinates(entry.endLatitude, entry.endLongitude);

  if (!start && !end) {
    return null;
  }

  const locationLabel =
    entry.locationCorrect === true
      ? t('timeEntries.locationCorrectYes')
      : entry.locationCorrect === false
        ? t('timeEntries.locationCorrectNo')
        : t('timeEntries.locationCorrectUnknown');

  const timeLabel =
    entry.timeCorrect === true
      ? t('timeEntries.timeCorrectYes')
      : entry.timeCorrect === false
        ? t('timeEntries.timeCorrectNo')
        : t('timeEntries.timeCorrectUnknown');

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 700 }}>
        {t('timeEntries.locationSectionTitle')}
      </Typography>
      <Stack spacing={1.5}>
        {start && (
          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('timeEntries.startLocation')}
            </Typography>
            <Typography variant="body2">{start}</Typography>
          </Box>
        )}
        {end && (
          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('timeEntries.endLocation')}
            </Typography>
            <Typography variant="body2">{end}</Typography>
          </Box>
        )}
        <Stack direction="row" spacing={1}>
          <Chip
            size="small"
            label={locationLabel}
            color={entry.locationCorrect === false ? 'warning' : 'default'}
            variant="outlined"
          />
          <Chip
            size="small"
            label={timeLabel}
            color={entry.timeCorrect === false ? 'warning' : 'default'}
            variant="outlined"
          />
        </Stack>
      </Stack>
    </Paper>
  );
}
