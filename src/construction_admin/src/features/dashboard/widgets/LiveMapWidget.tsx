import { useBranchFilter } from '../../branches/BranchContext';
import { Alert, Box, Stack, Typography } from '@mui/material';
import { AdvancedMarker, APIProvider, Map, Pin } from '@vis.gl/react-google-maps';
import { useQuery } from '@tanstack/react-query';

import { locationsApi } from '../../../api/locations';
import { config, hasGoogleMapsKey } from '../../../config';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { MoreLink, Pill } from './widgetUi';

/** A compact preview of the full Live Map page — same query, same marker style, no project filter or click-through info window (that's what "View all" is for). */
export function LiveMapWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const { branchId } = useBranchFilter();

  const { data: page, isLoading, error } = useQuery({
    queryKey: ['dashboard', 'locations', 'current', branchId ?? null] as const,
    queryFn: () => locationsApi.current({ branchId }),
    refetchInterval: config.liveMapRefreshMs,
  });

  const locations = page?.items ?? [];
  // How many the server counted, not how many the page happens to hold.
  const onMap = page?.totalCount ?? locations.length;

  return (
    <WidgetShell title={t('dashboard.widget.LiveMap')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Pill tone={onMap > 0 ? 'accent' : 'neutral'} sx={{ alignSelf: 'flex-start', fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
          {t('dashboard.liveMap.onMap', { count: onMap })}
        </Pill>

        <Box sx={{ flex: 1, minHeight: 160, borderRadius: '16px', overflow: 'hidden', position: 'relative' }}>
          {!hasGoogleMapsKey ? (
            <Alert severity="info">{t('map.noKey')}</Alert>
          ) : locations.length === 0 ? (
            <Typography color="text.secondary" variant="body2" sx={{ p: 2 }}>
              {t('map.empty')}
            </Typography>
          ) : (
            <APIProvider apiKey={config.googleMapsApiKey}>
              <Map
                mapId="dashboard-live-map"
                defaultCenter={{ lat: locations[0].latitude, lng: locations[0].longitude }}
                defaultZoom={11}
                gestureHandling="greedy"
                disableDefaultUI
                style={{ width: '100%', height: '100%' }}
              >
                {locations.map((location) => (
                  <AdvancedMarker key={location.employeeId} position={{ lat: location.latitude, lng: location.longitude }}>
                    <Pin background="#e65100" borderColor="#bf360c" glyphColor="#fff" scale={0.8} />
                  </AdvancedMarker>
                ))}
              </Map>
            </APIProvider>
          )}
        </Box>

        <MoreLink to={paths.map}>{t('common.viewAll')}</MoreLink>
      </Stack>
    </WidgetShell>
  );
}
