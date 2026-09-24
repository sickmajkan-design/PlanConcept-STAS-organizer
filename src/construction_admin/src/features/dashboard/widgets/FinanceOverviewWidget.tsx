import { AccountBalanceWalletOutlined, TrendingDownOutlined, TrendingUpOutlined } from '@mui/icons-material';
import { Box, Typography } from '@mui/material';

import { useI18n } from '../../../i18n/useI18n';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod, percentChange } from '../../finance/periods';
import { useFinanceSeries } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { StatTile } from './StatTile';
import { WidgetShell } from './WidgetShell';

type Hint = { text: string; color: 'success.main' | 'error.main' | 'text.secondary' };

/**
 * Income, spending and profit for the board's period, each against the period
 * before it. `higherIsBetter` is what turns a rise into green or red: more
 * income is good news, more spending is not.
 */
export function FinanceOverviewWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const { data, isLoading, error } = useFinanceSeries();

  const hint = (current: number, previous: number, higherIsBetter: boolean): Hint => {
    const change = percentChange(current, previous);
    if (change === null) return { text: `— ${t('finance.vsPrevious')}`, color: 'text.secondary' };
    const rounded = Math.round(change * 10) / 10;
    const arrow = rounded > 0 ? '▲' : rounded < 0 ? '▼' : '=';
    const good = rounded === 0 ? null : rounded > 0 === higherIsBetter;
    return {
      text: `${arrow} ${Math.abs(rounded).toFixed(1)}% ${t('finance.vsPrevious')}`,
      color: good === null ? 'text.secondary' : good ? 'success.main' : 'error.main',
    };
  };

  const totals = data?.totals;
  const previous = data?.previous;
  const isEmpty = !!totals && totals.revenue === 0 && totals.expense === 0;

  return (
    <WidgetShell
      title={`${t('dashboard.widget.FinanceOverview')} — ${formatPeriod(period.from, period.to)}`}
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
        totals &&
        previous && (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, flex: 1, alignContent: 'flex-start' }}>
            <StatTile
              icon={<TrendingUpOutlined fontSize="small" />}
              label={t('finance.revenue')}
              value={formatMoney(totals.revenue, locale)}
              {...toTileHint(hint(totals.revenue, previous.revenue, true))}
              accent="#00897b"
            />
            <StatTile
              icon={<TrendingDownOutlined fontSize="small" />}
              label={t('finance.expense')}
              value={formatMoney(totals.expense, locale)}
              {...toTileHint(hint(totals.expense, previous.expense, false))}
              accent="#e65100"
            />
            <StatTile
              icon={<AccountBalanceWalletOutlined fontSize="small" />}
              label={t('finance.profit')}
              value={formatMoney(totals.profit, locale)}
              {...toTileHint(hint(totals.profit, previous.profit, true))}
              accent={totals.profit < 0 ? '#c62828' : '#37474f'}
            />
            {totals.marginPercent !== null && (
              <Typography variant="body2" color="text.secondary" sx={{ width: '100%' }}>
                {t('finance.margin')}: {totals.marginPercent.toFixed(1)}%
              </Typography>
            )}
          </Box>
        )
      )}
    </WidgetShell>
  );
}

function toTileHint(hint: Hint) {
  return { hint: hint.text, hintColor: hint.color };
}
