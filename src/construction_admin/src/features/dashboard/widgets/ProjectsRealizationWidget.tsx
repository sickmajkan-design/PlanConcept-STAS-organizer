import { Alert, Box, Button, Divider, LinearProgress, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import { PieChart } from '@mui/x-charts/PieChart';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { costsApi } from '../../../api/costs';
import { realizationApi } from '../../../api/projects';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
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

export function ProjectsRealizationWidget({
  instanceId: _instanceId,
  dragHandleProps,
  onRemove,
}: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const year = new Date().getFullYear();
  const { from, to } = yearBounds(year);

  const planQuery = useQuery({
    queryKey: ['dashboard', 'projects-realization', year] as const,
    queryFn: () => realizationApi.plan(year),
  });

  const costQuery = useQuery({
    queryKey: ['dashboard', 'cost-composition', year] as const,
    queryFn: () => costsApi.projectReport({ from, to }),
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
        { id: 'labour', label: t('dashboard.projectsRealization.labour'), value: cost.totalLabourCost },
        { id: 'material', label: t('dashboard.projectsRealization.material'), value: cost.totalMaterialCost },
        {
          id: 'general',
          label: t('dashboard.projectsRealization.generalExpenses'),
          value: cost.totalGeneralExpenseCost,
        },
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
      onRemove={onRemove}
      dragHandleProps={dragHandleProps}
    >
      {rows.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.projectsRealization.empty')}
        </Typography>
      ) : (
        <Stack spacing={1.5}>
          {rows.map((row) => (
            <Box key={row.projectId}>
              <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.5 }}>
                <Typography
                  variant="body2"
                  noWrap
                  sx={{ fontWeight: 600, maxWidth: '65%' }}
                >
                  {row.projectName}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {formatMoney(row.realizedToDate, locale)} / {formatMoney(row.contractValue, locale)}
                </Typography>
              </Stack>
              <LinearProgress
                variant="determinate"
                value={Math.min(100, row.percentOfContract ?? 0)}
              />
            </Box>
          ))}
        </Stack>
      )}
      <Button component={Link} to={paths.annualRealization} size="small" sx={{ mt: 2 }}>
        {t('common.viewAll')}
      </Button>

      <Divider sx={{ my: 2 }} />

      <Typography variant="caption" color="text.secondary">
        {t('dashboard.projectsRealization.costComposition')}
      </Typography>
      {costQuery.error ? (
        <Alert severity="error" sx={{ mt: 1 }}>
          {t('common.somethingWentWrong')}
        </Alert>
      ) : !costQuery.isLoading && costSlices.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.projectsRealization.noCostData')}
        </Typography>
      ) : (
        <PieChart
          height={180}
          series={[{ data: costSlices, innerRadius: 30 }]}
          hideLegend={false}
        />
      )}

      <Divider sx={{ my: 2 }} />

      <Typography variant="caption" color="text.secondary">
        {t('dashboard.projectsRealization.monthlyTrend')}
      </Typography>
      {revenueQuery.error ? (
        <Alert severity="error" sx={{ mt: 1 }}>
          {t('common.somethingWentWrong')}
        </Alert>
      ) : (
        <BarChart
          height={160}
          series={[{ data: monthly, label: t('dashboard.projectsRealization.revenue') }]}
          xAxis={[{ scaleType: 'band', data: monthLabels }]}
          margin={{ top: 10, bottom: 30, left: 40, right: 10 }}
        />
      )}
    </WidgetShell>
  );
}
