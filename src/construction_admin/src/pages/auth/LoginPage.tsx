import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Link as MuiLink, Stack, TextField } from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate, type Location } from 'react-router-dom';

import { ApiError } from '../../api/apiError';
import { useAuth } from '../../auth/useAuth';
import { loginSchema, type LoginFormValues } from '../../auth/validation';
import { AuthCard } from '../../components/AuthCard';
import { PasswordField } from '../../components/PasswordField';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const t = useT();
  const location = useLocation() as Location & { state?: { from?: Location } };
  const [error, setError] = useState<ApiError | null>(null);

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  const onSubmit = async (values: LoginFormValues) => {
    setError(null);

    try {
      await signIn(values.email, values.password);
      const redirectTo = location.state?.from?.pathname ?? paths.home;
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(t('common.somethingWentWrong')));
    }
  };

  return (
    <AuthCard title={t('nav.appName')} subtitle={t('auth.signInToConsole')}>
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
                autoFocus
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
                label={t('auth.password')}
                fullWidth
                error={!!fieldState.error || !!error?.errorFor('password')}
                helperText={fieldState.error?.message ?? error?.errorFor('password')}
              />
            )}
          />

          <Button type="submit" variant="contained" size="large" loading={isSubmitting}>
            {t('auth.signIn')}
          </Button>

          <MuiLink
            component={Link}
            to={paths.forgotPassword}
            variant="body2"
            sx={{ textAlign: 'center' }}
          >
            {t('auth.forgotPasswordShort')}
          </MuiLink>
        </Stack>
      </form>
    </AuthCard>
  );
}
