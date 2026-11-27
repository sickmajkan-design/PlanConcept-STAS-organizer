import { CheckCircleOutlineOutlined } from '@mui/icons-material';
import { ChevronLeftOutlined, ChevronRightOutlined } from '@mui/icons-material';
import { Alert, Box, Button, ButtonBase, Chip, LinearProgress, MenuItem, Paper, Snackbar, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, ToggleButton, ToggleButtonGroup, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useMemo, useState } from 'react';

import { isLate, type PersonAttendance } from '../../features/planning/attendance';
import type { BoardAlert } from '../../features/planning/equipment';
import {
  CAPACITY_WEEKS,
  capacity,
  dayCounts,
  decisionsFor,
  load,
  shortfall,
  siteLines,
  SOON_DAYS,
  weekMatrix,
  withExtraNeed,
  type CapacityRow,
  type CapacityWeek,
  type Decision,
  type SiteLine,
} from '../../features/planning/planner';
import { addDays, mondayOf, type AwayItem, type Plan, type PlanPerson } from '../../features/planning/planningLogic';
import { toApiError } from '../../api/apiError';
import { usePlanningActions } from '../../features/planning/usePlanning';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { durationText } from './attendanceUi';
import { conflictText } from './conflictText';
import { CrewsSection, HoursSection, WeekTools } from './PlannerTools';
import { shortDate, siteColor, WEEKDAYS } from './planningUi';
import type { PlanningHandlers } from './planningTypes';

type Section = 'today' | 'week' | 'crews' | 'capacity' | 'hours';
type Tone = 'error' | 'warning' | 'success' | 'info' | 'neutral';

export interface PlannerViewProps extends Pick<PlanningHandlers, 'openWorker' | 'openSite'> {
  plan: Plan;
  day: number;
  focusDay: string;
  setFocusDay: (day: string) => void;
  readOnly: boolean;
  /** Clock-in and position of today's people; undefined for any other day. */
  attendance?: Map<string, PersonAttendance>;
  onFind: (item: AwayItem) => void;
  /** Sends the planner to the by-site board with one kind of trouble highlighted. */
  onPickAlert: (alert: BoardAlert) => void;
}

/** How many items of "Needs a decision" show before "show more". */
const DECISIONS_SHOWN = 8;

const paperSx = { borderRadius: 2.5, p: { xs: 1.5, sm: 2 } } as const;

const toneColor = (tone: Tone) => (tone === 'neutral' ? 'text.secondary' : `${tone}.main`);

/**
 * The Planner: what needs a decision today, the week by site, and whether the people will last. Everything is worked out from the
 * plan the Schedule already holds, so it never disagrees with the other tabs, and every button opens a dialog they already have.
 */
export function PlannerView(props: PlannerViewProps) {
  const t = useT();
  const [section, setSection] = useState<Section>('today');

  return (
    <Stack spacing={2}>
      <ToggleButtonGroup
        size="small"
        exclusive
        value={section}
        onChange={(_, next: Section | null) => next && setSection(next)}
        aria-label={t('planning.tab.planner')}
        sx={{ alignSelf: 'flex-start', '& .MuiToggleButton-root': { textTransform: 'none', px: 2, py: 0.75, fontWeight: 600 } }}
      >
        <ToggleButton value="today">{t('planner.section.today')}</ToggleButton>
        <ToggleButton value="week">{t('planner.section.week')}</ToggleButton>
        {!props.plan.isScoped && <ToggleButton value="crews">{t('planner.section.crews')}</ToggleButton>}
        <ToggleButton value="capacity">{t('planner.section.capacity')}</ToggleButton>
        <ToggleButton value="hours">{t('planner.section.hours')}</ToggleButton>
      </ToggleButtonGroup>

      {section === 'today' && <TodaySection {...props} />}
      {section === 'week' && <WeekSection {...props} open={(date) => { props.setFocusDay(date); setSection('today'); }} />}
      {section === 'crews' && !props.plan.isScoped && <CrewsSection plan={props.plan} day={props.day} focusDay={props.focusDay} />}
      {section === 'capacity' && <CapacitySection plan={props.plan} focusDay={props.focusDay} />}
      {section === 'hours' && <HoursSection focusDay={props.focusDay} />}
    </Stack>
  );
}

/* ---------------------------------------------------------------------------------------------------- today */

