import { Alert, Box, Button, Chip, Paper, Stack, TextField, Typography } from '@mui/material';
import { QRCodeSVG } from 'qrcode.react';
import { useState } from 'react';

import { type ApiError, toApiError } from '../../api/apiError';
import { authApi } from '../../api/auth';
import type { MfaSetup } from '../../api/types';
import { useAuth } from '../../auth/useAuth';
import { useMfaStatusQuery } from '../../features/mfa/useMfaStatus';
import { PasswordField } from '../../components/PasswordField';
import { useT } from '../../i18n/useI18n';

type Step =
  | { name: 'overview' }
  | { name: 'setup'; setup: MfaSetup }
  | { name: 'recovery'; codes: string[] }
  | { name: 'disable' };

/**
 * Where an account turns its authenticator on or off.
 *
 * The recovery codes are shown exactly once, on the screen that follows
 * confirming the first code — the API stores only their hashes and cannot show
 * them again, so leaving without saving them means asking a Super Admin to
 * reset the account.
 */
export function TwoStepPage() {
  const t = useT();
  const { user, refreshProfile } = useAuth();
  const [step, setStep] = useState<Step>({ name: 'overview' });
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);
  const [code, setCode] = useState('');
  const [password, setPassword] = useState('');
  const [copied, setCopied] = useState(false);

  const enabled = !!user?.mfaEnabled;
  const { data: status } = useMfaStatusQuery();
  const unavailable = status !== undefined && !status.available && !enabled;

  const run = async (action: () => Promise<void>) => {
    setError(null);
    setBusy(true);

    try {
      await action();
    } catch (err) {
      setError(toApiError(err));
    } finally {
      setBusy(false);
    }
  };

  const begin = () =>
    run(async () => {
      setCode('');
      setStep({ name: 'setup', setup: await authApi.mfaSetup() });
    });

  const confirm = () =>
    run(async () => {
      const result = await authApi.mfaEnable(code);
      setCode('');
      await refreshProfile();
      setStep({ name: 'recovery', codes: result.recoveryCodes });
    });

  const switchOff = () =>
    run(async () => {
      await authApi.mfaDisable(password, code);
      setCode('');
      setPassword('');
      await refreshProfile();
      setStep({ name: 'overview' });
    });

  const copyAll = async (codes: string[]) => {
    try {
      await navigator.clipboard.writeText(codes.join('\n'));
      setCopied(true);
    } catch {
      // Clipboard access can be refused; the codes are on screen to copy by hand.
    }
  };

  return (
    <Box sx={{ maxWidth: 520, mx: 'auto', py: 4 }}>
      <Paper sx={{ p: 4 }}>
        <Stack spacing={3}>
          <Box>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {t('mfa.title')}
              </Typography>
              <Chip
                size="small"
                color={enabled ? 'success' : 'default'}
                label={enabled ? t('mfa.statusOn') : t('mfa.statusOff')}
              />
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {t('mfa.intro')}
            </Typography>
          </Box>

          {error && <Alert severity="error">{error.message}</Alert>}

          {unavailable && <Alert severity="info">{t('mfa.notAvailable')}</Alert>}

          {step.name === 'overview' && !unavailable && (
            <Box>
              {enabled ? (
                <Button
                  color="error"
                  variant="outlined"
                  onClick={() => {
                    setError(null);
                    setCode('');
                    setStep({ name: 'disable' });
                  }}
                >
                  {t('mfa.disable')}
                </Button>
              ) : (
                <Button variant="contained" onClick={begin} loading={busy}>
                  {t('mfa.setUp')}
                </Button>
              )}
            </Box>
          )}

          {step.name === 'setup' && (
            <Stack spacing={2.5}>
              <Typography variant="body2">{t('mfa.setupStep1')}</Typography>

              <Box sx={{ display: 'flex', justifyContent: 'center', p: 2, bgcolor: '#fff', borderRadius: 1 }}>
                <QRCodeSVG value={step.setup.provisioningUri} size={184} marginSize={0} />
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  {t('mfa.setupKey')}
                </Typography>
                <Typography sx={{ fontFamily: 'monospace', letterSpacing: 1, wordBreak: 'break-all' }}>
                  {step.setup.secret.replace(/(.{4})/g, '$1 ').trim()}
                </Typography>
              </Box>

              <Typography variant="body2">{t('mfa.setupStep2')}</Typography>

              <TextField
                value={code}
                onChange={(event) => setCode(event.target.value)}
                label={t('mfa.codeLabel')}
                autoComplete="one-time-code"
                slotProps={{ htmlInput: { inputMode: 'numeric', maxLength: 7 } }}
                fullWidth
              />

              <Stack direction="row" spacing={1.5}>
                <Button variant="contained" onClick={confirm} loading={busy} disabled={!code.trim()}>
                  {t('mfa.enable')}
                </Button>
                <Button
                  onClick={() => {
                    setError(null);
                    setCode('');
                    setStep({ name: 'overview' });
                  }}
                >
                  {t('mfa.cancel')}
                </Button>
              </Stack>
            </Stack>
          )}

          {step.name === 'recovery' && (
            <Stack spacing={2.5}>
              <Box>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {t('mfa.recoveryTitle')}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('mfa.recoveryHint')}
                </Typography>
              </Box>

              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(2, 1fr)',
                  gap: 1,
                  p: 2,
                  borderRadius: 1,
                  border: '1px solid',
                  borderColor: 'divider',
                  fontFamily: 'monospace',
                }}
              >
                {step.codes.map((recoveryCode) => (
                  <Typography key={recoveryCode} sx={{ fontFamily: 'monospace' }}>
                    {recoveryCode}
                  </Typography>
                ))}
              </Box>

              <Stack direction="row" spacing={1.5}>
                <Button variant="outlined" onClick={() => void copyAll(step.codes)}>
                  {copied ? t('mfa.recoveryCopied') : t('mfa.recoveryCopy')}
                </Button>
                <Button
                  variant="contained"
                  onClick={() => {
                    setCopied(false);
                    setStep({ name: 'overview' });
                  }}
                >
                  {t('mfa.recoverySaved')}
                </Button>
              </Stack>
            </Stack>
          )}

          {step.name === 'disable' && (
            <Stack spacing={2.5}>
              <Box>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {t('mfa.disableTitle')}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('mfa.disableHint')}
                </Typography>
              </Box>

              <PasswordField
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                label={t('auth.password')}
                autoComplete="current-password"
                fullWidth
              />

              <TextField
                value={code}
                onChange={(event) => setCode(event.target.value)}
                label={t('mfa.codeLabel')}
                autoComplete="one-time-code"
                slotProps={{ htmlInput: { inputMode: 'numeric', maxLength: 7 } }}
                fullWidth
              />

              <Stack direction="row" spacing={1.5}>
                <Button
                  color="error"
                  variant="contained"
                  onClick={switchOff}
                  loading={busy}
                  disabled={!password || !code.trim()}
                >
                  {t('mfa.disable')}
                </Button>
                <Button
                  onClick={() => {
                    setError(null);
                    setPassword('');
                    setCode('');
                    setStep({ name: 'overview' });
                  }}
                >
                  {t('mfa.cancel')}
                </Button>
              </Stack>
            </Stack>
          )}
        </Stack>
      </Paper>
    </Box>
  );
}
