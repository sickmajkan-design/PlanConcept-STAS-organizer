import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Link as MuiLink, Stack, TextField } from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';

import { authApi } from '../../api/auth';
import { toApiError } from '../../api/apiError';
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from '../../auth/validation';
import { AuthCard } from '../../components/AuthCard';
import { paths } from '../../routes/paths';

export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const onSubmit = async (values: ForgotPasswordFormValues) => {
    setErrorMessage(null);

    try {
      await authApi.forgotPassword(values.email);
      setSent(true);
    } catch (err) {
      setErrorMessage(toApiError(err).message);
    }
  };

  return (
    <AuthCard
      title="Reset password"
      subtitle="Enter the email address of your admin account. If an account exists, we will send a link to choose a new password."
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Stack spacing={2.5}>
          {sent && (
            <Alert severity="success">
              If that address belongs to an account, a reset link is on its way. The
              link is valid for one hour.
            </Alert>
          )}
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

          <Controller
            name="email"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                label="Email"
                type="email"
                autoComplete="username"
                autoFocus
                fullWidth
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />

          <Button type="submit" variant="contained" size="large" loading={isSubmitting}>
            {sent ? 'Send again' : 'Send reset link'}
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
