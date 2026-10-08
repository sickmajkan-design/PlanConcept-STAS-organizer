import { Alert, Box, Button, Chip, CircularProgress, Paper, Snackbar, Stack, TextField, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import { ChevronLeftOutlined, ChevronRightOutlined } from '@mui/icons-material';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { exportsApi } from '../../api/exports';
import { canSeeLabourCost } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { ErrorState } from '../../components/ErrorState';
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import {
  addDays,
  awayItems,
  candidates,
  conflicts,
  freeCount,
  mondayOf,
  missing,
  unconfirmedCount,
  periodBounds,
  segmentsOf,
  diffDays,
  weekday,
  workdayIndexes,
  type AwayItem,
  type Conflict,
  type Plan,
  type RangeMode,
} from '../../features/planning/planningLogic';
import { MAX_WINDOW_DAYS, usePlanningActions, usePlanningQuery } from '../../features/planning/usePlanning';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate } from '../../utils/formatting';
import { CostView } from './CostView';
import { DayView } from './DayView';
import { ListView } from './ListView';
import { NeedView } from './NeedView';
import { HistoryDialog } from './HistoryDialog';
import { NeedsDialog, ReplaceDialog, SiteDialog, WorkerDialog, type PlanningActions } from './PlanningDialogs';
import { TimelineView } from './TimelineView';
import { PlanningTour } from './PlanningTour';
import { fetchWindow, shortDate, WEEKDAYS } from './planningUi';

type View = 'day' | 'timeline' | 'need' | 'list' | 'cost';

type Panel =
  | { kind: 'worker'; personId: string; from: string; to: string }
  | { kind: 'site'; projectId: string; from: string; to: string; position?: string }
  | { kind: 'replace'; item: AwayItem }
  | { kind: 'needs'; projectId: string };

const todayLocal = (): string => {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};

function rangeFor(mode: RangeMode, anchor: string, current: { from: string; to: string }): { from: string; to: string } {
  if (mode === 'W') return { from: mondayOf(anchor), to: addDays(mondayOf(anchor), 6) };
  if (mode === '2W') return { from: mondayOf(anchor), to: addDays(mondayOf(anchor), 13) };
  if (mode === 'M' || mode === 'Q') {
    const [from, to] = periodBounds(mode, anchor);
    return { from, to };
  }
  return current;
}

/**
 * Scheduling in one place: who is on which site over any stretch of days, where the sites are short
 * of people or of a skill, and who can stand in for somebody who is away.
 */
