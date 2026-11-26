import { CheckCircleOutlined, HourglassEmpty } from '@mui/icons-material';
import { Avatar, Box, Button, ButtonBase, Chip, Paper, Stack, TextField, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useState, type ReactNode } from 'react';

import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { heldBy, personFlags, siteEquipment, type EquipmentAlert, type EquipmentItem } from '../../features/planning/equipment';
import { EquipmentIcon } from '../../components/EquipmentIcon';
import {
  EquipmentCluster,
  SiteEquipmentCard,
  EQ_WARN,
  SiteEquipmentChips,
  matchesAlert,
  useHoverCard,
  type BindCard,
} from './equipmentUi';
import {
  isActive,
  needOf,
  positionKey,
  roleCount,
  shortage,
  type Plan,
  type PlanPerson,
} from '../../features/planning/planningLogic';
import { fillsGap, useDragInfo, useDragSource, useDropTarget, type DropTarget } from './planningDnd';
import { ContractorChip } from '../../components/ContractorChip';
import { contractorColor, contractorNameColor } from '../../theme';
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
  equipment,
  plan,
  bind,
  mark,
}: {
  person: PlanPerson;
  day: number;
  date: string;
  /** What the person holds, with the card to show on hover. */
  equipment?: { items: EquipmentItem[]; mismatch: boolean; idle: boolean };
  plan: Plan;
  bind?: BindCard;
  /** Highlighted: it is what the equipment alert the planner clicked is about. */
  mark?: boolean;
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
        transition: 'background-color .15s, box-shadow .15s',
        outline: drop.isOver ? '2px solid' : undefined,
        outlineColor: drop.isOver ? 'primary.main' : undefined,
        bgcolor: drop.isOver ? (theme) => alpha(theme.palette.primary.main, 0.1) : mark ? alpha(EQ_WARN, 0.12) : undefined,
        boxShadow: mark && !drop.isOver ? `inset 0 0 0 2px ${EQ_WARN}` : undefined,
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
          ...(person.isContractor && { outline: `2px solid ${contractorColor.main}`, outlineOffset: 1 }),
          border: color ? 'none' : '1px dashed',
          borderColor: 'divider',
        }}
      >
        {initials(person.name)}
      </Avatar>
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', minWidth: 0 }}>
          <Typography
            variant="body2"
            noWrap
            sx={{ fontWeight: 500, color: person.isContractor ? contractorNameColor : undefined }}
          >
            {person.name}
          </Typography>
          {person.isContractor && <ContractorChip sx={{ height: 18, fontSize: 11 }} />}
        </Stack>
        <Typography variant="caption" color="text.secondary" noWrap>
          {awayLabel ?? person.position}
        </Typography>
      </Box>
      {equipment && bind && (
        <EquipmentCluster plan={plan} person={person} day={day} items={equipment.items} mismatch={equipment.mismatch} idle={equipment.idle} bind={bind} />
      )}
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
  mark,
  children,
}: {
  id: string;
  target: DropTarget;
  disabled: boolean;
  /** Highlighted: it is what the equipment alert the planner clicked is about. */
  mark?: boolean;
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
        borderColor: carrying && fits ? OK : isOver && fits === false ? WARN : mark ? EQ_WARN : undefined,
        borderWidth: carrying && (fits || isOver) || mark ? 2 : 1,
        bgcolor: carrying && fits ? alpha(OK, 0.06) : undefined,
        opacity: carrying && fits === false && !isOver ? 0.7 : 1,
        boxShadow: isOver ? `0 0 0 4px ${alpha(fits === false ? WARN : OK, 0.25)}` : mark ? `0 0 0 3px ${alpha(EQ_WARN, 0.3)}` : undefined,
      }}
    >
      {children}
    </Paper>
  );
}

