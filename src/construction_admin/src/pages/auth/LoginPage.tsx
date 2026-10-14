import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Link as MuiLink, Stack, TextField } from '@mui/material';
import { useState, type FormEvent } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate, type Location } from 'react-router-dom';

import { ApiError } from '../../api/apiError';
import { useAuth } from '../../auth/useAuth';
import { loginSchema, type LoginFormValues } from '../../auth/validation';
import { AuthCard } from '../../components/AuthCard';
import { PasswordField } from '../../components/PasswordField';
import { config } from '../../config';
import { useCompanyBrandingQuery } from '../../features/companySettings/useCompanySettings';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

export function LoginPage() {
  const { signIn, completeMfa } = useAuth();
  const navigate = useNavigate();
  const t = useT();
  const location = useLocation() as Location & { state?: { from?: Location } };
  const [error, setError] = useState<ApiError | null>(null);
  // No auth token available yet — this is the one screen that must render
  // before there is a session, which is exactly what this query is for.
  const { data: branding } = useCompanyBrandingQuery();

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  // Set once the password was accepted but the account also wants a code. The
  // challenge lives only here: leaving the page drops it, and the password step
  // simply has to be done again.
  const [challenge, setChallenge] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [verifying, setVerifying] = useState(false);

  const finish = () => {
    const redirectTo = location.state?.from?.pathname ?? paths.home;
    navigate(redirectTo, { replace: true });
  };

  const onSubmit = async (values: LoginFormValues) => {
    setError(null);

    try {
      const outcome = await signIn(values.email, values.password);

      if (outcome.status === 'mfa') {
        setChallenge(outcome.mfaToken);
        return;
      }

      finish();
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(t('common.somethingWentWrong')));
    }
  };

  const onVerify = async (event: FormEvent) => {
    event.preventDefault();

    if (!challenge || !code.trim()) {
      return;
    }

    setError(null);
    setVerifying(true);

    try {
      await completeMfa(challenge, code);
      finish();
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(t('common.somethingWentWrong')));
      setCode('');
    } finally {
      setVerifying(false);
    }
  };

  if (challenge) {
    return (
      <AuthCard title={branding?.name || t('nav.appName')} subtitle={t('mfa.title')}>
        <form onSubmit={onVerify} noValidate>
          <Stack spacing={2.5}>
            <Alert severity="info">{t('mfa.signInHint')}</Alert>

            {error && <Alert severity="error">{error.message}</Alert>}

            <TextField
              value={code}
              onChange={(event) => setCode(event.target.value)}
              label={t('mfa.codeOrRecovery')}
              autoComplete="one-time-code"
              autoFocus
              fullWidth
              slotProps={{ htmlInput: { inputMode: 'text', maxLength: 32 } }}
            />

            <Button type="submit" variant="contained" size="large" loading={verifying}>
              {t('mfa.verify')}
            </Button>

            <MuiLink
              component="button"
              type="button"
              variant="body2"
              sx={{ textAlign: 'center' }}
              onClick={() => {
                setChallenge(null);
                setCode('');
                setError(null);
              }}
            >
              {t('mfa.backToSignIn')}
            </MuiLink>
          </Stack>
        </form>
      </AuthCard>
    );
  }

  return (
    <AuthCard title={branding?.name || t('nav.appName')} subtitle={t('auth.signInToConsole')}>
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

          {config.appDownloadUrl && (
            <MuiLink
              href={config.appDownloadUrl}
              variant="body2"
              sx={{ textAlign: 'center' }}
            >
              {t('auth.downloadApp')}
            </MuiLink>
          )}
        </Stack>
      </form>
    </AuthCard>
  );
}
