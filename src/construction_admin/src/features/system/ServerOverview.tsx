import CheckRounded from '@mui/icons-material/CheckRounded';
import PriorityHighRounded from '@mui/icons-material/PriorityHighRounded';
import { Box, Button, Stack, Typography, useMediaQuery, type Theme } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useQuery } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';

import { systemApi, type ServerStatus } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDuration } from './formatDuration';
import { formatSize } from './formatSize';
import type { ServerAlert } from './serverStatusAlerts';
import { cpuPercent, memoryUsedPercent, outageDays, toneAt } from './overviewMath';
import { BigNumber, PANEL_RADIUS, Panel, Ring, StatusDot, type Tone } from './serverUi';
import { StorageCleanupDialog } from './StorageCleanupDialog';

/**
 * The answer to "is the server all right?" before any detail: one line, and a chip for each thing that can be down. The
 * summary element keeps its test id because it is also what a screen reader and the tests read first.
 */
export function StatusHero({ status, alerts, tone }: { status: ServerStatus; alerts: ServerAlert[]; tone: 'warning' | 'error' | null }) {
  const t = useT();
  const ok = tone === null;
  const accent: Tone = tone ?? 'success';
  const { services, database, docker } = status;

  const chips: { key: string; label: string; tone: Tone }[] = [
    { key: 'db', label: t('serverStatus.database'), tone: database.reachable ? 'success' : 'error' },
    {
      key: 'av',
      label: t('serverStatus.virusScanner'),
      tone: !services.virusScannerEnabled || services.virusScannerReachable ? 'success' : 'error',
    },
    { key: 'queue', label: `${t('serverStatus.outboxPending')}: ${services.outboxPending}`, tone: services.outboxAbandoned > 0 ? 'error' : 'success' },
  ];
  if (docker.security.length > 0) {
    chips.push({ key: 'security', label: t('serverSecurity.title'), tone: docker.security.some((c) => c.status === 'warn') ? 'warning' : 'success' });
  }

  return (
    <Panel>
      <Stack direction="row" sx={{ alignItems: 'center', gap: 3, flexWrap: 'wrap' }}>
        <Box
          sx={(theme) => ({
            width: 72,
            height: 72,
            borderRadius: '50%',
            flexShrink: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: alpha(theme.palette[accent].main, 0.14),
            color: `${accent}.main`,
          })}
        >
          {ok ? <CheckRounded sx={{ fontSize: 38 }} /> : <PriorityHighRounded sx={{ fontSize: 38 }} />}
        </Box>

        <Box sx={{ flex: '1 1 320px', minWidth: 0 }}>
          {ok ? (
            <Typography component="div" data-testid="status-summary" sx={{ fontSize: 34, lineHeight: 1.1, fontWeight: 700, letterSpacing: '-0.03em' }}>
              {t('serverStatus.allWell')}
            </Typography>
          ) : (
            <>
              <Typography component="div" sx={{ fontSize: 34, lineHeight: 1.1, fontWeight: 700, letterSpacing: '-0.03em' }}>
                {t('serverStatus.attention', { count: alerts.length })}
              </Typography>
              <Stack component="ul" data-testid="status-summary" sx={{ m: 0, mt: 1, pl: 2.5, gap: 0.5, color: 'text.secondary', fontSize: 16 }}>
                {alerts.map((alert) => (
                  <li key={alert.key}>{t(`serverStatus.alert.${alert.key}`)}</li>
                ))}
              </Stack>
            </>
          )}
        </Box>

        <Stack direction="row" sx={{ gap: 1.25, flexWrap: 'wrap' }}>
          {chips.map((chip) => (
            <Stack
              key={chip.key}
              direction="row"
              sx={(theme) => ({ alignItems: 'center', gap: 1, borderRadius: 999, px: 2, py: 1.25, fontSize: 15, fontWeight: 500, bgcolor: alpha(theme.palette.text.primary, 0.05) })}
            >
              <StatusDot tone={chip.tone} />
              {chip.label}
            </Stack>
          ))}
        </Stack>
      </Stack>
    </Panel>
  );
}

function Tile({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box
      component="section"
      sx={(theme) => ({
        bgcolor: 'background.paper',
        borderRadius: PANEL_RADIUS,
        p: { xs: 2, sm: 3 },
        boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 2px rgba(0, 0, 0, 0.04), 0 8px 28px rgba(0, 0, 0, 0.05)',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
        minWidth: 0,
      })}
    >
      <Typography sx={{ fontSize: 15, fontWeight: 600, color: 'text.secondary' }}>{label}</Typography>
      {children}
    </Box>
  );
}

