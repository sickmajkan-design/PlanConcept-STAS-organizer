import { Stack } from '@mui/material';

import { useI18n } from '../../../i18n/useI18n';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceSeries } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { TrendChart } from './widgetCharts';
import { BigNumber, EmptyNote, Pill } from './widgetUi';

/** "24.09" for a day, "24.09–30.09" for a week, "09.2026" for a month — as short as the point allows. */
function pointLabel(from: string, to: string, granularity: string): string {
  const [year, month, day] = from.split('-');
  if (granularity === 'Month') return `${month}.${year}`;
  if (granularity === 'Day' || from === to) return `${day}.${month}`;
  const [, toMonth, toDay] = to.split('-');
  return `${day}.${month}–${toDay}.${toMonth}`;
}

/**
 * Spending across the board's period, day by day, week by week or month by month,
 * against its own average — "are we spending more than usual". The average is
 * the period's total over its number of points, the same total the overview shows.
 */
export function SpendingTrendWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const { data, isLoading, error } = useFinanceSeries();

  const buckets = data?.buckets ?? [];
  const isEmpty = !!data && data.totals.expense === 0;
  const average = buckets.length > 0 ? (data?.totals.expense ?? 0) / buckets.length : 0;
  const highest = buckets.reduce<(typeof buckets)[number] | null>(
    (top, bucket) => (top === null || bucket.expense > top.expense ? bucket : top),
    null,
  );

  return (
    <WidgetShell
      title={t('dashboard.widget.SpendingTrend')}
      subtitle={formatPeriod(period.from, period.to)}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {isEmpty ? (
        <EmptyNote>{t('finance.noData')}</EmptyNote>
      ) : (
        data && (
          <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
            <BigNumber value={formatMoney(data.totals.expense, locale)} caption={t('finance.expense')} />
            <TrendChart
              points={buckets.map((b) => ({ label: pointLabel(b.from, b.to, data.granularity), value: b.expense }))}
              format={(value) => formatMoney(value, locale)}
              reference={average}
              referenceLabel={t('finance.average')}
              minHeight={170}
            />
            <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, flexShrink: 0 }}>
              <Pill>
                {t('finance.average')}: {formatMoney(average, locale)}
              </Pill>
              {highest && highest.expense > 0 && (
                <Pill tone="warn">
                  {t('finance.highest')}: {formatMoney(highest.expense, locale)} ({pointLabel(highest.from, highest.to, data.granularity)})
                </Pill>
              )}
            </Stack>
          </Stack>
        )
      )}
    </WidgetShell>
  );
}
