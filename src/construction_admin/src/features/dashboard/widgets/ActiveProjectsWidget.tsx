import { Box, Stack } from '@mui/material';
import { projectsApi } from '../../../api/projects';
import { projectStatuses, type ProjectStatus } from '../../../api/types';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { daysUntil, formatDate } from '../../../utils/formatting';
import { useActiveProjects, useCounts } from '../dashboardData';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, InsetList, InsetRow, MoreLink, Pill } from './widgetUi';

const SHOWN = 6;
const ENDING_SOON_DAYS = 14;
const ONE = { pageNumber: 1, pageSize: 1 } as const;

export { daysUntil } from '../../../utils/formatting';

/**
 * How many projects there are in each stage, and which of the running ones
 * want a look: those with nobody on them, and those about to end.
 *
 * A project a foreman is not posted to is not in the list the API returns to
 * them, so the same widget shows a foreman their own sites and nobody's else.
 */
export function ActiveProjectsWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const enumLabel = useEnumLabel();

  // How many there are in each stage is the server's own count per status; the running ones are read in full (every
  // page) because the widget sorts and flags them itself.
  const query = useActiveProjects();
  const stages = useCounts(
    'projects',
    Object.fromEntries(projectStatuses.map((status) => [status, (unit: object) => projectsApi.list({ ...ONE, status, ...unit })])),
  );

  const countByStatus = new Map<ProjectStatus, number>(projectStatuses.map((status) => [status, stages.counts[status] ?? 0]));

  const active = query.data?.items ?? [];

  const rows = active
    .map((project) => ({
      project,
      noCrew: project.employeeCount === 0,
      endsIn: project.endDate ? daysUntil(project.endDate) : null,
    }))
    .sort((a, b) => Number(b.noCrew) - Number(a.noCrew) || (a.endsIn ?? 9999) - (b.endsIn ?? 9999))
    .slice(0, SHOWN);

  return (
    <WidgetShell
      title={t('dashboard.widget.ActiveProjects')}
      isLoading={query.isLoading || stages.isLoading}
      error={query.error ?? stages.error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, flexShrink: 0 }}>
          {projectStatuses
            .filter((status) => (countByStatus.get(status) ?? 0) > 0)
            .map((status) => (
              <Pill key={status} tone={status === 'Active' ? 'accent' : 'neutral'} sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
                {`${enumLabel('projectStatus', status)}: ${countByStatus.get(status)}`}
              </Pill>
            ))}
        </Stack>

        {rows.length === 0 && !query.isLoading ? (
          <EmptyNote>{t('dashboard.activeProjects.empty')}</EmptyNote>
        ) : (
          <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            <InsetList>
              {rows.map(({ project, noCrew, endsIn }) => (
                <InsetRow
                  key={project.id}
                  to={paths.projectDetail(project.id)}
                  primary={project.name}
                  secondary={`${t('dashboard.activeProjects.crew', { count: project.employeeCount })}${
                    project.endDate ? ` · ${t('dashboard.activeProjects.ends', { date: formatDate(project.endDate) })}` : ''
                  }`}
                  action={
                    <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
                      {noCrew && <Pill tone="warn">{t('dashboard.activeProjects.noCrew')}</Pill>}
                      {endsIn !== null && endsIn >= 0 && endsIn <= ENDING_SOON_DAYS && (
                        <Pill tone="warn">{t('dashboard.activeProjects.endingSoon', { count: endsIn })}</Pill>
                      )}
                      {endsIn !== null && endsIn < 0 && <Pill tone="bad">{t('dashboard.activeProjects.overdue')}</Pill>}
                    </Stack>
                  }
                />
              ))}
            </InsetList>
          </Box>
        )}

        <MoreLink to={paths.projects}>{t('common.viewAll')}</MoreLink>
      </Stack>
    </WidgetShell>
  );
}
