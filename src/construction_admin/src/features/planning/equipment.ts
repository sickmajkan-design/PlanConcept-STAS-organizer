import type { PlanningEquipment, PlanningEquipmentOn, PlanningProject } from '../../api/planning';
import type { Plan, PlanPerson } from './planningLogic';

/*
 * Who holds which vehicle and tool, for the planning screen. The plan carries the present state; another day's holders come from a request of their own
 * (the history for an earlier day, the planned changes for a later one).
 * Plain data and plain functions, tested without a browser.
 */

/** The kinds the board draws an icon for, in the order they are shown. */
export const equipmentGroups = ['vehicle', 'kit', 'battery', 'measuring', 'cable', 'other'] as const;
export type EquipmentGroup = (typeof equipmentGroups)[number];

const FROM_API: Record<string, EquipmentGroup> = {
  Vehicle: 'vehicle',
  Kit: 'kit',
  BatterySet: 'battery',
  MeasuringSet: 'measuring',
  CableSet: 'cable',
  Other: 'other',
};

/** Statuses of a vehicle or tool that is not in use: in service, out of service, or being repaired. */
const NOT_USABLE = new Set(['InService', 'OutOfService', 'UnderRepair']);

export interface EquipmentItem {
  id: string;
  kind: 'Vehicle' | 'Tool';
  group: EquipmentGroup;
  label: string;
  status: string;
  /** The item is out of use (service, repair), so planning with it is a mistake worth saying. */
  notUsable: boolean;
  employeeId: string | null;
  projectId: string | null;
}

export interface EquipmentIndex {
  items: EquipmentItem[];
  /** Everything a person holds, whatever site it is on. */
  byEmployee: Map<string, EquipmentItem[]>;
  /** On a site and held by nobody. */
  onSite: Map<string, EquipmentItem[]>;
  /** Held by nobody and on no site: in the store. */
  store: EquipmentItem[];
}

export function indexEquipment(list: PlanningEquipment[] | undefined): EquipmentIndex {
  const items: EquipmentItem[] = (list ?? []).map((e) => ({
    id: e.id,
    kind: e.kind === 'Vehicle' ? 'Vehicle' : 'Tool',
    group: FROM_API[e.group] ?? 'other',
    label: e.label,
    status: e.status,
    notUsable: NOT_USABLE.has(e.status),
    employeeId: e.employeeId ?? null,
    projectId: e.projectId ?? null,
  }));

  return groupItems(items);
}

/** Sorts items into "held by a person", "on a site" and "in the store". */
function groupItems(items: EquipmentItem[]): EquipmentIndex {
  const byEmployee = new Map<string, EquipmentItem[]>();
  const onSite = new Map<string, EquipmentItem[]>();
  const store: EquipmentItem[] = [];

  for (const item of items) {
    if (item.employeeId) {
      byEmployee.set(item.employeeId, [...(byEmployee.get(item.employeeId) ?? []), item]);
    } else if (item.projectId) {
      onSite.set(item.projectId, [...(onSite.get(item.projectId) ?? []), item]);
    } else {
      store.push(item);
    }
  }

  return { items, byEmployee, onSite, store };
}

/** One count per kind, in display order, leaving out the kinds with none. */
export function groupCounts(items: EquipmentItem[]): { group: EquipmentGroup; count: number }[] {
  return equipmentGroups
    .map((group) => ({ group, count: items.filter((i) => i.group === group).length }))
    .filter((g) => g.count > 0);
}

export const heldBy = (plan: Plan, personId: string): EquipmentItem[] => plan.equipment.byEmployee.get(personId) ?? [];

/** An item held by somebody posted to another site than the one it is tied to. */
export const isMismatch = (item: EquipmentItem, holderSite: string | null): boolean =>
  !!item.projectId && !!holderSite && item.projectId !== holderSite;

export interface SiteEquipment {
  /** The people on the site that day who hold something, with what they hold. */
  held: { person: PlanPerson; items: EquipmentItem[] }[];
  /** On the site itself, held by nobody. */
  free: EquipmentItem[];
  /** Everything on the site, held or not. */
  all: EquipmentItem[];
}

