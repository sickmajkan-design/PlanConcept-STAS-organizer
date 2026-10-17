import { Avatar, Box, Stack, useTheme } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { absencesApi } from '../../../api/absences';
import { employeesApi } from '../../../api/employees';
import { timeEntriesApi } from '../../../api/timeEntries';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate, initialsOf } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { BarRow, BigNumber, InsetList, InsetRow, MoreLink, Segments } from './widgetUi';

const today = () => new Date().toISOString().slice(0, 10);
// 100 is GetTimeEntriesQuery's own max page size — a bigger request 400s.
const ON_SITE_PAGE_SIZE = 100;
const TOP_PROJECTS_SHOWN = 6;

/** `employeeName` on this endpoint is one combined string, not first/last fields. */
function splitName(fullName: string): [string, string] {
  const [first, ...rest] = fullName.trim().split(/\s+/);
  return [first ?? '', rest.join(' ')];
}

/** Headcount per project among currently open (clocked-in, not yet out) shifts. */
function headcountByProject(entries: { projectName: string | null }[]) {
  const counts = new Map<string, number>();
  for (const entry of entries) {
    const key = entry.projectName ?? '—';
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, TOP_PROJECTS_SHOWN);
}

export function AbsencesBalanceWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const theme = useTheme();

  const pendingQuery = useQuery({
    queryKey: ['dashboard', 'absences', 'pending'] as const,
    queryFn: () => absencesApi.list({ pageNumber: 1, pageSize: 5, status: 'Requested' }),
  });

  const activeQuery = useQuery({
    queryKey: ['dashboard', 'absences', 'active-today'] as const,
    queryFn: () => {
      const day = today();
      return absencesApi.list({ pageNumber: 1, pageSize: 5, status: 'Approved', from: day, to: day });
    },
  });

  const activeEmployeesQuery = useQuery({
    queryKey: ['dashboard', 'kpi', 'active-employees'] as const,
    queryFn: () => employeesApi.list({ pageNumber: 1, pageSize: 1, status: 'Active' }),
  });

  const onSiteQuery = useQuery({
    queryKey: ['dashboard', 'workforce', 'on-site'] as const,
    queryFn: () => timeEntriesApi.list({ pageNumber: 1, pageSize: ON_SITE_PAGE_SIZE, openOnly: true }),
  });

  const isLoading = pendingQuery.isLoading || activeQuery.isLoading || onSiteQuery.isLoading;
  const error = pendingQuery.error ?? activeQuery.error ?? onSiteQuery.error;
  const pendingCount = pendingQuery.data?.totalCount ?? 0;
  const activeCount = activeQuery.data?.totalCount ?? 0;
  const pendingItems = pendingQuery.data?.items ?? [];

  const onSiteEntries = onSiteQuery.data?.items ?? [];
  const byProject = headcountByProject(onSiteEntries);

  return (
    <WidgetShell title={t('dashboard.widget.AbsencesBalance')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
        <Stack direction="row" sx={{ alignItems: 'baseline', gap: 1.25, flexWrap: 'wrap', flexShrink: 0 }}>
          <BigNumber value={onSiteEntries.length} caption={t('dashboard.absencesBalance.ofActive', { count: activeEmployeesQuery.data?.totalCount ?? onSiteEntries.length })} />
        </Stack>
        <Segments
          parts={[
            { label: t('dashboard.absencesBalance.legendOnSite'), value: onSiteEntries.length, color: theme.palette.primary.main },
            { label: t('dashboard.absencesBalance.legendPending'), value: pendingCount, color: theme.palette.warning.main },
            { label: t('dashboard.absencesBalance.legendOnLeave'), value: activeCount, color: theme.palette.text.disabled },
          ]}
        />

        {byProject.length > 0 && (
          <Box sx={{ display: 'grid', gap: 1.5 }}>
            {byProject.map(([name, count]) => (
              <BarRow key={name} label={name} value={count} percent={(count / (byProject[0]?.[1] ?? 1)) * 100} />
            ))}
          </Box>
        )}

        {pendingItems.length > 0 && (
          <Box sx={{ flexShrink: 0 }}>
            <InsetList>
              {pendingItems.map((absence) => (
                <InsetRow
                  key={absence.id}
                  leading={
                    <Avatar sx={{ width: 32, height: 32, fontSize: 13, bgcolor: 'secondary.main' }}>
                      {initialsOf(...splitName(absence.employeeName))}
                    </Avatar>
                  }
                  primary={absence.employeeName}
                  secondary={`${enumLabel('absenceType', absence.type)} · ${formatDate(absence.startDate)} - ${formatDate(absence.endDate)}`}
                />
              ))}
            </InsetList>
          </Box>
        )}

        <MoreLink to={paths.absences}>{t('common.viewAll')}</MoreLink>
      </Stack>
    </WidgetShell>
  );
}
