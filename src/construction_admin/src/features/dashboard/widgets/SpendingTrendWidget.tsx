import { Box, Stack, Typography } from '@mui/material';
import { LineChart } from '@mui/x-charts/LineChart';

import { useI18n } from '../../../i18n/useI18n';
import { chartPalette } from '../../../theme';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceSeries } from '../../finance/useFinanceSeries';
import { useElementSize } from '../useElementSize';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

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
  const chartSize = useElementSize<HTMLDivElement>();

  const buckets = data?.buckets ?? [];
  const isEmpty = !!data && data.totals.expense === 0;
  const average = buckets.length > 0 ? (data?.totals.expense ?? 0) / buckets.length : 0;
  const highest = buckets.reduce<(typeof buckets)[number] | null>(
    (top, bucket) => (top === null || bucket.expense > top.expense ? bucket : top),
    null,
  );

  return (
    <WidgetShell
      title={`${t('dashboard.widget.SpendingTrend')} — ${formatPeriod(period.from, period.to)}`}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {isEmpty ? (
        <Typography color="text.secondary" variant="body2">
          {t('finance.noData')}
        </Typography>
      ) : (
        data && (
          <Stack spacing={1} sx={{ flex: 1, minHeight: 0 }}>
            <Box ref={chartSize.ref} sx={{ flex: 1, minHeight: 180 }}>
              {chartSize.width > 0 && chartSize.height > 0 && (
                <LineChart
                  width={chartSize.width}
                  height={chartSize.height}
                  colors={[chartPalette[0]!, chartPalette[3]!]}
                  xAxis={[{ scaleType: 'point', data: buckets.map((b) => pointLabel(b.from, b.to, data.granularity)) }]}
                  series={[
                    { data: buckets.map((b) => b.expense), label: t('finance.expense'), showMark: buckets.length <= 31 },
                    { data: buckets.map(() => average), label: t('finance.average'), showMark: false },
                  ]}
                  margin={{ top: 10, bottom: 30, left: 64, right: 10 }}
                />
              )}
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ flexShrink: 0 }}>
              {t('finance.average')}: <strong>{formatMoney(average, locale)}</strong>
              {highest && highest.expense > 0 && (
                <>
                  {' · '}
                  {t('finance.highest')}: <strong>{formatMoney(highest.expense, locale)}</strong> (
                  {pointLabel(highest.from, highest.to, data.granularity)})
                </>
              )}
            </Typography>
          </Stack>
        )
      )}
    </WidgetShell>
  );
}
