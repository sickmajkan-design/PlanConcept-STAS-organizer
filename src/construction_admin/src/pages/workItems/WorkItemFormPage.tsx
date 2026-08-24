import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControl,
  FormControlLabel,
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
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  workItemKinds,
  workItemPriorities,
  type WorkItemInput,
} from '../../api/types';
import { AttachmentList } from '../../components/AttachmentList';
import { ErrorState } from '../../components/ErrorState';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import {
  useCreateWorkItem,
  useUpdateWorkItem,
  useWorkItemQuery,
} from '../../features/workItems/useWorkItems';
import {
  workItemFormSchema,
  type WorkItemFormValues,
} from '../../features/workItems/validation';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const emptyValues: WorkItemFormValues = {
  kind: 'Task',
  title: '',
  description: '',
  projectId: '',
  assignedEmployeeId: '',
  priority: 'Normal',
  dueDate: '',
  requiresAcknowledgment: false,
};

export function WorkItemFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();

  const { data: existing, isLoading, isError, error, refetch } = useWorkItemQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();
  const { data: allProjects } = useAllProjectsQuery();
  const createItem = useCreateWorkItem();
  const updateItem = useUpdateWorkItem(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<WorkItemFormValues>({
    resolver: zodResolver(workItemFormSchema),
    defaultValues: emptyValues,
  });

  // A defect must name a site, so the picker says so while it is empty
  // instead of waiting for a submit to explain it.
  const kind = useWatch({ control, name: 'kind' });

  useEffect(() => {
    if (existing) {
      reset({
        kind: existing.kind,
        title: existing.title,
        description: existing.description ?? '',
        projectId: existing.projectId ?? '',
        assignedEmployeeId: existing.assignedEmployeeId ?? '',
        priority: existing.priority,
        dueDate: existing.dueDate ?? '',
        requiresAcknowledgment: existing.requiresAcknowledgment,
      });
    }
  }, [existing, reset]);

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = async (values: WorkItemFormValues) => {
    const input: WorkItemInput = {
      kind: values.kind,
      title: values.title.trim(),
      description: values.description || null,
      projectId: values.projectId || null,
      assignedEmployeeId: values.assignedEmployeeId || null,
      priority: values.priority,
      dueDate: values.dueDate || null,
      requiresAcknowledgment: values.requiresAcknowledgment,
    };

    try {
      if (isEdit) {
        await updateItem.mutateAsync(input);
      } else {
        await createItem.mutateAsync(input);
      }

      navigate(paths.workItems);
    } catch (err) {
      const apiError = toApiError(err);

      for (const field of Object.keys(apiError.fieldErrors)) {
        const key = field.charAt(0).toLowerCase() + field.slice(1);
        if (key in emptyValues) {
          setError(key as keyof WorkItemFormValues, {
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
        {isEdit ? t('workItems.editTitle') : t('workItems.newTitle')}
      </Typography>

      <Paper sx={{ p: 3, mt: 2 }}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="kind"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="wi-kind-label">{t('workItems.kind')}</InputLabel>
                      <Select {...field} labelId="wi-kind-label" label={t('workItems.kind')}>
                        {workItemKinds.map((value) => (
                          <MenuItem key={value} value={value}>
                            {enumLabel('workItemKind', value)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="priority"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="wi-priority-label">
                        {t('workItems.priority')}
                      </InputLabel>
                      <Select
                        {...field}
                        labelId="wi-priority-label"
                        label={t('workItems.priority')}
                      >
                        {workItemPriorities.map((value) => (
                          <MenuItem key={value} value={value}>
                            {enumLabel('workItemPriority', value)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid size={12}>
                <Controller
                  name="title"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('workItems.itemTitle')}
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
                      label={t('workItems.description')}
                      fullWidth
                      multiline
                      minRows={3}
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
                  render={({ field, fieldState }) => (
                    <FormControl fullWidth error={!!fieldState.error}>
                      <InputLabel id="wi-project-label">
                        {t('workItems.project')}
                      </InputLabel>
                      <Select
                        {...field}
                        labelId="wi-project-label"
                        label={t('workItems.project')}
                      >
                        <MenuItem value="">{t('workItems.noProject')}</MenuItem>
                        {(allProjects?.items ?? []).map((project) => (
                          <MenuItem key={project.id} value={project.id}>
                            {project.name}
                          </MenuItem>
                        ))}
                      </Select>
                      {(fieldState.error || kind === 'Defect') && (
                        <Typography
                          variant="caption"
                          color={fieldState.error ? 'error' : 'text.secondary'}
                          sx={{ mt: 0.5, ml: 1.75 }}
                        >
                          {fieldState.error?.message ??
                            t('workItems.defectNeedsProject')}
                        </Typography>
                      )}
                    </FormControl>
                  )}
                />
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name="assignedEmployeeId"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth>
                      <InputLabel id="wi-assignee-label">
                        {t('workItems.assignee')}
                      </InputLabel>
                      <Select
                        {...field}
                        labelId="wi-assignee-label"
                        label={t('workItems.assignee')}
                      >
                        <MenuItem value="">{t('workItems.unassigned')}</MenuItem>
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
                  name="dueDate"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('workItems.dueDate')}
                      type="date"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message ?? t('workItems.noDueDate')}
                    />
                  )}
                />
              </Grid>

              <Grid size={12}>
                <Controller
                  name="requiresAcknowledgment"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Checkbox {...field} checked={field.value} />}
                      label={t('workItems.requiresAcknowledgment')}
                    />
                  )}
                />
              </Grid>
            </Grid>

            <Stack direction="row" spacing={1.5} sx={{ pt: 1 }}>
              <Button type="submit" variant="contained" disabled={isSubmitting}>
                {isEdit ? t('common.save') : t('workItems.create')}
              </Button>
              <Button onClick={() => navigate(paths.workItems)}>
                {t('common.cancel')}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>

      {/* Only once it exists: an attachment needs something to hang off. */}
      {isEdit && (
        <Card sx={{ mt: 3 }}>
          <CardContent>
            <AttachmentList
              ownerType="WorkItem"
              ownerId={id!}
              categories={['Photo', 'SiteDocument', 'Other']}
              canDelete
            />
          </CardContent>
        </Card>
      )}
    </Box>
  );
}
