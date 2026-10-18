import { AccessTimeOutlined } from '@mui/icons-material';
import { Avatar, Box, Stack } from '@mui/material';

import { useTimeEntrySummaryQuery } from '../../timeEntries/useTimeEntries';
import { useFormatRelative } from '../../../i18n/useFormatRelative';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatTimeOfDay, initialsOf, companyDayBounds, splitMinutes } from '../../../utils/formatting';
import { useActiveEmployeesCount, useOpenTimeEntries } from '../dashboardData';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { Ring } from './widgetCharts';
import { EmptyNote, InsetList, InsetRow, MoreLink, Pill } from './widgetUi';

const SHOWN = 8;

/** Minutes a shift that has not ended has run so far, less its break. Never negative. */
function runningMinutes(entry: { startedAt: string; breakMinutes: number }, now: number): number {
  return Math.max(0, Math.floor((now - new Date(entry.startedAt).getTime()) / 60_000) - entry.breakMinutes);
}

function splitName(fullName: string): [string, string] {
  const [first, ...rest] = fullName.trim().split(/\s+/);
  return [first ?? '', rest.join(' ')];
}

/**
 * Who's actually clocked in right now, and today's totals — the same
 * `openOnly` query and polling interval the Work Time board itself uses, so
 * this reads as live rather than a stale morning snapshot.
 *
 * The hours are what has been worked today: shifts that are finished (the server's summary, which leaves a running
 * shift out since it has no duration yet) plus how long each running one has run. Today is the local day, from
 * its own midnight to the next, not the UTC date, which is yesterday for two hours after midnight.
 */
export function TodayAttendanceWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const formatRelative = useFormatRelative();
  const day = companyDayBounds();

  const openQuery = useOpenTimeEntries();
  // Who the ring is measured against: everyone who works here. The same query the key figures use, so one fetch.
  const activeQuery = useActiveEmployeesCount();
  const summaryQuery = useTimeEntrySummaryQuery({ from: day.from, to: day.to });

  const isLoading = openQuery.isLoading || summaryQuery.isLoading;
  const error = openQuery.error ?? summaryQuery.error;

  const running = openQuery.data?.items ?? [];
  const onSiteNow = openQuery.data?.totalCount ?? 0;
  const entries = [...running].sort((a, b) => a.startedAt.localeCompare(b.startedAt)).slice(0, SHOWN);

  const now = Date.now();
  const totalToday = splitMinutes(
    (summaryQuery.data?.totalMinutes ?? 0) + running.reduce((sum, entry) => sum + runningMinutes(entry, now), 0),
  );
  const pendingCount = summaryQuery.data?.pendingCount ?? 0;

  return (
    <WidgetShell title={t('dashboard.widget.TodayAttendance')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Stack direction="row" sx={{ alignItems: 'center', gap: 2, flexWrap: 'wrap', flexShrink: 0 }}>
          <Ring
            value={onSiteNow}
            total={Math.max(activeQuery.data ?? 0, onSiteNow)}
            center={onSiteNow}
            caption={t('dashboard.todayAttendance.ofActive', { count: Math.max(activeQuery.data ?? 0, onSiteNow) })}
          />
          <Stack sx={{ gap: 1, alignItems: 'flex-start' }}>
            <Pill tone="accent" sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
              {t('dashboard.todayAttendance.onSiteNow', { count: onSiteNow })}
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
