import { AddOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { PlanningNeed } from '../../api/planning';
import {
  candidates,
  isWorkday,
  needOf,
  positionKey,
  shortage,
  type AwayItem,
  type Candidate,
  type Plan,
  type PlanPerson,
} from '../../features/planning/planningLogic';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { siteColor } from './planningUi';

export interface PlanningActions {
  assign: (
    personId: string,
    projectId: string | null,
    from: string,
    to: string,
    options?: { onlyFree?: boolean },
  ) => Promise<void>;
  swap: (aId: string, bId: string, from: string, to: string) => Promise<void>;
  saveNeeds: (projectId: string, needs: PlanningNeed[]) => Promise<void>;
}

/** Runs an action, closing on success and showing the refusal in the dialog on failure. */
function useAttempt(onClose: () => void) {
  const [failure, setFailure] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const attempt = async (action: () => Promise<void>) => {
    setPending(true);
    setFailure(null);

    try {
      await action();
      onClose();
    } catch (error) {
      setFailure(toApiError(error).message);
    } finally {
      setPending(false);
    }
  };

  return { failure, pending, attempt };
}

function PeriodFields({
  plan,
  from,
  to,
  onChange,
}: {
  plan: Plan;
  from: string;
  to: string;
  onChange: (from: string, to: string) => void;
}) {
  const t = useT();

  return (
    <Stack direction="row" spacing={1.5}>
      <TextField
        type="date"
        size="small"
        label={t('planning.from')}
        value={from}
        onChange={(e) => onChange(e.target.value, to < e.target.value ? e.target.value : to)}
        slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: plan.from, max: plan.to } }}
      />
      <TextField
        type="date"
        size="small"
        label={t('planning.to')}
        value={to}
        onChange={(e) => onChange(from > e.target.value ? e.target.value : from, e.target.value)}
        slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: plan.from, max: plan.to } }}
      />
    </Stack>
  );
}

function useCandidateText() {
  const t = useT();

  return (c: Candidate, plan: Plan) => {
    const note =
      c.kind === 'free'
        ? t('planning.cand.free')
        : c.kind === 'partial'
          ? t('planning.cand.partial', { free: c.freeDays, total: c.totalDays })
          : c.kind === 'surplus'
            ? t('planning.cand.surplusAt', { site: plan.projectById.get(c.fromProjectId ?? '')?.name ?? '' })
            : t('planning.cand.otherSkill');

    if (c.tier === 3) return note;

    const fit = c.tier <= 2 ? t('planning.cand.fits') : t('planning.cand.noFit');
    return `${fit} · ${note}`;
  };
}

function CandidateList({ plan, list, onPick, pending }: { plan: Plan; list: Candidate[]; onPick: (c: Candidate) => void; pending: boolean }) {
  const t = useT();
  const text = useCandidateText();

  if (list.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        {t('planning.replace.noOne')}
      </Typography>
    );
  }

  return (
    <List dense disablePadding sx={{ maxHeight: 300, overflow: 'auto' }}>
      {list.map((c) => (
        <ListItemButton key={c.person.id} disabled={pending} onClick={() => onPick(c)} sx={{ borderRadius: 1.5 }}>
          <ListItemText
            primary={`${c.person.name} · ${c.person.position}`}
            secondary={text(c, plan)}
            slotProps={{ secondary: { sx: { color: c.tier <= 2 ? 'success.main' : c.tier === 3 ? 'warning.main' : 'error.main' } } }}
          />
        </ListItemButton>
      ))}
    </List>
  );
}

function summary(plan: Plan, person: PlanPerson, from: string, to: string, awayText: (type: string) => string, freeText: string): string {
  const counts = new Map<string, number>();

  for (let d = from; d <= to; d = new Date(Date.parse(`${d}T00:00:00Z`) + 864e5).toISOString().slice(0, 10)) {
    if (!isWorkday(d)) continue;
    const i = plan.indexOf(d);
    if (i < 0 || i >= plan.days) continue;

    const cell = person.cells[i];
    const label = cell.away ? awayText(cell.away) : cell.project ? (plan.projectById.get(cell.project)?.name ?? '?') : freeText;
    counts.set(label, (counts.get(label) ?? 0) + 1);
  }

  return [...counts.entries()].map(([label, n]) => `${label} (${n})`).join(', ') || '—';
}