/** Who is where on one day, a card per site, with the places still to fill shown as dashed slots. */
export function DayView({
  plan,
  day,
  openWorker,
  openSite,
  readOnly,
  eqAlert,
  onDecideEquipment,
}: ViewProps & { day: number; eqAlert?: EquipmentAlert | null; onDecideEquipment?: (personId: string) => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { person: carried } = useDragInfo();
  const hover = useHoverCard(!!carried);
  const date = plan.date(day);
  const sites = plan.projects.filter((p) => (!p.startDate || date >= p.startDate) && (!p.endDate || date <= p.endDate));
  const soon = plan.projects.filter((p) => p.startDate && p.startDate > date && p.needs.length > 0);
  const work = plan.isWork[day];
  // A drop on a non-working day would change nothing, so there is nothing to aim at.
  const dropOff = !!readOnly || !work;

  const [freeQuery, setFreeQuery] = useState('');
  const free = work ? plan.people.filter((p) => !p.cells[day].project && !p.cells[day].away) : [];
  const q = freeQuery.trim().toLocaleLowerCase();
  const shownFree = free.filter((p) => !q || `${p.name} ${p.position}`.toLocaleLowerCase().includes(q));
  const away = plan.people.filter((p) => p.cells[day].away);

  /** What a person holds and what is worth a look about it, or nothing when they hold nothing. */
  const equipmentOf = (p: PlanPerson) => {
    const items = heldBy(plan, p.id);
    if (items.length === 0) return undefined;

    const flags = personFlags(plan, p, day);

    return { items, flags, mismatch: flags.mismatch, idle: flags.idle };
  };
  /** Under a free or absent person who still holds equipment: the way to decide about it. */
  const decideRow = (p: PlanPerson) =>
    !readOnly && onDecideEquipment && equipmentOf(p)?.idle ? (
      <Box sx={{ pl: 5.5, pb: 0.75 }}>
        <Button size="small" color="warning" variant="outlined" onClick={() => onDecideEquipment(p.id)} sx={{ textTransform: 'none', borderRadius: 999 }}>
          {t('planning.eq.decide')}
        </Button>
      </Box>
    ) : null;
  // A site-level alert (free equipment) leaves the people as they are; the person-level ones mark who they are about.
  const markPerson = (p: PlanPerson) => !!eqAlert && eqAlert !== 'free' && matchesAlert(eqAlert, equipmentOf(p)?.flags ?? {});

  return (
    <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: 'minmax(0,1fr)', md: 'minmax(0,1fr) 280px' }, alignItems: 'start' }}>
      <Box data-tour="day-sites" sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))' }}>
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
          const eq = siteEquipment(plan, site, crew);
          const siteHit = { free: eq.free.length > 0, mismatch: crew.some((p) => personFlags(plan, p, day).mismatch) };

          return (
            <DropCard
              key={site.id}
              id={`site:${site.id}`}
              target={{ kind: 'site', projectId: site.id }}
              disabled={dropOff || here}
              fits={carried ? fillsGap(plan, site.id, carried, date, date) : undefined}
              mark={!!eqAlert && matchesAlert(eqAlert, siteHit)}
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
              {eq.all.length > 0 && (
                <Box sx={{ pt: 1 }}>
                  <SiteEquipmentChips
                    data={eq}
                    label={t('planning.eq.chipsLabel', { site: site.name })}
                    hover={hover.bind(() => <SiteEquipmentCard plan={plan} siteName={site.name} day={day} data={eq} />)}
                  />
                </Box>
              )}
              <Stack sx={{ p: 0.75 }} spacing={0.25}>
                {crew.map((p) => (
                  <PersonButton key={p.id} person={p} day={day} date={date} disabled={readOnly} showConfirmation={date >= plan.today} onOpen={() => openWorker(p.id, date, date)} equipment={equipmentOf(p)} plan={plan} bind={hover.bind} mark={markPerson(p)} />
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
            {free.length > 1 && (
              <Stack spacing={0.75} sx={{ px: 0.5, pb: 1 }}>
                <TextField
                  size="small"
                  type="search"
                  value={freeQuery}
                  onChange={(e) => setFreeQuery(e.target.value)}
                  placeholder={t('planning.free.search')}
                  slotProps={{ htmlInput: { 'aria-label': t('planning.free.search') } }}
                />
              </Stack>
            )}
            {free.length === 0 || shownFree.length === 0 ? (
              <Typography variant="body2" color="text.secondary" sx={{ p: 1 }}>
                {t(free.length === 0 ? 'planning.day.noFree' : 'planning.free.noMatch')}
              </Typography>
            ) : (
              // The panel is sticky on desktop, so the list scrolls inside it and never hands the scroll over to the page.
              <Box sx={{ maxHeight: { md: 'calc(100vh - 260px)' }, overflowY: { md: 'auto' }, overscrollBehavior: 'contain' }}>
                {shownFree.map((p) => (
                  <Box key={p.id}>
                    <PersonButton person={p} day={day} date={date} disabled={readOnly} onOpen={() => openWorker(p.id, date, date)} equipment={equipmentOf(p)} plan={plan} bind={hover.bind} mark={markPerson(p)} />
                    {decideRow(p)}
                  </Box>
                ))}
              </Box>
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
              <Box key={p.id}>
              <PersonButton
                person={p}
                day={day}
                date={date}
                awayLabel={enumLabel('absenceType', p.cells[day].away!)}
                disabled={readOnly}
                onOpen={() => openWorker(p.id, date, date)}
                equipment={equipmentOf(p)}
                plan={plan}
                bind={hover.bind}
                mark={markPerson(p)}
              />
              {decideRow(p)}
              </Box>
            ))
          )}
        </Paper>

        {plan.equipment.store.length > 0 && (
          <Paper variant="outlined" sx={{ borderRadius: 2.5, p: 1.5 }}>
            <Typography variant="subtitle2" sx={{ display: 'flex', justifyContent: 'space-between' }}>
              {t('planning.eq.storeTitle')} <span>{plan.equipment.store.length}</span>
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
              {t('planning.eq.storeHint')}
            </Typography>
            <Box sx={{ maxHeight: 220, overflowY: 'auto', overscrollBehavior: 'contain' }}>
              {plan.equipment.store.map((item) => (
                <Stack key={item.id} direction="row" spacing={1} sx={{ alignItems: 'center', py: 0.5 }}>
                  <Box component="span" sx={{ display: 'inline-flex', color: 'text.secondary', flex: 'none' }}><EquipmentIcon group={item.group} size={20} /></Box>
                  <Typography variant="body2" sx={{ minWidth: 0, overflowWrap: 'anywhere' }}>
                    {item.label}
                  </Typography>
                </Stack>
              ))}
            </Box>
          </Paper>
        )}

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
      {hover.element}
    </Box>
  );
}
