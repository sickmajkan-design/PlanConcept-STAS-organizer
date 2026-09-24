import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link, useParams } from 'react-router-dom';

import { ApiError, toApiError } from '../../api/apiError';
import { onboardingApi } from '../../api/onboarding';
import { acceptInvitationSchema, type AcceptInvitationFormValues } from '../../auth/validation';
import { AuthCard } from '../../components/AuthCard';
import { PasswordField } from '../../components/PasswordField';
import { useInvitationPreviewQuery } from '../../features/onboarding/useOnboarding';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

/**
 * Where an invitation link lands. The person chooses a password and their
 * account is created, tied to the employee record the invitation was for — no
 * one else ever sees or types that password.
 *
 * Every way the link can be unusable (used, expired, unknown) looks the same
 * here, as it does on the server, so a guessed link reveals nothing.
 */
export function AcceptInvitationPage() {
  const t = useT();
  const { token } = useParams<{ token: string }>();
  const preview = useInvitationPreviewQuery(token);
  const [createdEmail, setCreatedEmail] = useState<string | null>(null);
  const [error, setError] = useState<ApiError | null>(null);

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<AcceptInvitationFormValues>({
    resolver: zodResolver(acceptInvitationSchema),
    values: { email: preview.data?.suggestedEmail ?? '', password: '', confirmPassword: '' },
  });

  const onSubmit = async (values: AcceptInvitationFormValues) => {
    setError(null);

    try {
      const created = await onboardingApi.acceptInvitation(token!, values.email, values.password);
      setCreatedEmail(created.email);
    } catch (err) {
      setError(toApiError(err));
    }
  };

  if (createdEmail) {
    return (
      <AuthCard title={t('onboarding.accept.doneTitle')}>
        <Stack spacing={2.5}>
          <Alert severity="success">{t('onboarding.accept.done', { email: createdEmail })}</Alert>
          <Button component={Link} to={paths.login} variant="contained" size="large">
            {t('onboarding.accept.signIn')}
          </Button>
        </Stack>
      </AuthCard>
    );
  }

  if (preview.isPending) {
    return (
      <AuthCard title={t('onboarding.accept.loading')}>
        <Typography variant="body2" color="text.secondary">
          &nbsp;
        </Typography>
      </AuthCard>
    );
  }

  if (preview.isError) {
    return (
      <AuthCard title={t('onboarding.accept.invalid')}>
        <Button component={Link} to={paths.login} variant="outlined" size="large">
          {t('onboarding.accept.signIn')}
        </Button>
      </AuthCard>
    );
  }

  return (
    <AuthCard
      title={t('onboarding.accept.title', { name: preview.data.firstName })}
      subtitle={t('onboarding.accept.subtitle')}
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Stack spacing={2.5}>
          {error && <Alert severity="error">{error.message}</Alert>}

          <Controller
            name="email"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                label={t('auth.email')}
                type="email"
                autoComplete="username"
                fullWidth
                error={!!fieldState.error || !!error?.errorFor('email')}
                helperText={fieldState.error?.message ?? error?.errorFor('email')}
              />
            )}
          />

          <Controller
            name="password"
            control={control}
            render={({ field, fieldState }) => (
              <PasswordField
                {...field}
                label={t('auth.newPassword')}
                autoComplete="new-password"
                fullWidth
                error={!!fieldState.error || !!error?.errorFor('password')}
                helperText={fieldState.error?.message ?? error?.errorFor('password') ?? t('auth.passwordRule')}
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
            {t('onboarding.accept.submit')}
          </Button>
        </Stack>
      </form>
    </AuthCard>
  );
}