function RingTile({ label, value, sub, percent, tone, aria, action }: { label: string; value: string; sub: ReactNode; percent: number | null; tone: Tone; aria: string; action?: ReactNode }) {
  const narrow = useMediaQuery((theme: Theme) => theme.breakpoints.down('sm'));
  const ringSize = narrow ? 60 : 76;

  return (
    <Tile label={label}>
      <Stack direction={{ xs: 'column-reverse', sm: 'row' }} sx={{ alignItems: { xs: 'flex-start', sm: 'flex-end' }, justifyContent: 'space-between', gap: 1.5 }}>
        <Box sx={{ minWidth: 0 }}>
          <BigNumber>{value}</BigNumber>
          <Typography variant="body2" color="text.secondary" component="div" sx={{ mt: 1, lineHeight: 1.35 }}>
            {sub}
          </Typography>
        </Box>
        {percent !== null && <Ring percent={percent} tone={tone} label={aria} size={ringSize} />}
      </Stack>
      {action}
    </Tile>
  );
}

/**
 * Processor, memory, disk and availability as four tiles. Each reads what the page already has; only the disk forecast
 * comes from the load history. A figure the machine does not report is a dash, never a zero.
 */
export function MetricTiles({ status }: { status: ServerStatus }) {
  const t = useT();
  const [cleaning, setCleaning] = useState(false);
  const metrics = useQuery({ queryKey: ['system', 'metrics'] as const, queryFn: systemApi.metrics, refetchInterval: 5 * 60_000 });

  const { host, storage, uptime } = status;
  const cpu = cpuPercent(host);
  const memory = memoryUsedPercent(host);
  const disk = storage.known && storage.totalBytes > 0 ? (storage.usedBytes / storage.totalBytes) * 100 : null;
  const dash = t('serverStatus.unknown');

  const forecast = metrics.data;
  const diskSub =
    disk === null
      ? dash
      : [
          t('serverTiles.diskUsed', { used: formatSize(storage.usedBytes), total: formatSize(storage.totalBytes) }),
          forecast && forecast.diskDaysLeft !== null ? t('serverTiles.diskDays', { days: forecast.diskDaysLeft }) : null,
        ]
          .filter(Boolean)
          .join(' · ');

  const flags = uptime.known ? outageDays(uptime.outages, 30, Date.now()) : [];

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', md: 'repeat(4, minmax(0, 1fr))' }, gap: 2 }}>
      <RingTile
        label={t('serverTiles.cpu')}
        value={cpu === null ? dash : `${Math.round(cpu)}%`}
        sub={cpu === null ? dash : t('serverTiles.cpuSub', { load: (host.load1 ?? 0).toFixed(2), cores: host.cores })}
        percent={cpu}
        tone={cpu === null ? 'success' : toneAt(cpu, 100, 200)}
        aria={t('serverTiles.cpu')}
      />
      <RingTile
        label={t('serverTiles.memory')}
        value={memory === null ? dash : `${Math.round(memory)}%`}
        sub={
          memory === null || !host.memoryTotalBytes
            ? dash
            : t('serverTiles.memorySub', { used: formatSize(host.memoryTotalBytes - (host.memoryAvailableBytes ?? 0)), total: formatSize(host.memoryTotalBytes) })
        }
        percent={memory}
        tone={memory === null ? 'success' : toneAt(memory, 85, 93)}
        aria={t('serverTiles.memory')}
      />
      <RingTile
        label={t('serverTiles.disk')}
        value={disk === null ? dash : `${Math.round(disk)}%`}
        sub={diskSub}
        percent={disk}
        tone={storage.level === 2 ? 'error' : storage.level === 1 ? 'warning' : 'success'}
        aria={t('serverTiles.disk')}
        action={
          <Box>
            <Button size="small" variant="outlined" onClick={() => setCleaning(true)}>
              {t('storage.cleanup.open')}
            </Button>
            <StorageCleanupDialog open={cleaning} onClose={() => setCleaning(false)} />
          </Box>
        }
      />
      <Tile label={t('serverStatus.uptimeAvailability', { days: uptime.windowDays || 30 })}>
        <Box
          role="img"
          aria-label={t('serverTiles.availabilityAria')}
          sx={{ display: 'flex', alignItems: 'flex-end', gap: '3px', height: 44 }}
        >
          {(flags.length > 0 ? flags : Array.from({ length: 30 }, () => false)).map((down, index) => (
            <Box
              key={index}
              sx={{ flex: 1, minWidth: 2, height: down ? '62%' : '100%', borderRadius: '3px', bgcolor: uptime.known ? (down ? 'warning.main' : 'success.main') : 'action.disabledBackground' }}
            />
          ))}
        </Box>
        <Box>
          <BigNumber>{uptime.known ? `${uptime.availabilityPercent.toLocaleString('sr-Latn-RS', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%` : dash}</BigNumber>
          <Typography variant="body2" color="text.secondary" component="div" sx={{ mt: 1, lineHeight: 1.35 }}>
            {uptime.known
              ? uptime.downSeconds > 0
                ? t('serverTiles.downTotal', { time: formatDuration(uptime.downSeconds) })
                : t('serverStatus.uptimeNoDown')
              : t('serverStatus.uptimeNone')}
          </Typography>
        </Box>
      </Tile>
    </Box>
  );
}
