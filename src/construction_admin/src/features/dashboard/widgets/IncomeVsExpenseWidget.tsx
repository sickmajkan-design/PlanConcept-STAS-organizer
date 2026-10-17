import { Stack, useTheme } from '@mui/material';

import { useI18n } from '../../../i18n/useI18n';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceSeries } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { BarsChart } from './widgetCharts';
import { BigNumber, EmptyNote } from './widgetUi';

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
  const theme = useTheme();

  const isEmpty = !!data && data.totals.revenue === 0 && data.totals.expense === 0;

  return (
    <WidgetShell
      title={t('dashboard.widget.IncomeVsExpense')}
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
          <Stack spacing={1} sx={{ flex: 1, minHeight: 0 }}>
            <BarsChart
              groups={data.buckets.map((b) => ({ label: barLabel(b.from, b.to, data.granularity), values: [b.revenue, b.expense] }))}
              names={[t('finance.revenue'), t('finance.expense')]}
              colors={[theme.palette.success.main, theme.palette.primary.main]}
              format={(value) => formatMoney(value, locale)}
              minHeight={200}
            />
            <Stack direction="row" sx={{ flexShrink: 0, gap: 1, alignItems: 'baseline', flexWrap: 'wrap' }}>
              <BigNumber size="md" value={formatMoney(data.totals.profit, locale)} tone={data.totals.profit < 0 ? 'bad' : 'good'} caption={t('finance.profit')} />
            </Stack>
          </Stack>
        )
      )}
    </WidgetShell>
  );
}
