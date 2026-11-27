import type { PlanningProject } from '../../api/planning';
import { isLate, isWrongPlace, type PersonAttendance } from './attendance';
import { equipmentSummary, type EquipmentAlert } from './equipment';
import {
  addDays,
  awayItems,
  conflicts,
  freeCount,
  isActive,
  missing,
  needOf,
  positionKey,
  roleCount,
  shortage,
  unconfirmedCount,
  workdayIndexes,
  type AwayItem,
  type Conflict,
  type Plan,
  type PlanPerson,
} from './planningLogic';

/*
 * What the Planner tab shows, worked out from the plan the Schedule already loads. Nothing here asks the server for anything
 * new, so the numbers always agree with the other tabs.
 */

export interface DayCounts {
  assigned: number;
  free: number;
  away: number;
  short: number;
  unconfirmed: number;
}

export function dayCounts(plan: Plan, day: number): DayCounts {
  const away = plan.people.filter((p) => p.cells[day].away).length;
  const free = freeCount(plan, day);

  return { assigned: plan.people.length - free - away, free, away, short: missing(plan, day), unconfirmed: unconfirmedCount(plan, day) };
}

export interface SiteLine {
  project: PlanningProject;
  /** People the site needs on the day, zero when it does not work that day. */
  need: number;
  short: number;
  people: PlanPerson[];
  /** Posted to the site but on approved leave. */
  away: PlanPerson[];
  gaps: { position: string; count: number }[];
}

/** The sites that have people or open places on a day, the ones with most missing first. */
export function siteLines(plan: Plan, day: number): SiteLine[] {
  if (day < 0 || !plan.isWork[day]) return [];

  return plan.projects
    .map((project): SiteLine => {
      const active = isActive(plan, project, day);
      const at = plan.people.filter((p) => p.cells[day].project === project.id);

      return {
        project,
        need: active ? needOf(project) : 0,
        short: shortage(plan, project, day),
        people: at.filter((p) => !p.cells[day].away),
        away: at.filter((p) => p.cells[day].away),
        gaps: active
          ? project.needs
              .map((n) => ({ position: n.position, count: Math.max(0, n.count - roleCount(plan, project.id, day, n.position)) }))
              .filter((g) => g.count > 0)
          : [],
      };
    })
    .filter((line) => line.people.length > 0 || line.away.length > 0 || line.gaps.length > 0)
    .sort((a, b) => b.short - a.short || a.project.name.localeCompare(b.project.name));
}

/* ---- what needs a decision ---- */

export type Decision =
  | { kind: 'short'; key: string; project: PlanningProject; position: string; count: number; awayNames: string[] }
  | { kind: 'late'; key: string; person: PlanPerson; project: PlanningProject | undefined; minutes: number }
  | { kind: 'wrong'; key: string; person: PlanPerson; project: PlanningProject | undefined; meters: number | undefined }
  | { kind: 'conflict'; key: string; conflict: Conflict }
  | { kind: 'equipment'; key: string; alert: EquipmentAlert; count: number }
  | { kind: 'unconfirmed'; key: string; count: number }
  | { kind: 'soon'; key: string; item: AwayItem };

/** How far ahead somebody's leave is listed as "soon". */
export const SOON_DAYS = 7;

/** Everything on the day that wants a decision, most urgent first; leave that starts within a week comes last as `soon`. */
export function decisionsFor(plan: Plan, day: number, attendance: Map<string, PersonAttendance> | undefined): Decision[] {
  if (day < 0 || !plan.isWork[day]) return [];

  const out: Decision[] = [];
  const date = plan.date(day);
  const notPast = date >= plan.today;

  for (const project of plan.projects) {
    if (!isActive(plan, project, day)) continue;

    for (const need of project.needs) {
      const count = need.count - roleCount(plan, project.id, day, need.position);
      if (count <= 0) continue;

      const awayNames = plan.people
        .filter((p) => p.cells[day].project === project.id && p.cells[day].away && positionKey(p.position) === positionKey(need.position))
        .map((p) => p.name);

      out.push({ kind: 'short', key: `short|${project.id}|${positionKey(need.position)}`, project, position: need.position, count, awayNames });
    }
  }

  if (attendance) {
    for (const person of plan.people) {
      const found = attendance.get(person.id);
      if (!found) continue;

      const project = plan.projectById.get(person.cells[day].project ?? '');

      if (isLate(found)) out.push({ kind: 'late', key: `late|${person.id}`, person, project, minutes: found.clock.minutesLate ?? 0 });
      else if (isWrongPlace(found)) out.push({ kind: 'wrong', key: `wrong|${person.id}`, person, project, meters: found.pin?.meters });
    }
  }

  for (const conflict of conflicts(plan, day, day)) {
    out.push({ kind: 'conflict', key: `conflict|${conflict.person.id}|${conflict.kind}|${conflict.projectId}|${conflict.otherProjectId ?? ''}|${conflict.certificate ?? ''}`, conflict });
  }

  if (notPast) {
    const eq = equipmentSummary(plan, day);

    for (const alert of ['free', 'mismatch', 'idle'] as const) {
      if (eq[alert] > 0) out.push({ kind: 'equipment', key: `equipment|${alert}`, alert, count: eq[alert] });
    }

    const unconfirmed = unconfirmedCount(plan, day);
    if (unconfirmed > 0) out.push({ kind: 'unconfirmed', key: 'unconfirmed', count: unconfirmed });

    for (const item of awayItems(plan, day + 1, day + SOON_DAYS)) {
      if (item.days[0] > day) out.push({ kind: 'soon', key: `soon|${item.person.id}|${item.projectId}|${item.from}`, item });
    }
  }

  return out;
}

