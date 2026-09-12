import {
  ChevronLeftOutlined,
  ChevronRightOutlined,
  EventBusyOutlined,
} from '@mui/icons-material';
import {
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControlLabel,
  MenuItem,
  Paper,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { ScheduleQuery } from '../../api/absences';
import type { ScheduleRow } from '../../api/types';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useScheduleQuery } from '../../features/absences/useAbsences';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import {
  addDays,
  barPlacement,
  fromIsoDate,
  isWeekend,
  startOfWeek,
  todayIsoDate,
  weekDays,
} from './weekWindow';

/** Wide enough for a name and role, narrow enough to leave the week the space. */
const NAME_COLUMN = '220px';

/** First letter of up to the first two words — "QATEST Marko" -> "QM". */
function initialsOfName(name: string): string {
  const letters = name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word[0]);
  return letters.join('').toUpperCase() || '?';
}

export function SchedulePage() {
  const t = useT();
  const navigate = useNavigate();
  const [weekStart, setWeekStart] = useState(() => startOfWeek(todayIsoDate()));
  const [projectId, setProjectId] = useState('');
  const [assignedOnly, setAssignedOnly] = useState(false);

  const projects = useAllProjectsQuery();

  const query: ScheduleQuery = useMemo(
    () => ({
      from: weekStart,
      to: addDays(weekStart, 6),
      projectId: projectId || undefined,
      assignedOnly: assignedOnly || undefined,
    }),
    [assignedOnly, projectId, weekStart],
  );

  const { data, isLoading, isError, error, refetch } = useScheduleQuery(query);

  const days = useMemo(() => weekDays(weekStart), [weekStart]);

  // At-a-glance counts for the week being viewed — this is the actual
  // question the page exists to answer, so it gets said in three numbers
  // before anyone has to read a single bar.
  const counts = useMemo(() => {
    const rows = data?.rows ?? [];
    let onSite = 0;
    let away = 0;
    let free = 0;

    for (const row of rows) {
      if (row.assignments.length > 0) onSite += 1;
      else if (row.absences.length > 0) away += 1;
      else free += 1;
    }

    return { onSite, away, free };
  }, [data]);

  return (
    <Box>
      <PageHeader
        title={t('schedule.title')}
        subtitle={t('schedule.subtitle')}
        action={{
          label: t('schedule.openAbsences'),
          icon: <EventBusyOutlined />,
          onClick: () => navigate(paths.absences),
        }}
      />

      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { md: 'center' }, justifyContent: 'space-between' }}
      >
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ alignItems: { sm: 'center' } }}
        >
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Tooltip title={t('schedule.previousWeek')}>
              <Button
                size="small"
                onClick={() => setWeekStart(addDays(weekStart, -7))}
                aria-label={t('schedule.previousWeek')}
              >
                <ChevronLeftOutlined />
              </Button>
            </Tooltip>
            <Button
              size="small"
              onClick={() => setWeekStart(startOfWeek(todayIsoDate()))}
            >
              {t('schedule.thisWeek')}
            </Button>
            <Tooltip title={t('schedule.nextWeek')}>
              <Button
                size="small"
                onClick={() => setWeekStart(addDays(weekStart, 7))}
                aria-label={t('schedule.nextWeek')}
              >
                <ChevronRightOutlined />
              </Button>
            </Tooltip>
          </Stack>

          <TextField
            select
            size="small"
            label={t('schedule.project')}
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
            sx={{ minWidth: 200 }}
          >
            <MenuItem value="">{t('schedule.allProjects')}</MenuItem>
            {projects.data?.items.map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {project.name}
              </MenuItem>
            ))}
          </TextField>

          <FormControlLabel
            control={
              <Switch
                checked={assignedOnly}
                onChange={(event) => setAssignedOnly(event.target.checked)}
              />
            }
            label={t('schedule.assignedOnly')}
          />
        </Stack>

        {data && data.rows.length > 0 && (
          <Stack direction="row" spacing={1}>
            <CountChip color="success" label={t('schedule.legendFree')} count={counts.free} />
            <CountChip color="primary" label={t('schedule.legendAssigned')} count={counts.onSite} />
            <CountChip color="warning" label={t('schedule.legendAway')} count={counts.away} />
          </Stack>
        )}
      </Stack>

      {isError ? (
        <ErrorState error={error} onRetry={() => void refetch()} />
      ) : isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : !data || data.rows.length === 0 ? (
        <EmptyState message={t('schedule.empty')} />
      ) : (
        <Board rows={data.rows} weekStart={weekStart} days={days} />
      )}
    </Box>
  );
}

/** One of the three summary counts above the board — the same colors the bars use, so the two read as one system. */
function CountChip({
  color,
  label,
  count,
}: {
  color: 'success' | 'primary' | 'warning';
  label: string;
  count: number;
}) {
  return (
    <Chip
      size="small"
      variant={count > 0 ? 'filled' : 'outlined'}
      color={count > 0 ? color : undefined}
      label={`${label}: ${count}`}
      sx={{ fontWeight: 600 }}
    />
  );
}

function Board({
  rows,
  weekStart,
  days,
}: {
  rows: ScheduleRow[];
  weekStart: string;
  days: string[];
}) {
  const today = todayIsoDate();

  return (
    // The board scrolls inside itself rather than widening the page: seven
    // day columns plus a name will not fit a phone, and a horizontally
    // scrolling page makes every other screen unusable too.
    <Paper variant="outlined" sx={{ overflowX: 'auto', borderRadius: 2 }}>
      <Box sx={{ minWidth: 760 }}>
        <DayHeader days={days} today={today} />
        {rows.map((row, index) => (
          <EmployeeRow
            key={row.employeeId}
            row={row}
            weekStart={weekStart}
            days={days}
            today={today}
            striped={index % 2 === 1}
          />
        ))}
      </Box>
    </Paper>
  );
}

