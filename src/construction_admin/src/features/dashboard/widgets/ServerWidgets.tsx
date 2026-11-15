import CheckRounded from '@mui/icons-material/CheckRounded';
import PriorityHighRounded from '@mui/icons-material/PriorityHighRounded';
import { Box, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { systemApi, type ServerStatus } from '../../../api/system';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatRelative } from '../../../utils/formatting';
import { formatSize } from '../../system/formatSize';
import { cpuPercent, memoryUsedPercent, toneAt } from '../../system/overviewMath';
import { serverAlerts, worstTone } from '../../system/serverStatusAlerts';
import { Ring, StatusDot, type Tone } from '../../system/serverUi';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

/** The same query the Server status page keeps, so a board and the page share one answer and one refresh. */
function useServerStatus() {
  return useQuery({ queryKey: ['system', 'status'] as const, queryFn: systemApi.status, refetchInterval: 30_000 });
}

function OpenServerPage() {
  const t = useT();

  return (
    <Typography
      component={Link}
      to={paths.system}
      variant="body2"
      sx={{ color: 'primary.main', textDecoration: 'none', fontWeight: 600, flexShrink: 0 }}
    >
      {t('dashboard.serverWidgets.open')}
    </Typography>
  );
}

/** One line that says whether the server is all right, and a chip for each thing that can be down. */
export function ServerHealthWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const { data, isLoading, error } = useServerStatus();

  const alerts = data ? serverAlerts(data) : [];
  const tone = worstTone(alerts);
  const accent: Tone = tone ?? 'success';

  const chips: { key: string; label: string; tone: Tone }[] = data
    ? [
        { key: 'db', label: t('serverStatus.database'), tone: data.database.reachable ? 'success' : 'error' },
        {
          key: 'av',
          label: t('serverStatus.virusScanner'),
          tone: !data.services.virusScannerEnabled || data.services.virusScannerReachable ? 'success' : 'error',
        },
        { key: 'queue', label: `${t('serverStatus.outboxPending')}: ${data.services.outboxPending}`, tone: data.services.outboxAbandoned > 0 ? 'error' : 'success' },
      ]
    : [];

  return (
    <WidgetShell title={t('dashboard.widget.ServerHealth')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      {data && (
        <Stack sx={{ gap: 1.5 }}>
          <Stack direction="row" sx={{ alignItems: 'center', gap: 1.5 }}>
            <Box
              sx={(theme) => ({
                width: 44,
                height: 44,
                borderRadius: '50%',
                flexShrink: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: alpha(theme.palette[accent].main, 0.14),
                color: `${accent}.main`,
              })}
            >
              {tone === null ? <CheckRounded /> : <PriorityHighRounded />}
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Typography sx={{ fontWeight: 700, fontSize: 18, lineHeight: 1.25 }}>
                {tone === null ? t('serverStatus.allWell') : t('serverStatus.attention', { count: alerts.length })}
              </Typography>
              {tone !== null && (
                <Typography variant="body2" color="text.secondary" noWrap>
                  {t(`serverStatus.alert.${alerts[0].key}`)}
                </Typography>
              )}
            </Box>
          </Stack>

          <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
            {chips.map((chip) => (
              <Stack key={chip.key} direction="row" sx={{ alignItems: 'center', gap: 0.75, px: 1.25, py: 0.5, borderRadius: 999, bgcolor: 'action.hover' }}>
                <StatusDot tone={chip.tone} />
                <Typography variant="body2">{chip.label}</Typography>
              </Stack>
            ))}
          </Stack>

          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
            <Typography variant="body2" color="text.secondary">
              {data.uptime.known ? `${t('serverStatus.uptimeAvailability', { days: data.uptime.windowDays || 30 })}: ${data.uptime.availabilityPercent.toFixed(2)}%` : ''}
            </Typography>
            <OpenServerPage />
          </Stack>
        </Stack>
      )}
    </WidgetShell>
  );
}

function RingFigure({ label, percent, tone, sub }: { label: string; percent: number | null; tone: Tone; sub?: string }) {
  const dash = '—';

  return (
    <Stack sx={{ alignItems: 'center', gap: 0.5, minWidth: 0 }}>
      <Box sx={{ position: 'relative', display: 'inline-flex' }}>
        <Ring percent={percent ?? 0} tone={tone} label={label} size={72} />
        <Typography sx={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 15 }}>
          {percent === null ? dash : `${Math.round(percent)}%`}
        </Typography>
      </Box>
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        {label}
      </Typography>
      {sub && (
        <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'center' }}>
          {sub}
        </Typography>
      )}
    </Stack>
  );
}

function resources(status: ServerStatus) {
  const cpu = cpuPercent(status.host);
  const memory = memoryUsedPercent(status.host);
  const { storage } = status;
  const disk = storage.known && storage.totalBytes > 0 ? (storage.usedBytes / storage.totalBytes) * 100 : null;

  return { cpu, memory, disk, storage };
}

