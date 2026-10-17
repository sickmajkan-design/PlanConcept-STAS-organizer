import { AccessTimeOutlined } from '@mui/icons-material';
import { Avatar, Box, Stack } from '@mui/material';

import { useTimeEntriesQuery, useTimeEntrySummaryQuery } from '../../timeEntries/useTimeEntries';
import { useFormatRelative } from '../../../i18n/useFormatRelative';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatTimeOfDay, initialsOf, splitMinutes } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, InsetList, InsetRow, MoreLink, Pill } from './widgetUi';

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
        <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, flexShrink: 0 }}>
          <Pill tone="accent" sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
            {t('dashboard.todayAttendance.onSiteNow', { count: openQuery.data?.totalCount ?? 0 })}
          </Pill>
          <Pill sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
            {t('dashboard.todayAttendance.hoursToday', { hours: totalToday.hours, minutes: totalToday.minutes })}
          </Pill>
          {pendingCount > 0 && (
            <Pill tone="warn" sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
              {t('dashboard.todayAttendance.pending', { count: pendingCount })}
            </Pill>
          )}
        </Stack>

        {entries.length === 0 ? (
          <EmptyNote>{t('dashboard.todayAttendance.empty')}</EmptyNote>
        ) : (
          <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            <InsetList>
              {entries.map((entry) => (
                <InsetRow
                  key={entry.id}
                  leading={
                    <Avatar sx={{ width: 32, height: 32, fontSize: 13, bgcolor: 'primary.main' }}>
                      {initialsOf(...splitName(entry.employeeName))}
                    </Avatar>
                  }
                  primary={entry.employeeName}
                  secondary={`${entry.projectName ?? '—'} · ${formatTimeOfDay(entry.startedAt)} (${formatRelative(entry.startedAt)})`}
                  action={<AccessTimeOutlined fontSize="small" sx={{ color: 'text.disabled', flexShrink: 0 }} />}
                />
              ))}
            </InsetList>
          </Box>
        )}

        <MoreLink to={paths.timeEntries}>{t('common.viewAll')}</MoreLink>
      </Stack>
    </WidgetShell>
  );
}