function DayHeader({ days, today }: { days: string[]; today: string }) {
  const { locale } = useI18n();

  const format = useMemo(
    () =>
      new Intl.DateTimeFormat(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
        weekday: 'short',
        day: 'numeric',
        month: 'numeric',
        timeZone: 'UTC',
      }),
    [locale],
  );

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: `${NAME_COLUMN} repeat(7, 1fr)`,
        borderBottom: 2,
        borderColor: 'divider',
        position: 'sticky',
        top: 0,
        bgcolor: 'background.paper',
        zIndex: 2,
      }}
    >
      <Box sx={{ p: 1 }} />
      {days.map((day) => (
        <Box
          key={day}
          sx={{
            p: 1,
            textAlign: 'center',
            bgcolor: day === today ? 'primary.main' : isWeekend(day) ? 'action.hover' : undefined,
            borderRadius: day === today ? 1 : 0,
            mx: day === today ? 0.5 : 0,
            my: day === today ? 0.5 : 0,
          }}
        >
          <Typography
            variant="caption"
            sx={{ fontWeight: 700 }}
            color={day === today ? 'primary.contrastText' : 'text.secondary'}
          >
            {format.format(fromIsoDate(day))}
          </Typography>
        </Box>
      ))}
    </Box>
  );
}

function EmployeeRow({
  row,
  weekStart,
  days,
  today,
  striped,
}: {
  row: ScheduleRow;
  weekStart: string;
  days: string[];
  today: string;
  striped: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const isFree = row.assignments.length === 0 && row.absences.length === 0;

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: `${NAME_COLUMN} repeat(7, 1fr)`,
        borderBottom: 1,
        borderColor: 'divider',
        bgcolor: striped ? 'action.hover' : undefined,
        '&:last-of-type': { borderBottom: 0 },
        '&:hover': { bgcolor: 'action.selected' },
      }}
    >
      <Stack direction="row" spacing={1.25} sx={{ p: 1, minWidth: 0, alignItems: 'center' }}>
        <Avatar sx={{ width: 32, height: 32, fontSize: 13, bgcolor: 'grey.400' }}>
          {initialsOfName(row.employeeName)}
        </Avatar>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="body2" noWrap sx={{ fontWeight: 600, lineHeight: 1.3 }}>
            {row.employeeName}
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap component="div">
            {row.position}
          </Typography>
        </Box>
      </Stack>

      {/* The seven day cells are drawn first as a background grid; the bars
          are then laid over them in the same grid area, so a bar spanning
          three days is one element rather than three cells pretending to be. */}
      <Box
        sx={{
          gridColumn: '2 / -1',
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          gridTemplateRows: 'auto',
          position: 'relative',
          minHeight: 52,
          alignContent: 'center',
          gap: 0.5,
          py: 1,
        }}
      >
        {days.map((day, index) => (
          <Box
            key={day}
            sx={{
              gridRow: '1 / -1',
              gridColumn: index + 1,
              bgcolor: day === today
                ? (theme) => alpha(theme.palette.primary.main, 0.08)
                : isWeekend(day) ? 'action.hover' : undefined,
              // Behind the bars, and never intercepting a click meant for one.
              zIndex: 0,
              pointerEvents: 'none',
              m: -0.25,
            }}
          />
        ))}

        {isFree && (
          <Chip
            size="small"
            variant="outlined"
            color="success"
            label={t('schedule.free')}
            sx={{ gridColumn: '1 / 4', gridRow: 1, zIndex: 1, justifySelf: 'start', ml: 0.5 }}
          />
        )}

        {row.assignments.map((assignment, index) => {
          const { column, span } = barPlacement(
            weekStart,
            assignment.from,
            assignment.to,
          );

          return (
            <Tooltip
              key={assignment.id}
              title={
                assignment.continuesAfter
                  ? `${assignment.projectName} — ${t('schedule.continues')}`
                  : assignment.projectName
              }
            >
              <Box
                sx={{
                  gridRow: index + 1,
                  gridColumn: `${column} / span ${span}`,
                  zIndex: 1,
                  bgcolor: 'primary.main',
                  color: 'primary.contrastText',
                  borderRadius: 1.5,
                  boxShadow: 1,
                  px: 1,
                  py: 0.5,
                  minWidth: 0,
                  // The right edge is squared off when the posting runs past
                  // the end of the week, so an open-ended one does not read as
                  // ending on Sunday.
                  borderTopRightRadius: assignment.continuesAfter ? 0 : undefined,
                  borderBottomRightRadius: assignment.continuesAfter ? 0 : undefined,
                }}
              >
                <Typography variant="caption" noWrap component="div" sx={{ fontWeight: 600 }}>
                  {assignment.projectName}
                </Typography>
              </Box>
            </Tooltip>
          );
        })}

        {row.absences.map((absence, index) => {
          const { column, span } = barPlacement(weekStart, absence.from, absence.to);

          return (
            <Tooltip key={absence.id} title={enumLabel('absenceType', absence.type)}>
              <Box
                sx={{
                  gridRow: row.assignments.length + index + 1,
                  gridColumn: `${column} / span ${span}`,
                  zIndex: 1,
                  bgcolor: 'warning.light',
                  color: 'warning.contrastText',
                  borderRadius: 1.5,
                  boxShadow: 1,
                  px: 1,
                  py: 0.5,
                  minWidth: 0,
                }}
              >
                <Typography variant="caption" noWrap component="div" sx={{ fontWeight: 600 }}>
                  {enumLabel('absenceType', absence.type)}
                </Typography>
              </Box>
            </Tooltip>
          );
        })}
      </Box>
    </Box>
  );
}
