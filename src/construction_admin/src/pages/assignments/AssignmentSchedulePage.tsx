import {
  AddOutlined,
  CloseOutlined,
  ExpandMoreOutlined,
  HandymanOutlined,
  LocalShippingOutlined,
  WarningAmberOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  ButtonBase,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
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
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type {
  AssignmentSchedule,
  ScheduleDayStatus,
  ScheduleEmployee,
  ScheduleProject,
} from '../../api/assignments';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import {
  useAssignOnBoard,
  useAssignmentScheduleQuery,
  useRemoveOnBoard,
} from '../../features/assignments/useAssignmentBoard';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { dateOnlyOffset } from '../../utils/formatting';

type Filter = 'all' | 'on' | 'free' | 'away' | 'issues';

const SITE_COLORS = ['#d9480f', '#1c7ed6', '#2f9e44', '#9c36b5', '#c2255c', '#0b7285', '#e67700', '#5c940d'];
const LEAVE_COLOR = '#4c51bf';
const SICK_COLOR = '#b26a00';
const STATUS_COLOR: Record<ScheduleDayStatus, string> = {
  Free: 'text.disabled',
  Expected: 'info.main',
  OnSite: 'success.main',
  Late: 'warning.main',
  NoShow: 'error.main',
  Leave: LEAVE_COLOR,
  Sick: SICK_COLOR,
};

const iso = (date: Date) => date.toISOString().slice(0, 10);
const addDays = (value: string, days: number) => {
  const date = new Date(`${value}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return iso(date);
};
const mondayOf = (value: string) => {
  const day = new Date(`${value}T00:00:00Z`).getUTCDay();
  return addDays(value, -((day + 6) % 7));
};
/** `2026-10-09` as `09.10.` */
const shortDate = (value: string) => `${value.slice(8)}.${value.slice(5, 7)}.`;
const dayOfWeek = (value: string) => new Date(`${value}T00:00:00Z`).getUTCDay();
const isSunday = (value: string) => dayOfWeek(value) === 0;
/** Weekdays always; Saturday and Sunday only on sites that work them. */
const worksOn = (project: ScheduleProject, day: string) => {
  const dow = dayOfWeek(day);
  if (dow === 6) return project.worksSaturdays;
  if (dow === 0) return project.worksSundays;
  return true;
};
const covers = (start: string, end: string | null, day: string) =>
  start <= day && (end === null || end >= day);

/** `07:00:00` UTC (what the site stores) as the viewer's local `HH:mm`. */
function localShift(utcTime: string | null): string | null {
  if (!utcTime) return null;
  const [h, m] = utcTime.split(':').map(Number);
  const date = new Date();
  date.setUTCHours(h, m, 0, 0);
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
}

function localClock(value: string | null): string {
  return value
    ? new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false })
    : '';
}

/** Everything the day cells need to know about the sites, computed once per load. */
interface DayContext {
  project: (id: string) => ScheduleProject | undefined;
  /** Whether at least one site works that day, so a free day can be offered for posting. */
  anySiteWorks: (day: string) => boolean;
}

function makeContext(data: AssignmentSchedule | undefined): DayContext {
  const byId = new Map((data?.projects ?? []).map((p) => [p.id, p]));
  return {
    project: (id) => byId.get(id),
    anySiteWorks: (day) => {
      const dow = dayOfWeek(day);
      if (dow !== 0 && dow !== 6) return true;
      return (data?.projects ?? []).some((p) => worksOn(p, day));
    },
  };
}

interface Cell {
  kind: 'site' | 'leave' | 'sick' | 'free' | 'off';
  projects: string[];
  conflict: boolean;
}

/** The planning picture of one worker on one day. Sundays and Saturdays count only where a site works them. */
function cellFor(employee: ScheduleEmployee, day: string, ctx: DayContext): Cell {
  const absence = employee.absences.find((a) => covers(a.startDate, a.endDate, day));
  const projects = employee.postings
    .filter((p) => covers(p.startDate, p.endDate, day))
    .map((p) => p.projectId)
    .filter((id) => {
      const project = ctx.project(id);
      return project ? worksOn(project, day) : true;
    });

  if (absence) {
    return { kind: absence.type === 'SickLeave' ? 'sick' : 'leave', projects, conflict: projects.length > 0 };
  }
  if (projects.length > 0) return { kind: 'site', projects, conflict: false };
  return { kind: ctx.anySiteWorks(day) ? 'free' : 'off', projects, conflict: false };
}

/** An approved absence on days someone is posted to a site: the position is empty and needs a replacement. */
interface Vacancy {
  key: string;
  employeeId: string;
  projectId: string;
  from: string;
  to: string;
  /** Working days of the site in the range. */
  days: number;
}

const eachDay = (from: string, to: string) => {
  const days: string[] = [];
  for (let d = from; d <= to; d = addDays(d, 1)) days.push(d);
  return days;
};

/**
 * Empty positions: every posting that overlaps an approved absence, from today on, counted over
 * the days the site actually works. One that somebody already covers (another worker with a
 * bounded posting on the same site across the whole stretch) is not listed. That is what "Assign
 * replacement" creates, so the entry goes away once it is done.
 */
function buildVacancies(data: AssignmentSchedule | undefined): Vacancy[] {
  if (!data) return [];
  const result: Vacancy[] = [];
  const projects = new Map(data.projects.map((p) => [p.id, p]));

  for (const e of data.employees) {
    for (const a of e.absences) {
      for (const p of e.postings) {
        const project = projects.get(p.projectId);
        const from = [a.startDate, p.startDate, data.today].sort().at(-1)!;
        const to = [a.endDate, p.endDate ?? a.endDate].sort()[0];
        const working = eachDay(from, to).filter((d) => (project ? worksOn(project, d) : !isSunday(d)));
        if (working.length === 0) continue;
        const first = working[0];
        const last = working[working.length - 1];

        const covered = data.employees.some(
          (o) =>
            o.id !== e.id &&
            o.postings.some(
              (q) => q.projectId === p.projectId && q.endDate !== null && q.startDate <= first && q.endDate >= last,
            ),
        );
        if (covered) continue;

        result.push({ key: `${e.id}-${p.projectId}-${first}`, employeeId: e.id, projectId: p.projectId, from: first, to: last, days: working.length });
      }
    }
  }
  return result.sort((x, y) => x.from.localeCompare(y.from));
}

interface Candidate {
  employee: ScheduleEmployee;
  freeDays: number;
  sameTrade: boolean;
}

/** Workers with at least one free working day in the vacancy, same trade first. */
function candidatesFor(data: AssignmentSchedule, vacancy: Vacancy, ctx: DayContext): Candidate[] {
  const absent = data.employees.find((e) => e.id === vacancy.employeeId);
  const project = ctx.project(vacancy.projectId);
  const window = eachDay(vacancy.from, vacancy.to).filter((d) => (project ? worksOn(project, d) : !isSunday(d)));
  return data.employees
    .filter((e) => e.id !== vacancy.employeeId)
    .map((employee) => ({
      employee,
      freeDays: window.filter((d) => cellFor(employee, d, ctx).kind === 'free').length,
      sameTrade: employee.position === absent?.position,
    }))
    .filter((c) => c.freeDays > 0)
    .sort((a, b) => Number(b.sameTrade) - Number(a.sameTrade) || b.freeDays - a.freeDays)
    .slice(0, 5);
}

interface AttentionItem {
  key: string;
  level: 'crit' | 'warn' | 'info';
  text: string;
  employeeId: string;
}

/** One run of days drawn as a single bar. */
interface Segment {
  start: number;
  length: number;
  sig: string;
  cell: Cell;
  vacancyProject?: string;
}

/**
 * A worker's week as bars instead of one box per day: consecutive days with the same site (or the
 * same absence) are one bar. A day nobody works (a Sunday between two days at the same site) does
 * not break the bar. Free days stay separate so they can be clicked.
 */
function segmentsFor(
  employee: ScheduleEmployee,
  dayList: string[],
  ctx: DayContext,
  vacancyOf: (employeeId: string, day: string) => Vacancy | undefined,
): { segments: Segment[]; free: number[] } {
  const cells = dayList.map((day) => cellFor(employee, day, ctx));
  const sigs = cells.map((cell, i) => {
    if (cell.kind === 'free' || cell.kind === 'off') return '';
    if (cell.kind === 'site') return `site|${[...cell.projects].sort().join(',')}`;
    return `${cell.kind}|${vacancyOf(employee.id, dayList[i])?.projectId ?? ''}${cell.conflict ? '|x' : ''}`;
  });

  // Bridge days off between two identical neighbours.
  for (let i = 0; i < sigs.length; i++) {
    if (sigs[i] !== '' || cells[i].kind !== 'off') continue;
    let j = i;
    while (j < sigs.length && sigs[j] === '' && cells[j].kind === 'off') j++;
    if (i > 0 && j < sigs.length && sigs[i - 1] !== '' && sigs[i - 1] === sigs[j]) {
      for (let k = i; k < j; k++) sigs[k] = sigs[j];
    }
    i = j;
  }

  const segments: Segment[] = [];
  const free: number[] = [];
  for (let i = 0; i < sigs.length; i++) {
    if (sigs[i] === '') {
      if (cells[i].kind === 'free') free.push(i);
      continue;
    }
    const last = segments[segments.length - 1];
    if (last && last.sig === sigs[i] && last.start + last.length === i) {
      last.length += 1;
    } else {
      segments.push({ start: i, length: 1, sig: sigs[i], cell: cells[i], vacancyProject: vacancyOf(employee.id, dayList[i])?.projectId });
    }
  }
  return { segments, free };
}

const NAME_COL = 236;

export function AssignmentSchedulePage() {
  const t = useT();
  const [weeks, setWeeks] = useState<1 | 2 | 4>(2);
  const [offset, setOffset] = useState(0);
  const [filter, setFilter] = useState<Filter>('all');
  const [search, setSearch] = useState('');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const [assignFor, setAssignFor] = useState<{ employeeId?: string; projectId?: string; date: string } | null>(null);
  const [view, setView] = useState<'people' | 'sites'>('people');
  const [replacingKey, setReplacingKey] = useState<string | null>(null);
  const [attentionOpen, setAttentionOpen] = useState<boolean | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const today = iso(new Date());
  const from = addDays(mondayOf(today), offset * weeks * 7);
  const days = weeks * 7;
  const { data, isLoading, isError, error: loadError, refetch } = useAssignmentScheduleQuery(from, days);
  const remove = useRemoveOnBoard();
  const assignReplacement = useAssignOnBoard();

  const dayList = useMemo(() => Array.from({ length: days }, (_, i) => addDays(from, i)), [from, days]);
  const ctx = useMemo(() => makeContext(data), [data]);
  const projectsById = useMemo(() => new Map((data?.projects ?? []).map((p) => [p.id, p])), [data]);
  const colorOf = (projectId: string) => {
    const index = (data?.projects ?? []).findIndex((p) => p.id === projectId);
    return SITE_COLORS[Math.max(index, 0) % SITE_COLORS.length];
  };

  const attention = useMemo(() => buildAttention(data, t), [data, t]);
  const vacancies = useMemo(() => buildVacancies(data), [data]);
  const vacancyOf = (employeeId: string, day: string) => vacancies.find((v) => v.employeeId === employeeId && v.from <= day && v.to >= day);
  const flagged = useMemo(
    () => new Set([...attention.filter((a) => a.level !== 'info').map((a) => a.employeeId), ...vacancies.map((v) => v.employeeId)]),
    [attention, vacancies],
  );

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (data?.employees ?? []).filter((e) => {
      if (term) {
        const project = e.todayProjectId ? projectsById.get(e.todayProjectId)?.name : '';
        const hay = [e.fullName, e.position, project, ...e.vehicles, ...e.tools].join(' ').toLowerCase();
        if (!hay.includes(term)) return false;
      }
      if (filter === 'on') return e.status === 'OnSite' || e.status === 'Late' || e.status === 'Expected';
      if (filter === 'free') return e.status === 'Free';
      if (filter === 'away') return e.status === 'Leave' || e.status === 'Sick';
      if (filter === 'issues') return flagged.has(e.id);
      return true;
    });
  }, [data, search, filter, flagged, projectsById]);

  const counts = useMemo(() => {
    const all = data?.employees ?? [];
    return {
      all: all.length,
      on: all.filter((e) => e.status === 'OnSite' || e.status === 'Late' || e.status === 'Expected').length,
      free: all.filter((e) => e.status === 'Free').length,
      away: all.filter((e) => e.status === 'Leave' || e.status === 'Sick').length,
      issues: attention.filter((a) => a.level !== 'info').length + vacancies.length,
    };
  }, [data, attention, vacancies]);

  const groups = useMemo(() => {
    const byProject = new Map<string, ScheduleEmployee[]>();
    const free: ScheduleEmployee[] = [];
    const away: ScheduleEmployee[] = [];
    for (const e of visible) {
      if (e.status === 'Leave' || e.status === 'Sick') away.push(e);
      else if (e.todayProjectId) byProject.set(e.todayProjectId, [...(byProject.get(e.todayProjectId) ?? []), e]);
      else free.push(e);
    }
    return { byProject, free, away };
  }, [visible]);

  const selected = (data?.employees ?? []).find((e) => e.id === selectedId) ?? null;
  const replacingVacancy = vacancies.find((v) => v.key === replacingKey) ?? null;
  const compact = weeks === 4;
  const unit = weeks === 1 ? 88 : weeks === 2 ? 52 : 24;
  const columns = `${NAME_COL}px ${dayList.map((d) => (isSunday(d) ? `minmax(${Math.round(unit * 0.55)}px, 0.55fr)` : `minmax(${unit}px, 1fr)`)).join(' ')}`;
  const minWidth = NAME_COL + dayList.reduce((sum, d) => sum + (isSunday(d) ? Math.round(unit * 0.55) : unit), 0);
  const barHeight = compact ? 26 : 34;

  const toggleGroup = (key: string) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  if (isLoading) return <Typography color="text.secondary">{t('common.loading')}</Typography>;
  if (isError || !data) return <ErrorState error={loadError} onRetry={() => void refetch()} />;

  const attentionCount = vacancies.length + attention.length;
  const showAttention = attentionOpen ?? attentionCount <= 4;

  const openReplacement = (v: Vacancy) => { setReplacingKey(v.key); setSelectedId(v.employeeId); setView('people'); };

  const renderBar = (e: ScheduleEmployee, seg: Segment) => {
    const { cell } = seg;
    const base = { gridColumn: `${seg.start + 2} / span ${seg.length}`, gridRow: 1, zIndex: 1, my: '3px', mx: '1px', minWidth: 0 } as const;

    if (cell.kind === 'leave' || cell.kind === 'sick') {
      const color = cell.kind === 'leave' ? LEAVE_COLOR : SICK_COLOR;
      const site = seg.vacancyProject ? projectsById.get(seg.vacancyProject)?.name : undefined;
      return (
        <Box
          key={`${e.id}-${seg.start}`}
          title={site ? t('schedule.vacantHere', { site }) : cell.conflict ? t('schedule.conflictLeave') : undefined}
          sx={{
            ...base, borderRadius: 1, display: 'flex', alignItems: 'center', gap: 0.75, px: 0.75, overflow: 'hidden',
            color, fontSize: 12, fontWeight: 600, whiteSpace: 'nowrap',
            border: site ? '2px dashed' : cell.conflict ? '2px solid' : '1px solid', borderColor: site ? 'warning.main' : cell.conflict ? 'error.main' : `${color}66`,
            background: `repeating-linear-gradient(135deg, ${color}2e 0 5px, ${color}0f 5px 10px)`,
          }}
        >
          {compact ? null : <span>{t(cell.kind === 'leave' ? 'schedule.leave' : 'schedule.sick')}</span>}
          {site && !compact && seg.length >= 2 ? (
            <Box component="span" sx={{ color: 'warning.main', fontSize: 11, overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {t('schedule.vacantShort', { site })}
            </Box>
          ) : null}
        </Box>
      );
    }

    const split = cell.projects.length > 1;
    return (
      <Box key={`${e.id}-${seg.start}`} sx={{ ...base, display: 'flex', flexDirection: 'column', gap: '2px' }}>
        {cell.projects.map((projectId) => {
          const project = projectsById.get(projectId);
          const color = colorOf(projectId);
          const shift = localShift(project?.shiftStartTime ?? null);
          const showShift = !compact && !split && seg.length >= 2 && shift;
          return (
            <Box
              key={projectId}
              title={`${project?.name ?? ''}${shift ? ` · ${t('schedule.from', { time: shift })}` : ''}`}
              sx={{
                flex: 1, minHeight: 0, borderRadius: compact ? 0.5 : 1, px: compact ? 0 : 0.75, minWidth: 0,
                display: 'flex', alignItems: 'center', gap: 0.75, fontSize: 12, fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden',
                bgcolor: `${color}26`, borderLeft: `3px solid ${color}`,
              }}
            >
              {compact ? null : <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>{project?.name}</Box>}
              {showShift ? (
                <Typography component="span" sx={{ fontSize: 11, fontFamily: 'monospace', color: 'text.secondary', fontWeight: 400 }}>{shift}</Typography>
              ) : null}
              {!compact && seg.length >= 3 && !split ? (
                <Box component="span" sx={{ ml: 'auto', display: 'inline-flex', gap: 0.5, color: 'text.secondary' }}>
                  {e.vehicles.length > 0 ? <LocalShippingOutlined sx={{ fontSize: 14 }} /> : null}
                  {e.tools.length > 0 ? <HandymanOutlined sx={{ fontSize: 14 }} /> : null}
                </Box>
              ) : null}
            </Box>
          );
        })}
      </Box>
    );
  };

  const renderRow = (e: ScheduleEmployee) => {
    const { segments, free } = segmentsFor(e, dayList, ctx, vacancyOf);
    const hasRequest = vacancies.some((v) => v.employeeId === e.id);
    return (
      <Box
        key={e.id}
        sx={{ display: 'grid', gridTemplateColumns: columns, borderTop: 1, borderColor: 'divider', bgcolor: selectedId === e.id ? 'action.hover' : 'transparent', minHeight: barHeight + 10 }}
      >
        <ButtonBase onClick={() => { setSelectedId(e.id); setReplacingKey(null); }} sx={{ gridColumn: 1, gridRow: 1, justifyContent: 'flex-start', textAlign: 'left', px: 2, py: 0.5, minWidth: 0 }}>
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="body2" noWrap sx={{ fontWeight: 600, lineHeight: 1.2 }}>{e.fullName}</Typography>
            <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', minWidth: 0 }}>
              <Box sx={{ width: 7, height: 7, borderRadius: '50%', bgcolor: STATUS_COLOR[e.status], flexShrink: 0 }} />
              <Typography variant="caption" color="text.secondary" noWrap>
                {e.position} · {t(`schedule.status.${e.status}` as 'schedule.status.Free')}
                {e.status === 'OnSite' || e.status === 'Late' ? ` ${localClock(e.clockedInAt)}` : ''}
                {hasRequest ? ` · ${t('schedule.needsCover')}` : ''}
              </Typography>
            </Stack>
          </Box>
        </ButtonBase>

        {dayList.map((day, i) => (
          <Box
            key={day}
            aria-hidden
            sx={{
              gridColumn: i + 2, gridRow: 1, borderLeft: 1, borderColor: day.slice(8) === '01' || dayOfWeek(day) === 1 ? 'divider' : 'transparent',
              bgcolor: day === today ? 'action.selected' : isSunday(day) ? 'action.hover' : 'transparent',
              borderLeftWidth: dayOfWeek(day) === 1 ? 2 : 1,
            }}
          />
        ))}

        {free.map((i) => (
          <ButtonBase
            key={`f-${i}`}
            aria-label={t('schedule.assignOn', { name: e.fullName, date: dayList[i] })}
            onClick={() => setAssignFor({ employeeId: e.id, date: dayList[i] })}
            sx={{ gridColumn: i + 2, gridRow: 1, zIndex: 1, my: '3px', mx: '1px', borderRadius: 1, color: 'transparent', border: '1px dashed transparent', '&:hover': { color: 'primary.main', borderColor: 'primary.main', bgcolor: 'action.hover' } }}
          >
            {compact ? null : <AddOutlined fontSize="small" />}
          </ButtonBase>
        ))}

        {segments.map((seg) => renderBar(e, seg))}
      </Box>
    );
  };

  const groupHeader = (key: string, title: string, meta: string, color?: string, warn?: string) => (
    <ButtonBase
      key={`h-${key}`}
      onClick={() => toggleGroup(key)}
      aria-expanded={!collapsed.has(key)}
      sx={{ width: '100%', justifyContent: 'flex-start', gap: 1, px: 1.5, py: 0.5, borderTop: 1, borderColor: 'divider', bgcolor: 'action.hover', textAlign: 'left', position: 'sticky', left: 0 }}
    >
      <ExpandMoreOutlined fontSize="small" sx={{ transform: collapsed.has(key) ? 'rotate(-90deg)' : 'none', transition: 'transform .15s' }} />
      {color ? <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: color }} /> : null}
      <Typography variant="body2" sx={{ fontWeight: 700 }}>{title}</Typography>
      <Typography variant="caption" color="text.secondary">{meta}</Typography>
      {warn ? <Typography variant="caption" color="error" sx={{ fontWeight: 600 }}>{warn}</Typography> : null}
    </ButtonBase>
  );

  const periodLabel = `${shortDate(dayList[0])} – ${shortDate(dayList[dayList.length - 1])}${dayList[dayList.length - 1].slice(0, 4)}`;
  const filters: Filter[] = ['all', 'on', 'free', 'away', 'issues'];

  return (
    <Box>
      <PageHeader
        title={t('schedule.boardTitle')}
        description={t('schedule.description', { minutes: data.lateToleranceMinutes })}
        secondaryActions={
          <Button component={Link} to={paths.assignmentBoardClassic} size="small">
            {t('schedule.classic')}
          </Button>
        }
      />

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5, alignItems: 'center' }}>
        <ToggleButtonGroup size="small" exclusive value={weeks} onChange={(_, value: 1 | 2 | 4 | null) => { if (value) { setWeeks(value); setOffset(0); } }} aria-label={t('schedule.range')}>
          <ToggleButton value={1}>{t('schedule.weeks1')}</ToggleButton>
          <ToggleButton value={2}>{t('schedule.weeks2')}</ToggleButton>
          <ToggleButton value={4}>{t('schedule.weeks4')}</ToggleButton>
        </ToggleButtonGroup>
        <Button size="small" onClick={() => setOffset((o) => o - 1)} aria-label={t('schedule.previous')}>‹</Button>
        <Button size="small" onClick={() => setOffset(0)} disabled={offset === 0}>{t('schedule.today')}</Button>
        <Button size="small" onClick={() => setOffset((o) => o + 1)} aria-label={t('schedule.next')}>›</Button>
        <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>{periodLabel}</Typography>
        <ToggleButtonGroup size="small" exclusive value={view} onChange={(_, value: 'people' | 'sites' | null) => { if (value) setView(value); }} aria-label={t('schedule.view')}>
          <ToggleButton value="people">{t('schedule.viewPeople')}</ToggleButton>
          <ToggleButton value="sites">{t('schedule.viewSites')}</ToggleButton>
        </ToggleButtonGroup>
        <Box sx={{ flex: 1 }} />
        <SearchField value={search} onChange={setSearch} placeholder={t('schedule.search')} />
      </Stack>

      <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5, alignItems: 'center' }}>
        {filters.map((key) => (
          <ButtonBase
            key={key}
            onClick={() => setFilter(key)}
            aria-pressed={filter === key}
            sx={{
              gap: 0.75, px: 1.25, py: 0.5, borderRadius: 4, border: 1, fontSize: 13,
              borderColor: filter === key ? 'text.primary' : 'divider', bgcolor: filter === key ? 'action.selected' : 'background.paper',
            }}
          >
            <Box component="span" sx={{ fontWeight: 700, color: key === 'issues' && counts.issues > 0 ? 'error.main' : 'text.primary' }}>{counts[key]}</Box>
            <Box component="span" sx={{ color: 'text.secondary' }}>{t(`schedule.filter.${key}` as 'schedule.filter.all')}</Box>
          </ButtonBase>
        ))}
      </Stack>

      {attentionCount > 0 && (
        <Paper variant="outlined" sx={{ mb: 1.5, borderColor: vacancies.length > 0 ? 'warning.main' : 'divider' }}>
          <ButtonBase
            onClick={() => setAttentionOpen(!showAttention)}
            aria-expanded={showAttention}
            sx={{ width: '100%', justifyContent: 'flex-start', gap: 1, px: 1.5, py: 0.75, textAlign: 'left' }}
          >
            <ExpandMoreOutlined fontSize="small" sx={{ transform: showAttention ? 'none' : 'rotate(-90deg)', transition: 'transform .15s' }} />
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{t('schedule.attention')}</Typography>
            {vacancies.length > 0 && <Typography variant="caption" sx={{ fontWeight: 700, color: 'warning.main' }}>{t('schedule.summaryVacancies', { count: vacancies.length })}</Typography>}
            {attention.filter((a) => a.level === 'crit').length > 0 && <Typography variant="caption" sx={{ fontWeight: 700, color: 'error.main' }}>{t('schedule.summaryUrgent', { count: attention.filter((a) => a.level === 'crit').length })}</Typography>}
            {attention.filter((a) => a.level === 'warn').length > 0 && <Typography variant="caption" sx={{ fontWeight: 700, color: 'warning.main' }}>{t('schedule.summaryWarn', { count: attention.filter((a) => a.level === 'warn').length })}</Typography>}
          </ButtonBase>
          {showAttention && (
            <Box sx={{ px: 1.5, pb: 1 }}>
              {vacancies.map((v) => {
                const absent = data.employees.find((e) => e.id === v.employeeId);
                return (
                  <Box key={v.key} sx={{ display: 'flex', gap: 1.5, alignItems: 'center', py: 0.5, borderTop: 1, borderColor: 'divider', bgcolor: replacingKey === v.key ? 'action.hover' : 'transparent' }}>
                    <Typography variant="caption" sx={{ fontWeight: 700, textTransform: 'uppercase', color: 'warning.main', flexShrink: 0 }}>{t('schedule.level.vacancy')}</Typography>
                    <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }}>
                      {t('schedule.vacancyLine', { name: absent?.fullName ?? '', from: shortDate(v.from), to: shortDate(v.to), days: v.days })}{' '}
                      <Typography component="span" variant="caption" color="text.secondary">{t('schedule.vacancyHint', { site: projectsById.get(v.projectId)?.name ?? '' })}</Typography>
                    </Typography>
                    <Button size="small" variant="contained" onClick={() => openReplacement(v)}>{t('schedule.findReplacement')}</Button>
                  </Box>
                );
              })}
              {attention.map((a) => (
                <Box key={a.key} sx={{ display: 'flex', gap: 1.5, alignItems: 'center', py: 0.5, borderTop: 1, borderColor: 'divider' }}>
                  <Typography variant="caption" sx={{ fontWeight: 700, textTransform: 'uppercase', color: a.level === 'crit' ? 'error.main' : a.level === 'warn' ? 'warning.main' : 'text.secondary', flexShrink: 0 }}>
                    {t(`schedule.level.${a.level}` as 'schedule.level.crit')}
                  </Typography>
                  <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }}>{a.text}</Typography>
                  <Button size="small" onClick={() => { setSelectedId(a.employeeId); setReplacingKey(null); setFilter('all'); setSearch(''); }}>{t('schedule.open')}</Button>
                </Box>
              ))}
            </Box>
          )}
        </Paper>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) 300px' }, gap: 1.5, alignItems: 'start' }}>
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
          {view === 'sites' ? (
            <SitesView
              data={data}
              visible={visible}
              today={today}
              colorOf={colorOf}
              vacancies={vacancies}
              onPick={(id) => { setSelectedId(id); setReplacingKey(null); }}
              onReplace={(key, employeeId) => { setReplacingKey(key); setSelectedId(employeeId); }}
              onAdd={(projectId) => setAssignFor({ projectId, date: today })}
            />
          ) : (
            <Box sx={{ overflowX: 'auto' }}>
              <Box sx={{ minWidth }}>
                {weeks > 1 && (
                  <Box sx={{ display: 'grid', gridTemplateColumns: columns }}>
                    <Box />
                    {Array.from({ length: weeks }, (_, k) => (
                      <Box key={k} sx={{ gridColumn: `${k * 7 + 2} / span 7`, px: 1, py: 0.25, fontSize: 11, fontWeight: 600, color: 'text.secondary', borderLeft: 2, borderColor: 'divider' }}>
                        {t('schedule.weekLabel', { from: shortDate(dayList[k * 7]), to: shortDate(dayList[k * 7 + 6]) })}
                      </Box>
                    ))}
                  </Box>
                )}
                <Box sx={{ display: 'grid', gridTemplateColumns: columns, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="caption" color="text.secondary" sx={{ px: 2, py: 0.75, alignSelf: 'end' }}>{t('schedule.worker')}</Typography>
                  {dayList.map((day) => (
                    <Box
                      key={day}
                      sx={{
                        textAlign: 'center', py: 0.5, borderLeft: dayOfWeek(day) === 1 ? 2 : 1, borderColor: dayOfWeek(day) === 1 ? 'divider' : 'transparent',
                        bgcolor: day === today ? 'action.selected' : isSunday(day) ? 'action.hover' : 'transparent',
                      }}
                    >
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', lineHeight: 1.1, fontSize: compact ? 9 : 11 }}>
                        {new Date(`${day}T00:00:00Z`).toLocaleDateString([], { weekday: compact ? 'narrow' : 'short', timeZone: 'UTC' })}
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700, fontSize: compact ? 10 : 13, lineHeight: 1.2, color: day === today ? 'primary.main' : 'text.primary' }}>
                        {day.slice(8)}
                      </Typography>
                    </Box>
                  ))}
                </Box>

                {[...groups.byProject.entries()].map(([projectId, members]) => {
                  const project: ScheduleProject | undefined = projectsById.get(projectId);
                  const shift = localShift(project?.shiftStartTime ?? null);
                  const over = project?.endDate && project.endDate < today ? t('schedule.overdue', { date: shortDate(project.endDate) }) : undefined;
                  return (
                    <Box key={projectId}>
                      {groupHeader(projectId, project?.name ?? t('schedule.unknownSite'), `${members.length}${shift ? ` · ${t('schedule.from', { time: shift })}` : ''}`, colorOf(projectId), over)}
                      {!collapsed.has(projectId) && members.map(renderRow)}
                    </Box>
                  );
                })}
                {groups.free.length > 0 && (
                  <Box>
                    {groupHeader('free', t('schedule.groupFree'), String(groups.free.length))}
                    {!collapsed.has('free') && groups.free.map(renderRow)}
                  </Box>
                )}
                {groups.away.length > 0 && (
                  <Box>
                    {groupHeader('away', t('schedule.groupAway'), String(groups.away.length))}
                    {!collapsed.has('away') && groups.away.map(renderRow)}
                  </Box>
                )}
                {visible.length === 0 && <Typography color="text.secondary" sx={{ p: 4, textAlign: 'center' }}>{t('common.noResults')}</Typography>}
              </Box>
            </Box>
          )}
          <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', px: 2, py: 0.75, borderTop: 1, borderColor: 'divider', color: 'text.secondary', fontSize: 12 }}>
            <span>{t('schedule.legend.site')}</span>
            <span>{t('schedule.legend.leave')}</span>
            <span>{t('schedule.legend.vacancy')}</span>
            <span>{t('schedule.legend.split')}</span>
            <span>{t('schedule.legend.weekend')}</span>
            <span>{t('schedule.legend.free')}</span>
          </Stack>
        </Paper>

        <Paper variant="outlined" sx={{ p: 2, position: { lg: 'sticky' }, top: 12 }}>
          {replacingVacancy ? (
            <ReplacementPanel
              schedule={data}
              ctx={ctx}
              vacancy={replacingVacancy}
              colorOf={colorOf}
              pending={assignReplacement.isPending}
              onCancel={() => setReplacingKey(null)}
              onPick={(employeeId) => setSelectedId(employeeId)}
              onAssign={(candidate) =>
                assignReplacement.mutate(
                  { employeeId: candidate.id, projectId: replacingVacancy.projectId, startDate: replacingVacancy.from, endDate: replacingVacancy.to },
                  {
                    onSuccess: () => { setReplacingKey(null); setSelectedId(candidate.id); setMessage(t('schedule.replacementAssigned', { name: candidate.fullName })); },
                    onError: (err) => setError(toApiError(err).message),
                  },
                )
              }
            />
          ) : selected ? (
            <Inspector
              employee={selected}
              schedule={data}
              colorOf={colorOf}
              onAssign={() => setAssignFor({ employeeId: selected.id, date: today })}
              onRemove={(projectId) =>
                remove.mutate(
                  { employeeId: selected.id, projectId },
                  {
                    onSuccess: () => setMessage(t('schedule.removed', { name: selected.fullName })),
                    onError: (err) => setError(toApiError(err).message),
                  },
                )
              }
            />
          ) : (
            <Typography color="text.secondary">{t('schedule.pickWorker')}</Typography>
          )}
        </Paper>
      </Box>

      <AssignDialog
        key={assignFor ? `${assignFor.employeeId ?? ''}-${assignFor.projectId ?? ''}-${assignFor.date}` : 'closed'}
        target={assignFor}
        employees={data.employees}
        projects={data.projects}
        onClose={() => setAssignFor(null)}
        onDone={(name) => { setAssignFor(null); setMessage(t('schedule.assigned', { name })); }}
        onError={setError}
      />

      <Snackbar open={!!message} autoHideDuration={4000} onClose={() => setMessage(null)} message={message ?? ''} />
      <Snackbar open={!!error} autoHideDuration={8000} onClose={() => setError(null)}>
        <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>
      </Snackbar>
    </Box>
  );
}

function buildAttention(
  data: AssignmentSchedule | undefined,
  t: ReturnType<typeof useT>,
): AttentionItem[] {
  if (!data) return [];
  const projects = new Map(data.projects.map((p) => [p.id, p]));
  const items: AttentionItem[] = [];

  for (const e of data.employees) {
    const site = e.todayProjectId ? projects.get(e.todayProjectId)?.name ?? '' : '';
    if (e.status === 'NoShow') {
      items.push({ key: `no-${e.id}`, level: 'crit', employeeId: e.id, text: t('schedule.alert.noShow', { name: e.fullName, site }) });
    } else if (e.status === 'Late') {
      items.push({ key: `late-${e.id}`, level: 'warn', employeeId: e.id, text: t('schedule.alert.late', { name: e.fullName, site, minutes: data.lateToleranceMinutes }) });
    }
  }

  const free = data.employees.filter((e) => e.status === 'Free').length;
  if (free > 0) {
    const first = data.employees.find((e) => e.status === 'Free')!;
    items.push({ key: 'free', level: 'info', employeeId: first.id, text: t('schedule.alert.free', { count: free }) });
  }
  return items;
}

function Inspector({
  employee,
  schedule,
  colorOf,
  onAssign,
  onRemove,
}: {
  employee: ScheduleEmployee;
  schedule: AssignmentSchedule;
  colorOf: (projectId: string) => string;
  onAssign: () => void;
  onRemove: (projectId: string) => void;
}) {
  const t = useT();
  const projects = new Map(schedule.projects.map((p) => [p.id, p]));
  const todaySite = employee.todayProjectId ? projects.get(employee.todayProjectId) : undefined;
  const shift = localShift(todaySite?.shiftStartTime ?? null);

  return (
    <Stack spacing={1.5}>
      <Box>
        <Typography variant="caption" color="text.secondary">{employee.position}</Typography>
        <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.15 }}>{employee.fullName}</Typography>
      </Box>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Box sx={{ width: 9, height: 9, borderRadius: '50%', bgcolor: STATUS_COLOR[employee.status] }} />
        <Typography sx={{ fontWeight: 600 }}>{t(`schedule.status.${employee.status}` as 'schedule.status.Free')}</Typography>
        {employee.clockedInAt ? (
          <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
            {t('schedule.clockedIn', { time: localClock(employee.clockedInAt) })}
          </Typography>
        ) : null}
      </Stack>
      {employee.status === 'NoShow' && (
        <Alert severity="error" icon={<WarningAmberOutlined />}>{t('schedule.noShowHint', { time: shift ?? '' })}</Alert>
      )}
      {employee.status === 'Late' && (
        <Alert severity="warning">{t('schedule.lateHint', { minutes: schedule.lateToleranceMinutes })}</Alert>
      )}
      {todaySite && shift ? (
        <Typography variant="body2" color="text.secondary">
          {todaySite.name} · {t('schedule.from', { time: shift })}
        </Typography>
      ) : null}
      <Box>
        <Typography variant="caption" color="text.secondary">{t('schedule.postings')}</Typography>
        {employee.postings.length === 0 && <Typography variant="body2">—</Typography>}
        {employee.postings.map((p) => (
          <Stack key={`${p.projectId}-${p.startDate}`} direction="row" spacing={1} sx={{ alignItems: 'center', py: 0.25 }}>
            <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: colorOf(p.projectId) }} />
            <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }} noWrap>
              {projects.get(p.projectId)?.name ?? t('schedule.unknownSite')}
              <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1, fontFamily: 'monospace' }}>
                {shortDate(p.startDate)}{p.endDate ? ` – ${shortDate(p.endDate)}` : ' →'}
              </Typography>
            </Typography>
            <Tooltip title={t('schedule.removeFromSite')}>
              <IconButton size="small" onClick={() => onRemove(p.projectId)} aria-label={t('schedule.removeFromSite')}>
                <CloseOutlined fontSize="inherit" />
              </IconButton>
            </Tooltip>
          </Stack>
        ))}
      </Box>
      {employee.absences.length > 0 && (
        <Box>
          <Typography variant="caption" color="text.secondary">{t('schedule.absences')}</Typography>
          {employee.absences.map((a) => (
            <Typography key={`${a.type}-${a.startDate}`} variant="body2" sx={{ fontFamily: 'monospace' }}>
              {shortDate(a.startDate)} – {shortDate(a.endDate)}
            </Typography>
          ))}
        </Box>
      )}
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', color: 'text.secondary', alignItems: 'center' }}>
        <LocalShippingOutlined fontSize="small" />
        <Typography variant="body2">{employee.vehicles.join(', ') || '—'}</Typography>
      </Stack>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', color: 'text.secondary', alignItems: 'center' }}>
        <HandymanOutlined fontSize="small" />
        <Typography variant="body2">{employee.tools.join(', ') || '—'}</Typography>
      </Stack>
      <Button variant="contained" onClick={onAssign}>{t('schedule.assign')}</Button>
    </Stack>
  );
}

function ReplacementPanel({
  schedule,
  ctx,
  vacancy,
  colorOf,
  pending,
  onCancel,
  onPick,
  onAssign,
}: {
  schedule: AssignmentSchedule;
  ctx: DayContext;
  vacancy: Vacancy;
  colorOf: (projectId: string) => string;
  pending: boolean;
  onCancel: () => void;
  onPick: (employeeId: string) => void;
  onAssign: (employee: ScheduleEmployee) => void;
}) {
  const t = useT();
  const absent = schedule.employees.find((e) => e.id === vacancy.employeeId);
  const site = schedule.projects.find((p) => p.id === vacancy.projectId);
  const candidates = candidatesFor(schedule, vacancy, ctx);
  const shift = localShift(site?.shiftStartTime ?? null);

  return (
    <Stack spacing={1.5}>
      <Box>
        <Typography variant="caption" color="text.secondary">{t('schedule.replacementFor')}</Typography>
        <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.15 }}>{absent?.fullName}</Typography>
      </Box>
      <Alert severity="warning">
        {t('schedule.vacancyBanner', { from: shortDate(vacancy.from), to: shortDate(vacancy.to), days: vacancy.days })}
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mt: 0.5 }}>
          <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: colorOf(vacancy.projectId) }} />
          <Typography variant="body2" sx={{ fontWeight: 600 }}>{site?.name}{shift ? ` · ${t('schedule.from', { time: shift })}` : ''}</Typography>
        </Stack>
      </Alert>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{t('schedule.suggested')}</Typography>
        <Typography variant="caption" color="text.secondary">{t('schedule.suggestedHint', { trade: absent?.position ?? '' })}</Typography>
      </Box>
      <Stack spacing={1}>
        {candidates.length === 0 && <Typography variant="body2" color="text.secondary">{t('schedule.noCandidates')}</Typography>}
        {candidates.map((c) => (
          <Box key={c.employee.id} sx={{ display: 'flex', gap: 1, alignItems: 'center', border: 1, borderColor: 'divider', borderRadius: 2, p: 1 }}>
            <ButtonBase onClick={() => onPick(c.employee.id)} sx={{ flex: 1, minWidth: 0, textAlign: 'left', flexDirection: 'column', alignItems: 'flex-start' }}>
              <Typography variant="body2" noWrap sx={{ fontWeight: 600 }}>{c.employee.fullName}</Typography>
              <Typography variant="caption" color="text.secondary" noWrap>
                {c.employee.position} · {c.sameTrade ? t('schedule.sameTrade') : t('schedule.otherTrade')} · {t('schedule.freeDays', { free: c.freeDays, total: vacancy.days })}
              </Typography>
            </ButtonBase>
            <Button size="small" variant="contained" disabled={pending} onClick={() => onAssign(c.employee)}>{t('schedule.assignShort')}</Button>
          </Box>
        ))}
      </Stack>
      <Typography variant="caption" color="text.secondary">{t('schedule.replacementNote')}</Typography>
      <Button onClick={onCancel}>{t('common.cancel')}</Button>
    </Stack>
  );
}

function SitesView({
  data,
  visible,
  today,
  colorOf,
  vacancies,
  onPick,
  onReplace,
  onAdd,
}: {
  data: AssignmentSchedule;
  visible: ScheduleEmployee[];
  today: string;
  colorOf: (projectId: string) => string;
  vacancies: Vacancy[];
  onPick: (employeeId: string) => void;
  onReplace: (key: string, employeeId: string) => void;
  onAdd: (projectId: string) => void;
}) {
  const t = useT();
  const shown = new Set(visible.map((e) => e.id));

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(auto-fill, minmax(300px, 1fr))' }, gap: 1.5, p: 1.5 }}>
      {data.projects.map((project) => {
        const crew = data.employees.filter((e) => e.postings.some((p) => p.projectId === project.id && covers(p.startDate, p.endDate, today)));
        const mine = vacancies.filter((v) => v.projectId === project.id);
        const onSite = crew.filter((e) => e.status === 'OnSite' || e.status === 'Late').length;
        const shift = localShift(project.shiftStartTime);
        const vehicles = [...new Set(crew.flatMap((e) => e.vehicles))];
        const tools = [...new Set(crew.flatMap((e) => e.tools))];
        const color = colorOf(project.id);
        if (crew.length === 0 && mine.length === 0 && visible.length !== data.employees.length) return null;

        return (
          <Paper key={project.id} variant="outlined" sx={{ p: 1.5, display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: color }} />
              <Typography sx={{ fontWeight: 700, flex: 1 }} noWrap>{project.name}</Typography>
              {project.endDate && project.endDate < today ? (
                <Typography variant="caption" color="error" sx={{ fontWeight: 700 }}>{t('schedule.overdue', { date: shortDate(project.endDate) })}</Typography>
              ) : null}
            </Stack>
            <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
              {t('schedule.siteCounts', { posted: crew.length, onSite })}{shift ? ` · ${t('schedule.from', { time: shift })}` : ''}
            </Typography>
            <Box sx={{ display: 'flex', gap: '3px' }} aria-hidden>
              {crew.map((e) => (
                <Box key={e.id} sx={{ flex: 1, height: 8, borderRadius: 0.5, bgcolor: e.status === 'OnSite' ? 'success.main' : e.status === 'Late' ? 'warning.main' : e.status === 'NoShow' ? 'error.main' : 'divider' }} />
              ))}
            </Box>
            <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexWrap: 'wrap' }}>
              {crew.filter((e) => shown.has(e.id)).map((e) => (
                <ButtonBase key={e.id} onClick={() => onPick(e.id)} sx={{ border: 1, borderColor: 'divider', borderRadius: 4, px: 1, py: 0.25, gap: 0.5, fontSize: 12 }}>
                  <Box sx={{ width: 7, height: 7, borderRadius: '50%', bgcolor: STATUS_COLOR[e.status] }} />
                  {e.fullName}
                </ButtonBase>
              ))}
              {crew.length === 0 && <Typography variant="caption" color="text.secondary">{t('schedule.noCrew')}</Typography>}
            </Stack>
            {mine.map((v) => (
              <Alert key={v.key} severity="warning" action={<Button color="inherit" size="small" onClick={() => onReplace(v.key, v.employeeId)}>{t('schedule.findReplacement')}</Button>}>
                {t('schedule.siteVacancy', { name: data.employees.find((e) => e.id === v.employeeId)?.fullName ?? '', from: shortDate(v.from), to: shortDate(v.to) })}
              </Alert>
            ))}
            <Stack direction="row" spacing={1.5} sx={{ color: 'text.secondary', alignItems: 'center' }}>
              <LocalShippingOutlined fontSize="small" />
              <Typography variant="caption" noWrap>{vehicles.join(', ') || '—'}</Typography>
              <HandymanOutlined fontSize="small" />
              <Typography variant="caption" noWrap>{tools.join(', ') || '—'}</Typography>
            </Stack>
            <Button size="small" onClick={() => onAdd(project.id)}>{t('schedule.addWorker')}</Button>
          </Paper>
        );
      })}
    </Box>
  );
}

function AssignDialog({
  target,
  employees,
  projects,
  onClose,
  onDone,
  onError,
}: {
  target: { employeeId?: string; projectId?: string; date: string } | null;
  employees: ScheduleEmployee[];
  projects: ScheduleProject[];
  onClose: () => void;
  onDone: (name: string) => void;
  onError: (message: string) => void;
}) {
  const t = useT();
  const assign = useAssignOnBoard();
  const [employeeId, setEmployeeId] = useState(target?.employeeId ?? '');
  const [projectId, setProjectId] = useState(target?.projectId ?? '');
  const [start, setStart] = useState(target?.date ?? dateOnlyOffset(0));
  const [end, setEnd] = useState('');
  const employee = employees.find((e) => e.id === employeeId);
  const sites = start ? projects.filter((p) => worksOn(p, start)) : projects;
  const valid = !!employeeId && !!projectId && sites.some((p) => p.id === projectId) && !!start && (!end || end >= start);

  return (
    <Dialog open={!!target} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{employee ? t('schedule.assignTitle', { name: employee.fullName }) : t('schedule.addWorker')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {target?.employeeId ? null : (
            <TextField select label={t('schedule.worker')} value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
              {employees.map((e) => <MenuItem key={e.id} value={e.id}>{e.fullName} · {e.position}</MenuItem>)}
            </TextField>
          )}
          <TextField select label={t('schedule.site')} value={projectId} onChange={(e) => setProjectId(e.target.value)}>
            {sites.map((p) => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
          </TextField>
          <TextField type="date" label={t('schedule.startDate')} value={start} onChange={(e) => setStart(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField
            type="date" label={t('schedule.endDate')} value={end} onChange={(e) => setEnd(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
            error={!!end && end < start} helperText={end && end < start ? t('schedule.endsBeforeStart') : t('schedule.endOptional')}
          />
          {start && dayOfWeek(start) === 6 || start && dayOfWeek(start) === 0 ? (
            <Typography variant="caption" color="text.secondary">{t('schedule.weekendHint')}</Typography>
          ) : null}
          <Typography variant="caption" color="text.secondary">{t('schedule.splitHint')}</Typography>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!valid || assign.isPending}
          onClick={() =>
            employee && assign.mutate(
              { employeeId: employee.id, projectId, startDate: start, endDate: end || null },
              { onSuccess: () => onDone(employee.fullName), onError: (err) => onError(toApiError(err).message) },
            )
          }
        >
          {t('schedule.assign')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
