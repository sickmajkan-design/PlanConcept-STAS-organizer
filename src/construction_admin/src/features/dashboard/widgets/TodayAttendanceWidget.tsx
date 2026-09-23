import { AccessTimeOutlined } from '@mui/icons-material';
import { Avatar, Box, Button, Chip, List, ListItem, ListItemAvatar, ListItemText, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { useTimeEntriesQuery, useTimeEntrySummaryQuery } from '../../timeEntries/useTimeEntries';
import { useFormatRelative } from '../../../i18n/useFormatRelative';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatTimeOfDay, initialsOf, splitMinutes } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const ON_SITE_PAGE_SIZE = 100;
const SHOWN = 8;

const today = () => new Date().toISOString().slice(0, 10);

/** The summary endpoint requires `to` strictly after `from` — same convention as TimeEntrySummaryPage's own `endOfDay`. */
function endOfDay(date: string): string {
  const next = new Date(`${date}T00:00`);
  next.setDate(next.getDate() + 1);
  return next.toISOString();
}

function splitName(fullName: string): [string, string] {
  const [first, ...rest] = fullName.trim().split(/\s+/);
  return [first ?? '', rest.join(' ')];
}

/**
 * Who's actually clocked in right now, and today's totals — the same
 * `openOnly` query and polling interval the Work Time board itself uses, so
 * this reads as live rather than a stale morning snapshot.
 */
export function TodayAttendanceWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const formatRelative = useFormatRelative();
  const day = today();

  const openQuery = useTimeEntriesQuery({ pageNumber: 1, pageSize: ON_SITE_PAGE_SIZE, openOnly: true });
  const summaryQuery = useTimeEntrySummaryQuery({ from: `${day}T00:00`, to: endOfDay(day) });

  const isLoading = openQuery.isLoading || summaryQuery.isLoading;
  const error = openQuery.error ?? summaryQuery.error;

  const entries = [...(openQuery.data?.items ?? [])]
    .sort((a, b) => a.startedAt.localeCompare(b.startedAt))
    .slice(0, SHOWN);

  const totalToday = splitMinutes(summaryQuery.data?.totalMinutes);
  const pendingCount = summaryQuery.data?.pendingCount ?? 0;

  return (
    <WidgetShell title={t('dashboard.widget.TodayAttendance')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', rowGap: 1, flexShrink: 0 }}>
          <Chip
            color="primary"
            label={t('dashboard.todayAttendance.onSiteNow', { count: openQuery.data?.totalCount ?? 0 })}
            sx={{ fontWeight: 700 }}
          />
          <Chip
            label={t('dashboard.todayAttendance.hoursToday', { hours: totalToday.hours, minutes: totalToday.minutes })}
            sx={{ fontWeight: 700 }}
          />
          {pendingCount > 0 && (
            <Chip color="warning" label={t('dashboard.todayAttendance.pending', { count: pendingCount })} sx={{ fontWeight: 700 }} />
          )}
        </Stack>

        {entries.length === 0 ? (
          <Typography color="text.secondary" variant="body2">
            {t('dashboard.todayAttendance.empty')}
          </Typography>
        ) : (
          <List dense disablePadding sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            {entries.map((entry) => (
              <ListItem key={entry.id} disableGutters>
                <ListItemAvatar sx={{ minWidth: 44 }}>
                  <Avatar sx={{ width: 32, height: 32, fontSize: 13, bgcolor: 'primary.main' }}>
                    {initialsOf(...splitName(entry.employeeName))}
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={entry.employeeName}
                  secondary={`${entry.projectName ?? '—'} · ${formatTimeOfDay(entry.startedAt)} (${formatRelative(entry.startedAt)})`}
                  slotProps={{ primary: { sx: { fontWeight: 600 } } }}
                />
                <AccessTimeOutlined fontSize="small" sx={{ color: 'text.disabled', flexShrink: 0 }} />
              </ListItem>
            ))}
          </List>
        )}

        <Box sx={{ flexShrink: 0 }}>
          <Button component={Link} to={paths.timeEntries} size="small">
            {t('common.viewAll')}
          </Button>
        </Box>
      </Stack>
    </WidgetShell>
  );
}
