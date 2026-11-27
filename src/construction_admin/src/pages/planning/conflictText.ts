import type { Conflict, Plan } from '../../features/planning/planningLogic';
import type { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

/** One sentence about something in the plan that looks like a mistake, shared by the alerts line and the Planner. */
export function conflictText(plan: Plan, c: Conflict, t: ReturnType<typeof useT>): string {
  const name = (id: string | undefined) => plan.projectById.get(id ?? '')?.name ?? '?';
  const base = { name: c.person.name, from: formatDate(c.from), to: formatDate(c.to) };
  const site = plan.projectById.get(c.projectId);

  if (c.kind === 'double') return t('planning.conflict.double', { ...base, a: name(c.projectId), b: name(c.otherProjectId) });
  if (c.kind === 'housedIdle') return t('planning.conflict.housedIdle', { ...base, place: c.place ?? '' });
  if (c.kind === 'certificate') return t('planning.conflict.certificate', { ...base, site: name(c.projectId), certificate: c.certificate ?? '' });
  if (c.kind === 'afterEnd') return t('planning.conflict.afterEnd', { ...base, site: name(c.projectId), date: formatDate(site?.endDate) });

  return t('planning.conflict.beforeStart', { ...base, site: name(c.projectId), date: formatDate(site?.startDate) });
}
