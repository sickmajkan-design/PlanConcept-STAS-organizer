import {
  DndContext,
  DragOverlay,
  MouseSensor,
  TouchSensor,
  pointerWithin,
  rectIntersection,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type CollisionDetection,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import { Avatar, Box, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { createContext, useContext, useState, type ReactNode } from 'react';

import { shortage, workdayIndexes, type Plan, type PlanPerson } from '../../features/planning/planningLogic';
import { useT } from '../../i18n/useI18n';
import { initials, siteColor } from './planningUi';

/** What is being dragged: one person over the stretch of days the grab stands for (a day, or a bar in the timeline). */
export interface DragPayload {
  personId: string;
  from: string;
  to: string;
}

export type DropTarget = { kind: 'site'; projectId: string } | { kind: 'free' } | { kind: 'person'; personId: string };

interface DragState {
  person: PlanPerson | null;
  payload: DragPayload | null;
}

const DragContext = createContext<DragState>({ person: null, payload: null });

/** The person being dragged right now, if any, so sites and slots can show whether they fit. */
export function useDragInfo(): DragState {
  return useContext(DragContext);
}

/**
 * Whether a person's skill is missing at a site on any working day of a stretch. The same shortage rule the
 * dialogs use, so "fits" on the drop target and "fills an empty place" in the dialog never disagree.
 */
export function fillsGap(plan: Plan, projectId: string, person: PlanPerson, from: string, to: string): boolean {
  const site = plan.projectById.get(projectId);
  const start = plan.indexOf(from);
  const end = plan.indexOf(to);

  if (!site || start < 0 || end < 0) return false;

  return workdayIndexes(plan, start, end).some((i) => shortage(plan, site, i, person.position) > 0);
}

const collisions: CollisionDetection = (args) => {
  const within = pointerWithin(args);
  return within.length > 0 ? within : rectIntersection(args);
};

/**
 * Drag and drop for the schedule. It only ever asks "this person, these days, onto this": the page opens the
 * same kind of window as a click does, with the site and the dates already filled in, so nothing is changed by
 * the drop alone and the rules stay where they are.
 */
export function PlanningDndProvider({
  plan,
  enabled,
  onDrop,
  children,
}: {
  plan: Plan;
  enabled: boolean;
  onDrop: (payload: DragPayload, target: DropTarget) => void;
  children: ReactNode;
}) {
  const [active, setActive] = useState<DragPayload | null>(null);

  // A short press-and-hold on a phone, so scrolling the page still works and a plain tap still opens the window.
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 8 } }),
  );

  const person = active ? (plan.people.find((p) => p.id === active.personId) ?? null) : null;

  const handleStart = (event: DragStartEvent) => {
    setActive((event.active.data.current as DragPayload | undefined) ?? null);
  };

  const handleEnd = (event: DragEndEvent) => {
    const payload = event.active.data.current as DragPayload | undefined;
    const target = event.over?.data.current?.target as DropTarget | undefined;
    setActive(null);

    if (!payload || !target) return;
    if (target.kind === 'person' && target.personId === payload.personId) return;

    onDrop(payload, target);
  };

  return (
    <DragContext.Provider value={{ person: enabled ? person : null, payload: enabled ? active : null }}>
      <DndContext sensors={sensors} collisionDetection={collisions} onDragStart={handleStart} onDragEnd={handleEnd} onDragCancel={() => setActive(null)}>
        {children}
        <DragOverlay dropAnimation={null}>{person && <DragChip person={person} payload={active} />}</DragOverlay>
      </DndContext>
    </DragContext.Provider>
  );
}

function DragChip({ person, payload }: { person: PlanPerson; payload: DragPayload | null }) {
  const color = payload ? '#2f9e44' : undefined;

  return (
    <Box
      sx={{
        display: 'flex',
        gap: 1.25,
        alignItems: 'center',
        bgcolor: 'background.paper',
        border: 1,
        borderColor: color ?? 'divider',
        borderRadius: 2,
        boxShadow: 6,
        px: 1.25,
        py: 0.75,
        transform: 'rotate(-2deg)',
        cursor: 'grabbing',
        maxWidth: 280,
      }}
    >
      <Avatar sx={{ width: 30, height: 30, fontSize: 11, fontWeight: 600, bgcolor: alpha('#2f9e44', 0.16), color: '#2f9e44' }}>{initials(person.name)}</Avatar>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="body2" noWrap sx={{ fontWeight: 600 }}>
          {person.name}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap component="div">
          {person.position}
        </Typography>
      </Box>
    </Box>
  );
}

/** Makes an element something a person can be lifted by. `disabled` leaves it a plain element. */
export function useDragSource(personId: string, from: string, to: string, disabled: boolean) {
  const payload: DragPayload = { personId, from, to };
  const { setNodeRef, listeners, isDragging } = useDraggable({ id: `drag:${personId}:${from}:${to}`, data: payload, disabled });

  return { setNodeRef, listeners: disabled ? undefined : listeners, isDragging };
}

/** Makes an element something a person can be dropped on. */
export function useDropTarget(id: string, target: DropTarget, disabled: boolean) {
  const { setNodeRef, isOver } = useDroppable({ id, data: { target }, disabled });

  return { setNodeRef, isOver: isOver && !disabled };
}

/**
 * The sites as places to drop a bar in the timeline, where the rows are people and there is no site to aim at.
 * Always on screen, so the table does not jump when a drag starts; it doubles as the colour legend.
 */
export function SiteTray({ plan }: { plan: Plan }) {
  const t = useT();
  const { person, payload } = useDragInfo();

  return (
    <Box data-tour="site-tray" sx={{ display: 'flex', alignItems: 'center', gap: 1, overflowX: 'auto', py: 0.5 }}>
      <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'nowrap', flex: 'none' }}>
        {t('planning.dnd.tray')}
      </Typography>
      {plan.projects.map((site) => (
        <TrayChip
          key={site.id}
          id={`site:${site.id}`}
          target={{ kind: 'site', projectId: site.id }}
          label={site.name}
          color={siteColor(site.id)}
          fits={!!person && !!payload && fillsGap(plan, site.id, person, payload.from, payload.to)}
          dragging={!!person}
        />
      ))}
      <TrayChip id="free" target={{ kind: 'free' }} label={t('planning.l.free')} dashed fits={false} dragging={!!person} />
    </Box>
  );
}

function TrayChip({
  id,
  target,
  label,
  color,
  dashed,
  fits,
  dragging,
}: {
  id: string;
  target: DropTarget;
  label: string;
  color?: string;
  dashed?: boolean;
  fits: boolean;
  dragging: boolean;
}) {
  const { setNodeRef, isOver } = useDropTarget(id, target, false);

  return (
    <Box
      ref={setNodeRef}
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 0.75,
        flex: 'none',
        px: 1.25,
        py: 0.5,
        borderRadius: 4,
        fontSize: 13,
        whiteSpace: 'nowrap',
        border: dashed ? '1px dashed' : '1px solid',
        borderColor: isOver ? 'success.main' : fits ? 'success.main' : 'divider',
        bgcolor: isOver ? alpha('#2f9e44', 0.18) : fits ? alpha('#2f9e44', 0.08) : 'transparent',
        boxShadow: isOver ? `0 0 0 3px ${alpha('#2f9e44', 0.25)}` : undefined,
        fontWeight: isOver || fits ? 600 : 400,
        opacity: dragging && !fits && !isOver ? 0.75 : 1,
        transition: 'background-color .12s, box-shadow .12s, border-color .12s',
      }}
    >
      {color && <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: color, flex: 'none' }} />}
      {label}
    </Box>
  );
}