function Kpi({ label, value, sub, tone = 'neutral' }: { label: string; value: string; sub?: string; tone?: Tone }) {
  return (
    <Paper variant="outlined" sx={{ ...paperSx, display: 'flex', flexDirection: 'column', gap: 0.25 }}>
      <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.6, fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography component="p" sx={{ fontSize: 30, fontWeight: 700, lineHeight: 1.1, letterSpacing: '-0.03em', fontVariantNumeric: 'tabular-nums', color: tone === 'neutral' ? 'text.primary' : `${tone}.dark` }}>
        {value}
      </Typography>
      {sub && (
        <Typography variant="caption" color="text.secondary">
          {sub}
        </Typography>
      )}
    </Paper>
  );
}

function TodaySection({ plan, day, readOnly, attendance, openWorker, openSite, onFind, onPickAlert }: PlannerViewProps) {
  const t = useT();
  const date = plan.date(day);
  const counts = useMemo(() => dayCounts(plan, day), [plan, day]);
  const decisions = useMemo(() => decisionsFor(plan, day, attendance), [plan, day, attendance]);
  const lines = useMemo(() => siteLines(plan, day), [plan, day]);
  const [allDecisions, setAllDecisions] = useState(false);
  const { remind: remindMutation } = usePlanningActions();
  const [reminded, setReminded] = useState<{ ok: boolean; text: string } | null>(null);
  const canRemind = !readOnly && date >= plan.today;
  const remind = () =>
    remindMutation.mutate(date, {
      onSuccess: (count) => setReminded({ ok: true, text: t('planner.remind.done', { count }) }),
      onError: (e) => setReminded({ ok: false, text: toApiError(e).message }),
    });

  if (!plan.isWork[day]) {
    return (
      <Paper variant="outlined" sx={paperSx}>
        <Typography>{plan.holidayNames.get(date) ?? t('planning.pill.weekend')}</Typography>
      </Paper>
    );
  }

  const att = attendance ? [...attendance.values()] : [];
  const late = att.filter(isLate).length;
  const clockedIn = att.filter((a) => a.clock.state === 'in' || a.clock.state === 'out').length;
  const upcoming = date >= plan.today;

  const urgent = decisions.filter((d) => d.kind !== 'soon');
  const soon = decisions.filter((d) => d.kind === 'soon');

  return (
    <Stack spacing={2}>
      <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', md: 'repeat(4, minmax(0, 1fr))' } }}>
        {attendance ? (
          <Kpi label={t('planner.kpi.working')} value={`${clockedIn} / ${counts.assigned}`} sub={t('planner.kpi.workingSub', { count: Math.max(0, counts.assigned - clockedIn) })} tone="success" />
        ) : (
          <Kpi label={t('planner.kpi.assigned')} value={String(counts.assigned)} />
        )}
        {attendance ? <Kpi label={t('planner.kpi.late')} value={String(late)} tone={late ? 'warning' : 'neutral'} /> : <Kpi label={t('planner.kpi.free')} value={String(counts.free)} />}
        <Kpi label={t('planner.kpi.short')} value={String(counts.short)} tone={counts.short ? 'error' : 'success'} />
        {upcoming ? <Kpi label={t('planner.kpi.unconfirmed')} value={String(counts.unconfirmed)} tone={counts.unconfirmed ? 'warning' : 'neutral'} /> : <Kpi label={t('planner.kpi.away')} value={String(counts.away)} />}
      </Box>

      <Box sx={{ display: 'grid', gap: 2, alignItems: 'start', gridTemplateColumns: { xs: 'minmax(0, 1fr)', lg: 'minmax(0, 5fr) minmax(0, 7fr)' } }}>
        <Paper variant="outlined" component="section" aria-label={t('planner.decide.title')} sx={paperSx}>
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
            <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
              {t('planner.decide.title')}
            </Typography>
            {urgent.length > 0 && <Chip size="small" color="error" label={urgent.length} />}
          </Stack>

          {urgent.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.75, py: 1 }}>
              <CheckCircleOutlineOutlined sx={{ fontSize: 18, color: 'success.main' }} />
              {t('planner.decide.none')}
            </Typography>
          )}

          {(allDecisions ? urgent : urgent.slice(0, DECISIONS_SHOWN)).map((d, i) => (
            <DecisionRow key={d.key} first={i === 0} decision={d} date={date} readOnly={readOnly} plan={plan} openWorker={openWorker} openSite={openSite} onFind={onFind} onPickAlert={onPickAlert} onRemind={canRemind ? remind : undefined} />
          ))}
          {!allDecisions && urgent.length > DECISIONS_SHOWN && (
            <Button size="small" variant="outlined" color="inherit" onClick={() => setAllDecisions(true)} sx={{ mt: 1, textTransform: 'none' }}>
              {t('planner.sites.more', { count: urgent.length - DECISIONS_SHOWN })}
            </Button>
          )}

          {soon.length > 0 && (
            <>
              <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mt: 2, fontWeight: 600 }}>
                {t('planner.decide.soon', { days: SOON_DAYS })}
              </Typography>
              {soon.map((d, i) => (
                <DecisionRow key={d.key} first={i === 0} decision={d} date={date} readOnly={readOnly} plan={plan} openWorker={openWorker} openSite={openSite} onFind={onFind} onPickAlert={onPickAlert} onRemind={canRemind ? remind : undefined} />
              ))}
            </>
          )}
        </Paper>

        <SitesPanel key={date} lines={lines} date={date} attendance={attendance} readOnly={readOnly} openWorker={openWorker} openSite={openSite} />
      </Box>

      <Snackbar open={!!reminded} autoHideDuration={7000} onClose={() => setReminded(null)}>
        <Alert severity={reminded?.ok ? 'success' : 'error'} onClose={() => setReminded(null)}>
          {reminded?.text}
        </Alert>
      </Snackbar>
    </Stack>
  );
}

