import {
  AssignmentTurnedInOutlined,
  BuildOutlined,
  GroupsOutlined,
  LocalShippingOutlined,
  PaidOutlined,
} from '@mui/icons-material';
import { Box, Chip, Stack } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link as RouterLink } from 'react-router-dom';

import { canViewFinance } from '../../../auth/authHelpers';
import { useAuth } from '../../../auth/useAuth';
import { costsApi } from '../../../api/costs';
import { employeesApi } from '../../../api/employees';
import { toolsApi } from '../../../api/tools';
import { vehiclesApi } from '../../../api/vehicles';
import { workItemsApi } from '../../../api/workItems';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { StatTile } from './StatTile';
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

export function CompanyKpiWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const from = startOfMonth();
  const to = today();

  const { user } = useAuth();
  const mayViewFinance = canViewFinance(user);

  const costQuery = useQuery({
    queryKey: ['dashboard', 'kpi', 'company-cost-this-month', from, to] as const,
    // Everything the company spent, not only what is tied to a project.
    queryFn: () => costsApi.companyReport({ from, to }),
    // The server refuses this to anyone without the finance right, which
    // would put the whole widget into its error state over one tile.
    enabled: mayViewFinance,
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

  const vehiclesAvailable = vehiclesQuery.data?.items.filter((v) => v.status === 'Available').length ?? 0;
  const toolsAvailable = toolsQuery.data?.items.filter((tool) => tool.status === 'Available').length ?? 0;
  const overdueCount = overdueWorkItemsQuery.data?.totalCount ?? 0;

  const links = [
    ...(mayViewFinance ? [{ to: paths.costs, label: t('nav.costs') }] : []),
    { to: paths.employees, label: t('nav.employees') },
    { to: paths.workItems, label: t('nav.workItems') },
    { to: paths.vehicles, label: t('nav.vehicles') },
    { to: paths.tools, label: t('nav.tools') },
  ];

  return (
    <WidgetShell title={t('dashboard.widget.CompanyKpi')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={2} sx={{ flex: 1, minHeight: 0 }}>
        <Box
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: 1.5,
            flex: 1,
            alignContent: 'flex-start',
          }}
        >
          {/* A month with no costs recorded shows no cost tile at all, rather than a zero. */}
          {(costQuery.data?.total ?? 0) > 0 && (
            <StatTile
              icon={<PaidOutlined fontSize="small" />}
              label={t('dashboard.companyKpi.costThisMonth')}
              value={formatMoney(costQuery.data?.total ?? 0, locale)}
              accent="#e65100"
            />
          )}
          <StatTile
            icon={<GroupsOutlined fontSize="small" />}
            label={t('dashboard.companyKpi.activeEmployees')}
            value={String(employeesQuery.data?.totalCount ?? 0)}
            accent="#37474f"
          />
          <StatTile
            icon={<AssignmentTurnedInOutlined fontSize="small" />}
            label={t('dashboard.companyKpi.openWorkItems')}
            value={String(openWorkItemsQuery.data?.totalCount ?? 0)}
            hint={overdueCount > 0 ? t('dashboard.companyKpi.overdue', { count: overdueCount }) : undefined}
            hintColor="warning.main"
            accent="#f9a825"
          />
          <StatTile
            icon={<LocalShippingOutlined fontSize="small" />}
            label={t('dashboard.companyKpi.vehiclesAvailable')}
            value={`${vehiclesAvailable} / ${vehiclesQuery.data?.totalCount ?? 0}`}
            accent="#00897b"
          />
          <StatTile
            icon={<BuildOutlined fontSize="small" />}
            label={t('dashboard.companyKpi.toolsAvailable')}
            value={`${toolsAvailable} / ${toolsQuery.data?.totalCount ?? 0}`}
            accent="#8d6e63"
          />
        </Box>

        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', rowGap: 1, flexShrink: 0 }}>
          {links.map((link) => (
            <Chip
              key={link.to}
              component={RouterLink}
              to={link.to}
              clickable
              size="small"
              label={link.label}
              variant="outlined"
            />
          ))}
        </Stack>
      </Stack>
    </WidgetShell>
  );
}
