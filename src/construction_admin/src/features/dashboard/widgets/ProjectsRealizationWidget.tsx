import { Alert, Box, Button, Divider, Stack, Typography, useTheme } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { costsApi } from '../../../api/costs';
import { realizationApi } from '../../../api/projects';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { chartPalette } from '../../../theme';
import { formatMoney } from '../../../utils/formatting';
import { fetchAllPages } from '../dashboardData';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { BarsChart, Donut } from './widgetCharts';
import { BarRow, EmptyNote, MoreLink } from './widgetUi';
import { companyNow } from '../../../utils/companyTime';

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
  const theme = useTheme();
  const year = companyNow().getFullYear();
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
    // 200 is this endpoint's own max page size (GetProjectRevenuesQuery), not an arbitrary choice — a bigger request
    // 400s outright. Every page is read: the months are added up from the payments, and a year can hold more than 200.
    queryFn: () => fetchAllPages((pageNumber) => realizationApi.revenues.list({ pageNumber, pageSize: 200, from, to })),
  });

  // The API gives a share as a fraction (0.98 is 98%); the bar and the "complete" colour are in percent.
  const percentOf = (row: { percentOfContract: number | null }) => (row.percentOfContract ?? 0) * 100;

  const rows = [...(planQuery.data?.rows ?? [])]
    .filter((row) => row.contractValue > 0)
    .sort((a, b) => percentOf(b) - percentOf(a))
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
          <EmptyNote>{t('dashboard.projectsRealization.empty')}</EmptyNote>
        ) : (
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              flexShrink: 0,
              // One column on a narrow widget, two once it is wide enough to hold them.
              gridTemplateColumns: '1fr',
              '@container widget (min-width: 640px)': { gridTemplateColumns: '1fr 1fr' },
            }}
          >
            {rows.map((row) => (
              <BarRow
                key={row.projectId}
                label={row.projectName}
                value={`${formatMoney(row.realizedToDate, locale)} / ${formatMoney(row.contractValue, locale)}`}
                percent={percentOf(row)}
                tone={percentOf(row) >= 100 ? 'good' : 'accent'}
              />
            ))}
          </Box>
        )}
        <MoreLink to={paths.annualRealization}>{t('common.viewAll')}</MoreLink>

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
              <Box sx={{ mt: 1 }}>
                <Donut
                  parts={costSlices.map((slice, index) => ({
                    label: slice.label,
                    value: slice.value,
                    color: chartPalette[index % chartPalette.length]!,
                    display: formatMoney(slice.value, locale),
                  }))}
                />
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
          ) : monthly.every((value) => value === 0) ? (
            // An axis with nothing on it says less than a sentence does.
            <Stack spacing={1} sx={{ mt: 1, alignItems: 'flex-start' }}>
              <Typography variant="body2" color="text.secondary">
                {t('dashboard.projectsRealization.noRevenue')}
              </Typography>
              {rows.length === 0 && (
                <Button component={Link} to={paths.projectNew} size="small" variant="outlined" color="inherit" sx={{ borderColor: 'divider' }}>
                  {t('projects.add')}
                </Button>
              )}
            </Stack>
          ) : (
            <BarsChart
              groups={monthly.map((value, index) => ({ label: monthLabels[index] ?? '', values: [value] }))}
              names={[t('dashboard.projectsRealization.revenue')]}
              colors={[theme.palette.primary.main]}
              format={(value) => formatMoney(value, locale)}
              minHeight={180}
            />
          )}
        </Box>
      </Stack>
    </WidgetShell>
  );
}