function DecisionRow({
  decision,
  first,
  date,
  readOnly,
  plan,
  openWorker,
  openSite,
  onFind,
  onPickAlert,
  onRemind,
}: Pick<PlannerViewProps, 'plan' | 'readOnly' | 'openWorker' | 'openSite' | 'onFind' | 'onPickAlert'> & { decision: Decision; first: boolean; date: string; onRemind?: () => void }) {
  const t = useT();

  let tone: Tone = 'warning';
  let title = '';
  let sub: string | undefined;
  let action: { label: string; run: () => void; primary?: boolean } | undefined;
  let secondary: { label: string; run: () => void } | undefined;

  switch (decision.kind) {
    case 'short':
      tone = 'error';
      title = t('planner.d.short', { site: decision.project.name, position: decision.position, count: decision.count });
      sub = decision.awayNames.length > 0 ? t('planner.d.shortAway', { names: decision.awayNames.join(', ') }) : undefined;
      action = { label: t('planner.act.find'), run: () => openSite(decision.project.id, date, date, decision.position), primary: true };
      break;
    case 'late':
      title = t('planner.d.late', { name: decision.person.name, duration: durationText(decision.minutes) });
      sub = decision.project?.name;
      action = { label: t('planner.act.open'), run: () => openWorker(decision.person.id, date, date) };
      break;
    case 'wrong':
      tone = 'error';
      title = t('planner.d.wrong', { name: decision.person.name });
      sub = [decision.project?.name, decision.meters != null ? t('planner.d.wrongMeters', { meters: Math.round(decision.meters) }) : null].filter(Boolean).join(' · ');
      action = { label: t('planner.act.open'), run: () => openWorker(decision.person.id, date, date) };
      break;
    case 'conflict':
      title = conflictText(plan, decision.conflict, t);
      action = { label: t('planner.act.open'), run: () => openWorker(decision.conflict.person.id, decision.conflict.from, decision.conflict.to) };
      break;
    case 'equipment':
      title = `${t(`planning.attn.${decision.alert}`)}: ${decision.count}`;
      action = { label: t('planner.act.look'), run: () => onPickAlert(decision.alert) };
      break;
    case 'unconfirmed':
      title = `${t('planning.attn.unconfirmed')}: ${decision.count}`;
      action = { label: t('planner.act.look'), run: () => onPickAlert('unconfirmed') };
      secondary = onRemind ? { label: t('planner.act.remind'), run: onRemind } : undefined;
      break;
    case 'soon':
      tone = 'info';
      title = t('planner.d.soon', { name: decision.item.person.name, from: formatDate(decision.item.from), to: formatDate(decision.item.to) });
      sub = `${plan.projectById.get(decision.item.projectId)?.name ?? ''} · ${decision.item.person.position}`;
      action = { label: t('planner.act.find'), run: () => onFind(decision.item) };
      break;
  }

  // Looking at the board needs no right to change anything; every other button opens a dialog that edits.
  const canPress = !!action && (!readOnly || decision.kind === 'equipment' || decision.kind === 'unconfirmed');

  return (
    <Stack direction="row" spacing={1.25} sx={{ py: 1.25, borderTop: first ? 0 : 1, borderColor: 'divider', alignItems: 'flex-start' }}>
      <Box aria-hidden sx={{ width: 10, height: 10, mt: 0.7, borderRadius: '50%', flexShrink: 0, bgcolor: toneColor(tone) }} />
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
          {title}
        </Typography>
        {sub && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            {sub}
          </Typography>
        )}
      </Box>
      <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexShrink: 0, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
        {secondary && (
          <Button size="small" variant="outlined" color="inherit" onClick={secondary.run} sx={{ textTransform: 'none', '&:active': { transform: 'scale(0.97)' } }}>
            {secondary.label}
          </Button>
        )}
        {canPress && action && (
          <Button size="small" variant={action.primary ? 'contained' : 'outlined'} color={action.primary ? 'primary' : 'inherit'} onClick={action.run} sx={{ textTransform: 'none', '&:active': { transform: 'scale(0.97)' } }}>
            {action.label}
          </Button>
        )}
      </Stack>
    </Stack>
  );
}

