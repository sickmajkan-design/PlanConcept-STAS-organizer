import { Box, Stack } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { projectsApi } from '../../../api/projects';
import { projectStatuses, type ProjectStatus } from '../../../api/types';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, InsetList, InsetRow, MoreLink, Pill } from './widgetUi';

const SHOWN = 6;
const ENDING_SOON_DAYS = 14;
// 100 is the list endpoints' own maximum page size; a bigger request 400s.
const PAGE = 100;

const DAY_MS = 24 * 60 * 60 * 1000;

/** Whole days from `today` to the `YYYY-MM-DD` date, negative once it has passed. */
export function daysUntil(date: string, today: Date = new Date()): number {
  const start = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const [year, month, day] = date.split('-').map(Number);

  return Math.round((Date.UTC(year, month - 1, day) - start) / DAY_MS);
}

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

  const query = useQuery({
    queryKey: ['dashboard', 'projects', 'overview'] as const,
    queryFn: () => projectsApi.list({ pageNumber: 1, pageSize: PAGE }),
  });

  const projects = query.data?.items ?? [];

  const countByStatus = new Map<ProjectStatus, number>();
  for (const project of projects) {
    countByStatus.set(project.status, (countByStatus.get(project.status) ?? 0) + 1);
  }

  const active = projects.filter((project) => project.status === 'Active');

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
      isLoading={query.isLoading}
      error={query.error}
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
