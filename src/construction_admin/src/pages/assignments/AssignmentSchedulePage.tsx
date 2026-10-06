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
  const [assignFor, setAssignFor] = useState<{ employeeId: string; date: string } | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const today = iso(new Date());
  const from = addDays(mondayOf(today), offset * weeks * 7);
  const days = weeks * 7;
  const { data, isLoading, isError, error: loadError, refetch } = useAssignmentScheduleQuery(from, days);
  const remove = useRemoveOnBoard();

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
  const flagged = useMemo(() => new Set(attention.filter((a) => a.level !== 'info').map((a) => a.employeeId)), [attention]);

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
      issues: attention.filter((a) => a.level !== 'info').length,
    };
  }, [data, attention]);

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
          title={cell.conflict ? t('schedule.conflictLeave') : undefined}
          sx={{
            flex: 1, borderRadius: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', px: 0.5,
            color, fontSize: 12, fontWeight: 600,
            border: cell.conflict ? '2px solid' : 'none', borderColor: 'error.main',
            background: `repeating-linear-gradient(135deg, ${color}2e 0 5px, ${color}0f 5px 10px)`,
          }}
        >
          {compact ? '' : t(cell.kind === 'leave' ? 'schedule.leave' : 'schedule.sick')}
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
      <ButtonBase onClick={() => setSelectedId(e.id)} sx={{ justifyContent: 'flex-start', textAlign: 'left', px: 2, py: 0.75, gap: 1, minWidth: 0 }}>
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
                <Button size="small" onClick={() => { setSelectedId(a.employeeId); setFilter('all'); setSearch(''); }}>{t('schedule.open')}</Button>
              </Box>
            ))}
          </Box>
        </Paper>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1fr) 320px' }, gap: 2, alignItems: 'start' }}>
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
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
          <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', px: 2, py: 1, borderTop: 1, borderColor: 'divider', color: 'text.secondary', fontSize: 12 }}>
            <span>{t('schedule.legend.site')}</span>
            <span>{t('schedule.legend.leave')}</span>
            <span>{t('schedule.legend.conflict')}</span>
            <span>{t('schedule.legend.split')}</span>
            <span>{t('schedule.legend.free')}</span>
          </Stack>
        </Paper>

        <Paper variant="outlined" sx={{ p: 2, position: { xl: 'sticky' }, top: 12 }}>
          {selected ? (
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
        key={assignFor ? `${assignFor.employeeId}-${assignFor.date}` : 'closed'}
        target={assignFor}
        employee={(data.employees ?? []).find((e) => e.id === assignFor?.employeeId)}
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

    const conflictDay = Array.from({ length: data.days }, (_, i) => addDays(data.from, i)).find((day) => {
      const cell = cellFor(e, day);
      return cell.conflict && !isWeekend(day);
    });
    if (conflictDay) {
      items.push({ key: `conf-${e.id}`, level: 'crit', employeeId: e.id, text: t('schedule.alert.conflict', { name: e.fullName, date: shortDate(conflictDay) }) });
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

function AssignDialog({
  target,
  employee,
  projects,
  onClose,
  onDone,
  onError,
}: {
  target: { employeeId: string; date: string } | null;
  employee: ScheduleEmployee | undefined;
  projects: ScheduleProject[];
  onClose: () => void;
  onDone: (name: string) => void;
  onError: (message: string) => void;
}) {
  const t = useT();
  const assign = useAssignOnBoard();
  const [projectId, setProjectId] = useState('');
  const [start, setStart] = useState(target?.date ?? dateOnlyOffset(0));
  const [end, setEnd] = useState('');
  const valid = !!projectId && !!start && (!end || end >= start);

  return (
    <Dialog open={!!target && !!employee} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('schedule.assignTitle', { name: employee?.fullName ?? '' })}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
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
