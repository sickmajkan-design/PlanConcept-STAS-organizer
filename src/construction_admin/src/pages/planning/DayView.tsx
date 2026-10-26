import { CheckCircleOutlined, HourglassEmpty } from '@mui/icons-material';
import { Avatar, Box, Button, ButtonBase, Chip, Paper, Stack, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import type { ReactNode } from 'react';

import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import {
  isActive,
  needOf,
  positionKey,
  roleCount,
  shortage,
  type PlanPerson,
} from '../../features/planning/planningLogic';
import { fillsGap, useDragInfo, useDragSource, useDropTarget, type DropTarget } from './planningDnd';
import { AWAY_COLOR, initials, siteColor } from './planningUi';
import type { ViewProps } from './planningTypes';
import { formatDate } from '../../utils/formatting';
import { ProjectLabel } from '../../components/ProjectLabel';

const OK = '#2f9e44';
const WARN = '#c28c0c';

function PersonButton({
  person,
  day,
  date,
  onOpen,
  awayLabel,
  showConfirmation,
  disabled,
}: {
  person: PlanPerson;
  day: number;
  date: string;
  /** Whether to show if the worker has confirmed; only meaningful for today and later. */
  showConfirmation?: boolean;
  disabled?: boolean;
  onOpen: () => void;
  awayLabel?: string;
}) {
  const t = useT();
  const cell = person.cells[day];
  const color = cell.away ? AWAY_COLOR : cell.project ? siteColor(cell.project) : undefined;

  // Someone who is away cannot be moved, and nothing is dropped on them either: the window says why.
  const locked = !!disabled || !!cell.away;
  const drag = useDragSource(person.id, date, date, locked);
  const drop = useDropTarget(`person:${person.id}`, { kind: 'person', personId: person.id }, locked);

  return (
    <ButtonBase
      ref={(node: HTMLElement | null) => {
        drag.setNodeRef(node);
        drop.setNodeRef(node);
      }}
      {...drag.listeners}
      onClick={onOpen}
      disabled={disabled}
      sx={{
        display: 'flex',
        gap: 1.25,
        p: 0.75,
        borderRadius: 1.5,
        width: '100%',
        justifyContent: 'flex-start',
        textAlign: 'left',
        cursor: locked ? undefined : 'grab',
        userSelect: 'none',
        WebkitTouchCallout: 'none',
        opacity: drag.isDragging ? 0.35 : 1,
        outline: drop.isOver ? '2px solid' : undefined,
        outlineColor: drop.isOver ? 'primary.main' : undefined,
        bgcolor: drop.isOver ? (theme) => alpha(theme.palette.primary.main, 0.1) : undefined,
        '&:hover': { bgcolor: 'action.hover' },
      }}
    >
      <Avatar
        sx={{
          width: 30,
          height: 30,
          fontSize: 11,
          fontWeight: 600,
          bgcolor: color ? alpha(color, 0.16) : 'transparent',
          color: color ?? 'text.secondary',
          border: color ? 'none' : '1px dashed',
          borderColor: 'divider',
        }}
      >
        {initials(person.name)}
      </Avatar>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="body2" noWrap sx={{ fontWeight: 500 }}>
          {person.name}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap>
          {awayLabel ?? person.position}
        </Typography>
      </Box>
      {showConfirmation && cell.project && !cell.away && (
        <Tooltip title={cell.acknowledged ? t('planning.ack.confirmed') : t('planning.ack.waiting')}>
          {cell.acknowledged ? (
            <CheckCircleOutlined fontSize="small" color="success" sx={{ ml: 'auto', flex: 'none' }} />
          ) : (
            <HourglassEmpty fontSize="small" color="warning" sx={{ ml: 'auto', flex: 'none' }} />
          )}
        </Tooltip>
      )}
    </ButtonBase>
  );
}

/**
 * A place a person can be dropped. While someone is being carried it shows whether the place wants their skill
 * (green), is already covered (dimmed, but still allowed: the window asks), and which one the pointer is over.
 */
function DropCard({
  id,
  target,
  disabled,
  fits,
  children,
}: {
  id: string;
  target: DropTarget;
  disabled: boolean;
  /** `undefined`: no opinion (the free list); true: fills a gap; false: covered already. */
  fits?: boolean;
  children: ReactNode;
}) {
  const { person } = useDragInfo();
  const { setNodeRef, isOver } = useDropTarget(id, target, disabled);
  const carrying = !!person && !disabled;

  return (
    <Paper
      ref={setNodeRef}
      variant="outlined"
      sx={{
        borderRadius: 2.5,
        minWidth: 0,
        transition: 'box-shadow .12s, border-color .12s, opacity .12s, background-color .12s',
        borderColor: carrying && fits ? OK : isOver && fits === false ? WARN : undefined,
        borderWidth: carrying && (fits || isOver) ? 2 : 1,
        bgcolor: carrying && fits ? alpha(OK, 0.06) : undefined,
        opacity: carrying && fits === false && !isOver ? 0.7 : 1,
        boxShadow: isOver ? `0 0 0 4px ${alpha(fits === false ? WARN : OK, 0.25)}` : undefined,
      }}
    >
      {children}
    </Paper>
  );
}

/** Who is where on one day, a card per site, with the places still to fill shown as dashed slots. */
export function DayView({ plan, day, openWorker, openSite, readOnly }: ViewProps & { day: number }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { person: carried } = useDragInfo();
  const date = plan.date(day);
  const sites = plan.projects.filter((p) => (!p.startDate || date >= p.startDate) && (!p.endDate || date <= p.endDate));
  const soon = plan.projects.filter((p) => p.startDate && p.startDate > date && p.needs.length > 0);
  const work = plan.isWork[day];
  // A drop on a non-working day would change nothing, so there is nothing to aim at.
  const dropOff = !!readOnly || !work;

  const free = work ? plan.people.filter((p) => !p.cells[day].project && !p.cells[day].away) : [];
  const away = plan.people.filter((p) => p.cells[day].away);

  return (
    <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: 'minmax(0,1fr)', md: 'minmax(0,1fr) 280px' }, alignItems: 'start' }}>
      <Box data-tour="day-sites" sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))' }}>
        {!readOnly && work && (
          <Typography variant="caption" color="text.secondary" sx={{ gridColumn: '1 / -1' }}>
            {t('planning.dnd.hint')}
          </Typography>
        )}
        {sites.length === 0 && <Typography color="text.secondary">{t('planning.day.noSites')}</Typography>}

        {sites.map((site) => {
          const crew = plan.people.filter((p) => p.cells[day].project === site.id && !p.cells[day].away);
          const short = shortage(plan, site, day);
          const need = needOf(site);
          const extra = isActive(plan, site, day) ? crew.length - (need - short) : 0;
          const slots = site.needs.flatMap((n) =>
            Array.from({ length: Math.max(0, n.count - roleCount(plan, site.id, day, n.position)) }, () => n.position),
          );
          const here = !!carried && carried.cells[day].project === site.id;

          return (
            <DropCard
              key={site.id}
              id={`site:${site.id}`}
              target={{ kind: 'site', projectId: site.id }}
              disabled={dropOff || here}
              fits={carried ? fillsGap(plan, site.id, carried, date, date) : undefined}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', p: 1.5, borderBottom: 1, borderColor: 'divider' }}>
                <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: siteColor(site.id), flex: 'none' }} />
                <Typography variant="subtitle1" sx={{ fontWeight: 700, flex: 1, minWidth: 0, overflowWrap: 'anywhere', lineHeight: 1.2 }}>
                  <ProjectLabel name={site.name} projectId={site.id} customerName={site.customerName} customerCountryCode={site.customerCountryCode} siteCountryCode={site.countryCode} variant="stacked" />
                </Typography>
                {extra > 0 && <Chip size="small" label={t('planning.day.surplus', { count: extra })} />}
                {need > 0 && (
                  <Chip
                    size="small"
                    color={!work || short === 0 ? 'default' : crew.length === 0 ? 'error' : 'warning'}
                    label={`${need - short} / ${need}`}
                    sx={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}
                  />
                )}
              </Stack>
              <Stack sx={{ p: 0.75 }} spacing={0.25}>
                {crew.map((p) => (
                  <PersonButton key={p.id} person={p} day={day} date={date} disabled={readOnly} showConfirmation={date >= plan.today} onOpen={() => openWorker(p.id, date, date)} />
                ))}
                {work &&
                  slots.map((position, i) => {
                    const wanted = !!carried && !here && positionKey(position) === carried.key;

                    return (
                      <Button
                        key={`${position}-${i}`}
                        variant="outlined"
                        color="inherit"
                        onClick={() => openSite(site.id, date, date, position)}
                        disabled={readOnly}
                        sx={{
                          justifyContent: 'flex-start',
                          borderStyle: 'dashed',
                          color: wanted ? 'success.dark' : 'text.secondary',
                          borderColor: wanted ? OK : undefined,
                          borderWidth: wanted ? 2 : undefined,
                          bgcolor: wanted ? alpha(OK, 0.1) : undefined,
                          fontWeight: wanted ? 600 : undefined,
                          textTransform: 'none',
                        }}
                      >
                        ＋ {position}
                      </Button>
                    );
                  })}
              </Stack>
            </DropCard>
          );
        })}
      </Box>

      <Stack spacing={1.5} sx={{ position: { md: 'sticky' }, top: 12 }}>
        <DropCard id="free" target={{ kind: 'free' }} disabled={dropOff || !carried?.cells[day].project} fits={undefined}>
          <Box sx={{ p: 1 }}>
            <Typography variant="subtitle2" sx={{ px: 1, py: 0.5, display: 'flex', justifyContent: 'space-between' }}>
              {t('planning.day.free')} <span>{free.length}</span>
            </Typography>
            {free.length === 0 ? (
              <Typography variant="body2" color="text.secondary" sx={{ p: 1 }}>
                {t('planning.day.noFree')}
              </Typography>
            ) : (
              free.map((p) => <PersonButton key={p.id} person={p} day={day} date={date} disabled={readOnly} onOpen={() => openWorker(p.id, date, date)} />)
            )}
          </Box>
        </DropCard>

        <Paper variant="outlined" sx={{ borderRadius: 2.5, p: 1 }}>
          <Typography variant="subtitle2" sx={{ px: 1, py: 0.5, display: 'flex', justifyContent: 'space-between' }}>
            {t('planning.day.away')} <span>{away.length}</span>
          </Typography>
          {away.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ p: 1 }}>
              {t('planning.day.noAway')}
            </Typography>
          ) : (
            away.map((p) => (
              <PersonButton
                key={p.id}
                person={p}
                day={day}
                date={date}
                awayLabel={enumLabel('absenceType', p.cells[day].away!)}
                disabled={readOnly}
                onOpen={() => openWorker(p.id, date, date)}
              />
            ))
          )}
        </Paper>

        {soon.length > 0 && (
          <Paper variant="outlined" sx={{ borderRadius: 2.5, p: 1.5 }}>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
              {t('planning.day.soon')}
            </Typography>
            {soon.map((s) => (
              <Typography key={s.id} variant="body2" color="text.secondary">
                {t('planning.day.soonLine', { name: s.name, date: formatDate(s.startDate), count: needOf(s) })}
              </Typography>
            ))}
          </Paper>
        )}
      </Stack>
    </Box>
  );
}
