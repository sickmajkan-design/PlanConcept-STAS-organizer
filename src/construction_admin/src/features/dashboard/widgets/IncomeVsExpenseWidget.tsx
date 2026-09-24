import { Box, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';

import { useI18n } from '../../../i18n/useI18n';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceSeries } from '../../finance/useFinanceSeries';
import { useElementSize } from '../useElementSize';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

/** "24.09" for a day, "24.09–30.09" for a week, "09.2026" for a month — as short as the bar allows. */
function barLabel(from: string, to: string, granularity: string): string {
  const [year, month, day] = from.split('-');
  if (granularity === 'Month') return `${month}.${year}`;
  if (granularity === 'Day' || from === to) return `${day}.${month}`;
  const [, toMonth, toDay] = to.split('-');
  return `${day}.${month}–${toDay}.${toMonth}`;
}

/**
 * Income against spending, one pair of bars per day, week or month of the
 * board's period. The profit is stated under the chart rather than drawn as a
 * third series: it is the difference between the two bars, and a number reads
 * better than a line hovering between them.
 */
export function IncomeVsExpenseWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const { data, isLoading, error } = useFinanceSeries();
  const chartSize = useElementSize<HTMLDivElement>();

  const isEmpty = !!data && data.totals.revenue === 0 && data.totals.expense === 0;

  return (
    <WidgetShell
      title={`${t('dashboard.widget.IncomeVsExpense')} — ${formatPeriod(period.from, period.to)}`}
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
                <BarChart
                  width={chartSize.width}
                  height={chartSize.height}
                  colors={['#00897b', '#e65100']}
                  xAxis={[
                    {
                      scaleType: 'band',
                      data: data.buckets.map((b) => barLabel(b.from, b.to, data.granularity)),
                    },
                  ]}
                  series={[
                    { data: data.buckets.map((b) => b.revenue), label: t('finance.revenue') },
                    { data: data.buckets.map((b) => b.expense), label: t('finance.expense') },
                  ]}
                  margin={{ top: 10, bottom: 30, left: 64, right: 10 }}
                />
              )}
            </Box>
            <Typography
              sx={{ fontWeight: 700, flexShrink: 0 }}
              color={data.totals.profit < 0 ? 'error.main' : 'success.main'}
            >
              {t('finance.profit')}: {formatMoney(data.totals.profit, locale)}
            </Typography>
          </Stack>
        )
      )}
    </WidgetShell>
  );
}
