import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Button, Paper, Stack, Typography } from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { ApiError, toApiError } from '../../api/apiError';
import { authApi } from '../../api/auth';
import { useAuth } from '../../auth/useAuth';
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
} from '../../auth/validation';
import { PasswordField } from '../../components/PasswordField';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

export function ChangePasswordPage() {
  const { signOut } = useAuth();
  const navigate = useNavigate();
  const t = useT();
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const onSubmit = async (values: ChangePasswordFormValues) => {
    setError(null);

    try {
      // The API revokes every session as part of this, so the operator must
      // sign in again with the new password.
      await authApi.changePassword(values.currentPassword, values.newPassword);
      setDone(true);
    } catch (err) {
      setError(toApiError(err));
    }
  };

  const finishAndSignOut = async () => {
    await signOut();
    navigate(paths.login, { replace: true });
  };

  return (
    <Box sx={{ maxWidth: 480, mx: 'auto', py: 4 }}>
      <Paper sx={{ p: 4 }}>
        <Stack spacing={3}>
          <Box>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              Change password
            </Typography>
            <Typography variant="body2" color="text.secondary">
              At least 8 characters with an upper-case letter, a lower-case letter
              and a digit.
            </Typography>
          </Box>

          {done ? (
            <>
              <Alert severity="success">
                Your password has been updated. For security, all your signed-in
                sessions were signed out. Please sign in again with the new
                password.
              </Alert>
              <Button variant="contained" size="large" onClick={finishAndSignOut}>
                Sign in again
              </Button>
            </>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
              <Stack spacing={2.5}>
                {error && <Alert severity="error">{error.message}</Alert>}

                <Controller
                  name="currentPassword"
                  control={control}
                  render={({ field, fieldState }) => (
                    <PasswordField
                      {...field}
                      label={t('auth.currentPassword')}
                      fullWidth
                      error={!!fieldState.error || !!error?.errorFor('currentPassword')}
                      helperText={
                        fieldState.error?.message ?? error?.errorFor('currentPassword')
                      }
                    />
                  )}
                />

                <Controller
                  name="newPassword"
                  control={control}
                  render={({ field, fieldState }) => (
                    <PasswordField
                      {...field}
                      label={t('auth.newPassword')}
                      autoComplete="new-password"
                      fullWidth
                      error={!!fieldState.error || !!error?.errorFor('newPassword')}
                      helperText={
                        fieldState.error?.message ?? error?.errorFor('newPassword')
                      }
                    />
                  )}
                />

                <Controller
                  name="confirmPassword"
                  control={control}
                  render={({ field, fieldState }) => (
                    <PasswordField
                      {...field}
                      label={t('auth.confirmPassword')}
                      autoComplete="new-password"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />

                <Button type="submit" variant="contained" size="large" loading={isSubmitting}>
                  Change password
                </Button>
              </Stack>
            </form>
          )}
        </Stack>
      </Paper>
    </Box>
  );
}
