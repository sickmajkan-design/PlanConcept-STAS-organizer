import { Alert, Box, Stack, Typography } from '@mui/material';
import { LineChart } from '@mui/x-charts/LineChart';
import { useQueries } from '@tanstack/react-query';

import { costsApi } from '../../../api/costs';
import { useI18n } from '../../../i18n/useI18n';
import { chartPalette } from '../../../theme';
import { formatMoney } from '../../../utils/formatting';
import { useElementSize } from '../useElementSize';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

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
  const chartSize = useElementSize<HTMLDivElement>();

  const months = Array.from({ length: MONTHS_SHOWN }, (_, i) => monthBounds(MONTHS_SHOWN - 1 - i));

  const results = useQueries({
    queries: months.map((month) => ({
      queryKey: ['dashboard', 'cost-trend', month.from, month.to] as const,
      queryFn: () => costsApi.projectReport({ from: month.from, to: month.to }),
    })),
  });

  const isLoading = results.some((r) => r.isLoading);
  const error = results.find((r) => r.error)?.error;

  const totals = results.map((r) => r.data?.total ?? 0);
  const monthLabels = months.map((m) =>
    m.label.toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', { month: 'short' }),
  );

  const latest = totals[totals.length - 1] ?? 0;
  const previous = totals[totals.length - 2] ?? 0;
  const delta = previous > 0 ? ((latest - previous) / previous) * 100 : null;

  return (
    <WidgetShell title={t('dashboard.widget.CostTrend')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'baseline', flexShrink: 0 }}>
          <Typography sx={{ fontWeight: 800, fontSize: 'clamp(1.5rem, 2.2vw, 2.25rem)', lineHeight: 1 }}>
            {formatMoney(latest, locale)}
          </Typography>
          {delta !== null && (
            <Typography
              variant="body2"
              sx={{ fontWeight: 700, color: delta > 0 ? 'error.main' : delta < 0 ? 'success.main' : 'text.secondary' }}
            >
              {delta > 0 ? '+' : ''}
              {delta.toFixed(1)}% {t('dashboard.costTrend.vsPreviousMonth')}
            </Typography>
          )}
        </Stack>

        {error ? (
          <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
        ) : (
          <Box ref={chartSize.ref} sx={{ flex: 1, minHeight: 150 }}>
            {chartSize.width > 0 && chartSize.height > 0 && (
              <LineChart
                width={chartSize.width}
                height={chartSize.height}
                colors={chartPalette}
                series={[{ data: totals, label: t('dashboard.costTrend.totalCost'), area: true, showMark: true }]}
                xAxis={[{ scaleType: 'point', data: monthLabels }]}
                margin={{ top: 10, bottom: 30, left: 56, right: 10 }}
                hideLegend
              />
            )}
          </Box>
        )}
      </Stack>
    </WidgetShell>
  );
}
