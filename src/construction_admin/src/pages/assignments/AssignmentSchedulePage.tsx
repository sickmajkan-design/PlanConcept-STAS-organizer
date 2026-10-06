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
const isWeekend = (value: string) => {
  const day = new Date(`${value}T00:00:00Z`).getUTCDay();
  return day === 0 || day === 6;
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

interface Cell {
  kind: 'site' | 'leave' | 'sick' | 'free' | 'off';
  projects: string[];
  conflict: boolean;
  absence?: string;
}

function cellFor(employee: ScheduleEmployee, day: string): Cell {
  const absence = employee.absences.find((a) => covers(a.startDate, a.endDate, day));
  const projects = employee.postings
    .filter((p) => covers(p.startDate, p.endDate, day))
    .map((p) => p.projectId);

  if (absence) {
    return {
      kind: absence.type === 'SickLeave' ? 'sick' : 'leave',
      projects,
      // Posted on a day with approved leave: somebody has to decide.
      conflict: projects.length > 0,
      absence: absence.type,
    };
  }

  if (isWeekend(day)) return { kind: 'off', projects, conflict: false };
  if (projects.length > 0) return { kind: 'site', projects, conflict: false };
  return { kind: 'free', projects, conflict: false };
}

/** An approved absence on days someone is posted to a site: the position is empty and needs a replacement. */
interface Vacancy {
  key: string;
  employeeId: string;
  projectId: string;
  from: string;
  to: string;
  /** Working days in the range. */
  days: number;
}

const eachDay = (from: string, to: string) => {
  const days: string[] = [];
  for (let d = from; d <= to; d = addDays(d, 1)) days.push(d);
  return days;
};

/**
 * Empty positions: every posting that overlaps an approved absence, from today on. One that
 * somebody already covers (another worker with a bounded posting on the same site across the
 * whole stretch) is not listed. That is what "Assign replacement" creates, so the card goes
 * away once it is done.
 */
function buildVacancies(data: AssignmentSchedule | undefined): Vacancy[] {
  if (!data) return [];
  const result: Vacancy[] = [];

  for (const e of data.employees) {
    for (const a of e.absences) {
      for (const p of e.postings) {
        const from = [a.startDate, p.startDate, data.today].sort().at(-1)!;
        const to = [a.endDate, p.endDate ?? a.endDate].sort()[0];
        const working = eachDay(from, to).filter((d) => !isWeekend(d));
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
function candidatesFor(data: AssignmentSchedule, vacancy: Vacancy): Candidate[] {
  const absent = data.employees.find((e) => e.id === vacancy.employeeId);
  const window = eachDay(vacancy.from, vacancy.to).filter((d) => !isWeekend(d));
  return data.employees
    .filter((e) => e.id !== vacancy.employeeId)
    .map((employee) => ({
      employee,
      freeDays: window.filter((d) => cellFor(employee, d).kind === 'free').length,
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
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const today = iso(new Date());
  const from = addDays(mondayOf(today), offset * weeks * 7);
  const days = weeks * 7;
  const { data, isLoading, isError, error: loadError, refetch } = useAssignmentScheduleQuery(from, days);
  const remove = useRemoveOnBoard();
  const assignReplacement = useAssignOnBoard();

  const dayList = useMemo(() => Array.from({ length: days }, (_, i) => addDays(from, i)), [from, days]);
  const projectsById = useMemo(
    () => new Map((data?.projects ?? []).map((p) => [p.id, p])),
    [data],
  );
  const colorOf = (projectId: string) => {
    const index = (data?.projects ?? []).findIndex((p) => p.id === projectId);
    return SITE_COLORS[Math.max(index, 0) % SITE_COLORS.length];
  };

  const attention = useMemo(() => buildAttention(data, t), [data, t]);
  const vacancies = useMemo(() => buildVacancies(data), [data]);
  const vacantDays = useMemo(() => {
    const set = new Set<string>();
    for (const v of vacancies) for (const d of eachDay(v.from, v.to)) set.add(`${v.employeeId}|${d}`);
    return set;
  }, [vacancies]);
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
  const columns = `250px repeat(${days}, minmax(${weeks === 1 ? 92 : weeks === 2 ? 56 : 26}px, 1fr))`;
  const compact = weeks === 4;

  const toggleGroup = (key: string) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  if (isLoading) return <Typography color="text.secondary">{t('common.loading')}</Typography>;
  if (isError || !data) return <ErrorState error={loadError} onRetry={() => void refetch()} />;

  const siteCells = (e: ScheduleEmployee, day: string, cell: Cell) => {
    const vacancy = vacantDays.has(`${e.id}|${day}`) ? vacancyOf(e.id, day) : undefined;
    if (cell.kind === 'off') return null;
    if (cell.kind === 'free') {
      return (
        <ButtonBase
          aria-label={t('schedule.assignOn', { name: e.fullName, date: day })}
          onClick={() => setAssignFor({ employeeId: e.id, date: day })}
          sx={{ flex: 1, border: '1px dashed', borderColor: 'divider', borderRadius: 1, color: 'transparent', '&:hover': { color: 'primary.main', borderColor: 'primary.main' } }}
        >
          {compact ? null : <AddOutlined fontSize="small" />}
        </ButtonBase>
      );
    }
    if (cell.kind === 'leave' || cell.kind === 'sick') {
      const color = cell.kind === 'leave' ? LEAVE_COLOR : SICK_COLOR;
      return (
        <Box
          title={vacancy ? t('schedule.vacantHere', { site: projectsById.get(vacancy.projectId)?.name ?? '' }) : cell.conflict ? t('schedule.conflictLeave') : undefined}
          sx={{
            flex: 1, borderRadius: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', px: 0.5,
            color, fontSize: 12, fontWeight: 600,
            border: vacancy ? '2px dashed' : cell.conflict ? '2px solid' : 'none', borderColor: vacancy ? 'warning.main' : 'error.main',
            background: `repeating-linear-gradient(135deg, ${color}2e 0 5px, ${color}0f 5px 10px)`,
          }}
        >
          {compact ? '' : (
            <Box sx={{ textAlign: 'center', lineHeight: 1.2 }}>
              {t(cell.kind === 'leave' ? 'schedule.leave' : 'schedule.sick')}
              {vacancy && weeks === 1 ? (
                <Box sx={{ color: 'warning.main', fontSize: 10 }}>{t('schedule.vacantShort', { site: projectsById.get(vacancy.projectId)?.name ?? '' })}</Box>
              ) : null}
            </Box>
          )}
        </Box>
      );
    }
    return (
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '2px', minWidth: 0 }}>
        {cell.projects.map((projectId) => {
          const project = projectsById.get(projectId);
          const color = colorOf(projectId);
          const shift = localShift(project?.shiftStartTime ?? null);
          return (
            <Box
              key={projectId}
              title={`${project?.name ?? ''}${shift ? ` · ${t('schedule.from', { time: shift })}` : ''}`}
              sx={{
                flex: 1, borderRadius: compact ? 0.5 : 1, px: compact ? 0 : 0.75, py: compact ? 0 : 0.25, minWidth: 0,
                fontSize: weeks === 1 ? 12 : 11, fontWeight: 500, lineHeight: 1.25,
                bgcolor: `${color}29`, border: '1px solid', borderColor: `${color}8c`,
                display: 'flex', flexDirection: 'column', justifyContent: 'space-between',
              }}
            >
              {compact ? null : (
                <>
                  <Box sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{project?.name}</Box>
                  {weeks === 1 && shift ? (
                    <Typography component="span" sx={{ fontSize: 10, fontFamily: 'monospace', color: 'text.secondary' }}>
                      {t('schedule.from', { time: shift })}
                    </Typography>
                  ) : null}
                </>
              )}
            </Box>
          );
        })}
      </Box>
    );
  };

  const renderRow = (e: ScheduleEmployee) => (
    <Box key={e.id} sx={{ display: 'grid', gridTemplateColumns: columns, borderTop: 1, borderColor: 'divider', bgcolor: selectedId === e.id ? 'action.hover' : 'transparent' }}>
      <ButtonBase onClick={() => { setSelectedId(e.id); setReplacingKey(null); }} sx={{ justifyContent: 'flex-start', textAlign: 'left', px: 2, py: 0.75, gap: 1, minWidth: 0 }}>
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Typography variant="body2" noWrap sx={{ fontWeight: 600 }}>{e.fullName}</Typography>
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
            {e.position}
          </Typography>
        </Box>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', flexShrink: 0 }}>
          <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: STATUS_COLOR[e.status] }} />
          <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
            {t(`schedule.status.${e.status}` as 'schedule.status.Free')}
            {e.status === 'OnSite' || e.status === 'Late' ? ` ${localClock(e.clockedInAt)}` : ''}
          </Typography>
        </Stack>
      </ButtonBase>
      {dayList.map((day) => (
        <Box
          key={day}
          sx={{
            borderLeft: compact ? 0 : 1, borderColor: 'divider', p: compact ? '2px 1px' : '5px 4px', display: 'flex', minHeight: compact ? 38 : 50,
            bgcolor: day === today ? 'action.selected' : 'transparent', opacity: isWeekend(day) ? 0.6 : 1,
          }}
        >
          {siteCells(e, day, cellFor(e, day))}
        </Box>
      ))}
    </Box>
  );

  const groupHeader = (key: string, title: string, meta: string, color?: string, warn?: string) => (
    <ButtonBase
      key={`h-${key}`}
      onClick={() => toggleGroup(key)}
      aria-expanded={!collapsed.has(key)}
      sx={{ width: '100%', justifyContent: 'flex-start', gap: 1.25, px: 2, py: 1, borderTop: 1, borderColor: 'divider', bgcolor: 'action.hover', textAlign: 'left' }}
    >
      <ExpandMoreOutlined fontSize="small" sx={{ transform: collapsed.has(key) ? 'rotate(-90deg)' : 'none', transition: 'transform .15s' }} />
      {color ? <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: color }} /> : null}
      <Typography sx={{ fontWeight: 600 }}>{title}</Typography>
      <Typography variant="caption" color="text.secondary">{meta}</Typography>
      {warn ? <Typography variant="caption" color="error" sx={{ fontWeight: 600 }}>{warn}</Typography> : null}
    </ButtonBase>
  );

  const periodLabel = `${shortDate(dayList[0])} – ${shortDate(dayList[dayList.length - 1])}${dayList[dayList.length - 1].slice(0, 4)}`;

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

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 2, alignItems: 'center' }}>
        <ToggleButtonGroup
          size="small"
          exclusive
          value={weeks}
          onChange={(_, value: 1 | 2 | 4 | null) => { if (value) { setWeeks(value); setOffset(0); } }}
          aria-label={t('schedule.range')}
        >
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

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, 1fr)', md: 'repeat(5, 1fr)' }, gap: 1, mb: 2 }}>
        {(['all', 'on', 'free', 'away', 'issues'] as Filter[]).map((key) => (
          <ButtonBase
            key={key}
            onClick={() => setFilter(key)}
            aria-pressed={filter === key}
            sx={{
              flexDirection: 'column', alignItems: 'flex-start', p: 1.5, borderRadius: 2, border: 1,
              borderColor: filter === key ? 'text.primary' : 'divider', bgcolor: 'background.paper',
            }}
          >
            <Typography variant="h5" sx={{ fontWeight: 700, color: key === 'issues' && counts.issues > 0 ? 'error.main' : 'text.primary' }}>
              {counts[key]}
            </Typography>
            <Typography variant="caption" color="text.secondary">{t(`schedule.filter.${key}` as 'schedule.filter.all')}</Typography>
          </ButtonBase>
        ))}
      </Box>

      {vacancies.length > 0 && (
        <Paper variant="outlined" sx={{ p: 1.5, mb: 2, borderColor: 'warning.main' }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{t('schedule.vacanciesTitle')}</Typography>
            <Typography variant="caption" sx={{ fontWeight: 700, color: 'warning.main' }}>{vacancies.length}</Typography>
          </Stack>
          {vacancies.map((v) => {
            const absent = data.employees.find((e) => e.id === v.employeeId);
            return (
              <Box key={v.key} sx={{ display: 'flex', gap: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', py: 0.75, borderTop: 1, borderColor: 'divider', bgcolor: replacingKey === v.key ? 'action.hover' : 'transparent' }}>
                <Box sx={{ minWidth: 240, flex: 1 }}>
                  <Typography variant="body2">
                    {t('schedule.vacancyLine', { name: absent?.fullName ?? '', from: shortDate(v.from), to: shortDate(v.to), days: v.days })}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {t('schedule.vacancyHint', { site: projectsById.get(v.projectId)?.name ?? '' })}
                  </Typography>
                </Box>
                <Button size="small" variant="contained" onClick={() => { setReplacingKey(v.key); setSelectedId(v.employeeId); setView('people'); }}>
                  {t('schedule.findReplacement')}
                </Button>
              </Box>
            );
          })}
        </Paper>
      )}

      {attention.length > 0 && (
        <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>{t('schedule.attention')}</Typography>
          <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0, display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' }, columnGap: 3 }}>
            {attention.map((a) => (
              <Box component="li" key={a.key} sx={{ display: 'flex', gap: 1, alignItems: 'baseline', py: 0.5, borderTop: 1, borderColor: 'divider' }}>
                <Typography variant="caption" sx={{ fontWeight: 700, textTransform: 'uppercase', color: a.level === 'crit' ? 'error.main' : a.level === 'warn' ? 'warning.main' : 'text.secondary', flexShrink: 0 }}>
                  {t(`schedule.level.${a.level}` as 'schedule.level.crit')}
                </Typography>
                <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }}>{a.text}</Typography>
                <Button size="small" onClick={() => { setSelectedId(a.employeeId); setReplacingKey(null); setFilter('all'); setSearch(''); }}>{t('schedule.open')}</Button>
              </Box>
            ))}
          </Box>
        </Paper>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1fr) 320px' }, gap: 2, alignItems: 'start' }}>
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
            <Box sx={{ minWidth: 250 + days * (weeks === 1 ? 92 : weeks === 2 ? 56 : 26) }}>
              <Box sx={{ display: 'grid', gridTemplateColumns: columns, position: 'sticky', top: 0, bgcolor: 'background.paper' }}>
                <Typography variant="caption" color="text.secondary" sx={{ px: 2, py: 1, alignSelf: 'end' }}>{t('schedule.worker')}</Typography>
                {dayList.map((day) => (
                  <Box key={day} sx={{ textAlign: 'center', py: 0.75, borderLeft: compact ? 0 : 1, borderColor: 'divider', bgcolor: day === today ? 'action.selected' : 'transparent', opacity: isWeekend(day) ? 0.6 : 1 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontSize: compact ? 9 : 12 }}>
                      {new Date(`${day}T00:00:00Z`).toLocaleDateString([], { weekday: compact ? 'narrow' : 'short', timeZone: 'UTC' })}
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600, fontSize: compact ? 10 : 14, color: day === today ? 'primary.main' : 'text.primary' }}>
                      {day.slice(8)}{weeks === 1 ? `.${day.slice(5, 7)}.` : ''}
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
          <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', px: 2, py: 1, borderTop: 1, borderColor: 'divider', color: 'text.secondary', fontSize: 12 }}>
            <span>{t('schedule.legend.site')}</span>
            <span>{t('schedule.legend.leave')}</span>
            <span>{t('schedule.legend.conflict')}</span>
            <span>{t('schedule.legend.split')}</span>
            <span>{t('schedule.legend.free')}</span>
          </Stack>
        </Paper>

        <Paper variant="outlined" sx={{ p: 2, position: { xl: 'sticky' }, top: 12 }}>
          {replacingVacancy ? (
            <ReplacementPanel
              schedule={data}
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
  vacancy,
  colorOf,
  pending,
  onCancel,
  onPick,
  onAssign,
}: {
  schedule: AssignmentSchedule;
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
  const candidates = candidatesFor(schedule, vacancy);
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
  const valid = !!employeeId && !!projectId && !!start && (!end || end >= start);

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
            {projects.map((p) => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
          </TextField>
          <TextField type="date" label={t('schedule.startDate')} value={start} onChange={(e) => setStart(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField
            type="date" label={t('schedule.endDate')} value={end} onChange={(e) => setEnd(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
            error={!!end && end < start} helperText={end && end < start ? t('schedule.endsBeforeStart') : t('schedule.endOptional')}
          />
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
