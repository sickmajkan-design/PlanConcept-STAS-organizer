import { Box, Stack } from '@mui/material';

import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatIsoWeek } from '../../../utils/isoWeek';
import { useWeeklySiteReportsSummaryQuery } from '../../weeklySiteReports/useWeeklySiteReports';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { InsetList, InsetRow, MiniStat, MoreLink, StatGrid } from './widgetUi';

const SHOWN = 6;

/**
 * What the office has to do about the weekly site reports: how many wait for a look, how many were sent back
 * and which sites are late, with the late ones and the oldest waiting ones listed.
 *
 * It reads the same summary the Weekly reports page counts its chips from, so approving a report there (or in
 * the preview) drops the numbers here at once.
 */
export function WeeklyReportsWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const { data, isLoading, error } = useWeeklySiteReportsSummaryQuery();

  const overdue = data?.overdue ?? [];
  const waiting = data?.waiting ?? [];
  const nothing = !!data && data.awaitingReview === 0 && data.correctionRequested === 0 && overdue.length === 0;

  return (
    <WidgetShell
      title={t('dashboard.widget.WeeklyReports')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      empty={nothing ? { text: t('dashboard.weeklyReports.empty') } : undefined}
    >
      {data && !nothing && (
        <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
          <StatGrid min={110}>
            <MiniStat
              label={t('dashboard.weeklyReports.awaiting')}
              value={data.awaitingReview}
              tone={data.awaitingReview > 0 ? 'warn' : 'neutral'}
            />
            <MiniStat
              label={t('dashboard.weeklyReports.correction')}
              value={data.correctionRequested}
              tone={data.correctionRequested > 0 ? 'warn' : 'neutral'}
            />
            <MiniStat
              label={t('dashboard.weeklyReports.overdue')}
              value={overdue.length}
              tone={overdue.length > 0 ? 'bad' : 'neutral'}
            />
          </StatGrid>

          <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            <InsetList>
              {overdue.slice(0, SHOWN).map((site) => (
                <InsetRow
                  key={site.projectId}
                  tone="bad"
                  primary={site.projectName}
                  secondary={
                    site.foremanNames.length > 0
                      ? t('dashboard.weeklyReports.lateRowForeman', {
                          week: data.isoWeek,
                          names: site.foremanNames.join(', '),
                        })
                      : t('dashboard.weeklyReports.lateRow', { week: data.isoWeek })
                  }
                  to={`${paths.weeklyReports}?view=overdue`}
                />
              ))}
              {waiting.slice(0, Math.max(0, SHOWN - overdue.length)).map((report) => (
                <InsetRow
                  key={report.id}
                  tone="warn"
                  primary={report.projectName}
                  secondary={`${formatIsoWeek(report.isoYear, report.isoWeek)} · ${report.submittedByEmployeeName}`}
                  to={`${paths.weeklyReports}?view=Submitted`}
                />
              ))}
            </InsetList>
          </Box>
        </Stack>
      )}
      <MoreLink to={paths.weeklyReports}>{t('common.viewAll')}</MoreLink>
    </WidgetShell>
  );
}
