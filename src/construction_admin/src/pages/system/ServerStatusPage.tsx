import RefreshOutlined from '@mui/icons-material/RefreshOutlined';
import { Alert, Box, Button, Card, CardContent, Chip, Grid, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';

import { systemApi, type ServerStatus } from '../../api/system';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { formatDuration } from '../../features/system/formatDuration';
import { formatSize } from '../../features/system/formatSize';
import { serverAlerts, worstTone, type AlertTone } from '../../features/system/serverStatusAlerts';
import { ServerAlertsCard } from '../../features/system/ServerAlertsCard';
import { ServerErrorsCard } from '../../features/system/ServerErrorsCard';
import { ServerMetricsCard } from '../../features/system/ServerMetricsCard';
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
              <UptimeSection status={data} />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <BackupsSection status={data} />
            </Grid>
            <Grid size={12}>
              <DockerSection status={data} />
            </Grid>
            {data.docker.releases.length > 0 && (
              <Grid size={12}>
                <ReleasesSection status={data} />
              </Grid>
            )}
            {data.docker.certificate && (
              <Grid size={12}>
                <CertificateSection status={data} />
              </Grid>
            )}
            <Grid size={12}>
              <ServerMetricsCard />
            </Grid>
            <Grid size={12}>
              <ServerErrorsCard />
            </Grid>
            <Grid size={12}>
              <ServerAlertsCard />
            </Grid>
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

const ROLLBACK_SCRIPT = '/opt/construction/scripts/rollback.sh';

function ReleasesSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const [copied, setCopied] = useState<string | null>(null);
  const { releases } = status.docker;
  const running = releases[0];

  if (!running) {
    return null;
  }

  const copy = (commit: string) => {
    // Needs a secure page and a click; when the browser refuses, the command is still on screen to select.
    void navigator.clipboard?.writeText(`${ROLLBACK_SCRIPT} ${commit}`).then(
      () => setCopied(commit),
      () => setCopied(null),
    );
  };

  return (
    <Section title={t('serverStatus.releases')}>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {t('serverStatus.releasesHint')}
      </Typography>
      {releases.map((release, index) => {
        const newer = running.migrations - release.migrations;

        return (
          <Box key={release.commit} sx={{ py: 0.75, borderTop: index === 0 ? 0 : 1, borderColor: 'divider' }}>
            <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
              <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 500 }}>
                {release.commit.slice(0, 7)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {formatDateTime(release.seenAtUtc)}
              </Typography>
              <Chip size="small" color={index === 0 ? 'success' : 'default'} label={index === 0 ? t('serverStatus.releaseActive') : t('serverStatus.releaseReady')} />
              {index > 0 && (
                <Button size="small" onClick={() => copy(release.commit)}>
                  {copied === release.commit ? t('serverStatus.releaseCopied') : t('serverStatus.releaseCopy')}
                </Button>
              )}
            </Stack>
            {index > 0 && newer > 0 && (
              <Typography variant="caption" color="warning.main" component="div">
                {t('serverStatus.releaseMigrations', { count: newer })}
              </Typography>
            )}
          </Box>
        );
      })}
      <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 1 }}>
        {t('serverStatus.releaseBack')} <code>{ROLLBACK_SCRIPT} latest</code>
      </Typography>
    </Section>
  );
}

function CertificateSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const certificate = status.docker.certificate;

  if (!certificate) {
    return null;
  }

  const days = Math.floor((Date.parse(certificate.notAfterUtc) - Date.now()) / 86_400_000);

  return (
    <Section title={t('serverStatus.certificate')}>
      <Row
        label={t('serverStatus.certificateExpires')}
        value={`${formatDateTime(certificate.notAfterUtc)} · ${days < 0 ? t('serverStatus.certificateExpired') : t('serverStatus.certificateDays', { days })}`}
        tone={days < 7 ? 'error' : days < 14 ? 'warning' : undefined}
      />
      {certificate.issuer && <Row label={t('serverStatus.certificateIssuer')} value={certificate.issuer} />}
    </Section>
  );
}

function DockerSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const { known, updatedAtUtc, containers, images, lastPrune } = status.docker;

  return (
    <Section title={t('serverStatus.docker')}>
      {!known && <Typography variant="body2" color="text.secondary">{t('serverStatus.dockerNone')}</Typography>}
      {known && (
        <>
          {containers.map((container) => (
            <Row
              key={container.name}
              label={`${container.name} · ${container.image}`}
              value={
                <Stack direction="row" sx={{ gap: 1, alignItems: 'center', justifyContent: 'flex-end' }}>
                  {container.status}
                  <Chip size="small" color={container.state === 'running' ? 'success' : 'error'} label={container.state} />
                </Stack>
              }
            />
          ))}
          {images && (
            <Row
              label={t('serverStatus.dockerImages')}
              value={t('serverStatus.dockerImagesValue', { total: images.total, active: images.active, size: images.size, reclaimable: images.reclaimable })}
            />
          )}
          {lastPrune && <Row label={t('serverStatus.dockerPrune')} value={lastPrune} />}
          {updatedAtUtc && (
            <Typography variant="caption" color="text.secondary">
              {t('serverStatus.dockerUpdated', { time: formatDateTime(updatedAtUtc) })}
            </Typography>
          )}
        </>
      )}
    </Section>
  );
}

function UptimeSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const { known, sinceUtc, windowDays, availabilityPercent, downSeconds, outages } = status.uptime;
  // The address an outside monitor (UptimeRobot and the like) should call: it answers 200 while the database is reachable.
  const monitorUrl = `${window.location.origin}/health/ready`;

  return (
    <Section title={t('serverStatus.uptimeTitle')}>
      {!known && <Typography variant="body2" color="text.secondary">{t('serverStatus.uptimeNone')}</Typography>}
      {known && (
        <>
          <Row
            label={t('serverStatus.uptimeAvailability', { days: windowDays })}
            value={`${availabilityPercent.toLocaleString('sr-Latn-RS', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%`}
            tone={availabilityPercent < 99 ? 'warning' : undefined}
          />
          <Row label={t('serverStatus.uptimeDown')} value={downSeconds > 0 ? formatDuration(downSeconds) : t('serverStatus.uptimeNoDown')} />
          {sinceUtc && <Row label={t('serverStatus.uptimeSince')} value={formatDateTime(sinceUtc)} />}
          {outages.length > 0 && (
            <Box sx={{ mt: 1 }}>
              <Typography variant="caption" color="text.secondary">{t('serverStatus.uptimeOutages')}</Typography>
              {outages.map((outage) => (
                <Row key={outage.startedAtUtc} label={formatDateTime(outage.startedAtUtc)} value={formatDuration(outage.seconds)} />
              ))}
            </Box>
          )}
        </>
      )}
      <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 1.5 }}>
        {t('serverStatus.uptimeMonitor')}
      </Typography>
      <Typography variant="body2" sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }} data-testid="monitor-url">
        {monitorUrl}
      </Typography>
    </Section>
  );
}

function BackupsSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const { known, items, restoreCheckedAtUtc } = status.backups;
  const newest = items[0];
  const yes = (ok: boolean) => <Chip size="small" color={ok ? 'success' : 'warning'} label={ok ? t('serverStatus.backupYes') : t('serverStatus.backupNo')} />;

  return (
    <Section title={t('serverStatus.backups')}>
      {!known && <Typography variant="body2" color="text.secondary">{t('serverStatus.backupsHidden')}</Typography>}
      {known && !newest && <Typography variant="body2" color="text.secondary">{t('serverStatus.backupsNone')}</Typography>}
      {known && newest && (
        <>
          <Row label={t('serverStatus.backupLast')} value={`${formatDateTime(newest.takenAtUtc)} · ${formatSize(newest.sizeBytes)}`} />
          <Row label={t('serverStatus.backupOffsite')} value={yes(newest.offsite)} />
          <Row
            label={t('serverStatus.backupRestore')}
            value={restoreCheckedAtUtc ? formatDateTime(restoreCheckedAtUtc) : t('serverStatus.backupNever')}
          />
          <Box sx={{ mt: 1 }}>
            {items.slice(0, 5).map((item) => (
              <Row
                key={item.name}
                label={`${formatDateTime(item.takenAtUtc)}${item.hasDocuments ? '' : ` (${t('serverStatus.backupNoDocs')})`}`}
                value={
                  <Stack direction="row" sx={{ gap: 1, alignItems: 'center', justifyContent: 'flex-end' }}>
                    {formatSize(item.sizeBytes)}
                    {yes(item.offsite)}
                  </Stack>
                }
              />
            ))}
          </Box>
        </>
      )}
    </Section>
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
