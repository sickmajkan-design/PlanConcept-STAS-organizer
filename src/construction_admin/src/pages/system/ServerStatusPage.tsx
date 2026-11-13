import RefreshOutlined from '@mui/icons-material/RefreshOutlined';
import { Alert, Box, Card, CardContent, Chip, Grid, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import type { ReactNode } from 'react';

import { systemApi, type ServerStatus } from '../../api/system';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { formatDuration } from '../../features/system/formatDuration';
import { formatSize } from '../../features/system/formatSize';
import { serverAlerts, worstTone, type AlertTone } from '../../features/system/serverStatusAlerts';
import { StorageCard } from '../../features/system/StorageCard';

/** How often the page asks again. Short enough to watch a deploy or a spike, long enough to cost nothing. */
const REFRESH_MS = 30_000;

function Row({ label, value, tone }: { label: string; value: ReactNode; tone?: AlertTone }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2, py: 0.5 }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography
        variant="body2"
        component="div"
        sx={{ fontWeight: 500, textAlign: 'right', color: tone ? `${tone}.main` : undefined }}
      >
        {value}
      </Typography>
    </Stack>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          {title}
        </Typography>
        {children}
      </CardContent>
    </Card>
  );
}

function okOrNot(ok: boolean, t: ReturnType<typeof useT>) {
  return <Chip size="small" color={ok ? 'success' : 'error'} label={ok ? t('serverStatus.up') : t('serverStatus.down')} />;
}

export function ServerStatusPage() {
  const t = useT();
  const { data, error, refetch, isFetching, dataUpdatedAt } = useQuery({
    queryKey: ['system', 'status'] as const,
    queryFn: systemApi.status,
    refetchInterval: REFRESH_MS,
  });

  const alerts = data ? serverAlerts(data) : [];
  const tone = worstTone(alerts);

  return (
    <Box>
      <PageHeader
        title={t('serverStatus.title')}
        description={t('serverStatus.description')}
        secondaryActions={
          <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
            {dataUpdatedAt > 0 && (
              <Typography variant="caption" color="text.secondary" data-testid="status-updated">
                {t('serverStatus.updated', { time: formatDateTime(new Date(dataUpdatedAt).toISOString()), seconds: REFRESH_MS / 1000 })}
              </Typography>
            )}
            <Tooltip title={t('serverStatus.refresh')}>
              <span>
                <IconButton size="small" onClick={() => void refetch()} disabled={isFetching} aria-label={t('serverStatus.refresh')}>
                  <RefreshOutlined fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
          </Stack>
        }
      />

      {error && !data && <ErrorState error={error} onRetry={() => void refetch()} />}

      {data && (
        <>
          <Alert severity={tone ?? 'success'} sx={{ mb: 2 }} data-testid="status-summary">
            {tone === null ? (
              t('serverStatus.allWell')
            ) : (
              <Stack component="ul" sx={{ m: 0, pl: 2 }}>
                {alerts.map((alert) => (
                  <li key={alert.key}>{t(`serverStatus.alert.${alert.key}`)}</li>
                ))}
              </Stack>
            )}
          </Alert>

          <StorageCard />

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 6 }}>
              <HostSection status={data} />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <ApplicationSection status={data} />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <DatabaseSection status={data} />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <ServicesSection status={data} />
            </Grid>
          </Grid>
        </>
      )}
    </Box>
  );
}

function HostSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const host = status.host;
  const unknown = t('serverStatus.unknown');

  const memoryFree = host.memoryTotalBytes && host.memoryAvailableBytes != null ? host.memoryAvailableBytes / host.memoryTotalBytes : null;
  const swapUsed = host.swapTotalBytes != null && host.swapFreeBytes != null ? host.swapTotalBytes - host.swapFreeBytes : null;
  const load = host.load1 != null && host.load5 != null && host.load15 != null
    ? `${host.load1.toFixed(2)} / ${host.load5.toFixed(2)} / ${host.load15.toFixed(2)}`
    : unknown;

  return (
    <Section title={t('serverStatus.host')}>
      <Row label={t('serverStatus.os')} value={host.os} />
      <Row label={t('serverStatus.uptime')} value={host.uptimeSeconds != null ? formatDuration(host.uptimeSeconds) : unknown} />
      <Row label={t('serverStatus.cores')} value={host.cores} />
      <Row
        label={t('serverStatus.load')}
        value={load}
        tone={host.load5 != null && host.load5 > host.cores * 2 ? 'error' : host.load5 != null && host.load5 > host.cores ? 'warning' : undefined}
      />
      <Row
        label={t('serverStatus.memory')}
        value={
          host.memoryTotalBytes && host.memoryAvailableBytes != null
            ? t('serverStatus.memoryValue', {
                available: formatSize(host.memoryAvailableBytes),
                total: formatSize(host.memoryTotalBytes),
                percent: Math.round((memoryFree ?? 0) * 100),
              })
            : unknown
        }
        tone={memoryFree != null && memoryFree < 0.07 ? 'error' : memoryFree != null && memoryFree < 0.15 ? 'warning' : undefined}
      />
      <Row
        label={t('serverStatus.swap')}
        value={
          host.swapTotalBytes ? t('serverStatus.swapValue', { used: formatSize(swapUsed ?? 0), total: formatSize(host.swapTotalBytes) }) : t('serverStatus.noSwap')
        }
      />
      {!host.known && (
        <Typography variant="caption" color="text.secondary">
          {t('serverStatus.hostUnknown')}
        </Typography>
      )}
    </Section>
  );
}

function ApplicationSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const app = status.application;

  return (
    <Section title={t('serverStatus.application')}>
      <Row label={t('serverStatus.version')} value={app.version} />
      <Row label={t('serverStatus.environment')} value={app.environment} />
      <Row label={t('serverStatus.runtime')} value={app.runtime} />
      <Row label={t('serverStatus.startedAt')} value={formatDateTime(app.startedAtUtc)} />
      <Row label={t('serverStatus.uptime')} value={formatDuration(app.uptimeSeconds)} />
      <Row label={t('serverStatus.processMemory')} value={formatSize(app.processMemoryBytes)} />
      <Row label={t('serverStatus.heap')} value={formatSize(app.managedHeapBytes)} />
      <Row label={t('serverStatus.threads')} value={app.threads} />
    </Section>
  );
}

function DatabaseSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const db = status.database;
  const used = db.maxConnections > 0 ? db.connections / db.maxConnections : 0;

  return (
    <Section title={t('serverStatus.database')}>
      <Row label={t('serverStatus.status')} value={okOrNot(db.reachable, t)} />
      {db.reachable && (
        <>
          <Row label={t('serverStatus.version')} value={db.version} />
          <Row label={t('serverStatus.size')} value={formatSize(db.sizeBytes)} />
          <Row
            label={t('serverStatus.connections')}
            value={t('serverStatus.connectionsValue', { used: db.connections, max: db.maxConnections, active: db.activeConnections })}
            tone={used > 0.9 ? 'error' : used > 0.7 ? 'warning' : undefined}
          />
          <Row label={t('serverStatus.cacheHit')} value={db.cacheHitPercent != null ? `${db.cacheHitPercent}%` : t('serverStatus.unknown')} />
          {db.largestTables.length > 0 && (
            <Box sx={{ mt: 1 }}>
              <Typography variant="caption" color="text.secondary">
                {t('serverStatus.largestTables')}
              </Typography>
              {db.largestTables.map((table) => (
                <Row key={table.name} label={table.name} value={formatSize(table.bytes)} />
              ))}
            </Box>
          )}
        </>
      )}
    </Section>
  );
}

function ServicesSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const services = status.services;

  return (
    <Section title={t('serverStatus.services')}>
      <Row label={t('serverStatus.database')} value={okOrNot(status.database.reachable, t)} />
      <Row
        label={t('serverStatus.virusScanner')}
        value={
          services.virusScannerEnabled ? okOrNot(services.virusScannerReachable, t) : <Chip size="small" label={t('serverStatus.off')} />
        }
      />
      <Row
        label={t('serverStatus.fileStorage')}
        value={services.fileStorage === 'objectStorage' ? t('serverStatus.objectStorage') : t('serverStatus.disk')}
      />
      <Row label={t('serverStatus.documents')} value={t('serverStatus.documentsValue', { count: status.storage.documentsCount, size: formatSize(status.storage.documentsBytes) })} />
      <Row label={t('serverStatus.outboxPending')} value={services.outboxPending} />
      <Row
        label={t('serverStatus.outboxAbandoned')}
        value={services.outboxAbandoned}
        tone={services.outboxAbandoned > 0 ? 'error' : undefined}
      />
    </Section>
  );
}
