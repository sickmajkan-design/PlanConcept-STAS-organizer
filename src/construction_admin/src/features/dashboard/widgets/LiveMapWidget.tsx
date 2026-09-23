import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material';
import { AdvancedMarker, APIProvider, Map, Pin } from '@vis.gl/react-google-maps';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { locationsApi } from '../../../api/locations';
import { config, hasGoogleMapsKey } from '../../../config';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

/** A compact preview of the full Live Map page — same query, same marker style, no project filter or click-through info window (that's what "View all" is for). */
export function LiveMapWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();

  const { data: page, isLoading, error } = useQuery({
    queryKey: ['dashboard', 'locations', 'current'] as const,
    queryFn: () => locationsApi.current({}),
    refetchInterval: config.liveMapRefreshMs,
  });

  const locations = page?.items ?? [];

  return (
    <WidgetShell title={t('dashboard.widget.LiveMap')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Chip
          size="small"
          color={locations.length > 0 ? 'primary' : 'default'}
          label={t('dashboard.liveMap.onMap', { count: locations.length })}
          sx={{ fontWeight: 700, alignSelf: 'flex-start', flexShrink: 0 }}
        />

        <Box sx={{ flex: 1, minHeight: 160, borderRadius: 2, overflow: 'hidden', position: 'relative' }}>
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

        <Box sx={{ flexShrink: 0 }}>
          <Button component={Link} to={paths.map} size="small">
            {t('common.viewAll')}
          </Button>
        </Box>
      </Stack>
    </WidgetShell>
  );
}
