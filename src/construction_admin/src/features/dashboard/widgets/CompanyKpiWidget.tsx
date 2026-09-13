import { Box, Divider, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { costsApi } from '../../../api/costs';
import { employeesApi } from '../../../api/employees';
import { toolsApi } from '../../../api/tools';
import { vehiclesApi } from '../../../api/vehicles';
import { workItemsApi } from '../../../api/workItems';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

// Same page sizes and query keys FleetStatusWidget already uses for these two
// lists — mounting both widgets on one board shares a single fetch instead of
// doubling it.
const FLEET_PAGE_SIZE = 100;

function startOfMonth(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`;
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

interface Tile {
  key: string;
  label: string;
  value: string;
  hint?: string;
}

export function CompanyKpiWidget({
  instanceId: _instanceId,
  dragHandleProps,
  onRemove,
}: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const from = startOfMonth();
  const to = today();

  const costQuery = useQuery({
    queryKey: ['dashboard', 'kpi', 'cost-this-month', from, to] as const,
    queryFn: () => costsApi.projectReport({ from, to }),
  });

  const employeesQuery = useQuery({
    queryKey: ['dashboard', 'kpi', 'active-employees'] as const,
    queryFn: () => employeesApi.list({ pageNumber: 1, pageSize: 1, status: 'Active' }),
  });

  const openWorkItemsQuery = useQuery({
    queryKey: ['dashboard', 'kpi', 'open-work-items'] as const,
    queryFn: () => workItemsApi.list({ pageNumber: 1, pageSize: 1, openOnly: true }),
  });

  const overdueWorkItemsQuery = useQuery({
    queryKey: ['dashboard', 'kpi', 'overdue-work-items'] as const,
    queryFn: () => workItemsApi.list({ pageNumber: 1, pageSize: 1, overdueOnly: true }),
  });

  const vehiclesQuery = useQuery({
    queryKey: ['dashboard', 'fleet', 'vehicles'] as const,
    queryFn: () => vehiclesApi.list({ pageNumber: 1, pageSize: FLEET_PAGE_SIZE }),
  });

  const toolsQuery = useQuery({
    queryKey: ['dashboard', 'fleet', 'tools'] as const,
    queryFn: () => toolsApi.list({ pageNumber: 1, pageSize: FLEET_PAGE_SIZE }),
  });

  const isLoading =
    costQuery.isLoading ||
    employeesQuery.isLoading ||
    openWorkItemsQuery.isLoading ||
    overdueWorkItemsQuery.isLoading ||
    vehiclesQuery.isLoading ||
    toolsQuery.isLoading;

  const error =
    costQuery.error ??
    employeesQuery.error ??
    openWorkItemsQuery.error ??
    overdueWorkItemsQuery.error ??
    vehiclesQuery.error ??
    toolsQuery.error;

  const vehiclesAvailable =
    vehiclesQuery.data?.items.filter((v) => v.status === 'Available').length ?? 0;
  const toolsAvailable =
    toolsQuery.data?.items.filter((tool) => tool.status === 'Available').length ?? 0;

  const overdueCount = overdueWorkItemsQuery.data?.totalCount ?? 0;

  const tiles: Tile[] = [
    {
      key: 'cost',
      label: t('dashboard.companyKpi.costThisMonth'),
      value: formatMoney(costQuery.data?.total ?? 0, locale),
    },
    {
      key: 'employees',
      label: t('dashboard.companyKpi.activeEmployees'),
      value: String(employeesQuery.data?.totalCount ?? 0),
    },
    {
      key: 'workItems',
      label: t('dashboard.companyKpi.openWorkItems'),
      value: String(openWorkItemsQuery.data?.totalCount ?? 0),
      hint:
        overdueCount > 0
          ? t('dashboard.companyKpi.overdue', { count: overdueCount })
          : undefined,
    },
    {
      key: 'vehicles',
      label: t('dashboard.companyKpi.vehiclesAvailable'),
      value: `${vehiclesAvailable} / ${vehiclesQuery.data?.totalCount ?? 0}`,
    },
    {
      key: 'tools',
      label: t('dashboard.companyKpi.toolsAvailable'),
      value: `${toolsAvailable} / ${toolsQuery.data?.totalCount ?? 0}`,
    },
  ];

  return (
    <WidgetShell
      title={t('dashboard.widget.CompanyKpi')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      dragHandleProps={dragHandleProps}
    >
      <Stack spacing={2}>
        {/* Plain stat tiles, not links — a drag handle is the only thing on
            this card that should ever pick up a pointer gesture. Navigation
            lives in the plain text links below instead, exactly like every
            other widget on this board (see FleetStatusWidget). */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))',
            gap: 2,
          }}
        >
          {tiles.map((tile) => (
            <Stack key={tile.key} spacing={0.25} sx={{ p: 1.5, borderRadius: 1, bgcolor: 'action.hover' }}>
              <Typography variant="caption" color="text.secondary" noWrap>
                {tile.label}
              </Typography>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {tile.value}
              </Typography>
              {tile.hint && (
                <Typography variant="caption" color="warning.main">
                  {tile.hint}
                </Typography>
              )}
            </Stack>
          ))}
        </Box>

        <Divider />

        <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
          <Typography variant="body2">
            <Link to={paths.costs}>{t('nav.costs')}</Link>
          </Typography>
          <Typography variant="body2">
            <Link to={paths.employees}>{t('nav.employees')}</Link>
          </Typography>
          <Typography variant="body2">
            <Link to={paths.workItems}>{t('nav.workItems')}</Link>
          </Typography>
          <Typography variant="body2">
            <Link to={paths.vehicles}>{t('nav.vehicles')}</Link>
          </Typography>
          <Typography variant="body2">
            <Link to={paths.tools}>{t('nav.tools')}</Link>
          </Typography>
        </Stack>
      </Stack>
    </WidgetShell>
  );
}