/** Processor, memory and disk as three rings: the three things that fill up. */
export function ServerResourcesWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const { data, isLoading, error } = useServerStatus();
  const r = data ? resources(data) : null;

  return (
    <WidgetShell title={t('dashboard.widget.ServerResources')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      {data && r && (
        <Stack sx={{ gap: 1.5 }}>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 1 }}>
            <RingFigure label={t('serverTiles.cpu')} percent={r.cpu} tone={r.cpu === null ? 'success' : toneAt(r.cpu, 100, 200)} />
            <RingFigure label={t('serverTiles.memory')} percent={r.memory} tone={r.memory === null ? 'success' : toneAt(r.memory, 85, 93)} />
            <RingFigure
              label={t('serverTiles.disk')}
              percent={r.disk}
              tone={r.storage.level === 2 ? 'error' : r.storage.level === 1 ? 'warning' : 'success'}
              sub={r.disk === null ? undefined : t('serverTiles.diskUsed', { used: formatSize(r.storage.usedBytes), total: formatSize(r.storage.totalBytes) })}
            />
          </Box>
          <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
            <OpenServerPage />
          </Stack>
        </Stack>
      )}
    </WidgetShell>
  );
}

/** When the last backup was made, how big it is, whether a copy is off the server and when a restore was last rehearsed. */
export function ServerBackupsWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const { data, isLoading, error } = useServerStatus();
  const last = data?.backups.items[0];
  const stale = !!last && Date.now() - new Date(last.takenAtUtc).getTime() > 36 * 3_600_000;
  const lastTone: Tone = !data?.backups.known || !last ? 'warning' : stale ? 'error' : 'success';

  const rows: { key: string; label: string; value: string; tone?: Tone }[] = data
    ? [
        { key: 'last', label: t('serverStatus.backupLast'), value: last ? `${formatRelative(last.takenAtUtc)} · ${formatSize(last.sizeBytes)}` : t('serverStatus.backupsNone'), tone: lastTone },
        { key: 'offsite', label: t('serverStatus.backupOffsite'), value: last?.offsite ? t('serverStatus.backupYes') : t('serverStatus.backupNo'), tone: last?.offsite ? 'success' : 'warning' },
        {
          key: 'restore',
          label: t('serverStatus.backupRestore'),
          value: data.backups.restoreCheckedAtUtc ? formatRelative(data.backups.restoreCheckedAtUtc) : t('serverStatus.backupNever'),
        },
      ]
    : [];

  return (
    <WidgetShell title={t('dashboard.widget.ServerBackups')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      {data &&
        (!data.backups.known ? (
          <Typography variant="body2" color="text.secondary">
            {t('serverStatus.backupsHidden')}
          </Typography>
        ) : (
          <Stack sx={{ gap: 1 }}>
            {rows.map((row) => (
              <Stack key={row.key} direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  {row.label}
                </Typography>
                <Stack direction="row" sx={{ alignItems: 'center', gap: 0.75 }}>
                  {row.tone && <StatusDot tone={row.tone} />}
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {row.value}
                  </Typography>
                </Stack>
              </Stack>
            ))}
            <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
              <OpenServerPage />
            </Stack>
          </Stack>
        ))}
    </WidgetShell>
  );
}

/** Failed sign-ins of the last 24 hours and week, SSH addresses blocked right now, and server errors today. */
export function ServerSignInsWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const attempts = useQuery({ queryKey: ['system', 'login-attempts'] as const, queryFn: systemApi.loginAttempts, refetchInterval: 60_000 });
  const errors = useQuery({ queryKey: ['system', 'errors'] as const, queryFn: systemApi.errors, refetchInterval: 60_000 });
  const errorsToday = (errors.data ?? []).reduce((sum, group) => sum + group.today, 0);

  const figures = attempts.data
    ? [
        { key: 'a', value: String(attempts.data.last24Hours), label: t('serverLogins.last24') },
        { key: 'b', value: attempts.data.ssh.known ? String(attempts.data.ssh.bannedNow) : '—', label: t('serverLogins.sshBanned') },
        { key: 'c', value: String(errorsToday), label: t('dashboard.serverWidgets.errorsToday'), tone: errorsToday > 0 ? ('error' as const) : undefined },
      ]
    : [];

  return (
    <WidgetShell title={t('dashboard.widget.ServerSignIns')} isLoading={attempts.isLoading} error={attempts.error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      {attempts.data && (
        <Stack sx={{ gap: 1.5 }}>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 1.5 }}>
            {figures.map((figure) => (
              <Box key={figure.key} sx={{ minWidth: 0 }}>
                <Typography sx={{ fontSize: 28, fontWeight: 700, lineHeight: 1.1, color: figure.tone ? `${figure.tone}.main` : 'text.primary' }}>{figure.value}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {figure.label}
                </Typography>
              </Box>
            ))}
          </Box>
          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
            <Typography variant="body2" color="text.secondary">
              {t('serverLogins.last7')}: {attempts.data.last7Days}
            </Typography>
            <OpenServerPage />
          </Stack>
        </Stack>
      )}
    </WidgetShell>
  );
}
