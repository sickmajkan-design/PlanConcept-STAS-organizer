import InfoOutlined from '@mui/icons-material/InfoOutlined';
import PriorityHighRounded from '@mui/icons-material/PriorityHighRounded';
import RefreshOutlined from '@mui/icons-material/RefreshOutlined';
import TaskAltRounded from '@mui/icons-material/TaskAltRounded';
import { Box, Button, Chip, Grid, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useQuery } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';

import { systemApi, type ServerStatus } from '../../api/system';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { formatDuration } from '../../features/system/formatDuration';
import { formatSize } from '../../features/system/formatSize';
import { serverAlerts, worstTone, type AlertTone } from '../../features/system/serverStatusAlerts';
import { ServerAlertsCard } from '../../features/system/ServerAlertsCard';
import { ServerErrorsCard, ServerErrorsTile } from '../../features/system/ServerErrorsCard';
import { ServerMetricsCard } from '../../features/system/ServerMetricsCard';
import { MetricTiles, StatusHero } from '../../features/system/ServerOverview';
import { containerUptime } from '../../features/system/overviewMath';
import { BigNumber, Panel, Ring, StatusDot } from '../../features/system/serverUi';

/** How often the page asks again. Short enough to watch a deploy or a spike, long enough to cost nothing. */
const REFRESH_MS = 30_000;

/** One line of a list: the label quiet on the left, the value firmer on the right, a hairline above all but the first. */
function Row({ label, value, tone }: { label: string; value: ReactNode; tone?: AlertTone }) {
  return (
    <Stack data-row direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2, py: 1.25 }}>
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
  return <Panel title={title}>{children}</Panel>;
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
        <Stack sx={{ gap: 2 }}>
          <StatusHero status={data} alerts={alerts} tone={tone} />
          <MetricTiles status={data} />

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, lg: 8 }}>
              <ServerMetricsCard />
            </Grid>
            <Grid size={{ xs: 12, lg: 4 }}>
              <BackupsSection status={data} />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <DockerSection status={data} />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <ReleasesSection status={data} />
            </Grid>
            {data.docker.security.length > 0 && (
              <Grid size={{ xs: 12, lg: 8 }}>
                <SecuritySection status={data} />
              </Grid>
            )}
            <Grid size={{ xs: 12, lg: data.docker.security.length > 0 ? 4 : 12 }}>
              <Stack sx={{ gap: 2 }}>
                {data.docker.certificate && <CertificateSection status={data} />}
                <ServerErrorsTile />
              </Stack>
            </Grid>
            <Grid size={12}>
              <ServerErrorsCard />
            </Grid>
            <Grid size={12}>
              <ServerAlertsCard />
            </Grid>
          </Grid>

          <Typography component="h2" sx={{ fontSize: 20, fontWeight: 600, letterSpacing: '-0.02em', mt: 2 }}>
            {t('serverStatus.details')}
          </Typography>
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
            <Grid size={12}>
              <UptimeSection status={data} />
            </Grid>
          </Grid>
        </Stack>
      )}
    </Box>
  );
}

