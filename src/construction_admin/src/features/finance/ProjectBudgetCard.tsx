import { Alert, Button, Card, CardContent, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { financeApi, type BudgetAlertBasis } from '../../api/finance';
import { useI18n } from '../../i18n/useI18n';
import { formatMoney } from '../../utils/formatting';

/**
 * A site's planned spending, next to what the customer pays. Rendered only for
 * an account with the finance right; the figure never travels with the project
 * itself, so nobody else can read it off the project page.
 */
export function ProjectBudgetCard({ projectId }: { projectId: string }) {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const [text, setText] = useState('');
  const [basis, setBasis] = useState<BudgetAlertBasis | ''>('');
  const [warnText, setWarnText] = useState('');

  const { data, error } = useQuery({
    queryKey: ['finance', 'budget', projectId] as const,
    queryFn: () => financeApi.budget.get(projectId),
  });

  useEffect(() => {
    if (data) {
      setText(data.budget === null ? '' : String(data.budget));
      setBasis(data.alertBasis ?? '');
      setWarnText(data.warnPercent === null ? '' : String(data.warnPercent));
    }
  }, [data]);

  const parsed = text.trim() === '' ? null : Number(text.replace(',', '.'));
  const budgetValid = parsed === null || (Number.isFinite(parsed) && parsed >= 0);

  const warnParsed = warnText.trim() === '' ? null : Number(warnText);
  const warnValid = warnParsed === null || (Number.isInteger(warnParsed) && warnParsed >= 1 && warnParsed <= 99);

  // Measuring against a budget that is not set could never warn — the server refuses it too.
  const basisValid = basis !== 'Budget' || (parsed !== null && parsed > 0);

  const valid = budgetValid && warnValid && basisValid;
  const unchanged =
    (data?.budget ?? null) === parsed &&
    (data?.alertBasis ?? '') === basis &&
    (data?.warnPercent ?? null) === warnParsed;

  const save = useMutation({
    mutationFn: () =>
      financeApi.budget.set(projectId, { budget: parsed, alertBasis: basis === '' ? null : basis, warnPercent: warnParsed }),
    onSuccess: (saved) => {
      queryClient.setQueryData(['finance', 'budget', projectId], saved);
      // The widgets that measure spending against it.
      void queryClient.invalidateQueries({ queryKey: ['finance', 'by-project'] });
      void queryClient.invalidateQueries({ queryKey: ['finance', 'budget-alerts'] });
    },
  });

  return (
    <Card>
      <CardContent>
        <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
          {t('finance.budget.title')}
        </Typography>

        {error ? (
          <Alert severity="error">{toApiError(error).message}</Alert>
        ) : (
          <Stack spacing={1.5}>
            <Typography variant="body2" color="text.secondary">
              {t('finance.budget.contract')}:{' '}
              {data?.contractValue == null ? '—' : formatMoney(data.contractValue, locale)}
            </Typography>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <TextField
                size="small"
                label={t('finance.budget.planned')}
                value={text}
                onChange={(event) => setText(event.target.value)}
                error={!budgetValid}
                helperText={t('finance.budget.help')}
                slotProps={{ htmlInput: { inputMode: 'decimal' } }}
                sx={{ maxWidth: 260 }}
              />
              <TextField
                select
                size="small"
                label={t('finance.budget.basis')}
                value={basis}
                error={!basisValid}
                onChange={(event) => setBasis(event.target.value as BudgetAlertBasis | '')}
                sx={{ minWidth: 260 }}
              >
                <MenuItem value="">{t('finance.budget.basisAuto')}</MenuItem>
                <MenuItem value="Budget">{t('finance.budget.basisBudget')}</MenuItem>
                <MenuItem value="Contract">{t('finance.budget.basisContract')}</MenuItem>
              </TextField>
              <TextField
                size="small"
                label={t('finance.budget.warnPercent')}
                value={warnText}
                error={!warnValid}
                onChange={(event) => setWarnText(event.target.value)}
                helperText={t('finance.budget.warnPercentHelp')}
                slotProps={{ htmlInput: { inputMode: 'numeric' } }}
                sx={{ maxWidth: 220 }}
              />
              <Button
                variant="outlined"
                disabled={!valid || unchanged}
                loading={save.isPending}
                onClick={() => save.mutate()}
                sx={{ alignSelf: { sm: 'flex-start' } }}
              >
                {t('common.save')}
              </Button>
            </Stack>

            {save.error && <Alert severity="error">{toApiError(save.error).message}</Alert>}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}
