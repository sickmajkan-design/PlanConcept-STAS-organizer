import { Box, LinearProgress, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { financeApi } from '../../../api/finance';
import { useI18n } from '../../../i18n/useI18n';
import type { MessageKey } from '../../../i18n/en';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const KIND_LABEL: Record<string, MessageKey> = {
  Labour: 'dashboard.projectsRealization.labour',
  ManualPay: 'dashboard.projectsRealization.manualPay',
  Material: 'dashboard.projectsRealization.material',
  GeneralExpenses: 'dashboard.projectsRealization.generalExpenses',
  Accommodation: 'dashboard.projectsRealization.accommodation',
  Vehicles: 'dashboard.projectsRealization.vehicles',
  Tools: 'dashboard.projectsRealization.tools',
};

/**
 * How income, spending and profit moved against the period before, and what
 * the spending was on — in percent, with no amount anywhere. This is what an
 * account that may see statistics but not money gets; it says the same to an
 * account that may see both.
 */
export function FinanceStatisticsWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t } = useI18n();
  const { period } = useFinancePeriod();
  const { data, isLoading, error } = useQuery({
    queryKey: ['finance', 'statistics', period.from, period.to] as const,
    queryFn: () => financeApi.statistics({ from: period.from, to: period.to }),
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  });

  /** Higher is better for income and profit, worse for spending. */
  const change = (label: string, value: number | null | undefined, higherIsBetter: boolean) => {
    const rounded = value == null ? null : Math.round(value * 10) / 10;
    const good = rounded === null || rounded === 0 ? null : rounded > 0 === higherIsBetter;

    return (
      <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1 }}>
        <Typography variant="body2">{label}</Typography>
        <Typography
          variant="body2"
          sx={{ fontWeight: 700 }}
          color={good === null ? 'text.secondary' : good ? 'success.main' : 'error.main'}
        >
          {rounded === null ? '—' : `${rounded > 0 ? '▲' : rounded < 0 ? '▼' : '='} ${Math.abs(rounded).toFixed(1)}%`}
        </Typography>
      </Stack>
    );
  };

  return (
    <WidgetShell
      title={`${t('dashboard.widget.FinanceStatistics')} — ${formatPeriod(period.from, period.to)}`}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {data && (
        <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <Typography variant="caption" color="text.secondary">
            {t('finance.statistics.noAmounts')}
          </Typography>

          <Stack spacing={0.75}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              {t('finance.statistics.change')}
            </Typography>
            {change(t('finance.revenue'), data.revenueChangePercent, true)}
            {change(t('finance.expense'), data.expenseChangePercent, false)}
            {change(t('finance.profit'), data.profitChangePercent, true)}
          </Stack>

          <Stack spacing={1}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              {t('finance.statistics.structure')}
            </Typography>
            {data.shares.length === 0 ? (
              <Typography color="text.secondary" variant="body2">
                {t('finance.noData')}
              </Typography>
            ) : (
              data.shares.map((share) => (
                <Box key={share.kind}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1 }}>
                    <Typography variant="body2">{t(KIND_LABEL[share.kind] ?? 'finance.expense')}</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 700 }}>
                      {share.sharePercent.toFixed(1)}%
                    </Typography>
                  </Stack>
                  <LinearProgress variant="determinate" value={share.sharePercent} sx={{ height: 6, borderRadius: 3 }} />
                </Box>
              ))
            )}
          </Stack>
        </Stack>
      )}
    </WidgetShell>
  );
}