/* ---- the week ---- */

export interface WeekCell {
  need: number;
  people: number;
  short: number;
}

export interface WeekRow {
  project: PlanningProject;
  /** Monday to Friday; null where the site does not work or the plan does not reach. */
  days: (WeekCell | null)[];
}

export interface WeekMatrix {
  rows: WeekRow[];
  free: (number | null)[];
  away: (number | null)[];
}

export function weekMatrix(plan: Plan, monday: string): WeekMatrix {
  const idx = [0, 1, 2, 3, 4].map((n) => plan.indexOf(addDays(monday, n)));

  const rows = plan.projects
    .map((project): WeekRow => ({
      project,
      days: idx.map((i) => {
        if (i < 0 || !plan.isWork[i]) return null;

        const people = plan.people.filter((p) => p.cells[i].project === project.id && !p.cells[i].away).length;
        const active = isActive(plan, project, i);

        return active || people > 0 ? { need: active ? needOf(project) : 0, people, short: shortage(plan, project, i) } : null;
      }),
    }))
    .filter((row) => row.days.some((d) => d && (d.need > 0 || d.people > 0)))
    .sort((a, b) => a.project.name.localeCompare(b.project.name));

  return {
    rows,
    free: idx.map((i) => (i < 0 || !plan.isWork[i] ? null : freeCount(plan, i))),
    away: idx.map((i) => (i < 0 || !plan.isWork[i] ? null : plan.people.filter((p) => p.cells[i].away).length)),
  };
}

/* ---- capacity ---- */

/** How many weeks ahead the capacity table looks. */
export const CAPACITY_WEEKS = 8;

export interface CapacityWeek {
  monday: string;
  /** People needed on an average working day of the week. */
  need: number;
  /** People of the position who are not on leave, on an average working day of the week. */
  available: number;
  /** False when the plan does not reach the week or nobody works in it. */
  known: boolean;
}

export interface CapacityRow {
  key: string;
  position: string;
  weeks: CapacityWeek[];
}

const round1 = (n: number): number => Math.round(n * 10) / 10;

/** Needs against people for each position over `weeks` weeks from `monday`, the tightest position first. */
export function capacity(plan: Plan, monday: string, weeks: number): CapacityRow[] {
  const positions = new Map<string, string>();
  for (const p of plan.people) if (!positions.has(p.key)) positions.set(p.key, p.position);
  for (const project of plan.projects) for (const n of project.needs) if (!positions.has(positionKey(n.position))) positions.set(positionKey(n.position), n.position);

  const rows: CapacityRow[] = [...positions.entries()].map(([key, position]) => ({ key, position, weeks: [] }));

  for (let w = 0; w < weeks; w++) {
    const start = addDays(monday, w * 7);
    const days = workdayIndexes(plan, plan.indexOf(start), plan.indexOf(addDays(start, 4)));
    const known = plan.indexOf(start) >= 0 && days.length > 0;

    for (const row of rows) {
      let need = 0;
      let available = 0;

      for (const i of days) {
        for (const project of plan.projects) {
          if (!isActive(plan, project, i)) continue;
          for (const n of project.needs) if (positionKey(n.position) === row.key) need += n.count;
        }
        available += plan.people.filter((p) => p.key === row.key && !p.cells[i].away).length;
      }

      const n = known ? days.length : 1;
      row.weeks.push({ monday: start, need: round1(need / n), available: round1(available / n), known });
    }
  }

  return rows
    .filter((row) => row.weeks.some((w) => w.need > 0 || w.available > 0))
    .sort((a, b) => peak(b) - peak(a) || a.position.localeCompare(b.position));
}

/** How full a week is: needs over people. A week with needs and nobody to meet them is infinitely full. */
export function load(week: CapacityWeek): number {
  if (!week.known) return 0;
  if (week.available <= 0) return week.need > 0 ? Infinity : 0;

  return week.need / week.available;
}

const peak = (row: CapacityRow): number => Math.max(0, ...row.weeks.map(load));

/** How many more people of the position the week would need. */
export const shortfall = (week: CapacityWeek): number => (week.known ? Math.max(0, Math.ceil(week.need - week.available - 1e-9)) : 0);

/**
 * "What if we take a new project": the capacity with `count` more people of a position needed from the week containing `from`,
 * for `weeks` weeks. Nothing is saved; the plan itself is untouched.
 */
export function withExtraNeed(rows: CapacityRow[], key: string, count: number, from: string, weeks: number): CapacityRow[] {
  return rows.map((row) => {
    if (row.key !== key) return row;

    const first = row.weeks.findIndex((w) => addDays(w.monday, 6) >= from);
    if (first < 0) return row;

    return { ...row, weeks: row.weeks.map((w, i) => (i >= first && i < first + weeks ? { ...w, need: round1(w.need + count) } : w)) };
  });
}