export function PlanningPage() {
  const { t, locale } = useI18n();
  const navigate = useNavigate();
  const today = useMemo(todayLocal, []);

  // The timeline is a wide grid meant for a desk; on a phone the answer people come
  // for is "who is on which site today", which the by-site view gives in one column.
  const [view, setView] = useState<View>(() =>
    typeof window !== 'undefined' && window.matchMedia?.('(max-width: 599.95px)').matches
      ? 'day'
      : 'timeline',
  );
  const [focusDay, setFocusDay] = useState(today);
  const [mode, setMode] = useState<RangeMode>('M');
  const [range, setRange] = useState(() => rangeFor('M', today, { from: today, to: today }));
  const [panel, setPanel] = useState<Panel | null>(null);
  const [undo, setUndo] = useState<{ message: string; run: () => Promise<void> } | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const { user } = useAuth();
  const tourKey = `planning.tour.seen.${user?.id ?? ''}`;
  const [tourOpen, setTourOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [offerTour, setOfferTour] = useState(() => {
    try {
      return window.localStorage.getItem(tourKey) !== '1';
    } catch {
      return false;
    }
  });

  const rememberTour = () => {
    setOfferTour(false);
    try {
      window.localStorage.setItem(tourKey, '1');
    } catch {
      /* the offer simply shows again next time */
    }
  };

  const window_ = fetchWindow(range, focusDay, today, MAX_WINDOW_DAYS);
  const { plan, error, isLoading, refetch } = usePlanningQuery(window_.from, window_.to);
  const mutations = usePlanningActions();

  const periodView = view === 'timeline' || view === 'need' || view === 'cost';

  /* ---- actions, each remembering how to be taken back ---- */
  const actions: PlanningActions | null = plan
    ? {
        assign: async (personId, projectId, from, to, options) => {
          const person = plan.people.find((p) => p.id === personId);
          const before = segmentsOf(plan, personId, from, to);

          await mutations.assign.mutateAsync({ employeeId: personId, projectId, from, to, onlyFreeDays: options?.onlyFree });

          const site = projectId ? plan.projectById.get(projectId)?.name : null;
          setUndo({
            message: site
              ? t('planning.msg.assigned', { name: person?.name ?? '', site, from: formatDate(from), to: formatDate(to) })
              : t('planning.msg.freed', { name: person?.name ?? '', from: formatDate(from), to: formatDate(to) }),
            run: async () => {
              await mutations.assign.mutateAsync({ employeeId: personId, projectId: null, from, to });
              for (const seg of before) {
                if (seg.projectId) {
                  await mutations.assign.mutateAsync({ employeeId: personId, projectId: seg.projectId, from: seg.from, to: seg.to });
                }
              }
            },
          });
        },
        swap: async (aId, bId, from, to) => {
          const a = plan.people.find((p) => p.id === aId);
          const b = plan.people.find((p) => p.id === bId);

          await mutations.swap.mutateAsync({ employeeAId: aId, employeeBId: bId, from, to });

          setUndo({
            message: t('planning.msg.swapped', { a: a?.name ?? '', b: b?.name ?? '', from: formatDate(from), to: formatDate(to) }),
            run: async () => {
              await mutations.swap.mutateAsync({ employeeAId: aId, employeeBId: bId, from, to });
            },
          });
        },
        saveNeeds: async (projectId, needs, requiredCertificates) => {
          const before = plan.projectById.get(projectId)?.needs ?? [];
          const beforeCertificates = plan.projectById.get(projectId)?.requiredCertificates ?? [];

          await mutations.setNeeds.mutateAsync({ projectId, needs, requiredCertificates });

          setUndo({
            message: t('planning.msg.needsSaved'),
            run: async () => {
              await mutations.setNeeds.mutateAsync({ projectId, needs: before, requiredCertificates: beforeCertificates });
            },
          });
        },
      }
    : null;

  const attemptQuiet = async (action: () => Promise<void>) => {
    try {
      await action();
    } catch (e) {
      setProblem(toApiError(e).message);
    }
  };

  const runUndo = async () => {
    if (!undo) return;
    const current = undo;
    setUndo(null);
    await attemptQuiet(current.run);
  };

  /* ---- controls ---- */
  const chooseMode = (next: RangeMode) => {
    const anchor = range.from <= today && range.to >= today ? today : range.from;
    setMode(next);
    setRange(rangeFor(next, anchor, range));
  };

  const step = (direction: 1 | -1) => {
    if (mode === 'M' || mode === 'Q') {
      const [from, to] = periodBounds(mode, direction === 1 ? addDays(range.to, 1) : addDays(range.from, -1));
      setRange({ from, to });
    } else {
      const length = diffDays(range.from, range.to) + 1;
      setRange({ from: addDays(range.from, direction * length), to: addDays(range.to, direction * length) });
    }
  };

  const rangeLabel = (): string => {
    if (mode === 'M') {
      return new Date(`${range.from}T00:00:00Z`).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en', { month: 'long', year: 'numeric', timeZone: 'UTC' });
    }
    if (mode === 'Q') {
      return t('planning.quarter', { n: Math.floor((Number(range.from.slice(5, 7)) - 1) / 3) + 1, year: range.from.slice(0, 4) });
    }
    return `${formatDate(range.from)} – ${formatDate(range.to)}`;
  };

  const dayIndex = plan ? plan.indexOf(focusDay) : -1;

  // What goes on paper: the period on screen (as much of it as fits a page), or the week around the day shown.
  const exportRange = periodView
    ? { from: range.from, to: diffDays(range.from, range.to) + 1 > 62 ? addDays(range.from, 61) : range.to }
    : { from: mondayOf(focusDay), to: addDays(mondayOf(focusDay), 6) };

  // A foreman without the right to move people sees the same screen with nothing to press.
  const readOnly = !!plan && !plan.canEdit;

  const openWorker = (personId: string, from: string, to: string) => {
    if (!readOnly) setPanel({ kind: 'worker', personId, from, to });
  };
  const openSite = (projectId: string, from: string, to: string, position?: string) => {
    if (!readOnly) setPanel({ kind: 'site', projectId, from, to, position });
  };

  return (
    <Box>
      <PageHeader
        title={t('planning.title')}
        description={t('planning.description')}
        secondaryActions={
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <ExportButton onExport={(language) => exportsApi.schedule({ ...exportRange, language })} />
            <Button variant="outlined" size="small" onClick={() => setHistoryOpen(true)}>
              {t('planning.history.button')}
            </Button>
            {!readOnly && (
              <Button variant="outlined" size="small" onClick={() => { rememberTour(); setTourOpen(true); }}>
                {t('planning.tour.button')}
              </Button>
            )}
          </Stack>
        }
      />

      {plan && (readOnly || plan.isScoped) && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {plan.scopeBranchName
            ? t(readOnly ? 'planning.readOnly.unit' : 'planning.scoped.unit', { unit: plan.scopeBranchName })
            : t('planning.readOnly.noUnit')}
        </Alert>
      )}

      {offerTour && !readOnly && (
        <Alert
          severity="info"
          sx={{ mb: 2 }}
          action={
            <Stack direction="row" spacing={1}>
              <Button color="inherit" size="small" onClick={rememberTour}>
                {t('planning.tour.dismiss')}
              </Button>
              <Button variant="contained" size="small" onClick={() => { rememberTour(); setTourOpen(true); }}>
                {t('planning.tour.start')}
              </Button>
            </Stack>
          }
        >
          {t('planning.tour.offer')}
        </Alert>
      )}

      <ToggleButtonGroup
        data-tour="tabs"
        exclusive
        value={view}
        onChange={(_, next: View | null) => next && setView(next)}
        sx={{ flexWrap: 'wrap', gap: 0.75, mb: 2, '& .MuiToggleButton-root': { border: 1, borderColor: 'divider', borderRadius: '10px !important', textAlign: 'left', textTransform: 'none', alignItems: 'flex-start', px: 1.5, py: 1 } }}
      >
        {(canSeeLabourCost(user) ? (['day', 'timeline', 'need', 'cost', 'list'] as const) : (['day', 'timeline', 'need', 'list'] as const)).map((v) => (
          <ToggleButton key={v} value={v}>
            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {t(`planning.tab.${v}`)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {t(`planning.tab.${v}Hint`)}
              </Typography>
            </Box>
          </ToggleButton>
        ))}
      </ToggleButtonGroup>

      {plan && (
        <Box data-tour="banner">
          <NeedBanner plan={plan} today={today} onFind={readOnly ? undefined : (item) => setPanel({ kind: 'replace', item })} />
        </Box>
      )}
      {plan && <ConflictBanner plan={plan} today={today} onOpen={readOnly ? undefined : (c) => openWorker(c.person.id, c.from, c.to)} />}

      <Stack data-tour="controls" direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', my: 2 }}>
        {periodView ? (
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
            <ToggleButtonGroup size="small" exclusive value={mode} onChange={(_, next: RangeMode | null) => next && chooseMode(next)} sx={{ maxWidth: '100%', '& .MuiToggleButton-root': { px: { xs: 0.75, sm: 1.5 } } }}>
              {(['W', '2W', 'M', 'Q', 'C'] as const).map((m) => (
                <ToggleButton key={m} value={m} sx={{ textTransform: 'none' }}>
                  {t(`planning.range.${m}`)}
                </ToggleButton>
              ))}
            </ToggleButtonGroup>
            <Button size="small" variant="outlined" color="inherit" onClick={() => step(-1)} aria-label={t('planning.prev')}>
              <ChevronLeftOutlined fontSize="small" />
            </Button>
            <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums', textTransform: 'capitalize' }}>{rangeLabel()}</Typography>
            <Button size="small" variant="outlined" color="inherit" onClick={() => step(1)} aria-label={t('planning.next')}>
              <ChevronRightOutlined fontSize="small" />
            </Button>
            {mode === 'C' && (
              <>
                <TextField type="date" size="small" label={t('planning.from')} value={range.from} onChange={(e) => e.target.value && setRange({ from: e.target.value, to: e.target.value > range.to ? e.target.value : range.to })} slotProps={{ inputLabel: { shrink: true } }} />
                <TextField type="date" size="small" label={t('planning.to')} value={range.to} onChange={(e) => e.target.value && setRange({ from: e.target.value < range.from ? e.target.value : range.from, to: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
              </>
            )}
            {view === 'need' && !readOnly && !plan?.isScoped && (
              <Button variant="contained" size="small" onClick={() => navigate(paths.projectNew)}>
                {t('planning.n.newProject')}
              </Button>
            )}
          </Stack>
        ) : (
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
            <Button size="small" variant="outlined" color="inherit" onClick={() => setFocusDay(addDays(focusDay, -1))} aria-label={t('planning.prev')}>
              <ChevronLeftOutlined fontSize="small" />
            </Button>
            <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
              {WEEKDAYS[weekday(focusDay)]} {shortDate(focusDay)}
              {focusDay === today ? t('planning.todayTag') : ''}
            </Typography>
            <Button size="small" variant="outlined" color="inherit" onClick={() => setFocusDay(addDays(focusDay, 1))} aria-label={t('planning.next')}>
              <ChevronRightOutlined fontSize="small" />
            </Button>
            <Button size="small" variant="outlined" color="inherit" onClick={() => setFocusDay(today)}>
              {t('planning.today')}
            </Button>
            <TextField type="date" size="small" value={focusDay} onChange={(e) => e.target.value && setFocusDay(e.target.value)} slotProps={{ htmlInput: { 'aria-label': t('planning.pickDay') } }} />
          </Stack>
        )}

        {plan && (
          <Box data-tour="summary">
            <Summary plan={plan} view={view} day={dayIndex} range={range} />
          </Box>
        )}
      </Stack>

      {error && !plan && <ErrorState error={error} onRetry={() => void refetch()} />}
      {isLoading && !plan && (
        <Box sx={{ py: 8, display: 'flex', justifyContent: 'center' }}>
          <CircularProgress />
        </Box>
      )}

      {plan && actions && (
        <>
          {view === 'day' && dayIndex >= 0 && <DayView readOnly={readOnly} plan={plan} day={dayIndex} openWorker={openWorker} openSite={openSite} editNeeds={(id) => setPanel({ kind: 'needs', projectId: id })} />}
          {view === 'timeline' && <TimelineView readOnly={readOnly} plan={plan} range={range} openWorker={openWorker} openSite={openSite} editNeeds={(id) => setPanel({ kind: 'needs', projectId: id })} />}
          {view === 'cost' && canSeeLabourCost(user) && <CostView range={range} />}
          {view === 'need' && <NeedView readOnly={readOnly} plan={plan} range={range} openWorker={openWorker} openSite={openSite} editNeeds={(id) => setPanel({ kind: 'needs', projectId: id })} />}
          {view === 'list' && dayIndex >= 0 && (
            <ListView
              readOnly={readOnly}
              plan={plan}
              day={dayIndex}
              openWorker={openWorker}
              openSite={openSite}
              editNeeds={(id) => setPanel({ kind: 'needs', projectId: id })}
              setSite={(personId, projectId) => void attemptQuiet(() => actions.assign(personId, projectId, focusDay, focusDay))}
            />
          )}

          {panel?.kind === 'worker' && <WorkerDialog key={`${panel.personId}${panel.from}${panel.to}`} plan={plan} personId={panel.personId} from={panel.from} to={panel.to} actions={actions} onClose={() => setPanel(null)} />}
          {panel?.kind === 'site' && <SiteDialog key={`${panel.projectId}${panel.from}${panel.to}${panel.position ?? ''}`} plan={plan} projectId={panel.projectId} from={panel.from} to={panel.to} position={panel.position} actions={actions} onClose={() => setPanel(null)} />}
          {panel?.kind === 'replace' && <ReplaceDialog plan={plan} item={panel.item} actions={actions} onClose={() => setPanel(null)} />}
          {panel?.kind === 'needs' && <NeedsDialog key={panel.projectId} plan={plan} projectId={panel.projectId} actions={actions} onClose={() => setPanel(null)} />}
        </>
      )}

      {historyOpen && <HistoryDialog onClose={() => setHistoryOpen(false)} />}
      <PlanningTour open={tourOpen} onClose={() => setTourOpen(false)} setView={setView} />

      <Snackbar
        open={!!undo}
        autoHideDuration={8000}
        onClose={(_, reason) => reason !== 'clickaway' && setUndo(null)}
        message={undo?.message}
        action={
          <Button color="inherit" size="small" onClick={() => void runUndo()}>
            {t('planning.undo')}
          </Button>
        }
      />
      <Snackbar open={!!problem} autoHideDuration={8000} onClose={() => setProblem(null)}>
        <Alert severity="error" onClose={() => setProblem(null)}>
          {problem}
        </Alert>
      </Snackbar>
    </Box>
  );
}

const wrapChip = { height: 'auto', '& .MuiChip-label': { whiteSpace: 'normal', py: 0.5 } } as const;

function Summary({ plan, view, day, range }: { plan: Plan; view: View; day: number; range: { from: string; to: string } }) {
  const t = useT();

  if (view === 'day' || view === 'list') {
    if (day < 0) return null;
    if (!plan.isWork[day]) return <Chip label={plan.holidayNames.get(plan.date(day)) ?? t('planning.pill.weekend')} />;

    const free = freeCount(plan, day);
    const away = plan.people.filter((p) => p.cells[day].away).length;
    const miss = missing(plan, day);

    return (
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
        <Chip label={t('planning.pill.assigned', { count: plan.people.length - free - away })} variant="outlined" />
        <Chip label={t('planning.pill.free', { count: free })} variant="outlined" />
        <Chip label={t('planning.pill.away', { count: away })} color={away ? 'warning' : 'default'} variant={away ? 'filled' : 'outlined'} />
        <Chip label={miss ? t('planning.pill.openSlots', { count: miss }) : t('planning.pill.allFilled')} color={miss ? 'error' : 'success'} />
        {plan.date(day) >= plan.today && unconfirmedCount(plan, day) > 0 && (
          <Chip color="warning" variant="outlined" label={t('planning.pill.unconfirmed', { count: unconfirmedCount(plan, day) })} />
        )}
      </Stack>
    );
  }

  const days = workdayIndexes(plan, plan.indexOf(range.from), plan.indexOf(range.to));
  let worst = 0;
  let worstDay = -1;
  for (const i of days) {
    const m = missing(plan, i);
    if (m > worst) {
      worst = m;
      worstDay = i;
    }
  }

  const idle = plan.people.filter((p) => days.length > 0 && days.every((i) => !p.cells[i].project)).length;
  const unfillable = [...new Map(plan.people.map((p) => [p.key, p.position])).entries()]
    .map(([key, position]) => [position, Math.max(0, ...days.map((i) => missing(plan, i, key) - freeCount(plan, i, key)))] as const)
    .filter(([, n]) => n > 0);

  return (
    <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
      <Chip sx={wrapChip} color={worst ? 'error' : 'success'} label={worst ? t('planning.pill.worst', { count: worst, date: formatDate(plan.date(worstDay)) }) : t('planning.pill.noShortage')} />
      <Chip sx={wrapChip} color={idle ? 'warning' : 'default'} variant={idle ? 'filled' : 'outlined'} label={t('planning.pill.idle', { count: idle })} />
      {unfillable.length > 0 && <Chip sx={wrapChip} color="error" label={t('planning.pill.cannotFill', { list: unfillable.map(([p, n]) => `${p} ${n}`).join(', ') })} />}
    </Stack>
  );
}

function NeedBanner({ plan, today, onFind }: { plan: Plan; today: string; onFind?: (item: AwayItem) => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();

  const from = Math.max(0, plan.indexOf(today));
  const items = awayItems(plan, from, Math.min(plan.days - 1, from + 30));

  if (items.length === 0) {
    return <Chip color="success" variant="outlined" label={t('planning.replace.allCovered')} />;
  }

  return (
    <Paper variant="outlined" sx={{ borderRadius: 2.5, borderColor: 'warning.main', p: 1.5 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5, display: 'flex', justifyContent: 'space-between' }}>
        {t('planning.replace.title')} <span>{items.length}</span>
      </Typography>
      {items.map((item, i) => (
        <Stack
          key={`${item.person.id}-${item.from}-${i}`}
          direction="row"
          spacing={1}
          useFlexGap
          sx={{ flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', py: 0.75, borderTop: i ? 1 : 0, borderColor: 'divider' }}
        >
          <Typography variant="body2">
            <b>{item.person.name}</b>{' '}
            <Typography component="span" variant="caption" color="text.secondary">
              {item.person.position} · {plan.projectById.get(item.projectId)?.name} · {enumLabel('absenceType', item.type)} {formatDate(item.from)} – {formatDate(item.to)}
            </Typography>
          </Typography>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <SuggestionCount plan={plan} item={item} />
            {onFind && (
              <Button size="small" variant="outlined" onClick={() => onFind(item)}>
                {t('planning.replace.find')}
              </Button>
            )}
          </Stack>
        </Stack>
      ))}
    </Paper>
  );
}


function SuggestionCount({ plan, item }: { plan: Plan; item: AwayItem }) {
  const t = useT();
  const good = candidates(plan, item.person.id, item.projectId, item.days).filter((c) => c.tier <= 3).length;

  return good > 0 ? (
    <Chip size="small" color="success" label={t('planning.replace.suggestions', { count: good })} />
  ) : (
    <Chip size="small" color="error" label={t('planning.replace.none')} />
  );
}

const CONFLICT_LIMIT = 4;

function ConflictBanner({ plan, today, onOpen }: { plan: Plan; today: string; onOpen?: (conflict: Conflict) => void }) {
  const t = useT();
  const [all, setAll] = useState(false);

  const from = Math.max(0, plan.indexOf(today));
  const found = conflicts(plan, from, plan.days - 1);

  if (found.length === 0) return null;

  const shown = all ? found : found.slice(0, CONFLICT_LIMIT);
  const name = (id: string | undefined) => plan.projectById.get(id ?? '')?.name ?? '?';

  const text = (c: Conflict): string => {
    const base = { name: c.person.name, from: formatDate(c.from), to: formatDate(c.to) };
    const site = plan.projectById.get(c.projectId);

    if (c.kind === 'double') return t('planning.conflict.double', { ...base, a: name(c.projectId), b: name(c.otherProjectId) });
    if (c.kind === 'housedIdle') return t('planning.conflict.housedIdle', { ...base, place: c.place ?? '' });
    if (c.kind === 'certificate') return t('planning.conflict.certificate', { ...base, site: name(c.projectId), certificate: c.certificate ?? '' });
    if (c.kind === 'afterEnd') return t('planning.conflict.afterEnd', { ...base, site: name(c.projectId), date: formatDate(site?.endDate) });
    return t('planning.conflict.beforeStart', { ...base, site: name(c.projectId), date: formatDate(site?.startDate) });
  };

  return (
    <Paper variant="outlined" sx={{ borderRadius: 2.5, p: 1.5, mt: 1.5 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5, display: 'flex', justifyContent: 'space-between' }}>
        {t('planning.conflict.title')} <span>{found.length}</span>
      </Typography>
      {shown.map((c, i) => (
        <Stack
          key={`${c.person.id}-${c.kind}-${c.from}-${i}`}
          direction="row"
          spacing={1}
          useFlexGap
          sx={{ flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', py: 0.5, borderTop: i ? 1 : 0, borderColor: 'divider' }}
        >
          <Typography variant="body2">{text(c)}</Typography>
          {onOpen && (
            <Button size="small" onClick={() => onOpen(c)}>
              {t('planning.conflict.open')}
            </Button>
          )}
        </Stack>
      ))}
      {found.length > CONFLICT_LIMIT && (
        <Button size="small" color="inherit" onClick={() => setAll(!all)} sx={{ mt: 0.5 }}>
          {all ? t('planning.conflict.less') : t('planning.conflict.more', { count: found.length - CONFLICT_LIMIT })}
        </Button>
      )}
    </Paper>
  );
}
