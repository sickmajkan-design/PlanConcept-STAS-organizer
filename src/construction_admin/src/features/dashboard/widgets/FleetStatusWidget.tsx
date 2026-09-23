import { Box, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { toolsApi } from '../../../api/tools';
import type { Tool, ToolStatus, Vehicle, VehicleStatus } from '../../../api/types';
import { vehiclesApi } from '../../../api/vehicles';
import { useI18n } from '../../../i18n/useI18n';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { paths } from '../../../routes/paths';
import { chartPalette } from '../../../theme';
import { useElementSize } from '../useElementSize';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

// 100 is GetVehiclesQuery/GetToolsQuery's own max page size — a bigger request 400s.
const FLEET_PAGE_SIZE = 100;

/** Statuses worth calling out as a short "needs attention" list. */
const VEHICLE_ATTENTION: VehicleStatus[] = ['OutOfService'];
const TOOL_ATTENTION: ToolStatus[] = ['UnderRepair', 'Lost'];

function countByStatus<TStatus extends string>(
  items: { status: TStatus }[],
  order: readonly TStatus[],
): { status: TStatus; count: number }[] {
  const counts = new Map<TStatus, number>();
  for (const item of items) {
    counts.set(item.status, (counts.get(item.status) ?? 0) + 1);
  }
  return order.map((status) => ({ status, count: counts.get(status) ?? 0 }));
}

const VEHICLE_STATUS_ORDER: VehicleStatus[] = ['Available', 'Assigned', 'InService', 'OutOfService', 'RentedOut'];

const TOOL_STATUS_ORDER: ToolStatus[] = ['Available', 'Assigned', 'UnderRepair', 'Lost', 'Retired', 'RentedOut'];

export function FleetStatusWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t } = useI18n();
  const enumLabel = useEnumLabel();
  const vehicleChartSize = useElementSize<HTMLDivElement>();
  const toolChartSize = useElementSize<HTMLDivElement>();

  const vehiclesQuery = useQuery({
    queryKey: ['dashboard', 'fleet', 'vehicles'] as const,
    queryFn: () => vehiclesApi.list({ pageNumber: 1, pageSize: FLEET_PAGE_SIZE }),
  });

  const toolsQuery = useQuery({
    queryKey: ['dashboard', 'fleet', 'tools'] as const,
    queryFn: () => toolsApi.list({ pageNumber: 1, pageSize: FLEET_PAGE_SIZE }),
  });

  const isLoading = vehiclesQuery.isLoading || toolsQuery.isLoading;
  const error = vehiclesQuery.error ?? toolsQuery.error;

  const vehicles: Vehicle[] = vehiclesQuery.data?.items ?? [];
  const tools: Tool[] = toolsQuery.data?.items ?? [];

  const vehicleCounts = countByStatus(vehicles, VEHICLE_STATUS_ORDER);
  const toolCounts = countByStatus(tools, TOOL_STATUS_ORDER);

  const attention = [
    ...vehicles
      .filter((v) => VEHICLE_ATTENTION.includes(v.status))
      .map((v) => ({
        id: v.id,
        label: `${v.brand} ${v.model} (${v.registrationNumber})`,
        detail: enumLabel('vehicleStatus', v.status),
      })),
    ...tools
      .filter((tool) => TOOL_ATTENTION.includes(tool.status))
      .map((tool) => ({
        id: tool.id,
        label: tool.name,
        detail: enumLabel('toolStatus', tool.status),
      })),
  ];

  return (
    <WidgetShell title={t('dashboard.widget.FleetStatus')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={2} sx={{ flex: 1, minHeight: 0 }}>
        <Box sx={{ flex: 1, minHeight: 150, display: 'flex', flexDirection: 'column' }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5, flexShrink: 0 }}>
            {t('dashboard.fleet.vehicles')}
          </Typography>
          <Box ref={vehicleChartSize.ref} sx={{ flex: 1, minHeight: 0 }}>
            {vehicleChartSize.width > 0 && vehicleChartSize.height > 0 && (
              <BarChart
                width={vehicleChartSize.width}
                height={vehicleChartSize.height}
                colors={chartPalette}
                series={[{ data: vehicleCounts.map((c) => c.count), label: t('dashboard.fleet.vehicles') }]}
                xAxis={[{ scaleType: 'band', data: vehicleCounts.map((c) => enumLabel('vehicleStatus', c.status)) }]}
                margin={{ top: 10, bottom: 40, left: 32, right: 10 }}
                borderRadius={6}
                hideLegend
              />
            )}
          </Box>
        </Box>

        <Box sx={{ flex: 1, minHeight: 150, display: 'flex', flexDirection: 'column' }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5, flexShrink: 0 }}>
            {t('dashboard.fleet.tools')}
          </Typography>
          <Box ref={toolChartSize.ref} sx={{ flex: 1, minHeight: 0 }}>
            {toolChartSize.width > 0 && toolChartSize.height > 0 && (
              <BarChart
                width={toolChartSize.width}
                height={toolChartSize.height}
                colors={chartPalette}
                series={[{ data: toolCounts.map((c) => c.count), label: t('dashboard.fleet.tools') }]}
                xAxis={[{ scaleType: 'band', data: toolCounts.map((c) => enumLabel('toolStatus', c.status)) }]}
                margin={{ top: 10, bottom: 40, left: 32, right: 10 }}
                borderRadius={6}
                hideLegend
              />
            )}
          </Box>
        </Box>

        {attention.length > 0 && (
          <List dense disablePadding sx={{ flexShrink: 0 }}>
            {attention.map((item) => (
              <ListItem key={item.id} disableGutters>
                <ListItemText
                  primary={item.label}
                  secondary={item.detail}
                  slotProps={{ primary: { sx: { fontWeight: 600 } } }}
                />
              </ListItem>
            ))}
          </List>
        )}

        <Stack direction="row" spacing={2} sx={{ flexShrink: 0 }}>
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