function SecuritySection({ status }: { status: ServerStatus }) {
  const t = useT();
  const { security } = status.docker;
  const needs = security.filter((check) => check.status !== 'ok');
  const fine = security.filter((check) => check.status === 'ok');

  return (
    <Panel title={t('serverSecurity.title')} description={t('serverSecurity.description')}>
      {needs.map((check) => {
        const warn = check.status === 'warn';

        return (
          <Stack key={check.key} data-row direction="row" sx={{ gap: 2, py: 2 }}>
            <Box
              sx={(theme) => ({
                width: 32,
                height: 32,
                borderRadius: '50%',
                flexShrink: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: warn ? alpha(theme.palette.warning.main, 0.16) : alpha(theme.palette.text.primary, 0.06),
                color: warn ? 'warning.main' : 'text.secondary',
              })}
            >
              {warn ? <PriorityHighRounded sx={{ fontSize: 18 }} /> : <InfoOutlined sx={{ fontSize: 18 }} />}
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Typography sx={{ fontSize: 16, fontWeight: 600, letterSpacing: '-0.01em' }}>{t(`serverSecurity.${check.key}.title`)}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25, lineHeight: 1.4, overflowWrap: 'anywhere' }}>
                {t(`serverSecurity.${check.key}.${check.status}` as MessageKey, { value: check.value, extra: check.extra })}
              </Typography>
              {warn && (
                <Box
                  sx={(theme) => ({
                    mt: 1,
                    display: 'inline-block',
                    maxWidth: '100%',
                    boxSizing: 'border-box',
                    fontSize: 13,
                    fontFamily: 'ui-monospace, "SF Mono", Menlo, monospace',
                    bgcolor: alpha(theme.palette.text.primary, 0.06),
                    borderRadius: '10px',
                    px: 1.5,
                    py: 1,
                    overflowWrap: 'anywhere',
                  })}
                >
                  {t(`serverSecurity.${check.key}.fix` as MessageKey)}
                </Box>
              )}
            </Box>
          </Stack>
        );
      })}
      {fine.length > 0 && (
        <Box data-row sx={{ pt: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            {t('serverSecurity.fineSummary', { count: fine.length })}
          </Typography>
          <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
            {fine.map((check) => (
              <Chip
                key={check.key}
                size="small"
                variant="outlined"
                icon={<TaskAltRounded sx={{ fontSize: 16 }} />}
                label={t(`serverSecurity.${check.key}.title`)}
                sx={{ '& .MuiChip-icon': { color: 'success.main' } }}
              />
            ))}
          </Stack>
        </Box>
      )}
    </Panel>
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
    <Panel title={t('serverStatus.releases')} description={t('serverStatus.releasesHint')}>
      {releases.map((release, index) => {
        const newer = running.migrations - release.migrations;

        return (
          <Box key={release.commit} data-row sx={{ py: 1.5 }}>
            <Stack direction="row" sx={{ gap: 1.5, alignItems: 'center', flexWrap: 'wrap' }}>
              <Typography sx={{ fontSize: 16, fontWeight: 600, fontFamily: 'ui-monospace, "SF Mono", Menlo, monospace' }}>
                {release.commit.slice(0, 7)}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ flexGrow: 1 }}>
                {formatDateTime(release.seenAtUtc)}
              </Typography>
              {index === 0 ? (
                <Chip size="small" color="success" label={t('serverStatus.releaseActive')} />
              ) : (
                <Button size="small" variant="outlined" onClick={() => copy(release.commit)} sx={{ borderRadius: 999, px: 2 }}>
                  {copied === release.commit ? t('serverStatus.releaseCopied') : t('serverStatus.releaseCopy')}
                </Button>
              )}
            </Stack>
            {index > 0 && newer > 0 && (
              <Typography variant="caption" color="warning.main" component="div" sx={{ mt: 0.5 }}>
                {t('serverStatus.releaseMigrations', { count: newer })}
              </Typography>
            )}
          </Box>
        );
      })}
      <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 1.5, mb: 0 }}>
        {t('serverStatus.releaseBack')} <code>{ROLLBACK_SCRIPT} latest</code>
      </Typography>
    </Panel>
  );
}

function CertificateSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const certificate = status.docker.certificate;

  if (!certificate) {
    return null;
  }

  const days = Math.floor((Date.parse(certificate.notAfterUtc) - Date.now()) / 86_400_000);
  const tone = days < 7 ? 'error' : days < 14 ? 'warning' : 'success';

  return (
    <Panel fill={false}>
      <Stack direction="row" sx={{ alignItems: 'center', gap: 2.5 }}>
        <Ring percent={Math.max(0, Math.min(100, (days / 90) * 100))} tone={tone} label={t('serverStatus.certificate')} size={84} />
        <Box sx={{ minWidth: 0 }}>
          <Typography sx={{ fontSize: 15, fontWeight: 600, color: 'text.secondary' }}>{t('serverStatus.certificate')}</Typography>
          <Box sx={{ mt: 0.5 }}>
            <BigNumber size={34}>{days < 0 ? t('serverStatus.certificateExpired') : `${days} ${t('serverStatus.daysShort')}`}</BigNumber>
          </Box>
          <Typography variant="caption" color="text.secondary" component="div" sx={{ mt: 0.75 }}>
            {t('serverStatus.certificateUntil', { date: formatDateTime(certificate.notAfterUtc) })}
            {certificate.issuer ? ` · ${certificate.issuer}` : ''}
          </Typography>
        </Box>
      </Stack>
    </Panel>
  );
}

