import { Box, Button, Stack, Typography, useTheme, type Theme } from '@mui/material';
import { useQueries } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { toolsApi } from '../../../api/tools';
import type { ToolStatus, VehicleStatus } from '../../../api/types';
import { vehiclesApi } from '../../../api/vehicles';
import { useI18n } from '../../../i18n/useI18n';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { paths } from '../../../routes/paths';
import { useCounts, useUnitParams } from '../dashboardData';
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

const VEHICLE_STATUS_ORDER: VehicleStatus[] = ['Available', 'Assigned', 'InService', 'OutOfService', 'RentedOut'];

const TOOL_STATUS_ORDER: ToolStatus[] = ['Available', 'Assigned', 'UnderRepair', 'Lost', 'Retired', 'RentedOut'];

const ONE = { pageNumber: 1, pageSize: 1 } as const;

export function FleetStatusWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t } = useI18n();
  const enumLabel = useEnumLabel();
  const theme = useTheme();

  const unit = useUnitParams();

  // Counts the server made, one per status: a fleet over a hundred is still counted exactly.
  const vehicleLoaders = Object.fromEntries([
    ['all', (u: typeof unit) => vehiclesApi.list({ ...ONE, ...u })],
    ...VEHICLE_STATUS_ORDER.map((status) => [status, (u: typeof unit) => vehiclesApi.list({ ...ONE, status, ...u })]),
  ]);
  const toolLoaders = Object.fromEntries([
    ['all', (u: typeof unit) => toolsApi.list({ ...ONE, ...u })],
    ...TOOL_STATUS_ORDER.map((status) => [status, (u: typeof unit) => toolsApi.list({ ...ONE, status, ...u })]),
  ]);
  const vehicleStats = useCounts('fleet-vehicles', vehicleLoaders);
  const toolStats = useCounts('fleet-tools', toolLoaders);

  const vehicleTotal = vehicleStats.counts.all ?? 0;
  const toolTotal = toolStats.counts.all ?? 0;
  const vehicleCounts = VEHICLE_STATUS_ORDER.map((status) => ({ status, count: vehicleStats.counts[status] ?? 0 }));
  const toolCounts = TOOL_STATUS_ORDER.map((status) => ({ status, count: toolStats.counts[status] ?? 0 }));

  // The ones that need a look are listed by name: only what is in those statuses is asked for, and only when there is any.
  const attentionVehicles = VEHICLE_ATTENTION.filter((status) => (vehicleStats.counts[status] ?? 0) > 0);
  const attentionTools = TOOL_ATTENTION.filter((status) => (toolStats.counts[status] ?? 0) > 0);
  const attentionQueries = useQueries({
    queries: [
      ...attentionVehicles.map((status) => ({
        queryKey: ['dashboard', 'fleet', 'attention', 'vehicles', status, unit.branchId ?? null] as const,
        queryFn: async () =>
          (await vehiclesApi.list({ pageNumber: 1, pageSize: FLEET_PAGE_SIZE, status, ...unit })).items.map((v) => ({
            id: v.id,
            label: `${v.brand} ${v.model} (${v.registrationNumber})`,
            detail: enumLabel('vehicleStatus', v.status),
          })),
      })),
      ...attentionTools.map((status) => ({
        queryKey: ['dashboard', 'fleet', 'attention', 'tools', status, unit.branchId ?? null] as const,
        queryFn: async () =>
          (await toolsApi.list({ pageNumber: 1, pageSize: FLEET_PAGE_SIZE, status, ...unit })).items.map((tool) => ({
            id: tool.id,
            label: tool.name,
            detail: enumLabel('toolStatus', tool.status),
          })),
      })),
    ],
  });
  const attention = attentionQueries.flatMap((query) => query.data ?? []);

  const isLoading = vehicleStats.isLoading || toolStats.isLoading;
  const error = vehicleStats.error ?? toolStats.error;

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
              {t('dashboard.fleet.vehicles')} · {vehicleTotal}
            </Typography>
            {vehicleTotal === 0 ? (
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
              {t('dashboard.fleet.tools')} · {toolTotal}
            </Typography>
            {toolTotal === 0 ? (
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