function stateColor(a: PersonAttendance | undefined): string {
  if (!a) return 'transparent';
  if (a.pin?.state === 'wrong') return 'error.main';
  if (a.clock.state === 'late') return 'warning.main';
  if (a.clock.state === 'in') return 'success.main';

  return 'action.disabled';
}

/** How many sites are shown at first and each time "show more" is pressed. */
const SITE_PAGE = 12;

interface SiteStats {
  line: SiteLine;
  late: number;
  wrong: number;
  /** The site is short of people, or somebody on it is late or not where they should be. */
  attention: boolean;
}

function statsOf(line: SiteLine, attendance: Map<string, PersonAttendance> | undefined): SiteStats {
  const found = attendance ? line.people.map((p) => attendance.get(p.id)) : [];
  const late = found.filter((a) => a && isLate(a)).length;
  const wrong = found.filter((a) => a?.pin?.state === 'wrong').length;

  return { line, late, wrong, attention: line.short > 0 || late > 0 || wrong > 0 };
}

/**
 * The sites of the day as a grid of small tiles, those that need something first, so forty sites are a screen and not a
 * scroll. A tile opens in place to show its people; "Show more" brings the next dozen.
 */
function SitesPanel({ lines, date, attendance, readOnly, openWorker, openSite }: { lines: SiteLine[]; date: string; attendance?: Map<string, PersonAttendance>; readOnly: boolean } & Pick<PlanningHandlers, 'openWorker' | 'openSite'>) {
  const t = useT();
  const stats = useMemo(() => lines.map((l) => statsOf(l, attendance)).sort((a, b) => Number(b.attention) - Number(a.attention) || b.line.short - a.line.short || a.line.project.name.localeCompare(b.line.project.name)), [lines, attendance]);
  const trouble = stats.filter((s) => s.attention).length;

  const [choice, setChoice] = useState<'attention' | 'all' | null>(null);
  const [query, setQuery] = useState('');
  const [limit, setLimit] = useState(SITE_PAGE);
  const [openId, setOpenId] = useState<string | null>(null);

  // Until one is chosen, what needs something is shown, or everything when nothing does.
  const filter = choice ?? (trouble > 0 ? 'attention' : 'all');
  const needle = query.trim().toLowerCase();
  const shown = stats.filter((s) => (filter === 'all' || s.attention) && (!needle || s.line.project.name.toLowerCase().includes(needle)));

  return (
    <Stack spacing={1.5} component="section" aria-label={t('planner.sites.title')}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700, mr: 1 }}>
          {t('planner.sites.title')}
        </Typography>
        <ToggleButtonGroup size="small" exclusive value={filter} onChange={(_, next: 'attention' | 'all' | null) => { if (next) { setChoice(next); setLimit(SITE_PAGE); } }} aria-label={t('planner.sites.title')} sx={{ '& .MuiToggleButton-root': { textTransform: 'none', px: 1.5, py: 0.5, fontWeight: 600 } }}>
          <ToggleButton value="attention">{t('planner.sites.attention', { count: trouble })}</ToggleButton>
          <ToggleButton value="all">{t('planner.sites.all', { count: stats.length })}</ToggleButton>
        </ToggleButtonGroup>
        {stats.length > SITE_PAGE && (
          <TextField size="small" type="search" placeholder={t('planner.sites.search')} value={query} onChange={(e) => { setQuery(e.target.value); setLimit(SITE_PAGE); }} slotProps={{ htmlInput: { 'aria-label': t('planner.sites.search') } }} sx={{ ml: 'auto', width: { xs: '100%', sm: 220 } }} />
        )}
      </Stack>

      {stats.length === 0 && (
        <Paper variant="outlined" sx={paperSx}>
          <Typography variant="body2" color="text.secondary">
            {t('planner.sites.none')}
          </Typography>
        </Paper>
      )}
      {stats.length > 0 && shown.length === 0 && (
        <Paper variant="outlined" sx={paperSx}>
          <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
            <CheckCircleOutlineOutlined sx={{ fontSize: 18, color: 'success.main' }} />
            {needle ? t('planner.sites.noMatch') : t('planner.sites.allFine')}
          </Typography>
        </Paper>
      )}

      <Box sx={{ display: 'grid', gap: 1.25, gridTemplateColumns: 'repeat(auto-fill, minmax(210px, 1fr))', alignItems: 'start' }}>
        {shown.slice(0, limit).map((s) => (
          <SiteTile key={s.line.project.id} stats={s} open={openId === s.line.project.id} onToggle={() => setOpenId(openId === s.line.project.id ? null : s.line.project.id)} date={date} attendance={attendance} readOnly={readOnly} openWorker={openWorker} openSite={openSite} />
        ))}
      </Box>

      {shown.length > limit && (
        <Button size="small" variant="outlined" color="inherit" onClick={() => setLimit(limit + SITE_PAGE)} sx={{ alignSelf: 'flex-start', textTransform: 'none' }}>
          {t('planner.sites.more', { count: shown.length - limit })}
        </Button>
      )}
    </Stack>
  );
}

