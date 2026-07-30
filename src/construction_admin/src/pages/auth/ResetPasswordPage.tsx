import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Link as MuiLink, Stack, TextField } from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link, useSearchParams } from 'react-router-dom';

import { ApiError, toApiError } from '../../api/apiError';
import { authApi } from '../../api/auth';
import {
  resetPasswordSchema,
  type ResetPasswordFormValues,
} from '../../auth/validation';
import { AuthCard } from '../../components/AuthCard';
import { PasswordField } from '../../components/PasswordField';
import { paths } from '../../routes/paths';

/**
 * Landing page for the link emailed by /api/auth/forgot-password. The email
 * and token arrive as query parameters, matching what the API's
 * ForgotPasswordCommandHandler puts in the link.
 */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const [done, setDone] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: searchParams.get('email') ?? '',
      token: searchParams.get('token') ?? '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (values: ResetPasswordFormValues) => {
    setError(null);

    try {
      await authApi.resetPassword(values.email, values.token, values.newPassword);
      setDone(true);
    } catch (err) {
      setError(toApiError(err));
    }
  };

  if (done) {
    return (
      <AuthCard title="Password updated">
        <Stack spacing={2.5}>
          <Alert severity="success">
            Your password has been changed. You can now sign in with the new
            password.
          </Alert>
          <Button component={Link} to={paths.login} variant="contained" size="large">
            Go to sign in
          </Button>
        </Stack>
      </AuthCard>
    );
  }

  return (
    <AuthCard title="Choose a new password" subtitle="At least 8 characters with an upper-case letter, a lower-case letter and a digit.">
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Stack spacing={2.5}>
          {error && <Alert severity="error">{error.message}</Alert>}

          <Controller
            name="email"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                label="Email"
                type="email"
                autoComplete="username"
                fullWidth
                error={!!fieldState.error || !!error?.errorFor('email')}
                helperText={fieldState.error?.message ?? error?.errorFor('email')}
              />
            )}
          />

          <Controller
            name="token"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                label="Reset code"
                fullWidth
                error={!!fieldState.error || !!error?.errorFor('token')}
                helperText={
                  fieldState.error?.message ??
                  error?.errorFor('token') ??
                  'Copied from the link in your email.'
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
                label="New password"
                autoComplete="new-password"
                fullWidth
                error={!!fieldState.error || !!error?.errorFor('newPassword')}
                helperText={fieldState.error?.message ?? error?.errorFor('newPassword')}
              />
            )}
          />

          <Controller
            name="confirmPassword"
            control={control}
            render={({ field, fieldState }) => (
              <PasswordField
                {...field}
                label="Confirm new password"
                autoComplete="new-password"
                fullWidth
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />

          <Button type="submit" variant="contained" size="large" loading={isSubmitting}>
            Set new password
          </Button>

          <MuiLink
            component={Link}
            to={paths.login}
            variant="body2"
            sx={{ textAlign: 'center' }}
          >
            Back to sign in
          </MuiLink>
        </Stack>
      </form>
    </AuthCard>
  );
}
