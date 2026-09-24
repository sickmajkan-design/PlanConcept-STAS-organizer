import { Alert, Box, Button, Divider, LinearProgress, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import { PieChart } from '@mui/x-charts/PieChart';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { costsApi } from '../../../api/costs';
import { realizationApi } from '../../../api/projects';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { chartPalette } from '../../../theme';
import { formatMoney } from '../../../utils/formatting';
import { useElementSize } from '../useElementSize';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const TOP_N = 5;

function yearBounds(year: number) {
  return { from: `${year}-01-01`, to: `${year}-12-31` };
}

/** 12 zero-initialized monthly buckets, so a month with no revenue still shows as a gap, not a shorter axis. */
function monthlyTotals(entries: { amount: number; occurredOn: string }[]): number[] {
  const totals = new Array<number>(12).fill(0);
  for (const entry of entries) {
    const month = Number(entry.occurredOn.slice(5, 7)) - 1;
    if (month >= 0 && month < 12) totals[month] += entry.amount;
  }
  return totals;
}

export function ProjectsRealizationWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const pieSize = useElementSize<HTMLDivElement>();
  const barSize = useElementSize<HTMLDivElement>();
  const year = new Date().getFullYear();
  const { from, to } = yearBounds(year);

  const planQuery = useQuery({
    queryKey: ['dashboard', 'projects-realization', year] as const,
    queryFn: () => realizationApi.plan(year),
  });

  const costQuery = useQuery({
    queryKey: ['dashboard', 'company-cost-composition', year] as const,
    queryFn: () => costsApi.companyReport({ from, to }),
  });

  const revenueQuery = useQuery({
    queryKey: ['dashboard', 'revenue-trend', year] as const,
    // 200 is this endpoint's own max page size (GetProjectRevenuesQuery),
    // not an arbitrary choice — a bigger request 400s outright.
    queryFn: () => realizationApi.revenues.list({ pageNumber: 1, pageSize: 200, from, to }),
  });

  const rows = [...(planQuery.data?.rows ?? [])]
    .filter((row) => row.contractValue > 0)
    .sort((a, b) => (b.percentOfContract ?? 0) - (a.percentOfContract ?? 0))
    .slice(0, TOP_N);

  const cost = costQuery.data;
  const costSlices = cost
    ? [
        { id: 'labour', label: t('dashboard.projectsRealization.labour'), value: cost.labour },
        { id: 'manualPay', label: t('dashboard.projectsRealization.manualPay'), value: cost.manualPay },
        { id: 'material', label: t('dashboard.projectsRealization.material'), value: cost.material },
        { id: 'general', label: t('dashboard.projectsRealization.generalExpenses'), value: cost.generalExpenses },
        { id: 'accommodation', label: t('dashboard.projectsRealization.accommodation'), value: cost.accommodation },
        { id: 'vehicles', label: t('dashboard.projectsRealization.vehicles'), value: cost.vehicles },
        { id: 'tools', label: t('dashboard.projectsRealization.tools'), value: cost.tools },
      ].filter((slice) => slice.value > 0)
    : [];

  const monthly = monthlyTotals(revenueQuery.data?.items ?? []);
  const monthLabels = Array.from({ length: 12 }, (_, i) =>
    new Date(year, i, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', { month: 'short' }),
  );

  return (
    <WidgetShell
      title={t('dashboard.widget.ProjectsRealization')}
      isLoading={planQuery.isLoading}
      error={planQuery.error}
      onRemove={onRemove} onExpandWidth={onExpandWidth}
    >
      <Stack spacing={2} sx={{ flex: 1, minHeight: 0 }}>
        {rows.length === 0 ? (
          <Typography color="text.secondary" variant="body2">
            {t('dashboard.projectsRealization.empty')}
          </Typography>
        ) : (
          <Stack spacing={1.75} sx={{ flexShrink: 0 }}>
            {rows.map((row) => (
              <Box key={row.projectId}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.5, alignItems: 'baseline' }}>
                  <Typography variant="body1" noWrap sx={{ fontWeight: 700, maxWidth: '60%' }}>
                    {row.projectName}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
                    {formatMoney(row.realizedToDate, locale)} / {formatMoney(row.contractValue, locale)}
                  </Typography>
                </Stack>
                <LinearProgress
                  variant="determinate"
                  value={Math.min(100, row.percentOfContract ?? 0)}
                  sx={{ height: 8, borderRadius: 999 }}
                />
              </Box>
            ))}
          </Stack>
        )}
        <Button component={Link} to={paths.annualRealization} size="small" sx={{ flexShrink: 0, alignSelf: 'flex-start' }}>
          {t('common.viewAll')}
        </Button>

        {/* No costs recorded this year: the whole section is left out, not shown empty. */}
        {(costQuery.isLoading || costQuery.error || costSlices.length > 0) && (
          <>
          <Divider sx={{ flexShrink: 0 }} />

          <Box sx={{ flex: 1, minHeight: 180, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, flexShrink: 0, mb: 0.5 }}>
              {t('dashboard.projectsRealization.costComposition')}
            </Typography>
            {costQuery.error ? (
              <Alert severity="error" sx={{ mt: 1 }}>
                {t('common.somethingWentWrong')}
              </Alert>
            ) : costSlices.length === 1 ? (
              // One category is not a composition: a full ring says nothing a
              // number does not say better.
              <Typography variant="body2" sx={{ mt: 1 }}>
                {costSlices[0].label}: <strong>{formatMoney(costSlices[0].value, locale)}</strong>
              </Typography>
            ) : (
              <Box ref={pieSize.ref} sx={{ flex: 1, minHeight: 0 }}>
                {pieSize.width > 0 && pieSize.height > 0 && (
                  <PieChart
                    width={pieSize.width}
                    height={pieSize.height}
                    colors={chartPalette}
                    series={[{ data: costSlices, innerRadius: 40 }]}
                    slotProps={{ legend: { direction: 'horizontal', position: { vertical: 'bottom', horizontal: 'center' } } }}
                  />
                )}
              </Box>
            )}
          </Box>
          </>
        )}

        <Divider sx={{ flexShrink: 0 }} />

        <Box sx={{ flex: 1, minHeight: 180, display: 'flex', flexDirection: 'column' }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, flexShrink: 0, mb: 0.5 }}>
            {t('dashboard.projectsRealization.monthlyTrend')}
          </Typography>
          {revenueQuery.error ? (
            <Alert severity="error" sx={{ mt: 1 }}>
              {t('common.somethingWentWrong')}
            </Alert>
          ) : (
            <Box ref={barSize.ref} sx={{ flex: 1, minHeight: 0 }}>
              {barSize.width > 0 && barSize.height > 0 && (
                <BarChart
                  width={barSize.width}
                  height={barSize.height}
                  colors={chartPalette}
                  series={[{ data: monthly, label: t('dashboard.projectsRealization.revenue') }]}
                  xAxis={[{ scaleType: 'band', data: monthLabels }]}
                  margin={{ top: 10, bottom: 30, left: 48, right: 10 }}
                  borderRadius={6}
                />
              )}
            </Box>
          )}
        </Box>
      </Stack>
    </WidgetShell>
  );
}
