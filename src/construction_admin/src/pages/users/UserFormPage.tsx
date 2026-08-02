import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
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
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { Role } from '../../api/types';
import { roles } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useAuth } from '../../auth/useAuth';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useCreateUser,
  useSetUserPassword,
  useUpdateUser,
  useUserQuery,
} from '../../features/users/useUsers';
import {
  createUserSchema,
  editUserSchema,
  setPasswordSchema,
  type CreateUserFormValues,
  type SetPasswordFormValues,
} from '../../features/users/validation';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

/** Roles the signed-in operator may hand out — mirrors the API's rule. */
const RANK: Record<Role, number> = {
  SuperAdmin: 1,
  Admin: 2,
  ProjectManager: 3,
  Foreman: 4,
  Worker: 5,
};

function assignableRoles(callerRole: Role | undefined): Role[] {
  if (!callerRole) {
    return [];
  }

  return roles.filter(
    (role) => callerRole === 'SuperAdmin' || RANK[callerRole] < RANK[role],
  );
}

export function UserFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { user: currentUser } = useAuth();

  const { data: existing, isLoading, isError, error, refetch } = useUserQuery(id);
  const { data: employees } = useAllEmployeesQuery();
  const createUser = useCreateUser();
  const updateUser = useUpdateUser(id ?? '');

  const allowedRoles = assignableRoles(currentUser?.role as Role | undefined);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<CreateUserFormValues>({
    // The edit form has no password field, so it validates against a schema
    // without one rather than requiring a value the screen never collects.
    resolver: zodResolver(
      (isEdit ? editUserSchema : createUserSchema) as typeof createUserSchema,
    ),
    defaultValues: {
      email: '',
      password: '',
      role: allowedRoles[0] ?? 'Worker',
      employeeId: '',
    },
  });

  useEffect(() => {
    if (existing) {
      reset({
        email: existing.email,
        password: '',
        role: existing.role,
        employeeId: existing.employeeId ?? '',
      });
    }
  }, [existing, reset]);

  const onSubmit = handleSubmit(async (values) => {
    const shared = {
      email: values.email.trim(),
      role: values.role,
      employeeId: values.employeeId ? values.employeeId : null,
    };

    try {
      if (isEdit) {
        await updateUser.mutateAsync(shared);
      } else {
        await createUser.mutateAsync({ ...shared, password: values.password });
      }

      navigate(paths.users);
    } catch (caught) {
      const apiError = toApiError(caught);

      // A duplicate email or an already-linked employee comes back as a
      // conflict; showing it on the field beats a banner the user has to
      // connect to a control themselves.
      if (apiError.status === 409 && apiError.message.toLowerCase().includes('account for')) {
        setError('email', { message: apiError.message });
        return;
      }

      setError('root', { message: apiError.message });
    }
  });

  if (isEdit && isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  return (
    <Box>
      <PageHeader title={isEdit ? 'Edit account' : 'New account'} />

      <Paper sx={{ p: 3, maxWidth: 720 }}>
        <form onSubmit={onSubmit} noValidate>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12 }}>
              <Controller
                name="email"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Email"
                    type="email"
                    fullWidth
                    required
                    disabled={isLoading && isEdit}
                    error={!!errors.email}
                    helperText={errors.email?.message ?? 'Used to sign in.'}
                  />
                )}
              />
            </Grid>

            {!isEdit && (
              <Grid size={{ xs: 12 }}>
                <Controller
                  name="password"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Initial password"
                      type="password"
                      fullWidth
                      required
                      error={!!errors.password}
                      helperText={
                        errors.password?.message ??
                        'At least 8 characters with upper case, lower case and a digit. Ask the holder to change it after the first sign-in.'
                      }
                    />
                  )}
                />
              </Grid>
            )}

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="role"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth error={!!errors.role}>
                    <InputLabel id="user-role-label">Role</InputLabel>
                    <Select {...field} labelId="user-role-label" label="Role">
                      {allowedRoles.map((role) => (
                        <MenuItem key={role} value={role}>
                          {humanizeEnum(role)}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="employeeId"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth>
                    <InputLabel id="user-employee-label">Employee</InputLabel>
                    <Select {...field} labelId="user-employee-label" label="Employee">
                      <MenuItem value="">
                        <em>Not linked</em>
                      </MenuItem>
                      {employees?.items.map((employee) => (
                        <MenuItem key={employee.id} value={employee.id}>
                          {employee.fullName} ({employee.employeeNumber})
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <Typography variant="caption" color="text.secondary">
                Link an account to an employee for anyone who uses the mobile app —
                GPS reporting identifies the person from that link, and an unlinked
                account is refused by those screens.
              </Typography>
            </Grid>

            {errors.root && (
              <Grid size={{ xs: 12 }}>
                <Alert severity="error">{errors.root.message}</Alert>
              </Grid>
            )}

            <Grid size={{ xs: 12 }}>
              <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                <Button onClick={() => navigate(paths.users)}>Cancel</Button>
                <Button type="submit" variant="contained" disabled={isSubmitting}>
                  {isEdit ? 'Save changes' : 'Create account'}
                </Button>
              </Stack>
            </Grid>
          </Grid>
        </form>
      </Paper>

      {isEdit && id && <SetPasswordSection userId={id} />}
    </Box>
  );
}

/**
 * Setting someone else's password. Separate from the main form because it is a
 * different action with different consequences — it ends their open sessions —
 * and should not ride along with an email correction.
 */
function SetPasswordSection({ userId }: { userId: string }) {
  const setPassword = useSetUserPassword(userId);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<SetPasswordFormValues>({
    resolver: zodResolver(setPasswordSchema),
    defaultValues: { newPassword: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await setPassword.mutateAsync(values.newPassword);
      reset({ newPassword: '' });
    } catch (caught) {
      setError('newPassword', { message: toApiError(caught).message });
    }
  });

  return (
    <Paper sx={{ p: 3, mt: 3, maxWidth: 720 }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
        Set password
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
        For someone with no mailbox to receive a reset link. This signs them out
        everywhere and clears any lockout.
      </Typography>

      <Divider sx={{ my: 2 }} />

      <form onSubmit={onSubmit} noValidate>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ alignItems: 'flex-start' }}
        >
          <Controller
            name="newPassword"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="New password"
                type="password"
                fullWidth
                error={!!errors.newPassword}
                helperText={errors.newPassword?.message}
              />
            )}
          />
          <Button type="submit" variant="outlined" disabled={isSubmitting} sx={{ mt: 1 }}>
            Set password
          </Button>
        </Stack>
      </form>

      {setPassword.isSuccess && (
        <Alert severity="success" sx={{ mt: 2 }}>
          Password updated. The account has been signed out everywhere.
        </Alert>
      )}
    </Paper>
  );
}