export function siteEquipment(plan: Plan, site: PlanningProject, crew: PlanPerson[]): SiteEquipment {
  const held = crew
    .map((person) => ({ person, items: heldBy(plan, person.id) }))
    .filter((h) => h.items.length > 0);
  const free = plan.equipment.onSite.get(site.id) ?? [];

  return { held, free, all: [...held.flatMap((h) => h.items), ...free] };
}

/** What a person on a given day is doing as far as their equipment goes. */
export function personFlags(plan: Plan, person: PlanPerson, day: number): { mismatch: boolean; idle: boolean } {
  const items = heldBy(plan, person.id);
  const cell = person.cells[day];
  const idle = items.length > 0 && (!!cell.away || !cell.project);
  const mismatch = !idle && items.some((i) => isMismatch(i, cell.project));

  return { mismatch, idle };
}

export type EquipmentAlert = 'free' | 'mismatch' | 'idle';

/** Everything a planner can click in the pill row to highlight: equipment trouble, or who is late / in the wrong place. */
export type BoardAlert = EquipmentAlert | 'late' | 'wrong' | 'unconfirmed' | 'short';

export interface EquipmentSummary {
  /** Items on a site and held by nobody. */
  free: number;
  /** Items held by somebody posted to another site. */
  mismatch: number;
  /** Items held by somebody who is free or away that day. */
  idle: number;
}

export function equipmentSummary(plan: Plan, day: number): EquipmentSummary {
  let free = 0;
  for (const site of plan.projects) free += plan.equipment.onSite.get(site.id)?.length ?? 0;

  let mismatch = 0;
  let idle = 0;

  for (const person of plan.people) {
    const items = heldBy(plan, person.id);
    if (items.length === 0) continue;

    const cell = person.cells[day];
    if (cell.away || !cell.project) idle += items.length;
    else mismatch += items.filter((i) => isMismatch(i, cell.project)).length;
  }

  return { free, mismatch, idle };
}

/* ---- the planner's decision about what a person holds ---- */

export type EquipmentChoiceKind = 'Stay' | 'HandOver' | 'ToStore';

export interface EquipmentChoiceState {
  choice: EquipmentChoiceKind;
  /** Who receives it, for a hand-over. */
  to: string | null;
}

/** Someone free that day with the same position as the person, else the first free one: who to suggest as the receiver. */
export function suggestReceiver(plan: Plan, person: PlanPerson, day: number): PlanPerson | null {
  const free = plan.people.filter((p) => p.id !== person.id && !p.cells[day]?.project && !p.cells[day]?.away);

  return free.find((p) => p.key === person.key) ?? free[0] ?? null;
}

/**
 * What to send for the choices made: only the items that change (a Stay is nothing to do), and whether a hand-over is
 * missing its receiver, which the dialog refuses to submit.
 */
export function choiceRequest(
  items: EquipmentItem[],
  choices: Record<string, EquipmentChoiceState>,
): { items: { kind: 'Vehicle' | 'Tool'; id: string; choice: EquipmentChoiceKind; toEmployeeId: string | null }[]; missingReceiver: boolean } {
  const chosen = items
    .map((item) => ({ item, state: choices[item.id] ?? { choice: 'Stay' as const, to: null } }))
    .filter(({ state }) => state.choice !== 'Stay');

  return {
    items: chosen.map(({ item, state }) => ({
      kind: item.kind,
      id: item.id,
      choice: state.choice,
      toEmployeeId: state.choice === 'HandOver' ? state.to : null,
    })),
    missingReceiver: chosen.some(({ state }) => state.choice === 'HandOver' && !state.to),
  };
}

/**
 * The same items with the holders of another day. An earlier day comes from the history, which does not reach back for
 * every item: an item it knows nothing about is left out rather than guessed. A later day keeps every item.
 */
export function equipmentOnDay(index: EquipmentIndex, on: PlanningEquipmentOn, isPast: boolean): EquipmentIndex {
  const holders = new Map(on.items.map((h) => [h.id, h]));
  const items = index.items
    .filter((item) => !isPast || holders.has(item.id))
    .map((item) => {
      const h = holders.get(item.id);

      return h ? { ...item, employeeId: h.employeeId, projectId: h.projectId } : item;
    });

  return groupItems(items);
}
