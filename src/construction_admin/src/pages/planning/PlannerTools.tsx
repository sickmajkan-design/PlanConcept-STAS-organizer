import { DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  LinearProgress,
  MenuItem,
  Paper,
  Snackbar,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';

import { toApiError } from '../../api/apiError';
import type { Crew, PlannedHoursProject, WeekApplyResult } from '../../api/planning';
import { addDays, diffDays, mondayOf, periodBounds, segmentsOf, type Plan } from '../../features/planning/planningLogic';
import { usePlannedHours, usePlanningActions, usePlanningCrews, usePlanningTemplates } from '../../features/planning/usePlanning';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { siteColor } from './planningUi';

/** A message with, when there is one, how to take the action back. */
interface Notice {
  message: string;
  undo?: () => Promise<void>;
}

function NoticeBar({ notice, onClose }: { notice: Notice | null; onClose: () => void }) {
  const t = useT();
  const [failed, setFailed] = useState<string | null>(null);

  return (
    <>
      <Snackbar
        open={!!notice}
        autoHideDuration={9000}
        onClose={(_, reason) => reason !== 'clickaway' && onClose()}
        message={notice?.message}
        action={
          notice?.undo && (
            <Button
              color="inherit"
              size="small"
              onClick={() => {
                const run = notice.undo!;
                onClose();
                run().catch((e: unknown) => setFailed(toApiError(e).message));
              }}
            >
              {t('planning.undo')}
            </Button>
          )
        }
      />
      <Snackbar open={!!failed} autoHideDuration={8000} onClose={() => setFailed(null)}>
        <Alert severity="error" onClose={() => setFailed(null)}>
          {failed}
        </Alert>
      </Snackbar>
    </>
  );
}

/* ---------------------------------------------------------------------------------------------------- week tools */

/**
 * Asks what putting a week (or a template) on a week would do, and does it on confirmation. Only free days are ever filled,
 * so nothing a person already has is overwritten, and the days written are exactly what taking it back has to free.
 */
function ApplyWeekDialog({
  title,
  previewKey,
  preview,
  run,
  onClose,
  onDone,
  children,
}: {
  title: string;
  /** Another key asks the preview again. */
  previewKey: string;
  preview: () => Promise<WeekApplyResult>;
  run: () => Promise<WeekApplyResult>;
  onClose: () => void;
  onDone: (result: WeekApplyResult) => void;
  children?: ReactNode;
}) {
  const t = useT();
  const [result, setResult] = useState<WeekApplyResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const previewRef = useRef(preview);
  previewRef.current = preview;

  useEffect(() => {
    let live = true;
    setResult(null);
    setError(null);
    previewRef.current().then((r) => live && setResult(r)).catch((e: unknown) => live && setError(toApiError(e).message));

    return () => {
      live = false;
    };
  }, [previewKey]);

  const apply = async () => {
    setBusy(true);
    try {
      onDone(await run());
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
      setBusy(false);
    }
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {children}
          {error && <Alert severity="error">{error}</Alert>}
          {!result && !error && (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
              <CircularProgress size={24} />
            </Box>
          )}
          {result && (
            <Stack spacing={0.75}>
              <Typography sx={{ fontWeight: 600 }}>{result.days > 0 ? t('planner.apply.will', { days: result.days, people: result.people }) : t('planner.apply.nothing')}</Typography>
              {result.skippedTaken > 0 && <Typography variant="body2" color="text.secondary">{t('planner.apply.taken', { count: result.skippedTaken })}</Typography>}
              {result.skippedOnLeave > 0 && <Typography variant="body2" color="text.secondary">{t('planner.apply.leave', { count: result.skippedOnLeave })}</Typography>}
              {result.skippedSite > 0 && <Typography variant="body2" color="text.secondary">{t('planner.apply.site', { count: result.skippedSite })}</Typography>}
              <Typography variant="caption" color="text.secondary">
                {t('planner.apply.note')}
              </Typography>
            </Stack>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="inherit">
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void apply()} disabled={!result || result.days === 0 || busy}>
          {t('planner.apply.confirm')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Copy the week before, put a template on the week, save the week as a template. */
export function WeekTools({ monday, canTemplates }: { monday: string; canTemplates: boolean }) {
  const t = useT();
  const actions = usePlanningActions();
  const [dialog, setDialog] = useState<'copy' | 'templates' | { templateId: string; name: string } | null>(null);
  const [source, setSource] = useState(() => addDays(monday, -7));
  const [notice, setNotice] = useState<Notice | null>(null);

  const freeWritten = (written: WeekApplyResult) => async () => {
    for (const r of written.ranges) {
      await actions.assign.mutateAsync({ employeeId: r.employeeId, projectId: null, from: r.from, to: r.to });
    }
  };

  const done = (result: WeekApplyResult) => setNotice({ message: t('planner.apply.done', { days: result.days, people: result.people }), undo: result.days > 0 ? freeWritten(result) : undefined });

  return (
    <>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5 }}>
        <Button size="small" variant="outlined" color="inherit" onClick={() => { setSource(addDays(monday, -7)); setDialog('copy'); }} sx={{ textTransform: 'none' }}>
          {t('planner.week.copy')}
        </Button>
        {canTemplates && (
          <Button size="small" variant="outlined" color="inherit" onClick={() => setDialog('templates')} sx={{ textTransform: 'none' }}>
            {t('planner.week.templates')}
          </Button>
        )}
      </Stack>

      {dialog === 'copy' && (
        <ApplyWeekDialog
          title={t('planner.copy.title', { to: formatDate(monday) })}
          previewKey={source}
          preview={() => actions.copyWeek.mutateAsync({ fromWeek: source, toWeek: monday, dryRun: true })}
          run={() => actions.copyWeek.mutateAsync({ fromWeek: source, toWeek: monday })}
          onClose={() => setDialog(null)}
          onDone={done}
        >
          <TextField
            type="date"
            size="small"
            label={t('planner.copy.from')}
            value={source}
            onChange={(e) => e.target.value && setSource(mondayOf(e.target.value))}
            helperText={t('planner.copy.fromHint', { from: formatDate(source), to: formatDate(addDays(source, 4)) })}
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </ApplyWeekDialog>
      )}

      {dialog === 'templates' && <TemplatesDialog monday={monday} onClose={() => setDialog(null)} onPick={(template) => setDialog(template)} onSaved={(name) => setNotice({ message: t('planner.tpl.saved', { name }) })} />}

      {typeof dialog === 'object' && dialog && (
        <ApplyWeekDialog
          title={t('planner.tpl.applyTitle', { name: dialog.name, to: formatDate(monday) })}
          previewKey={dialog.templateId}
          preview={() => actions.applyTemplate.mutateAsync({ templateId: dialog.templateId, week: monday, dryRun: true })}
          run={() => actions.applyTemplate.mutateAsync({ templateId: dialog.templateId, week: monday })}
          onClose={() => setDialog(null)}
          onDone={done}
        />
      )}

      <NoticeBar notice={notice} onClose={() => setNotice(null)} />
    </>
  );
}

function TemplatesDialog({ monday, onClose, onPick, onSaved }: { monday: string; onClose: () => void; onPick: (template: { templateId: string; name: string }) => void; onSaved: (name: string) => void }) {
  const t = useT();
  const actions = usePlanningActions();
  const { data, isLoading, error } = usePlanningTemplates(true);
  const [name, setName] = useState('');
  const [problem, setProblem] = useState<string | null>(null);

  const save = async () => {
    setProblem(null);
    try {
      await actions.saveTemplate.mutateAsync({ name: name.trim(), week: monday });
      onSaved(name.trim());
      setName('');
    } catch (e) {
      setProblem(toApiError(e).message);
    }
  };

  const remove = async (id: string) => {
    setProblem(null);
    try {
      await actions.deleteTemplate.mutateAsync(id);
    } catch (e) {
      setProblem(toApiError(e).message);
    }
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('planner.tpl.title')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {(error || problem) && <Alert severity="error">{problem ?? toApiError(error).message}</Alert>}
          {isLoading && <CircularProgress size={22} />}
          {data && data.length === 0 && (
            <Typography variant="body2" color="text.secondary">
              {t('planner.tpl.none')}
            </Typography>
          )}
          {data?.map((tpl) => (
            <Stack key={tpl.id} direction="row" spacing={1} sx={{ alignItems: 'center', py: 0.5, borderBottom: 1, borderColor: 'divider' }}>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography sx={{ fontWeight: 600 }}>{tpl.name}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {t('planner.tpl.meta', { people: tpl.people, days: tpl.days })}
                </Typography>
              </Box>
              <Button size="small" variant="outlined" onClick={() => onPick({ templateId: tpl.id, name: tpl.name })} sx={{ textTransform: 'none' }}>
                {t('planner.tpl.apply')}
              </Button>
              <Tooltip title={t('common.delete')}>
                <IconButton size="small" aria-label={t('common.delete')} onClick={() => void remove(tpl.id)}>
                  <DeleteOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            </Stack>
          ))}

          <Stack spacing={1} sx={{ pt: 1 }}>
            <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 600 }}>
              {t('planner.tpl.saveTitle', { from: formatDate(monday), to: formatDate(addDays(monday, 4)) })}
            </Typography>
            <Stack direction="row" spacing={1}>
              <TextField size="small" fullWidth value={name} onChange={(e) => setName(e.target.value)} placeholder={t('planner.tpl.name')} slotProps={{ htmlInput: { maxLength: 120, 'aria-label': t('planner.tpl.name') } }} />
              <Button variant="contained" onClick={() => void save()} disabled={!name.trim() || actions.saveTemplate.isPending} sx={{ textTransform: 'none', flexShrink: 0 }}>
                {t('planner.tpl.save')}
              </Button>
            </Stack>
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="inherit">
          {t('common.close')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/* ---------------------------------------------------------------------------------------------------- crews */

/** Names the sites the members of a crew are on, on a day: "Site A (4), free (1)". */
function whereToday(plan: Plan, crew: Crew, day: number, freeLabel: string, awayLabel: string): string {
  const counts = new Map<string, number>();

  for (const m of crew.members) {
    const person = plan.people.find((p) => p.id === m.employeeId);
    const cell = person?.cells[day];
    const label = !cell ? '?' : cell.away ? awayLabel : cell.project ? (plan.projectById.get(cell.project)?.name ?? '?') : freeLabel;
    counts.set(label, (counts.get(label) ?? 0) + 1);
  }

  return [...counts.entries()].map(([label, n]) => `${label} (${n})`).join(', ');
}

export function CrewsSection({ plan, day, focusDay }: { plan: Plan; day: number; focusDay: string }) {
  const t = useT();
  const { data: crews, isLoading, error } = usePlanningCrews(true);
  const actions = usePlanningActions();
  const [edit, setEdit] = useState<Crew | 'new' | null>(null);
  const [assign, setAssign] = useState<Crew | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const [problem, setProblem] = useState<string | null>(null);

  const remove = async (crew: Crew) => {
    setProblem(null);
    try {
      await actions.deleteCrew.mutateAsync(crew.id);
    } catch (e) {
      setProblem(toApiError(e).message);
    }
  };

  return (
    <Paper variant="outlined" component="section" aria-label={t('planner.section.crews')} sx={{ borderRadius: 2.5, p: { xs: 1.5, sm: 2 } }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }} useFlexGap>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          {t('planner.section.crews')}
        </Typography>
        <Box sx={{ flex: 1 }} />
        <Button size="small" variant="contained" onClick={() => setEdit('new')} sx={{ textTransform: 'none' }}>
          {t('planner.crew.new')}
        </Button>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {t('planner.crew.hint')}
      </Typography>

      {(error || problem) && <Alert severity="error" sx={{ mb: 1.5 }}>{problem ?? toApiError(error).message}</Alert>}
      {isLoading && <CircularProgress size={22} />}
      {crews && crews.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          {t('planner.crew.none')}
        </Typography>
      )}

      <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'repeat(2, minmax(0, 1fr))' } }}>
        {crews?.map((crew) => (
          <Paper key={crew.id} variant="outlined" sx={{ borderRadius: 2.5, p: 1.5, display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700, flex: 1, minWidth: 0 }}>
                {crew.name}
              </Typography>
              <Chip size="small" label={t('planner.crew.members', { count: crew.members.length })} />
            </Stack>
            {crew.leaderEmployeeId && (
              <Typography variant="caption" color="text.secondary">
                {t('planner.crew.leader', { name: crew.members.find((m) => m.employeeId === crew.leaderEmployeeId)?.fullName ?? '?' })}
              </Typography>
            )}
            <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
              {crew.members.map((m) => (
                <Chip key={m.employeeId} size="small" variant="outlined" label={`${m.fullName} · ${m.position}`} />
              ))}
            </Stack>
            {day >= 0 && (
              <Typography variant="body2" color="text.secondary">
                {t('planner.crew.where', { date: formatDate(focusDay), where: whereToday(plan, crew, day, t('planner.crew.free'), t('planner.crew.away')) })}
              </Typography>
            )}
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', pt: 0.5 }}>
              <Button size="small" variant="contained" onClick={() => setAssign(crew)} sx={{ textTransform: 'none' }}>
                {t('planner.crew.assign')}
              </Button>
              <Button size="small" variant="outlined" color="inherit" onClick={() => setEdit(crew)} sx={{ textTransform: 'none' }}>
                {t('common.edit')}
              </Button>
              <Button size="small" color="inherit" onClick={() => void remove(crew)} sx={{ textTransform: 'none', ml: 'auto' }}>
                {t('common.delete')}
              </Button>
            </Stack>
          </Paper>
        ))}
      </Box>

      {edit && <CrewDialog plan={plan} crew={edit === 'new' ? null : edit} crews={crews ?? []} onClose={() => setEdit(null)} />}
      {assign && <AssignCrewDialog plan={plan} crew={assign} focusDay={focusDay} onClose={() => setAssign(null)} onDone={setNotice} />}
      <NoticeBar notice={notice} onClose={() => setNotice(null)} />
    </Paper>
  );
}

