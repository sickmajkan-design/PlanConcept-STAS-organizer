import { Box } from '@mui/material';

import { canViewFinance } from '../../../auth/authHelpers';
import { useAuth } from '../../../auth/useAuth';
import { employeesApi } from '../../../api/employees';
import { toolsApi } from '../../../api/tools';
import { vehiclesApi } from '../../../api/vehicles';
import { workItemsApi } from '../../../api/workItems';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import { useCounts, useMonthToDateCost } from '../dashboardData';
import { useSinceYesterday } from '../useSinceYesterday';
import type { DashboardWidgetProps } from '../widgetTypes';
import { StatTile } from './StatTile';
import { WidgetShell } from './WidgetShell';

const ONE = { pageNumber: 1, pageSize: 1 } as const;

export function CompanyKpiWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();

  const { user } = useAuth();
  const mayViewFinance = canViewFinance(user);

  // This month so far against the same days of last month: against the whole of last month, a month a few days old
  // would always look like a fall.
  const cost = useMonthToDateCost(mayViewFinance);

  // Counts the server made, never the length of a page: a fleet of 140 is 140, not the 100 a page holds.
  const { counts, isLoading: countsLoading, error: countsError } = useCounts('kpi', {
    employees: (unit) => employeesApi.list({ ...ONE, status: 'Active', ...unit }),
    openWorkItems: (unit) => workItemsApi.list({ ...ONE, openOnly: true, ...unit }),
    overdueWorkItems: (unit) => workItemsApi.list({ ...ONE, overdueOnly: true, ...unit }),
    vehicles: (unit) => vehiclesApi.list({ ...ONE, ...unit }),
    vehiclesAvailable: (unit) => vehiclesApi.list({ ...ONE, status: 'Available', ...unit }),
    tools: (unit) => toolsApi.list({ ...ONE, ...unit }),
    toolsAvailable: (unit) => toolsApi.list({ ...ONE, status: 'Available', ...unit }),
  });

  const isLoading = cost.thisMonth.isLoading || countsLoading;
  const error = cost.thisMonth.error ?? countsError;

  const overdueCount = counts.overdueWorkItems ?? 0;

  const since = useSinceYesterday({
    'kpi.activeEmployees': counts.employees,
    'kpi.openWorkItems': counts.openWorkItems,
    'kpi.overdueWorkItems': counts.overdueWorkItems,
    'kpi.vehiclesAvailable': counts.vehiclesAvailable,
    'kpi.toolsAvailable': counts.toolsAvailable,
  });

  // More spending than last month is amber-red, less is green: the same way the cost trend reads.
  const thisMonthTotal = cost.thisMonth.data?.total ?? 0;
  const costChangePercent = cost.changePercent;
  const costChange =
    costChangePercent === null
      ? {}
      : {
          hint: `${costChangePercent > 0 ? '+' : ''}${costChangePercent.toFixed(1)}% ${t('dashboard.costTrend.vsSameDays')}`,
          hintColor: (costChangePercent > 0 ? 'error.main' : costChangePercent < 0 ? 'success.main' : 'text.secondary') as
            | 'error.main'
            | 'success.main'
            | 'text.secondary',
        };

  return (
    <WidgetShell title={t('dashboard.widget.CompanyKpi')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.25, flex: 1, alignContent: 'flex-start' }}>
        {/* A month with no costs recorded shows no cost tile at all, rather than a zero. */}
        {thisMonthTotal > 0 && (
          <StatTile
            label={t('dashboard.companyKpi.costThisMonth')}
            value={formatMoney(thisMonthTotal, locale)}
            to={paths.costs}
            {...costChange}
          />
        )}
        <StatTile label={t('dashboard.companyKpi.activeEmployees')} value={String(counts.employees ?? 0)} since={since('kpi.activeEmployees')} to={paths.employees} />
        <StatTile
          label={t('dashboard.companyKpi.openWorkItems')}
          value={String(counts.openWorkItems ?? 0)}
          hint={overdueCount > 0 ? t('dashboard.companyKpi.overdue', { count: overdueCount }) : undefined}
          hintColor="warning.main"
          since={since('kpi.openWorkItems', 'down')}
          to={paths.workItems}
        />
        <StatTile
          label={t('dashboard.companyKpi.vehiclesAvailable')}
          value={`${counts.vehiclesAvailable ?? 0} / ${counts.vehicles ?? 0}`}
          since={since('kpi.vehiclesAvailable', 'up')}
          to={paths.vehicles}
        />
        <StatTile
          label={t('dashboard.companyKpi.toolsAvailable')}
          value={`${counts.toolsAvailable ?? 0} / ${counts.tools ?? 0}`}
          since={since('kpi.toolsAvailable', 'up')}
          to={paths.tools}
        />
      </Box>
    </WidgetShell>
  );
}
