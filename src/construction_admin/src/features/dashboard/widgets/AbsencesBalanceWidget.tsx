import { Button, Chip, Divider, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { absencesApi } from '../../../api/absences';
import { timeEntriesApi } from '../../../api/timeEntries';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const today = () => new Date().toISOString().slice(0, 10);
// 100 is GetTimeEntriesQuery's own max page size — a bigger request 400s.
const ON_SITE_PAGE_SIZE = 100;
const TOP_PROJECTS_SHOWN = 6;

/** Headcount per project among currently open (clocked-in, not yet out) shifts. */
function headcountByProject(entries: { projectName: string | null }[]) {
  const counts = new Map<string, number>();
  for (const entry of entries) {
    const key = entry.projectName ?? '—';
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return [...counts.entries()]
    .sort((a, b) => b[1] - a[1])
    .slice(0, TOP_PROJECTS_SHOWN);
}

export function AbsencesBalanceWidget({
  instanceId: _instanceId,
  dragHandleProps,
  onRemove,
}: DashboardWidgetProps) {
  const t = useT();

  const pendingQuery = useQuery({
    queryKey: ['dashboard', 'absences', 'pending'] as const,
    queryFn: () =>
      absencesApi.list({ pageNumber: 1, pageSize: 5, status: 'Requested' }),
  });

  const activeQuery = useQuery({
    queryKey: ['dashboard', 'absences', 'active-today'] as const,
    queryFn: () => {
      const day = today();
      return absencesApi.list({
        pageNumber: 1,
        pageSize: 5,
        status: 'Approved',
        from: day,
        to: day,
      });
    },
  });

  const onSiteQuery = useQuery({
    queryKey: ['dashboard', 'workforce', 'on-site'] as const,
    queryFn: () =>
      timeEntriesApi.list({ pageNumber: 1, pageSize: ON_SITE_PAGE_SIZE, openOnly: true }),
  });

  const isLoading = pendingQuery.isLoading || activeQuery.isLoading || onSiteQuery.isLoading;
  const error = pendingQuery.error ?? activeQuery.error ?? onSiteQuery.error;
  const pendingCount = pendingQuery.data?.totalCount ?? 0;
  const activeCount = activeQuery.data?.totalCount ?? 0;
  const pendingItems = pendingQuery.data?.items ?? [];

  const onSiteEntries = onSiteQuery.data?.items ?? [];
  const byProject = headcountByProject(onSiteEntries);

  return (
    <WidgetShell
      title={t('dashboard.widget.AbsencesBalance')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      dragHandleProps={dragHandleProps}
    >
      <Stack direction="row" spacing={1} sx={{ mb: 1.5, flexWrap: 'wrap', rowGap: 1 }}>
        <Chip
          size="small"
          color="primary"
          label={t('dashboard.absencesBalance.onSiteNow', { count: onSiteEntries.length })}
        />
        <Chip
          size="small"
          color={pendingCount > 0 ? 'warning' : 'default'}
          label={t('dashboard.absencesBalance.pending', { count: pendingCount })}
        />
        <Chip
          size="small"
          label={t('dashboard.absencesBalance.onLeaveToday', { count: activeCount })}
        />
      </Stack>

      {byProject.length > 0 && (
        <BarChart
          height={160}
          series={[{ data: byProject.map(([, count]) => count), label: t('dashboard.absencesBalance.onSiteNow', { count: onSiteEntries.length }) }]}
          xAxis={[{ scaleType: 'band', data: byProject.map(([name]) => name) }]}
          margin={{ top: 10, bottom: 50, left: 30, right: 10 }}
        />
      )}

      {pendingItems.length > 0 && (
        <>
          <Divider sx={{ my: 1.5 }} />
          <List dense disablePadding>
            {pendingItems.map((absence) => (
              <ListItem key={absence.id} disableGutters>
                <ListItemText
                  primary={absence.employeeName}
                  secondary={`${absence.type} · ${formatDate(absence.startDate)} — ${formatDate(absence.endDate)}`}
                />
              </ListItem>
            ))}
          </List>
        </>
      )}

      <Typography variant="body2">
        <Button component={Link} to={paths.absences} size="small">
          {t('common.viewAll')}
        </Button>
      </Typography>
    </WidgetShell>
  );
}
