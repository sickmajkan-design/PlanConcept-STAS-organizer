import { useBranchFilter } from '../../branches/BranchContext';
import { Alert, Stack } from '@mui/material';
import { useQueries } from '@tanstack/react-query';

import { costsApi } from '../../../api/costs';
import { useI18n } from '../../../i18n/useI18n';
import { formatMoney } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { TrendChart } from './widgetCharts';
import { BigNumber, EmptyNote, Pill } from './widgetUi';

const MONTHS_SHOWN = 6;

/** The first and last calendar day of the month `monthsAgo` months before now. */
function monthBounds(monthsAgo: number): { from: string; to: string; label: Date } {
  const now = new Date();
  const first = new Date(now.getFullYear(), now.getMonth() - monthsAgo, 1);
  const last = new Date(now.getFullYear(), now.getMonth() - monthsAgo + 1, 0);
  const iso = (d: Date) => d.toISOString().slice(0, 10);
  return { from: iso(first), to: iso(last), label: first };
}

/**
 * There's no multi-period cost report endpoint — `costsApi.projectReport`
 * only ever answers for one `from`/`to` window at a time — so the trend is
 * assembled client-side from `MONTHS_SHOWN` parallel calls to that same
 * single-period endpoint, one per month.
 */
export function CostTrendWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { branchId, basis } = useBranchFilter();
  const people = branchId && basis === 'Employer' ? 'Employer' : undefined;

  const months = Array.from({ length: MONTHS_SHOWN }, (_, i) => monthBounds(MONTHS_SHOWN - 1 - i));

  const results = useQueries({
    queries: months.map((month) => ({
      queryKey: ['dashboard', 'company-cost-trend', month.from, month.to, branchId ?? null, people ?? null] as const,
      queryFn: () => costsApi.companyReport({ from: month.from, to: month.to, branchId, basis: people }),
    })),
  });

  const isLoading = results.some((r) => r.isLoading);
  const error = results.find((r) => r.error)?.error;

  const totals = results.map((r) => r.data?.total ?? 0);
  const monthLabels = months.map((m) =>
    m.label.toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', { month: 'short' }),
  );

  // Nothing spent in any of the months shown: there is no trend to draw.
  const hasCosts = totals.some((total) => total > 0);

  const latest = totals[totals.length - 1] ?? 0;
  const previous = totals[totals.length - 2] ?? 0;
  const delta = previous > 0 ? ((latest - previous) / previous) * 100 : null;

  return (
    <WidgetShell title={t('dashboard.widget.CostTrend')} subtitle={t('dashboard.costTrend.totalCost')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        {(isLoading || hasCosts) && (
          <Stack direction="row" sx={{ alignItems: 'flex-end', gap: 1.5, flexWrap: 'wrap', flexShrink: 0 }}>
            <BigNumber value={formatMoney(latest, locale)} />
            {delta !== null && (
              // More spending is not good news, so a rise is amber-red and a fall is green.
              <Pill tone={delta > 0 ? 'bad' : delta < 0 ? 'good' : 'neutral'}>
                {delta > 0 ? '+' : ''}
                {delta.toFixed(1)}% {t('dashboard.costTrend.vsPreviousMonth')}
              </Pill>
            )}
          </Stack>
        )}

        {error ? (
          <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
        ) : !isLoading && !hasCosts ? (
          <EmptyNote>{t('dashboard.costTrend.empty')}</EmptyNote>
        ) : (
          <TrendChart
            points={totals.map((value, i) => ({ label: monthLabels[i] ?? '', value }))}
            format={(value) => formatMoney(value, locale)}
            minHeight={150}
          />
        )}
      </Stack>
    </WidgetShell>
  );
}
