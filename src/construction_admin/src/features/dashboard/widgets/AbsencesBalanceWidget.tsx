import { Avatar, Box, Stack, useTheme } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { absencesApi } from '../../../api/absences';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate, initialsOf, todayCompany } from '../../../utils/formatting';
import { useActiveEmployeesCount, useOpenTimeEntries, useUnitParams } from '../dashboardData';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { BarRow, BigNumber, InsetList, InsetRow, MoreLink, Segments } from './widgetUi';

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

  const unit = useUnitParams();

  const pendingQuery = useQuery({
    queryKey: ['dashboard', 'absences', 'pending', unit.branchId ?? null] as const,
    queryFn: () => absencesApi.list({ pageNumber: 1, pageSize: 5, status: 'Requested', ...unit }),
  });

  // Today is the local day: the UTC date is yesterday for two hours after midnight.
  const day = todayCompany();
  const activeQuery = useQuery({
    queryKey: ['dashboard', 'absences', 'active-today', day, unit.branchId ?? null] as const,
    queryFn: () => absencesApi.list({ pageNumber: 1, pageSize: 5, status: 'Approved', from: day, to: day, ...unit }),
  });

  const activeEmployeesQuery = useActiveEmployeesCount();

  // Every running shift, all pages: the count and the split by site must not stop at the first hundred.
  const onSiteQuery = useOpenTimeEntries();

  const isLoading = pendingQuery.isLoading || activeQuery.isLoading || onSiteQuery.isLoading;
  const error = pendingQuery.error ?? activeQuery.error ?? onSiteQuery.error;
  const pendingCount = pendingQuery.data?.totalCount ?? 0;
  const activeCount = activeQuery.data?.totalCount ?? 0;
  const pendingItems = pendingQuery.data?.items ?? [];

  const onSiteEntries = onSiteQuery.data?.items ?? [];
  const onSiteCount = onSiteQuery.data?.totalCount ?? 0;
  const byProject = headcountByProject(onSiteEntries);

  return (
    <WidgetShell title={t('dashboard.widget.AbsencesBalance')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
        <Stack direction="row" sx={{ alignItems: 'baseline', gap: 1.25, flexWrap: 'wrap', flexShrink: 0 }}>
          <BigNumber value={onSiteCount} caption={t('dashboard.absencesBalance.ofActive', { count: Math.max(activeEmployeesQuery.data ?? 0, onSiteCount) })} />
        </Stack>
        <Segments
          parts={[
            { label: t('dashboard.absencesBalance.legendOnSite'), value: onSiteCount, color: theme.palette.primary.main },
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
