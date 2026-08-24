import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  OutlinedInput,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { z } from 'zod';

import { toApiError } from '../../api/apiError';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useCreateNotificationGroup,
  useNotificationGroupQuery,
  useUpdateNotificationGroup,
} from '../../features/notificationGroups/useNotificationGroups';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const formSchema = z.object({
  name: z.string().trim().min(1, 'A name is required.').max(128),
  employeeIds: z.array(z.string()),
});

type FormValues = z.infer<typeof formSchema>;

const emptyValues: FormValues = { name: '', employeeIds: [] };

export function NotificationGroupFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const t = useT();

  const { data: existing, isLoading, isError, error, refetch } = useNotificationGroupQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();
  const createGroup = useCreateNotificationGroup();
  const updateGroup = useUpdateNotificationGroup(id ?? '');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (existing) {
      reset({ name: existing.name, employeeIds: existing.memberEmployeeIds });
    }
  }, [existing, reset]);

  if (isEdit && isLoading) {
    return null;
  }

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const onSubmit = handleSubmit(async (values) => {
    const input = { name: values.name.trim(), employeeIds: values.employeeIds };

    try {
      if (isEdit) {
        await updateGroup.mutateAsync(input);
      } else {
        await createGroup.mutateAsync(input);
      }

      navigate(paths.notificationGroups);
    } catch (caught) {
      setError('root', { message: toApiError(caught).message });
    }
  });

  const rootError = errors.root as { message?: string } | undefined;
  const employeesById = new Map((allEmployees?.items ?? []).map((e) => [e.id, e]));

  return (
    <Box sx={{ maxWidth: 640 }}>
      <PageHeader
        title={isEdit ? t('notificationGroups.editTitle') : t('notificationGroups.newTitle')}
      />

      <Paper sx={{ p: 3 }}>
        <form onSubmit={onSubmit} noValidate>
          <Stack spacing={2.5}>
            {rootError?.message && <Alert severity="error">{rootError.message}</Alert>}

            <Controller
              name="name"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label={t('notificationGroups.name')}
                  fullWidth
                  autoFocus
                  error={!!errors.name}
                  helperText={errors.name?.message}
                />
              )}
            />

            <Controller
              name="employeeIds"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth>
                  <InputLabel id="ng-members-label">
                    {t('notificationGroups.members')}
                  </InputLabel>
                  <Select
                    {...field}
                    multiple
                    labelId="ng-members-label"
                    input={<OutlinedInput label={t('notificationGroups.members')} />}
                    renderValue={(selected) => (
                      <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                        {(selected as string[]).map((employeeId) => (
                          <Chip
                            key={employeeId}
                            size="small"
                            label={employeesById.get(employeeId)?.fullName ?? employeeId}
                          />
                        ))}
                      </Stack>
                    )}
                  >
                    {(allEmployees?.items ?? []).map((employee) => (
                      <MenuItem key={employee.id} value={employee.id}>
                        <Checkbox checked={field.value.includes(employee.id)} />
                        <ListItemText
                          primary={employee.fullName}
                          secondary={employee.employeeNumber}
                        />
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            />

            <Typography variant="caption" color="text.secondary">
              {t('notificationGroups.membersHint')}
            </Typography>

            <Stack direction="row" spacing={1.5} sx={{ pt: 1 }}>
              <Button type="submit" variant="contained" disabled={isSubmitting}>
                {isEdit ? t('common.save') : t('notificationGroups.create')}
              </Button>
              <Button onClick={() => navigate(paths.notificationGroups)}>
                {t('common.cancel')}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
}
