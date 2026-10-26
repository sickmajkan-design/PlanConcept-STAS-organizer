import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import {
  addDays,
  mondayOf,
  positionKey,
  roleCount,
  shortage,
  workdayIndexes,
  type Plan,
} from '../../features/planning/planningLogic';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { PeriodFields, summary, useAttempt, type PlanningActions } from './PlanningDialogs';
import type { DropTarget } from './planningDnd';
import { ProjectLabel } from '../../components/ProjectLabel';

/**
 * What opens when a person is dropped on a site, on the free list or on another person. The drop only chooses
 * who and where; this window says the site and the dates in plain words, lets them be changed, and does
 * nothing until it is confirmed. The change itself is the same assign or swap the other windows make.
 */
export function MoveDialog({
  plan,
  personId,
  target,
  from: initialFrom,
  to: initialTo,
  actions,
  onClose,
}: {
  plan: Plan;
  personId: string;
  target: DropTarget;
  from: string;
  to: string;
  actions: PlanningActions;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const [from, setFrom] = useState(initialFrom);
  const [to, setTo] = useState(initialTo);
  const { failure, pending, attempt } = useAttempt(onClose);

  const person = plan.people.find((p) => p.id === personId);
  const other = target.kind === 'person' ? plan.people.find((p) => p.id === target.personId) : undefined;
  const site = target.kind === 'site' ? plan.projectById.get(target.projectId) : undefined;

  if (!person || (target.kind === 'person' && !other) || (target.kind === 'site' && !site)) return null;

  const awayText = (type: string) => enumLabel('absenceType', type);
  const freeText = t('planning.l.free');
  const days = workdayIndexes(plan, Math.max(0, plan.indexOf(from)), Math.max(0, plan.indexOf(to)));
  const stretch = from === to ? formatDate(from) : `${formatDate(from)} – ${formatDate(to)}`;

  /* ---- the quick choices for how long ---- */
  const weekEnd = addDays(mondayOf(from), 4);
  const siteEnd = site?.endDate && site.endDate > from && site.endDate <= plan.to ? site.endDate : null;
  const presets: { key: string; label: string; to: string }[] = [
    { key: 'day', label: t('planning.m.day'), to: from },
    ...(weekEnd > from && weekEnd <= plan.to ? [{ key: 'week', label: t('planning.m.toWeekEnd'), to: weekEnd }] : []),
    ...(siteEnd ? [{ key: 'site', label: t('planning.m.toSiteEnd', { date: formatDate(siteEnd) }), to: siteEnd }] : []),
  ];

  /* ---- what the change does, said before it is made ---- */
  const notes: { tone: 'success' | 'warning' | 'info'; text: string }[] = [];

  if (site) {
    const fills = days.some((i) => shortage(plan, site, i, person.position) > 0);
    notes.push(
      fills
        ? { tone: 'success', text: t('planning.m.fills', { skill: person.position }) }
        : { tone: 'warning', text: t('planning.m.noNeed', { site: site.name, skill: person.position }) },
    );

    if ((site.endDate && to > site.endDate) || (site.startDate && from < site.startDate)) {
      notes.push({
        tone: 'warning',
        text: t('planning.s.outside', { from: site.startDate ? formatDate(site.startDate) : '…', to: site.endDate ? formatDate(site.endDate) : '…' }),
      });
    }
  }

  if (target.kind !== 'person') {
    // A site this person leaves may be left without their skill; say so, as the leave-approval window does.
    const leaving = new Set(days.map((i) => person.cells[i].project).filter((id): id is string => !!id && id !== site?.id));

    for (const id of leaving) {
      const left = plan.projectById.get(id);
      const need = left?.needs.find((n) => positionKey(n.position) === person.key);

      if (left && need && days.some((i) => person.cells[i].project === id && roleCount(plan, id, i, need.position) - 1 < need.count)) {
        notes.push({ tone: 'warning', text: t('planning.m.leaves', { site: left.name, skill: person.position }) });
      }
    }

    if (leaving.size > 0 && site) notes.push({ tone: 'info', text: t('planning.m.carry') });
  }

  if (days.some((i) => person.cells[i].away)) notes.push({ tone: 'info', text: t('planning.w.awayKept') });

  // A swap only changes the days they are on different sites and both present.
  const swapDays = other ? days.filter((i) => !person.cells[i].away && !other.cells[i].away && person.cells[i].project !== other.cells[i].project) : [];
  const swapNothing = !!other && swapDays.length === 0;

  const title =
    target.kind === 'site'
      ? t('planning.m.title', { name: person.name, site: site?.name ?? '' })
      : target.kind === 'free'
        ? t('planning.m.freeTitle', { name: person.name })
        : t('planning.m.swapTitle', { a: person.name, b: other?.name ?? '' });

  const onSomeSite = days.some((i) => person.cells[i].project);
  const confirmLabel =
    target.kind === 'site' ? (onSomeSite ? t('planning.m.move') : t('planning.m.assign')) : target.kind === 'free' ? t('planning.m.free') : t('planning.m.swap');

  const confirm = () =>
    void attempt(() =>
      target.kind === 'person'
        ? actions.swap(personId, target.personId, from, to)
        : actions.assign(personId, target.kind === 'site' ? target.projectId : null, from, to),
    );

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ pb: 0.5 }}>{title}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {person.position} · <b>{stretch}</b>
          {days.length > 0 ? ` (${t('planning.m.workdays', { count: days.length })})` : ''}
        </Typography>

        <Stack spacing={2}>
          <Stack direction="row" useFlexGap spacing={0.75} sx={{ flexWrap: 'wrap' }}>
            {presets.map((preset) => (
              <Button
                key={preset.key}
                size="small"
                variant={to === preset.to ? 'contained' : 'outlined'}
                color={to === preset.to ? 'primary' : 'inherit'}
                onClick={() => setTo(preset.to)}
                sx={{ textTransform: 'none' }}
              >
                {preset.label}
              </Button>
            ))}
          </Stack>

          <PeriodFields plan={plan} from={from} to={to} onChange={(f, e) => { setFrom(f); setTo(e); }} />

          {other ? (
            <Box>
              <Stack spacing={1}>
                {[
                  { who: person, next: other },
                  { who: other, next: person },
                ].map(({ who, next }) => (
                  <Box key={who.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 1.5, p: 1.25 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {who.name} · {who.position}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {summary(plan, who, from, to, awayText, freeText)} → <b>{summary(plan, next, from, to, awayText, freeText)}</b>
                    </Typography>
                  </Box>
                ))}
              </Stack>
              {person.key === other.key ? (
                <Alert severity="success" icon={false} sx={{ mt: 1, py: 0 }}>{t('planning.m.sameSkill')}</Alert>
              ) : (
                <Alert severity="warning" icon={false} sx={{ mt: 1, py: 0 }}>{t('planning.m.otherSkill', { a: person.position, b: other.position })}</Alert>
              )}
              {swapNothing && <Alert severity="info" icon={false} sx={{ mt: 1, py: 0 }}>{t('planning.m.swapNothing')}</Alert>}
            </Box>
          ) : (
            <Box>
              <Typography variant="body2">
                {t('planning.m.now')}: <b>{summary(plan, person, from, to, awayText, freeText)}</b>
              </Typography>
              <Typography variant="body2">
                {t('planning.m.after')}: <b>{site ? <ProjectLabel name={site.name} projectId={site.id} customerName={site.customerName} /> : freeText}</b>
              </Typography>
              <Stack spacing={0.75} sx={{ mt: 1.25 }}>
                {notes.map((note) => (
                  <Alert key={note.text} severity={note.tone} icon={false} sx={{ py: 0 }}>
                    {note.text}
                  </Alert>
                ))}
              </Stack>
            </Box>
          )}

          {failure && <Alert severity="error">{failure}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={pending || swapNothing || days.length === 0} onClick={confirm}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
