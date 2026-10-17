import { useBranchFilter } from '../../branches/BranchContext';
import { Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { financeApi } from '../../../api/finance';
import { useI18n } from '../../../i18n/useI18n';
import type { MessageKey } from '../../../i18n/en';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { chartPalette } from '../../../theme';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, MiniStat, Segments, StatGrid } from './widgetUi';

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
  const { branchId, basis } = useBranchFilter();
  const people = branchId && basis === 'Employer' ? 'Employer' : undefined;
  const { data, isLoading, error } = useQuery({
    queryKey: ['finance', 'statistics', period.from, period.to, branchId ?? null, people ?? null] as const,
    queryFn: () => financeApi.statistics({ from: period.from, to: period.to, branchId, basis: people }),
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  });

  /** Higher is better for income and profit, worse for spending. */
  const change = (label: string, value: number | null | undefined, higherIsBetter: boolean) => {
    const rounded = value == null ? null : Math.round(value * 10) / 10;
    const good = rounded === null || rounded === 0 ? null : rounded > 0 === higherIsBetter;

    return (
      <MiniStat
        key={label}
        label={label}
        tone={good === null ? 'neutral' : good ? 'good' : 'bad'}
        value={rounded === null ? '—' : `${rounded > 0 ? '▲' : rounded < 0 ? '▼' : '='} ${Math.abs(rounded).toFixed(1)}%`}
      />
    );
  };

  return (
    <WidgetShell
      title={t('dashboard.widget.FinanceStatistics')}
      subtitle={formatPeriod(period.from, period.to)}
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

          <Stack spacing={1}>
            <Typography variant="body2" color="text.secondary">
              {t('finance.statistics.change')}
            </Typography>
            <StatGrid min={110}>
              {change(t('finance.revenue'), data.revenueChangePercent, true)}
              {change(t('finance.expense'), data.expenseChangePercent, false)}
              {change(t('finance.profit'), data.profitChangePercent, true)}
            </StatGrid>
          </Stack>

          <Stack spacing={1}>
            <Typography variant="body2" color="text.secondary">
              {t('finance.statistics.structure')}
            </Typography>
            {data.shares.length === 0 ? (
              <EmptyNote>{t('finance.noData')}</EmptyNote>
            ) : (
              <Segments
                parts={data.shares.map((share, index) => ({
                  label: t(KIND_LABEL[share.kind] ?? 'finance.expense'),
                  value: share.sharePercent,
                  color: chartPalette[index % chartPalette.length]!,
                  display: `${share.sharePercent.toFixed(1)}%`,
                }))}
              />
            )}
          </Stack>
        </Stack>
      )}
    </WidgetShell>
  );
}