function CrewDialog({ plan, crew, crews, onClose }: { plan: Plan; crew: Crew | null; crews: Crew[]; onClose: () => void }) {
  const t = useT();
  const actions = usePlanningActions();
  const [name, setName] = useState(crew?.name ?? '');
  const [memberIds, setMemberIds] = useState<string[]>(crew?.members.map((m) => m.employeeId) ?? []);
  const [leader, setLeader] = useState<string>(crew?.leaderEmployeeId ?? '');
  const [error, setError] = useState<string | null>(null);

  // Somebody who is in another crew cannot be picked here: a worker belongs to one crew only.
  const taken = useMemo(() => new Set(crews.filter((c) => c.id !== crew?.id).flatMap((c) => c.members.map((m) => m.employeeId))), [crews, crew]);
  const options = useMemo(() => plan.people.filter((p) => !taken.has(p.id)), [plan, taken]);
  const picked = options.filter((p) => memberIds.includes(p.id));

  const save = async () => {
    setError(null);
    try {
      await actions.saveCrew.mutateAsync({ id: crew?.id, name: name.trim(), leaderEmployeeId: leader && memberIds.includes(leader) ? leader : null, employeeIds: memberIds });
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{crew ? t('planner.crew.edit') : t('planner.crew.new')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField size="small" label={t('planner.crew.name')} value={name} onChange={(e) => setName(e.target.value)} slotProps={{ htmlInput: { maxLength: 120 } }} autoFocus />
          <Autocomplete
            multiple
            size="small"
            options={options}
            value={picked}
            onChange={(_, next) => setMemberIds(next.map((p) => p.id))}
            getOptionLabel={(p) => `${p.name} · ${p.position}`}
            isOptionEqualToValue={(a, b) => a.id === b.id}
            renderInput={(params) => <TextField {...params} label={t('planner.crew.pick')} />}
            noOptionsText={t('planner.crew.noOptions')}
          />
          <TextField select size="small" label={t('planner.crew.leaderLabel')} value={memberIds.includes(leader) ? leader : ''} onChange={(e) => setLeader(e.target.value)} disabled={picked.length === 0}>
            <MenuItem value="">{t('planner.crew.noLeader')}</MenuItem>
            {picked.map((p) => (
              <MenuItem key={p.id} value={p.id}>
                {p.name}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="inherit">
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void save()} disabled={!name.trim() || memberIds.length === 0 || actions.saveCrew.isPending}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function AssignCrewDialog({ plan, crew, focusDay, onClose, onDone }: { plan: Plan; crew: Crew; focusDay: string; onClose: () => void; onDone: (notice: Notice) => void }) {
  const t = useT();
  const actions = usePlanningActions();
  const sites = useMemo(() => plan.projects.filter((p) => p.status !== 'Completed' && p.status !== 'Cancelled').sort((a, b) => a.name.localeCompare(b.name)), [plan]);
  const [projectId, setProjectId] = useState('');
  const [from, setFrom] = useState(focusDay);
  const [to, setTo] = useState(addDays(focusDay, 4));
  const [onlyFree, setOnlyFree] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const valid = !!projectId && from <= to && diffDays(from, to) < 400;

  // Members on approved leave on any of the days: they are posted all the same, and go back to the site after the leave.
  const onLeave = useMemo(() => {
    if (!valid) return 0;
    const lo = plan.indexOf(from);
    const hi = plan.indexOf(to);
    if (lo < 0 || hi < 0) return 0;

    return crew.members.filter((m) => plan.people.find((p) => p.id === m.employeeId)?.cells.slice(lo, hi + 1).some((c) => c.away)).length;
  }, [plan, crew, from, to, valid]);

  const apply = async () => {
    setError(null);

    const before = crew.members.map((m) => ({ id: m.employeeId, segments: segmentsOf(plan, m.employeeId, from, to) }));

    try {
      const moved = await actions.assignCrew.mutateAsync({ crewId: crew.id, projectId, from, to, onlyFreeDays: onlyFree });
      const site = plan.projectById.get(projectId)?.name ?? '';

      onDone({
        message: t('planner.crew.done', { crew: crew.name, count: moved, site }),
        undo: async () => {
          for (const member of before) {
            await actions.assign.mutateAsync({ employeeId: member.id, projectId: null, from, to });
            for (const seg of member.segments) {
              if (seg.projectId) await actions.assign.mutateAsync({ employeeId: member.id, projectId: seg.projectId, from: seg.from, to: seg.to });
            }
          }
        },
      });
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('planner.crew.assignTitle', { crew: crew.name })}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField select size="small" label={t('planner.crew.site')} value={projectId} onChange={(e) => setProjectId(e.target.value)}>
            {sites.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                <Box component="span" aria-hidden sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: siteColor(s.id), mr: 1, flexShrink: 0 }} />
                {s.name}
              </MenuItem>
            ))}
          </TextField>
          <Stack direction="row" spacing={1}>
            <TextField type="date" size="small" label={t('planning.from')} value={from} onChange={(e) => e.target.value && setFrom(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} fullWidth />
            <TextField type="date" size="small" label={t('planning.to')} value={to} onChange={(e) => e.target.value && setTo(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} fullWidth />
          </Stack>
          <FormControlLabel control={<Checkbox checked={onlyFree} onChange={(e) => setOnlyFree(e.target.checked)} />} label={t('planner.crew.onlyFree')} />
          <Typography variant="body2" color="text.secondary">
            {t('planner.crew.assignNote', { count: crew.members.length })}
          </Typography>
          {onLeave > 0 && <Alert severity="info">{t('planner.crew.onLeave', { count: onLeave })}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="inherit">
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void apply()} disabled={!valid || actions.assignCrew.isPending}>
          {t('planner.crew.assign')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/* ---------------------------------------------------------------------------------------------------- hours */

type Period = 'week' | 'month';

/** Over the plan by more than this share is flagged red; under it by more than the second one, amber. */
const OVER = 0.1;
const UNDER = 0.2;

function toneOf(row: PlannedHoursProject): 'error' | 'warning' | 'success' | 'neutral' {
  if (row.plannedHoursSoFar <= 0) return row.reportedHours > 0 ? 'warning' : 'neutral';
  const ratio = (row.reportedHours - row.plannedHoursSoFar) / row.plannedHoursSoFar;

  return ratio > OVER ? 'error' : ratio < -UNDER ? 'warning' : 'success';
}

export function HoursSection({ focusDay }: { focusDay: string }) {
  const t = useT();
  const [period, setPeriod] = useState<Period>('week');
  // The sites furthest from their plan come first, so a long list is read from the top and then left.
  const [limit, setLimit] = useState(12);

  const monday = mondayOf(focusDay);
  const [first, last] = period === 'week' ? [monday, addDays(monday, 6)] : periodBounds('M', focusDay);

  const { data, isLoading, error } = usePlannedHours(first, last, true);
  const rows = useMemo(() => [...(data?.projects ?? [])].sort((a, b) => Math.abs(b.reportedHours - b.plannedHoursSoFar) - Math.abs(a.reportedHours - a.plannedHoursSoFar)), [data]);
  const total = useMemo(() => rows.reduce((s, r) => ({ planned: s.planned + r.plannedHoursSoFar, reported: s.reported + r.reportedHours }), { planned: 0, reported: 0 }), [rows]);
  const worst = rows.find((r) => toneOf(r) === 'error');
  const scale = Math.max(1, ...rows.map((r) => Math.max(r.plannedHoursSoFar, r.reportedHours)));

  return (
    <Paper variant="outlined" component="section" aria-label={t('planner.section.hours')} sx={{ borderRadius: 2.5, p: { xs: 1.5, sm: 2 } }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }} useFlexGap>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          {t('planner.hours.title', { from: formatDate(first), to: formatDate(last) })}
        </Typography>
        <Box sx={{ flex: 1 }} />
        <ToggleButtonGroup size="small" exclusive value={period} onChange={(_, next: Period | null) => next && setPeriod(next)} aria-label={t('planner.hours.period')} sx={{ '& .MuiToggleButton-root': { textTransform: 'none', px: 1.5, py: 0.5 } }}>
          <ToggleButton value="week">{t('planner.hours.week')}</ToggleButton>
          <ToggleButton value="month">{t('planner.hours.month')}</ToggleButton>
        </ToggleButtonGroup>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {t('planner.hours.hint', { hours: data?.hoursPerDay ?? 8 })}
      </Typography>

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}
      {isLoading && <CircularProgress size={22} />}
      {data && rows.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          {t('planner.hours.none')}
        </Typography>
      )}

      {worst && (
        <Alert severity="warning" sx={{ mb: 1.5 }}>
          {t('planner.hours.over', { site: worst.name, hours: Math.round(worst.reportedHours - worst.plannedHoursSoFar) })}
        </Alert>
      )}

      {rows.length > 0 && (
        <Box sx={{ overflowX: 'auto' }}>
          <Box sx={{ minWidth: 520, display: 'grid', gap: 0.5 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'minmax(140px, 1.6fr) minmax(120px, 2fr) 76px 76px 76px', gap: 1, px: 0.5, color: 'text.secondary', typography: 'caption', fontWeight: 600 }}>
              <span>{t('planner.week.site')}</span>
              <span />
              <Box sx={{ textAlign: 'right' }}>{t('planner.hours.plan')}</Box>
              <Box sx={{ textAlign: 'right' }}>{t('planner.hours.reported')}</Box>
              <Box sx={{ textAlign: 'right' }}>{t('planner.hours.diff')}</Box>
            </Box>
            {rows.slice(0, limit).map((r) => {
              const tone = toneOf(r);
              const diff = Math.round((r.reportedHours - r.plannedHoursSoFar) * 10) / 10;

              return (
                <Box key={r.projectId} sx={{ display: 'grid', gridTemplateColumns: 'minmax(140px, 1.6fr) minmax(120px, 2fr) 76px 76px 76px', gap: 1, alignItems: 'center', py: 0.75, px: 0.5, borderTop: 1, borderColor: 'divider' }}>
                  <Typography variant="body2" sx={{ fontWeight: 600, borderLeft: 4, borderLeftColor: siteColor(r.projectId), pl: 1 }}>
                    {r.name}
                  </Typography>
                  <Box sx={{ position: 'relative', height: 14 }}>
                    <LinearProgress variant="determinate" value={Math.min(100, (r.plannedHoursSoFar / scale) * 100)} sx={{ position: 'absolute', inset: 0, height: 6, top: 0, borderRadius: 3, bgcolor: 'action.hover', '& .MuiLinearProgress-bar': { bgcolor: 'text.disabled' } }} />
                    <LinearProgress variant="determinate" value={Math.min(100, (r.reportedHours / scale) * 100)} color={tone === 'neutral' ? 'inherit' : tone} sx={{ position: 'absolute', left: 0, right: 0, top: 8, height: 6, borderRadius: 3, bgcolor: 'action.hover' }} />
                  </Box>
                  <Box sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{Math.round(r.plannedHoursSoFar)} h</Box>
                  <Box sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{Math.round(r.reportedHours * 10) / 10} h</Box>
                  <Box sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', fontWeight: 700, color: tone === 'neutral' ? 'text.secondary' : `${tone}.dark` }}>
                    {diff > 0 ? '+' : ''}
                    {diff} h
                  </Box>
                </Box>
              );
            })}
            {rows.length > limit && (
              <Button size="small" variant="outlined" color="inherit" onClick={() => setLimit(limit + 12)} sx={{ alignSelf: 'flex-start', textTransform: 'none', my: 1 }}>
                {t('planner.sites.more', { count: rows.length - limit })}
              </Button>
            )}
            <Box sx={{ display: 'grid', gridTemplateColumns: 'minmax(140px, 1.6fr) minmax(120px, 2fr) 76px 76px 76px', gap: 1, py: 0.75, px: 0.5, borderTop: 2, borderColor: 'divider', fontWeight: 700 }}>
              <span>{t('planner.hours.total')}</span>
              <span />
              <Box sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{Math.round(total.planned)} h</Box>
              <Box sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{Math.round(total.reported * 10) / 10} h</Box>
              <Box sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                {total.reported - total.planned > 0 ? '+' : ''}
                {Math.round((total.reported - total.planned) * 10) / 10} h
              </Box>
            </Box>
          </Box>
        </Box>
      )}
    </Paper>
  );
}
