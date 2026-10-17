import { Box, Button, Stack, Typography, useTheme, type Theme } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { toolsApi } from '../../../api/tools';
import type { Tool, ToolStatus, Vehicle, VehicleStatus } from '../../../api/types';
import { vehiclesApi } from '../../../api/vehicles';
import { useI18n } from '../../../i18n/useI18n';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { InsetList, InsetRow, MoreLink, Segments } from './widgetUi';

/** One colour per kind of status, shared by vehicles and tools: calm for what is in use, amber and red for what is not. */
function statusColor(theme: Theme, status: string): string {
  switch (status) {
    case 'Available':
      return theme.palette.success.main;
    case 'Assigned':
    case 'RentedOut':
      return theme.palette.primary.main;
    case 'InService':
    case 'UnderRepair':
      return theme.palette.warning.main;
    case 'OutOfService':
    case 'Lost':
      return theme.palette.error.main;
    default:
      return theme.palette.text.disabled;
  }
}

// 100 is GetVehiclesQuery/GetToolsQuery's own max page size — a bigger request 400s.
const FLEET_PAGE_SIZE = 100;

/** An axis with nothing on it says less than a sentence and the way to the first record. */
function EmptyChart({ text, to, action }: { text: string; to: string; action: string }) {
  return (
    <Stack spacing={1} sx={{ alignItems: 'flex-start', py: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {text}
      </Typography>
      <Button component={Link} to={to} size="small" variant="outlined" color="inherit" sx={{ borderColor: 'divider' }}>
        {action}
      </Button>
    </Stack>
  );
}

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
  const theme = useTheme();

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
      <Stack spacing={2.5} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
        <Box
          sx={{
            display: 'grid',
            gap: 3,
            alignContent: 'start',
            gridTemplateColumns: '1fr',
            // Vehicles and tools side by side once the widget is wide enough for both.
            '@container widget (min-width: 640px)': { gridTemplateColumns: '1fr 1fr' },
          }}
        >
          <Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {t('dashboard.fleet.vehicles')} · {vehicles.length}
            </Typography>
            {vehicles.length === 0 ? (
              <EmptyChart text={t('dashboard.fleet.noVehicles')} to={paths.vehicleNew} action={t('vehicles.add')} />
            ) : (
              <Segments
                parts={vehicleCounts
                  .filter((c) => c.count > 0)
                  .map((c) => ({ label: enumLabel('vehicleStatus', c.status), value: c.count, color: statusColor(theme, c.status) }))}
              />
            )}
          </Box>
          <Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {t('dashboard.fleet.tools')} · {tools.length}
            </Typography>
            {tools.length === 0 ? (
              <EmptyChart text={t('dashboard.fleet.noTools')} to={paths.toolNew} action={t('tools.add')} />
            ) : (
              <Segments
                parts={toolCounts
                  .filter((c) => c.count > 0)
                  .map((c) => ({ label: enumLabel('toolStatus', c.status), value: c.count, color: statusColor(theme, c.status) }))}
              />
            )}
          </Box>
        </Box>

        {attention.length > 0 && (
          <InsetList>
            {attention.map((item) => (
              <InsetRow key={item.id} tone="bad" primary={item.label} secondary={item.detail} />
            ))}
          </InsetList>
        )}

        <Stack direction="row" spacing={2} sx={{ flexShrink: 0 }}>
          <MoreLink to={paths.vehicles}>{t('nav.vehicles')}</MoreLink>
          <MoreLink to={paths.tools}>{t('nav.tools')}</MoreLink>
        </Stack>
      </Stack>
    </WidgetShell>
  );
}
