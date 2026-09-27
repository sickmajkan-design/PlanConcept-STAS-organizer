import { Alert, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { canAdministerAccounts, canViewFinance } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useT } from '../../i18n/useI18n';
import { useLeaveSettingsQuery, useUpdateLeaveSettings } from './useAbsences';

/**
 * What a day of annual leave pays, and whose public holidays are not leave days. The amount
 * is money, so this is only shown (and only works) for those who may see amounts.
 */
export function LeaveSettingsCard() {
  const t = useT();
  const { user } = useAuth();
  const allowed = canAdministerAccounts(user) && canViewFinance(user);
  const query = useLeaveSettingsQuery(allowed);
  const update = useUpdateLeaveSettings();

  const [rate, setRate] = useState('');
  const [country, setCountry] = useState('');
  const [message, setMessage] = useState<{ severity: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    if (query.data) {
      setRate(query.data.annualLeaveDailyRate === null ? '' : String(query.data.annualLeaveDailyRate));
      setCountry(query.data.holidayCountryCode ?? '');
    }
  }, [query.data]);

  if (!allowed) {
    return null;
  }

  const save = async () => {
    setMessage(null);

    const amount = rate.trim() === '' ? null : Number(rate.replace(',', '.'));

    if (amount !== null && (Number.isNaN(amount) || amount < 0)) {
      setMessage({ severity: 'error', text: t('leave.settings.rateInvalid') });
      return;
    }

    try {
      await update.mutateAsync({
        annualLeaveDailyRate: amount,
        holidayCountryCode: country.trim() === '' ? null : country.trim().toUpperCase(),
      });
      setMessage({ severity: 'success', text: t('leave.settings.saved') });
    } catch (err) {
      setMessage({ severity: 'error', text: toApiError(err).message });
    }
  };

  return (
    <Paper sx={{ p: 3, mt: 3 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
        {t('leave.settings.title')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('leave.settings.help')}
      </Typography>

      <Stack spacing={2}>
        {message && <Alert severity={message.severity}>{message.text}</Alert>}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField
            label={t('leave.settings.rate')}
            value={rate}
            onChange={(event) => setRate(event.target.value)}
            helperText={t('leave.settings.rateHelp')}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            fullWidth
          />
          <TextField
            label={t('leave.settings.country')}
            value={country}
            onChange={(event) => setCountry(event.target.value)}
            helperText={t('leave.settings.countryHelp')}
            slotProps={{ htmlInput: { maxLength: 2 } }}
            fullWidth
          />
        </Stack>
        <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
          <Button variant="contained" disabled={update.isPending} onClick={() => void save()}>
            {t('common.save')}
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}