/** Move one person, or swap them with somebody else, over a stretch of days. */
export function WorkerDialog({
  plan,
  personId,
  from: initialFrom,
  to: initialTo,
  actions,
  onClose,
}: {
  plan: Plan;
  personId: string;
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
  if (!person) return null;

  const awayText = (type: string) => enumLabel('absenceType', type);
  const days = Array.from({ length: Math.max(0, (Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / 864e5 + 1) }, (_, k) =>
    plan.indexOf(new Date(Date.parse(`${from}T00:00:00Z`) + k * 864e5).toISOString().slice(0, 10)),
  ).filter((i) => i >= 0 && i < plan.days && isWorkday(plan.date(i)));

  const others = plan.people.filter(
    (o) =>
      o.id !== personId &&
      days.some((i) => !o.cells[i].away && !person.cells[i].away && o.cells[i].project !== person.cells[i].project),
  );

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ pb: 0.5 }}>{person.name}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {person.position} · {formatDate(from)}
          {from !== to ? ` – ${formatDate(to)}` : ''} · <b>{summary(plan, person, from, to, awayText, t('planning.l.free'))}</b>
        </Typography>

        <Stack spacing={2.5}>
          <PeriodFields plan={plan} from={from} to={to} onChange={(f, e) => { setFrom(f); setTo(e); }} />

          <Box>
            <Typography variant="overline" color="text.secondary">
              {t('planning.w.assignTo')}
            </Typography>
            <Stack direction="row" useFlexGap spacing={0.75} sx={{ flexWrap: 'wrap', mt: 0.5 }}>
              {plan.projects.map((s) => (
                <Button
                  key={s.id}
                  variant="outlined"
                  size="small"
                  color="inherit"
                  disabled={pending}
                  startIcon={<Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: siteColor(s.id) }} />}
                  onClick={() => void attempt(() => actions.assign(personId, s.id, from, to))}
                  sx={{ textTransform: 'none' }}
                >
                  {s.name}
                </Button>
              ))}
              <Button variant="outlined" size="small" color="inherit" disabled={pending} onClick={() => void attempt(() => actions.assign(personId, null, from, to))} sx={{ textTransform: 'none' }}>
                {t('planning.l.free')}
              </Button>
            </Stack>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75 }}>
              {t('planning.w.awayKept')}
            </Typography>
          </Box>

          <Box>
            <Typography variant="overline" color="text.secondary">
              {t('planning.w.swapWith')}
            </Typography>
            {others.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('planning.w.noSwap')}
              </Typography>
            ) : (
              <List dense disablePadding sx={{ maxHeight: 200, overflow: 'auto' }}>
                {others.map((o) => (
                  <ListItemButton key={o.id} disabled={pending} onClick={() => void attempt(() => actions.swap(personId, o.id, from, to))} sx={{ borderRadius: 1.5 }}>
                    <ListItemText primary={`${o.name} · ${o.position}`} secondary={summary(plan, o, from, to, awayText, t('planning.l.free'))} />
                  </ListItemButton>
                ))}
              </List>
            )}
          </Box>

          {failure && <Alert severity="error">{failure}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('planning.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}

/** Fill a site: free people first, those of the skill that is missing at the top. */
export function SiteDialog({
  plan,
  projectId,
  from: initialFrom,
  to: initialTo,
  position,
  actions,
  onClose,
}: {
  plan: Plan;
  projectId: string;
  from: string;
  to: string;
  position?: string;
  actions: PlanningActions;
  onClose: () => void;
}) {
  const t = useT();
  const [from, setFrom] = useState(initialFrom);
  const [to, setTo] = useState(initialTo);
  const { failure, pending, attempt } = useAttempt(onClose);

  const site = plan.projectById.get(projectId);
  if (!site) return null;

  const indexes: number[] = [];
  for (let d = from; d <= to; d = new Date(Date.parse(`${d}T00:00:00Z`) + 864e5).toISOString().slice(0, 10)) {
    const i = plan.indexOf(d);
    if (i >= 0 && i < plan.days && isWorkday(d)) indexes.push(i);
  }

  const lacking = new Set(
    position
      ? [positionKey(position)]
      : site.needs.filter((n) => indexes.some((i) => shortage(plan, site, i, n.position) > 0)).map((n) => positionKey(n.position)),
  );
  const lackingNames = site.needs.filter((n) => lacking.has(positionKey(n.position))).map((n) => n.position);

  const rank = (p: PlanPerson) => (lacking.has(p.key) ? 0 : 1);

  const free = plan.people
    .map((p) => ({
      p,
      free: indexes.filter((i) => !p.cells[i].project && !p.cells[i].away).length,
      away: indexes.filter((i) => p.cells[i].away).length,
    }))
    .filter((x) => x.free > 0)
    .sort((a, b) => rank(a.p) - rank(b.p) || b.free - a.free || a.p.name.localeCompare(b.p.name));

  const move = plan.people
    .filter((p) => indexes.some((i) => p.cells[i].project && p.cells[i].project !== projectId && !p.cells[i].away))
    .sort((a, b) => rank(a) - rank(b) || a.name.localeCompare(b.name));

  const pick = (p: PlanPerson, onlyFree: boolean) => void attempt(() => actions.assign(p.id, projectId, from, to, { onlyFree }));

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ pb: 0.5 }}>{site.name}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary">
          {t('planning.s.needsLine', { total: needOf(site), list: site.needs.map((n) => `${n.count} ${n.position}`).join(', ') || '—' })}
        </Typography>
        {lackingNames.length > 0 && (
          <Typography variant="body2" color="error" sx={{ fontWeight: 600, mt: 0.5 }}>
            {t('planning.s.short', { list: lackingNames.join(', ') })}
          </Typography>
        )}

        <Stack spacing={2.5} sx={{ mt: 2 }}>
          <PeriodFields plan={plan} from={from} to={to} onChange={(f, e) => { setFrom(f); setTo(e); }} />

          <Box>
            <Typography variant="overline" color="text.secondary">
              {t('planning.s.freeIn')}
            </Typography>
            {free.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('planning.s.noFree')}
              </Typography>
            ) : (
              <List dense disablePadding sx={{ maxHeight: 220, overflow: 'auto' }}>
                {free.map((x) => (
                  <ListItemButton key={x.p.id} disabled={pending} onClick={() => pick(x.p, true)} sx={{ borderRadius: 1.5 }}>
                    <ListItemText
                      primary={`${x.p.name} · ${x.p.position}`}
                      secondary={`${lacking.has(x.p.key) ? `${t('planning.cand.fits')} · ` : ''}${t('planning.s.freeDays', { free: x.free, total: indexes.length })}${x.away ? `, ${t('planning.s.awayDays', { count: x.away })}` : ''}`}
                      slotProps={{ secondary: { sx: { color: lacking.has(x.p.key) ? 'success.main' : 'text.secondary' } } }}
                    />
                  </ListItemButton>
                ))}
              </List>
            )}
          </Box>

          <Box>
            <Typography variant="overline" color="text.secondary">
              {t('planning.s.moveFrom')}
            </Typography>
            <List dense disablePadding sx={{ maxHeight: 180, overflow: 'auto' }}>
              {move.map((p) => {
                const at = indexes.map((i) => p.cells[i].project).find((id) => id && id !== projectId);
                return (
                  <ListItemButton key={p.id} disabled={pending} onClick={() => pick(p, false)} sx={{ borderRadius: 1.5 }}>
                    <ListItemText
                      primary={`${p.name} · ${p.position}`}
                      secondary={`${lacking.has(p.key) ? `${t('planning.cand.fits')} · ` : ''}${plan.projectById.get(at ?? '')?.name ?? ''}`}
                      slotProps={{ secondary: { sx: { color: lacking.has(p.key) ? 'success.main' : 'text.secondary' } } }}
                    />
                  </ListItemButton>
                );
              })}
            </List>
          </Box>

          {failure && <Alert severity="error">{failure}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('planning.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}

/** Pick somebody to stand in for an absent worker, for the days of the absence only. */
export function ReplaceDialog({
  plan,
  item,
  actions,
  onClose,
}: {
  plan: Plan;
  item: AwayItem;
  actions: PlanningActions;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { failure, pending, attempt } = useAttempt(onClose);
  const site = plan.projectById.get(item.projectId);
  const list = candidates(plan, item.person.id, item.projectId, item.days);

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ pb: 0.5 }}>{t('planning.replace.dialogTitle', { name: item.person.name })}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {item.person.position} · {site?.name} · {enumLabel('absenceType', item.type)} {formatDate(item.from)} – {formatDate(item.to)}
        </Typography>
        <Typography variant="overline" color="text.secondary">
          {t('planning.replace.suggested')}
        </Typography>
        <CandidateList
          plan={plan}
          list={list}
          pending={pending}
          onPick={(c) => void attempt(() => actions.assign(c.person.id, item.projectId, item.from, item.to, { onlyFree: c.tier === 2 }))}
        />
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>
          {t('planning.replace.note', { name: item.person.name, site: site?.name ?? '' })}
        </Typography>
        {failure && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {failure}
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('planning.replace.skip')}</Button>
      </DialogActions>
    </Dialog>
  );
}