/** The part of a container's name a person recognises: `construction-api-1` is `api`. */
function shortName(name: string): string {
  return name.replace(/^construction[-_]/, '').replace(/[-_]\d+$/, '');
}

function DockerSection({ status }: { status: ServerStatus }) {
  const t = useT();
  const { known, updatedAtUtc, containers, images, lastPrune } = status.docker;

  return (
    <Panel title={t('serverStatus.docker')}>
      {!known && (
        <Typography variant="body2" color="text.secondary">
          {t('serverStatus.dockerNone')}
        </Typography>
      )}
      {known && (
        <>
          {containers.map((container) => (
            <Stack key={container.name} data-row direction="row" sx={{ alignItems: 'center', gap: 1.5, py: 1.5 }}>
              <StatusDot tone={container.state === 'running' ? 'success' : 'error'} size={10} />
              <Typography sx={{ fontSize: 16, fontWeight: 500, flexGrow: 1, minWidth: 0 }}>{shortName(container.name)}</Typography>
              <Typography variant="caption" sx={{ fontFamily: 'ui-monospace, "SF Mono", Menlo, monospace', color: 'text.secondary' }}>
                {container.image.split(':').pop()?.slice(0, 12)}
              </Typography>
              <Typography variant="body2" color={container.state === 'running' ? 'text.secondary' : 'error.main'} sx={{ minWidth: 70, textAlign: 'right' }}>
                {container.state === 'running' ? containerUptime(container.status) : container.state}
              </Typography>
            </Stack>
          ))}
          <Box sx={{ mt: 1.5 }}>
            {images && (
              <Typography variant="caption" color="text.secondary" component="div">
                {t('serverStatus.dockerImagesValue', { total: images.total, active: images.active, size: images.size, reclaimable: images.reclaimable })}
              </Typography>
            )}
            {lastPrune && (
              <Typography variant="caption" color="text.secondary" component="div">
                {t('serverStatus.dockerPrune')}: {lastPrune}
              </Typography>
            )}
            {updatedAtUtc && (
              <Typography variant="caption" color="text.secondary" component="div">
                {t('serverStatus.dockerUpdated', { time: formatDateTime(updatedAtUtc) })}
              </Typography>
            )}
          </Box>
        </>
      )}
    </Panel>
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

  return (
    <Panel title={t('serverStatus.backups')}>
      {!known && <Typography variant="body2" color="text.secondary">{t('serverStatus.backupsHidden')}</Typography>}
      {known && !newest && <Typography variant="body2" color="text.secondary">{t('serverStatus.backupsNone')}</Typography>}
      {known && newest && (
        <>
          <Box sx={{ mb: 1.5 }}>
            <BigNumber size={28}>{formatDateTime(newest.takenAtUtc)}</BigNumber>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
              {t('serverStatus.backupLast')} · {formatSize(newest.sizeBytes)}
            </Typography>
          </Box>
          <Stack data-row direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2, py: 1.25 }}>
            <Typography variant="body2" color="text.secondary">
              {t('serverStatus.backupOffsite')}
            </Typography>
            <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
              <StatusDot tone={newest.offsite ? 'success' : 'warning'} />
              <Typography variant="body2" sx={{ fontWeight: 500, color: newest.offsite ? 'text.primary' : 'warning.main' }}>
                {newest.offsite ? t('serverStatus.backupYes') : t('serverStatus.backupServerOnly')}
              </Typography>
            </Stack>
          </Stack>
          <Row
            label={t('serverStatus.backupRestore')}
            value={restoreCheckedAtUtc ? formatDateTime(restoreCheckedAtUtc) : t('serverStatus.backupNever')}
          />
          <Box sx={{ mt: 1.5 }}>
            {items.slice(1, 4).map((item) => (
              <Typography key={item.name} variant="caption" color="text.secondary" component="div">
                {formatDateTime(item.takenAtUtc)} · {formatSize(item.sizeBytes)}
                {item.hasDocuments ? '' : ` (${t('serverStatus.backupNoDocs')})`}
                {item.offsite ? '' : ` · ${t('serverStatus.backupServerOnly')}`}
              </Typography>
            ))}
          </Box>
        </>
      )}
    </Panel>
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
