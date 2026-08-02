import { PersonPinCircle } from '@mui/icons-material';
import {
  Alert,
  Box,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import {
  AdvancedMarker,
  APIProvider,
  InfoWindow,
  Map,
  Pin,
} from '@vis.gl/react-google-maps';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import { locationsApi } from '../../api/locations';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { config, hasGoogleMapsKey } from '../../config';
import { useFormatRelative } from '../../i18n/useFormatRelative';
import { useT } from '../../i18n/useI18n';
import { canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useAllProjectsQuery } from '../../features/projects/useProjects';


/** Zagreb — a reasonable default center until real fixes are loaded. */
const DEFAULT_CENTER = { lat: 45.815, lng: 15.9819 };

export function LiveMapPage() {
  const { user } = useAuth();
  const t = useT();
  const formatRelative = useFormatRelative();
  const [projectId, setProjectId] = useState('');
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);

  const { data: projects } = useAllProjectsQuery();

  const {
    data: locations,
    isLoading,
    isError,
    error,
    refetch,
    dataUpdatedAt,
  } = useQuery({
    queryKey: ['locations', 'current', projectId],
    queryFn: () => locationsApi.current({ projectId: projectId || undefined }),
    refetchInterval: config.liveMapRefreshMs,
  });

  const selected = locations?.find((l) => l.employeeId === selectedEmployeeId);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 112px)' }}>
      <PageHeader
        title={t('map.title')}
        subtitle={
          dataUpdatedAt
            ? t('map.updated', { when: formatRelative(new Date(dataUpdatedAt).toISOString()) })
            : undefined
        }
      />

      {canViewDirectory(user) && (
        <FormControl size="small" sx={{ minWidth: 240, mb: 2, alignSelf: 'flex-start' }}>
          <InputLabel id="map-project-filter-label">{t('map.project')}</InputLabel>
          <Select
            labelId="map-project-filter-label"
            label={t('map.project')}
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
          >
            <MenuItem value="">
              <em>{t('map.allProjects')}</em>
            </MenuItem>
            {(projects?.items ?? []).map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {project.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      )}

      <Paper sx={{ flex: 1, overflow: 'hidden', position: 'relative' }}>
        {!hasGoogleMapsKey ? (
          <Box sx={{ p: 4 }}>
            <Alert severity="info">
              The live map needs a Google Maps API key. Set{' '}
              <code>VITE_GOOGLE_MAPS_API_KEY</code> in the admin app's environment
              to enable it — see the README for details.
            </Alert>
          </Box>
        ) : isError ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : !isLoading && (locations?.length ?? 0) === 0 ? (
          <EmptyState
            message="No employee locations reported yet."
            icon={PersonPinCircle}
          />
        ) : (
          <APIProvider apiKey={config.googleMapsApiKey}>
            <Map
              mapId="construction-live-map"
              defaultCenter={
                locations?.[0]
                  ? { lat: locations[0].latitude, lng: locations[0].longitude }
                  : DEFAULT_CENTER
              }
              defaultZoom={locations?.length ? 12 : 8}
              gestureHandling="greedy"
              disableDefaultUI={false}
              style={{ width: '100%', height: '100%' }}
            >
              {locations?.map((location) => (
                <AdvancedMarker
                  key={location.employeeId}
                  position={{ lat: location.latitude, lng: location.longitude }}
                  onClick={() => setSelectedEmployeeId(location.employeeId)}
                >
                  <Pin
                    background="#e65100"
                    borderColor="#bf360c"
                    glyphColor="#fff"
                  />
                </AdvancedMarker>
              ))}

              {selected && (
                <InfoWindow
                  position={{ lat: selected.latitude, lng: selected.longitude }}
                  onCloseClick={() => setSelectedEmployeeId(null)}
                >
                  <Stack spacing={0.5} sx={{ minWidth: 160 }}>
                    <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                      {selected.fullName}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {selected.position} · {selected.employeeNumber}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatRelative(selected.timestamp)}
                      {selected.accuracy != null &&
                        ` · ±${Math.round(selected.accuracy)} m`}
                    </Typography>
                  </Stack>
                </InfoWindow>
              )}
            </Map>
          </APIProvider>
        )}
      </Paper>
    </Box>
  );
}