/** How many people of each position a project needs. */
export function NeedsDialog({
  plan,
  projectId,
  actions,
  onClose,
}: {
  plan: Plan;
  projectId: string;
  actions: PlanningActions;
  onClose: () => void;
}) {
  const t = useT();
  const site = plan.projectById.get(projectId);
  const [rows, setRows] = useState<{ position: string; count: string }[]>(
    () => (site?.needs ?? []).map((n) => ({ position: n.position, count: String(n.count) })),
  );
  const { failure, pending, attempt } = useAttempt(onClose);

  if (!site) return null;

  const update = (index: number, patch: Partial<{ position: string; count: string }>) =>
    setRows(rows.map((r, i) => (i === index ? { ...r, ...patch } : r)));

  const valid = rows.every((r) => r.position.trim() === '' || (Number(r.count) >= 0 && Number.isInteger(Number(r.count))));

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle sx={{ pb: 0.5 }}>{t('planning.nd.title', { name: site.name })}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('planning.nd.help')}
        </Typography>
        <Stack spacing={1.25}>
          {rows.map((row, i) => (
            <Stack key={i} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Autocomplete
                freeSolo
                size="small"
                options={plan.positions}
                value={row.position}
                onInputChange={(_, value) => update(i, { position: value })}
                sx={{ flex: 1 }}
                renderInput={(params) => <TextField {...params} label={t('planning.nd.position')} />}
              />
              <TextField
                size="small"
                type="number"
                label={t('planning.nd.count')}
                value={row.count}
                onChange={(e) => update(i, { count: e.target.value })}
                sx={{ width: 90 }}
                slotProps={{ htmlInput: { min: 0, max: 500 } }}
              />
              <IconButton aria-label={t('common.delete')} onClick={() => setRows(rows.filter((_, k) => k !== i))}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Stack>
          ))}
          <Box>
            <Button startIcon={<AddOutlined />} onClick={() => setRows([...rows, { position: '', count: '1' }])}>
              {t('planning.nd.add')}
            </Button>
          </Box>
          {failure && <Alert severity="error">{failure}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!valid || pending}
          onClick={() =>
            void attempt(() =>
              actions.saveNeeds(
                projectId,
                rows.filter((r) => r.position.trim() !== '' && Number(r.count) > 0).map((r) => ({ position: r.position.trim(), count: Number(r.count) })),
              ),
            )
          }
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