function SiteTile({ stats, open, onToggle, date, attendance, readOnly, openWorker, openSite }: { stats: SiteStats; open: boolean; onToggle: () => void; date: string; attendance?: Map<string, PersonAttendance>; readOnly: boolean } & Pick<PlanningHandlers, 'openWorker' | 'openSite'>) {
  const t = useT();
  const { line } = stats;
  const { project } = line;
  const covered = line.need - line.short;
  const full = line.need > 0 && line.short === 0;

  const person = (p: PlanPerson, away = false) => (
    <ButtonBase
      key={p.id}
      disabled={readOnly}
      onClick={() => openWorker(p.id, date, date)}
      sx={(theme) => ({
        gap: 0.75,
        px: 1.25,
        py: 0.5,
        borderRadius: 2,
        bgcolor: away ? alpha(theme.palette.warning.main, 0.14) : 'action.hover',
        typography: 'body2',
        textAlign: 'left',
        transition: 'background-color 150ms ease-out, transform 120ms ease-out',
        '&:active': { transform: 'scale(0.97)' },
        '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
      })}
    >
      {attendance && !away && <Box aria-hidden component="span" sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: stateColor(attendance.get(p.id)), flexShrink: 0 }} />}
      <span>{p.name}</span>
      <Typography component="span" variant="caption" color="text.secondary">
        {p.position}
      </Typography>
    </ButtonBase>
  );

  return (
    <Paper variant="outlined" sx={{ borderRadius: 2.5, borderLeft: 4, borderLeftColor: siteColor(project.id), gridColumn: open ? '1 / -1' : 'auto', overflow: 'hidden' }}>
      <ButtonBase
        onClick={onToggle}
        aria-expanded={open}
        sx={{ display: 'flex', flexDirection: 'column', alignItems: 'stretch', gap: 0.75, width: '100%', textAlign: 'left', p: 1.5, transition: 'background-color 150ms ease-out', '&:hover': { bgcolor: 'action.hover' }, '&:active': { transform: 'scale(0.99)' }, '@media (prefers-reduced-motion: reduce)': { transition: 'none' } }}
      >
        <Typography variant="subtitle2" component="span" sx={{ fontWeight: 700, lineHeight: 1.25, display: '-webkit-box', WebkitLineClamp: open ? 'none' : 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
          {project.name}
        </Typography>
        <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
          {line.need > 0 ? <Chip size="small" color={full ? 'success' : 'error'} label={t('planner.sites.covered', { covered, need: line.need })} /> : <Chip size="small" variant="outlined" label={t('planner.sites.people', { count: line.people.length })} />}
          {stats.late > 0 && <Chip size="small" color="warning" variant="outlined" label={t('planner.sites.late', { count: stats.late })} />}
          {stats.wrong > 0 && <Chip size="small" color="error" variant="outlined" label={t('planner.sites.wrong', { count: stats.wrong })} />}
          {line.away.length > 0 && <Chip size="small" variant="outlined" label={t('planner.sites.awayN', { count: line.away.length })} />}
        </Stack>
        {line.need > 0 && <LinearProgress variant="determinate" value={Math.min(100, Math.round((covered / line.need) * 100))} color={full ? 'success' : 'error'} sx={{ height: 5, borderRadius: 3 }} />}
      </ButtonBase>

      {open && (
        <Box sx={{ px: 1.5, pb: 1.5 }}>
          {project.customerName && (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
              {project.customerName}
            </Typography>
          )}
          <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexWrap: 'wrap' }}>
            {line.people.map((p) => person(p))}
            {line.away.map((p) => person(p, true))}
            {line.gaps.map((g) => (
              <Box key={g.position} sx={(theme) => ({ px: 1.25, py: 0.5, borderRadius: 2, border: `1.5px dashed ${theme.palette.error.main}`, color: 'error.dark', typography: 'body2', fontWeight: 600 })}>
                {t('planner.sites.gap', { count: g.count, position: g.position })}
              </Box>
            ))}
          </Stack>
          {!readOnly && (
            <Button size="small" variant="outlined" color="inherit" onClick={() => openSite(project.id, date, date)} sx={{ mt: 1.25, textTransform: 'none' }}>
              {t('planner.sites.open')}
            </Button>
          )}
        </Box>
      )}
    </Paper>
  );
}

