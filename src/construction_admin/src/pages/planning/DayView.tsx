import { CheckCircleOutlined, HourglassEmpty } from '@mui/icons-material';
import { Avatar, Box, Button, ButtonBase, Chip, Paper, Stack, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';

import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import {
  isActive,
  isWorkday,
  needOf,
  roleCount,
  shortage,
  type PlanPerson,
} from '../../features/planning/planningLogic';
import { AWAY_COLOR, initials, siteColor } from './planningUi';
import type { ViewProps } from './planningTypes';
import { formatDate } from '../../utils/formatting';

function PersonButton({
  person,
  day,
  onOpen,
  awayLabel,
  showConfirmation,
  disabled,
}: {
  person: PlanPerson;
  day: number;
  /** Whether to show if the worker has confirmed; only meaningful for today and later. */
  showConfirmation?: boolean;
  disabled?: boolean;
  onOpen: () => void;
  awayLabel?: string;
}) {
  const t = useT();
  const cell = person.cells[day];
  const color = cell.away ? AWAY_COLOR : cell.project ? siteColor(cell.project) : undefined;

  return (
    <ButtonBase
      onClick={onOpen}
      disabled={disabled}
      sx={{ display: 'flex', gap: 1.25, p: 0.75, borderRadius: 1.5, width: '100%', justifyContent: 'flex-start', textAlign: 'left', '&:hover': { bgcolor: 'action.hover' } }}
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

/** Who is where on one day, a card per site, with the places still to fill shown as dashed slots. */
export function DayView({ plan, day, openWorker, openSite, readOnly }: ViewProps & { day: number }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const date = plan.date(day);
  const sites = plan.projects.filter((p) => (!p.startDate || date >= p.startDate) && (!p.endDate || date <= p.endDate));
  const soon = plan.projects.filter((p) => p.startDate && p.startDate > date && p.needs.length > 0);
  const work = isWorkday(date);

  const free = work ? plan.people.filter((p) => !p.cells[day].project && !p.cells[day].away) : [];
  const away = plan.people.filter((p) => p.cells[day].away);

  return (
    <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: 'minmax(0,1fr)', md: 'minmax(0,1fr) 280px' }, alignItems: 'start' }}>
      <Box data-tour="day-sites" sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))' }}>
        {sites.length === 0 && <Typography color="text.secondary">{t('planning.day.noSites')}</Typography>}

        {sites.map((site) => {
          const crew = plan.people.filter((p) => p.cells[day].project === site.id && !p.cells[day].away);
          const short = shortage(plan, site, day);
          const need = needOf(site);
          const extra = isActive(plan, site, day) ? crew.length - (need - short) : 0;
          const slots = site.needs.flatMap((n) =>
            Array.from({ length: Math.max(0, n.count - roleCount(plan, site.id, day, n.position)) }, () => n.position),
          );

          return (
            <Paper key={site.id} variant="outlined" sx={{ borderRadius: 2.5, minWidth: 0 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', p: 1.5, borderBottom: 1, borderColor: 'divider' }}>
                <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: siteColor(site.id), flex: 'none' }} />
                <Typography variant="subtitle1" sx={{ fontWeight: 700, flex: 1, minWidth: 0, overflowWrap: 'anywhere', lineHeight: 1.2 }}>
                  {site.name}
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
                  <PersonButton key={p.id} person={p} day={day} disabled={readOnly} showConfirmation={date >= plan.today} onOpen={() => openWorker(p.id, date, date)} />
                ))}
                {work &&
                  slots.map((position, i) => (
                    <Button
                      key={`${position}-${i}`}
                      variant="outlined"
                      color="inherit"
                      onClick={() => openSite(site.id, date, date, position)}
                      disabled={readOnly}
                      sx={{ justifyContent: 'flex-start', borderStyle: 'dashed', color: 'text.secondary', textTransform: 'none' }}
                    >
                      ＋ {position}
                    </Button>
                  ))}
              </Stack>
            </Paper>
          );
        })}
      </Box>

      <Stack spacing={1.5} sx={{ position: { md: 'sticky' }, top: 12 }}>
        <Paper variant="outlined" sx={{ borderRadius: 2.5, p: 1 }}>
          <Typography variant="subtitle2" sx={{ px: 1, py: 0.5, display: 'flex', justifyContent: 'space-between' }}>
            {t('planning.day.free')} <span>{free.length}</span>
          </Typography>
          {free.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ p: 1 }}>
              {t('planning.day.noFree')}
            </Typography>
          ) : (
            free.map((p) => <PersonButton key={p.id} person={p} day={day} disabled={readOnly} onOpen={() => openWorker(p.id, date, date)} />)
          )}
        </Paper>

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
