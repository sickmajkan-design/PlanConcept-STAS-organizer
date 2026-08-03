import {
  ChevronLeftOutlined,
  ChevronRightOutlined,
  EventBusyOutlined,
} from '@mui/icons-material';
import {
  Box,
  Button,
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

/** Wide enough for a name, narrow enough to leave the week the space. */
const NAME_COLUMN = '200px';

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
        sx={{ mb: 2, alignItems: { md: 'center' } }}
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

      <Legend />
    </Box>
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
    <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
      <Box sx={{ minWidth: 760 }}>
        <DayHeader days={days} today={today} />
        {rows.map((row) => (
          <EmployeeRow
            key={row.employeeId}
            row={row}
            weekStart={weekStart}
            days={days}
            today={today}
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
        borderBottom: 1,
        borderColor: 'divider',
        position: 'sticky',
        top: 0,
        bgcolor: 'background.paper',
        zIndex: 1,
      }}
    >
      <Box sx={{ p: 1 }} />
      {days.map((day) => (
        <Box
          key={day}
          sx={{
            p: 1,
            textAlign: 'center',
            bgcolor: isWeekend(day) ? 'action.hover' : undefined,
          }}
        >
          <Typography
            variant="caption"
            sx={{ fontWeight: day === today ? 700 : 500 }}
            color={day === today ? 'primary.main' : 'text.secondary'}
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
}: {
  row: ScheduleRow;
  weekStart: string;
  days: string[];
  today: string;
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
        '&:last-of-type': { borderBottom: 0 },
      }}
    >
      <Box sx={{ p: 1, minWidth: 0 }}>
        <Typography variant="body2" noWrap sx={{ fontWeight: 600 }}>
          {row.employeeName}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap component="div">
          {row.position}
        </Typography>
      </Box>

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
          minHeight: 44,
          alignContent: 'center',
          gap: 0.5,
          py: 0.75,
        }}
      >
        {days.map((day, index) => (
          <Box
            key={day}
            sx={{
              gridRow: '1 / -1',
              gridColumn: index + 1,
              bgcolor: isWeekend(day) ? 'action.hover' : undefined,
              borderLeft: day === today ? 2 : 0,
              borderColor: 'primary.main',
              // Behind the bars, and never intercepting a click meant for one.
              zIndex: 0,
              pointerEvents: 'none',
              m: -0.25,
            }}
          />
        ))}

        {isFree && (
          <Typography
            variant="caption"
            color="text.disabled"
            sx={{ gridColumn: '1 / -1', gridRow: 1, pl: 1, zIndex: 1 }}
          >
            {t('schedule.free')}
          </Typography>
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
                  borderRadius: 1,
                  px: 1,
                  py: 0.25,
                  minWidth: 0,
                  // The right edge is squared off when the posting runs past
                  // the end of the week, so an open-ended one does not read as
                  // ending on Sunday.
                  borderTopRightRadius: assignment.continuesAfter ? 0 : undefined,
                  borderBottomRightRadius: assignment.continuesAfter ? 0 : undefined,
                }}
              >
                <Typography variant="caption" noWrap component="div">
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
                  borderRadius: 1,
                  px: 1,
                  py: 0.25,
                  minWidth: 0,
                }}
              >
                <Typography variant="caption" noWrap component="div">
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

function Legend() {
  const t = useT();

  return (
    <Stack direction="row" spacing={2} sx={{ mt: 2, alignItems: 'center' }}>
      <Swatch color="primary.main" label={t('schedule.legendAssigned')} />
      <Swatch color="warning.light" label={t('schedule.legendAway')} />
    </Stack>
  );
}

function Swatch({ color, label }: { color: string; label: string }) {
  return (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      <Box sx={{ width: 14, height: 14, borderRadius: 0.5, bgcolor: color }} />
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
    </Stack>
  );
}
