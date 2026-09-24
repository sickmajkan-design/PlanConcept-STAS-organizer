import { Alert, Button, Card, CardContent, Stack, TextField, Typography } from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { financeApi } from '../../api/finance';
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

  const { data, error } = useQuery({
    queryKey: ['finance', 'budget', projectId] as const,
    queryFn: () => financeApi.budget.get(projectId),
  });

  useEffect(() => {
    if (data) setText(data.budget === null ? '' : String(data.budget));
  }, [data]);

  const parsed = text.trim() === '' ? null : Number(text.replace(',', '.'));
  const valid = parsed === null || (Number.isFinite(parsed) && parsed >= 0);
  const unchanged = (data?.budget ?? null) === parsed;

  const save = useMutation({
    mutationFn: () => financeApi.budget.set(projectId, parsed),
    onSuccess: (saved) => {
      queryClient.setQueryData(['finance', 'budget', projectId], saved);
      // The widgets that measure spending against it.
      void queryClient.invalidateQueries({ queryKey: ['finance', 'by-project'] });
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

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
              <TextField
                size="small"
                label={t('finance.budget.planned')}
                value={text}
                onChange={(event) => setText(event.target.value)}
                error={!valid}
                helperText={t('finance.budget.help')}
                slotProps={{ htmlInput: { inputMode: 'decimal' } }}
                sx={{ maxWidth: 260 }}
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