/* ---------------------------------------------------------------------------------------------------- week */

function cellTone(need: number, people: number, short: number): Tone {
  if (need === 0) return 'neutral';

  return short > 0 ? (people === 0 ? 'error' : 'warning') : 'success';
}

function WeekSection({ plan, focusDay, setFocusDay, readOnly, open }: PlannerViewProps & { open: (date: string) => void }) {
  const t = useT();
  const monday = mondayOf(focusDay);
  const matrix = useMemo(() => weekMatrix(plan, monday), [plan, monday]);
  const days = [0, 1, 2, 3, 4].map((n) => addDays(monday, n));

  return (
    <Paper variant="outlined" component="section" aria-label={t('planner.section.week')} sx={paperSx}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          {t('planner.week.title', { from: shortDate(days[0]), to: shortDate(days[4]) })}
        </Typography>
        <Box sx={{ flex: 1 }} />
        <Button size="small" variant="outlined" color="inherit" onClick={() => setFocusDay(addDays(monday, -7))} aria-label={t('planning.prev')}>
          <ChevronLeftOutlined fontSize="small" />
        </Button>
        <Button size="small" variant="outlined" color="inherit" onClick={() => setFocusDay(addDays(monday, 7))} aria-label={t('planning.next')}>
          <ChevronRightOutlined fontSize="small" />
        </Button>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {t('planner.week.hint')}
      </Typography>
      {!readOnly && <WeekTools monday={monday} canTemplates={!plan.isScoped} />}

      {matrix.rows.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {t('planner.week.none')}
        </Typography>
      ) : (
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" sx={{ minWidth: 560, '& td, & th': { px: 1, py: 0.75 } }}>
            <TableHead>
              <TableRow>
                <TableCell>{t('planner.week.site')}</TableCell>
                {days.map((d, i) => (
                  <TableCell key={d} align="center" sx={{ whiteSpace: 'nowrap' }}>
                    {WEEKDAYS[i]} {shortDate(d)}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {matrix.rows.map((row) => (
                <TableRow key={row.project.id}>
                  <TableCell sx={{ borderLeft: 4, borderLeftColor: siteColor(row.project.id), fontWeight: 600 }}>{row.project.name}</TableCell>
                  {row.days.map((cell, i) => {
                    if (!cell) return <TableCell key={i} align="center" sx={{ color: 'text.disabled' }}>–</TableCell>;

                    const tone = cellTone(cell.need, cell.people, cell.short);

                    return (
                      <TableCell key={i} align="center" sx={{ p: '4px !important' }}>
                        <ButtonBase
                          onClick={() => open(days[i])}
                          aria-label={`${WEEKDAYS[i]} ${shortDate(days[i])}: ${cell.people} / ${cell.need}`}
                          sx={(theme) => ({
                            width: '100%',
                            minHeight: 36,
                            borderRadius: 1.5,
                            fontWeight: 600,
                            fontVariantNumeric: 'tabular-nums',
                            bgcolor: tone === 'neutral' ? 'action.hover' : alpha(theme.palette[tone].main, 0.14),
                            color: tone === 'neutral' ? 'text.primary' : `${tone}.dark`,
                            transition: 'transform 120ms ease-out',
                            '&:active': { transform: 'scale(0.97)' },
                          })}
                        >
                          {cell.need > 0 ? `${cell.people} / ${cell.need}` : cell.people}
                        </ButtonBase>
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))}
              <TableRow>
                <TableCell sx={{ color: 'text.secondary' }}>{t('planner.week.free')}</TableCell>
                {matrix.free.map((n, i) => (
                  <TableCell key={i} align="center" sx={{ color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
                    {n ?? '–'}
                  </TableCell>
                ))}
              </TableRow>
              <TableRow>
                <TableCell sx={{ color: 'text.secondary' }}>{t('planner.week.away')}</TableCell>
                {matrix.away.map((n, i) => (
                  <TableCell key={i} align="center" sx={{ color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
                    {n ?? '–'}
                  </TableCell>
                ))}
              </TableRow>
            </TableBody>
          </Table>
        </Box>
      )}
    </Paper>
  );
}

/* ---------------------------------------------------------------------------------------------------- capacity */

function loadTone(week: CapacityWeek): Tone {
  if (!week.known) return 'neutral';
  const n = load(week);

  return n > 1 ? 'error' : n > 0.85 ? 'warning' : n > 0 ? 'success' : 'neutral';
}

function percent(week: CapacityWeek, t: ReturnType<typeof useT>): string {
  if (!week.known) return '–';
  const n = load(week);

  return n === Infinity ? t('planner.cap.noPeople') : `${Math.min(999, Math.round(n * 100))}%`;
}

function LoadCell({ week, label }: { week: CapacityWeek; label?: string }) {
  const t = useT();
  const tone = loadTone(week);

  return (
    <Tooltip title={week.known ? t('planner.cap.cell', { need: week.need, available: week.available }) : ''} enterDelay={300}>
      <Box
        sx={(theme) => ({
          minHeight: 38,
          borderRadius: 1.5,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: 0.5,
          fontWeight: 600,
          fontSize: 13,
          fontVariantNumeric: 'tabular-nums',
          textAlign: 'center',
          bgcolor: tone === 'neutral' ? 'action.hover' : alpha(theme.palette[tone].main, 0.16),
          color: tone === 'neutral' ? 'text.secondary' : `${tone}.dark`,
          boxShadow: tone === 'error' ? `inset 0 0 0 1.5px ${theme.palette.error.main}` : 'none',
        })}
      >
        {label ?? percent(week, t)}
      </Box>
    </Tooltip>
  );
}

function CapacitySection({ plan, focusDay }: { plan: Plan; focusDay: string }) {
  const t = useT();
  const monday = mondayOf(focusDay);
  const rows = useMemo(() => capacity(plan, monday, CAPACITY_WEEKS), [plan, monday]);

  // A skill nobody needs says nothing about capacity; it stays in the what-if list, where it is the people that count.
  const needRows = useMemo(() => rows.filter((r) => r.weeks.some((w) => w.need > 0)), [rows]);

  const [key, setKey] = useState('');
  const [count, setCount] = useState(2);
  const [from, setFrom] = useState(() => addDays(monday, 7));
  const [weeks, setWeeks] = useState(8);

  const chosen = rows.find((r) => r.key === key) ?? rows[0];
  const after: CapacityRow | undefined = useMemo(
    () => (chosen ? withExtraNeed(rows, chosen.key, count, from, weeks).find((r) => r.key === chosen.key) : undefined),
    [rows, chosen, count, from, weeks],
  );

  const worst = after ? after.weeks.reduce((best, w) => (shortfall(w) > shortfall(best) ? w : best), after.weeks[0]) : undefined;
  const grid = { display: 'grid', gap: 0.75, alignItems: 'center', gridTemplateColumns: `minmax(110px, 1.2fr) repeat(${CAPACITY_WEEKS}, minmax(52px, 1fr))` } as const;

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" component="section" aria-label={t('planner.section.capacity')} sx={paperSx}>
        <Stack direction="row" spacing={1.5} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
          <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
            {t('planner.cap.title', { weeks: CAPACITY_WEEKS })}
          </Typography>
          <Box sx={{ flex: 1 }} />
          <Chip size="small" color="success" variant="outlined" label={t('planner.cap.low')} />
          <Chip size="small" color="warning" variant="outlined" label={t('planner.cap.mid')} />
          <Chip size="small" color="error" variant="outlined" label={t('planner.cap.high')} />
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('planner.cap.hint')}
        </Typography>

        {needRows.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('planner.cap.none')}
          </Typography>
        ) : (
          <Box sx={{ overflowX: 'auto' }}>
            <Box sx={{ minWidth: 640, display: 'grid', gap: 0.75 }}>
              <Box sx={grid}>
                <span />
                {needRows[0].weeks.map((w) => (
                  <Typography key={w.monday} variant="caption" color="text.secondary" sx={{ textAlign: 'center', fontWeight: 600 }}>
                    {shortDate(w.monday)}
                  </Typography>
                ))}
              </Box>
              {needRows.map((row) => (
                <Box key={row.key} sx={grid}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {row.position}
                  </Typography>
                  {row.weeks.map((w) => (
                    <LoadCell key={w.monday} week={w} />
                  ))}
                </Box>
              ))}
            </Box>
          </Box>
        )}
      </Paper>

      {chosen && after && worst && (
        <Paper variant="outlined" component="section" aria-label={t('planner.sim.title')} sx={paperSx}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
            <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
              {t('planner.sim.title')}
            </Typography>
            <Chip size="small" label={t('planner.sim.badge')} />
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            {t('planner.sim.hint')}
          </Typography>

          <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', md: 'repeat(4, minmax(0, 1fr))' }, mb: 2 }}>
            <TextField select size="small" label={t('planner.sim.position')} value={chosen.key} onChange={(e) => setKey(e.target.value)}>
              {rows.map((r) => (
                <MenuItem key={r.key} value={r.key}>
                  {r.position}
                </MenuItem>
              ))}
            </TextField>
            <TextField size="small" type="number" label={t('planner.sim.count')} value={count} onChange={(e) => setCount(Math.max(1, Math.min(200, Number(e.target.value) || 1)))} slotProps={{ htmlInput: { min: 1, max: 200 } }} />
            <TextField size="small" type="date" label={t('planner.sim.from')} value={from} onChange={(e) => e.target.value && setFrom(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
            <TextField size="small" type="number" label={t('planner.sim.weeks')} value={weeks} onChange={(e) => setWeeks(Math.max(1, Math.min(52, Number(e.target.value) || 1)))} slotProps={{ htmlInput: { min: 1, max: 52 } }} />
          </Box>

          <Box sx={{ overflowX: 'auto' }}>
            <Box sx={{ minWidth: 640, display: 'grid', gap: 0.75 }}>
              <Box sx={grid}>
                <Typography variant="body2" color="text.secondary">
                  {t('planner.sim.before')}
                </Typography>
                {chosen.weeks.map((w) => (
                  <LoadCell key={w.monday} week={w} />
                ))}
              </Box>
              <Box sx={grid}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {t('planner.sim.after')}
                </Typography>
                {after.weeks.map((w) => (
                  <LoadCell key={w.monday} week={w} label={shortfall(w) > 0 ? `−${shortfall(w)}` : percent(w, t)} />
                ))}
              </Box>
            </Box>
          </Box>

          <Typography variant="body1" sx={{ mt: 2, fontWeight: 600 }}>
            {shortfall(worst) > 0 ? t('planner.sim.short', { count: shortfall(worst), position: chosen.position, week: shortDate(worst.monday) }) : t('planner.sim.ok')}
          </Typography>
        </Paper>
      )}
    </Stack>
  );
}
