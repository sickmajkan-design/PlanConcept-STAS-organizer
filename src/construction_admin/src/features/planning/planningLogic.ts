import type { PlanningData, PlanningProject } from '../../api/planning';

/*
 * The rules of the scheduling screen, kept apart from the screen. Everything here works on a window
 * of consecutive days indexed from 0, and on plain data, so it is tested without a browser and the
 * leave-approval dialog uses the very same rules as the planning screen.
 *
 * Working days are Monday to Friday. A site's need is counted on those days only, inside the
 * site's own start and end dates.
 */

const DAY_MS = 864e5;

export const addDays = (value: string, days: number): string =>
  new Date(Date.parse(`${value}T00:00:00Z`) + days * DAY_MS).toISOString().slice(0, 10);

export const diffDays = (from: string, to: string): number =>
  Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / DAY_MS);

/** Monday is 0. */
export const weekday = (value: string): number => (new Date(`${value}T00:00:00Z`).getUTCDay() + 6) % 7;

export const isWorkday = (value: string): boolean => weekday(value) < 5;

/**
 * Whether a site works on a date: Monday to Friday always, Saturday and Sunday where the site says so,
 * and never on a public holiday of the country the site keeps.
 */
export function siteWorksOn(project: PlanningProject, date: string, holidays: ReadonlySet<string>): boolean {
  const wd = weekday(date);
  const base = wd < 5 || (wd === 5 && project.worksSaturdays) || (wd === 6 && project.worksSundays);
  return base && !(project.countryCode && holidays.has(`${project.countryCode}|${date}`));
}

export const mondayOf = (value: string): string => addDays(value, -weekday(value));

/** What makes two position names the same position. */
export const positionKey = (position: string): string => position.trim().toLowerCase();

export interface PlanCell {
  /** The site they are posted to that day, even while away. */
  project: string | null;
  /** The kind of approved absence that day, if any. */
  away: string | null;
  /** Other sites the person is posted to the same day, beyond `project`. */
  others: string[];
  /** True when the worker has confirmed the posting that `project` comes from. */
  acknowledged: boolean;
}

export interface PlanPerson {
  id: string;
  name: string;
  position: string;
  key: string;
  cells: PlanCell[];
  /** Certificates by name key, with the last valid day (null: never runs out). */
  certificates: Map<string, string | null>;
  /** Where the company houses them, as the days they are housed on each of which place. */
  stays: { place: string; from: string; to: string | null }[];
}

export interface Plan {
  from: string;
  to: string;
  days: number;
  today: string;
  people: PlanPerson[];
  projects: PlanningProject[];
  projectById: Map<string, PlanningProject>;
  positions: string[];
  certificateNames: string[];
  /** Whether anybody works on each day of the window: some site does, by its own calendar. */
  isWork: boolean[];
  /** `${country}|${date}`, one per public holiday. */
  holidays: ReadonlySet<string>;
  /** The name of the holiday on a date, when there is one. */
  holidayNames: Map<string, string>;
  /** Whether the caller may move people. A foreman without that right gets a read-only screen. */
  canEdit: boolean;
  isScoped: boolean;
  scopeBranchName: string | null;
  date: (index: number) => string;
  indexOf: (date: string) => number;
  /** People present on a site on a day, by `${projectId}|${positionKey}`. */
  present: Map<string, number>[];
  /** People with no posting and no absence on a day, by position key. */
  freeByKey: Map<string, number>[];
  freeTotal: number[];
}

export function buildPlan(data: PlanningData): Plan {
  const days = diffDays(data.from, data.to) + 1;
  const date = (i: number) => addDays(data.from, i);
  const indexOf = (d: string) => diffDays(data.from, d);

  const people: PlanPerson[] = data.employees.map((e) => {
    const cells: PlanCell[] = Array.from({ length: days }, () => ({ project: null, away: null, others: [], acknowledged: false }));

    for (const posting of e.postings) {
      const first = Math.max(0, indexOf(posting.startDate));
      const last = Math.min(days - 1, posting.endDate ? indexOf(posting.endDate) : days - 1);

      for (let i = first; i <= last; i++) {
        if (cells[i].project === null) {
          cells[i].project = posting.projectId;
          cells[i].acknowledged = !!posting.acknowledgedAt;
        } else if (cells[i].project !== posting.projectId && !cells[i].others.includes(posting.projectId)) {
          cells[i].others.push(posting.projectId);
        }
      }
    }

    for (const absence of e.absences) {
      const first = Math.max(0, indexOf(absence.startDate));
      const last = Math.min(days - 1, indexOf(absence.endDate));

      for (let i = first; i <= last; i++) {
        if (cells[i].away === null) cells[i].away = absence.type;
      }
    }

    const certificates = new Map<string, string | null>((e.certificates ?? []).map((c) => [positionKey(c.name), c.validUntil]));

    const stays = (e.stays ?? []).map((s) => ({ place: s.place, from: s.startDate, to: s.endDate }));

    return { id: e.id, name: e.fullName, position: e.position, key: positionKey(e.position), cells, certificates, stays };
  });

  const holidays = new Set((data.holidays ?? []).map((h) => `${h.countryCode}|${h.date}`));
  const holidayNames = new Map<string, string>();
  for (const h of data.holidays ?? []) {
    holidayNames.set(h.date, holidayNames.has(h.date) ? `${holidayNames.get(h.date)}, ${h.name}` : h.name);
  }

  // A day counts as one people work on when some site works on it. With no site in view, the ordinary week.
  const isWork = Array.from({ length: days }, (_, i) =>
    data.projects.length === 0
      ? isWorkday(date(i))
      : data.projects.some((p) => siteWorksOn(p, date(i), holidays)),
  );

  const present: Map<string, number>[] = [];
  const freeByKey: Map<string, number>[] = [];
  const freeTotal: number[] = [];

  for (let i = 0; i < days; i++) {
    const here = new Map<string, number>();
    const free = new Map<string, number>();
    let freeCount = 0;
    const work = isWork[i];

    for (const p of people) {
      const cell = p.cells[i];
      if (cell.away) continue;

      if (cell.project) {
        const k = `${cell.project}|${p.key}`;
        here.set(k, (here.get(k) ?? 0) + 1);
      } else if (work) {
        free.set(p.key, (free.get(p.key) ?? 0) + 1);
        freeCount++;
      }
    }

    present.push(here);
    freeByKey.push(free);
    freeTotal.push(freeCount);
  }

  return {
    from: data.from,
    to: data.to,
    days,
    today: data.today,
    people,
    projects: data.projects,
    projectById: new Map(data.projects.map((p) => [p.id, p])),
    positions: data.positions,
    isWork,
    holidays,
    holidayNames,
    certificateNames: data.certificateNames ?? [],
    canEdit: data.canEdit ?? true,
    isScoped: data.isScoped ?? false,
    scopeBranchName: data.scopeBranchName ?? null,
    date,
    indexOf,
    present,
    freeByKey,
    freeTotal,
  };
}

export const needOf = (project: PlanningProject): number =>
  project.needs.reduce((sum, n) => sum + n.count, 0);

/** True when the site is expected to have people on that day. */
export function isActive(plan: Plan, project: PlanningProject, index: number): boolean {
  const d = plan.date(index);
  return (
    siteWorksOn(project, d, plan.holidays) &&
    (!project.startDate || d >= project.startDate) &&
    (!project.endDate || d <= project.endDate)
  );
}

export const roleCount = (plan: Plan, projectId: string, index: number, position: string): number =>
  plan.present[index].get(`${projectId}|${positionKey(position)}`) ?? 0;

/** How many people the site is short on a day. A surplus of one position never covers another. */
export function shortage(plan: Plan, project: PlanningProject, index: number, position?: string): number {
  if (!isActive(plan, project, index)) return 0;

  const only = position === undefined ? null : positionKey(position);

  return project.needs.reduce((sum, n) => {
    if (only !== null && positionKey(n.position) !== only) return sum;
    return sum + Math.max(0, n.count - roleCount(plan, project.id, index, n.position));
  }, 0);
}

/** People of the site's needs that are actually covered on a day. */
export const covered = (plan: Plan, project: PlanningProject, index: number): number =>
  needOf(project) - shortage(plan, project, index);

/** Total shortage over every site on a day, optionally for one position. */
export function missing(plan: Plan, index: number, position?: string): number {
  return plan.projects.reduce((sum, p) => sum + shortage(plan, p, index, position), 0);
}

export function freeCount(plan: Plan, index: number, position?: string): number {
  if (position === undefined) return plan.freeTotal[index];
  return plan.freeByKey[index].get(positionKey(position)) ?? 0;
}

export function workdayIndexes(plan: Plan, from: number, to: number): number[] {
  const out: number[] = [];
  for (let i = Math.max(0, from); i <= Math.min(plan.days - 1, to); i++) {
    if (plan.isWork[i]) out.push(i);
  }
  return out;
}

/** The stay that covers a date, if the company houses the person that day. */
export function stayOn(person: PlanPerson, date: string): { place: string } | undefined {
  return person.stays.find((s) => s.from <= date && (s.to === null || s.to >= date));
}

/** The certificates the site requires that the person does not hold, valid, on every one of the days. */
export function missingCertificates(plan: Plan, person: PlanPerson, projectId: string, days: number[]): string[] {
  const required = plan.projectById.get(projectId)?.requiredCertificates ?? [];

  return required.filter((name) => {
    const key = positionKey(name);
    if (!person.certificates.has(key)) return true;
    const until = person.certificates.get(key);
    return until !== null && days.some((i) => plan.date(i) > until!);
  });
}

export type CandidateKind = 'free' | 'partial' | 'surplus' | 'otherSkill';

export interface Candidate {
  person: PlanPerson;
  tier: 1 | 2 | 3 | 4;
  kind: CandidateKind;
  freeDays: number;
  totalDays: number;
  /** The site the person would be taken from, for a surplus candidate. */
  fromProjectId?: string;
  /** Certificates the site requires that this person does not hold on every one of the days. */
  lacks: string[];
}

/**
 * Who could stand in at a site on the given days, best first: the same position and free all the
 * time, then free part of the time, then the same position from a site that has more of it than it
 * needs, and last somebody of another position who is free throughout. Anybody away on any of the
 * days is left out.
 */
export function candidates(
  plan: Plan,
  personId: string,
  projectId: string,
  days: number[],
): Candidate[] {
  const subject = plan.people.find((p) => p.id === personId);
  if (!subject || days.length === 0) return [];

  const out: Candidate[] = [];

  for (const person of plan.people) {
    if (person.id === personId || days.some((i) => person.cells[i].away)) continue;

    const freeDays = days.filter((i) => !person.cells[i].project).length;
    const base = { person, freeDays, totalDays: days.length, lacks: missingCertificates(plan, person, projectId, days) };

    if (person.key === subject.key) {
      if (freeDays === days.length) {
        out.push({ ...base, tier: 1, kind: 'free' });
      } else if (freeDays > 0) {
        out.push({ ...base, tier: 2, kind: 'partial' });
      } else {
        const onSurplus = days.every((i) => {
          const at = person.cells[i].project;
          if (!at || at === projectId) return false;
          const site = plan.projectById.get(at);
          const needed = site?.needs.find((n) => positionKey(n.position) === person.key)?.count ?? 0;
          return roleCount(plan, at, i, person.position) > needed;
        });

        if (onSurplus) {
          out.push({ ...base, tier: 3, kind: 'surplus', fromProjectId: person.cells[days[0]].project! });
        }
      }
    } else if (freeDays === days.length) {
      out.push({ ...base, tier: 4, kind: 'otherSkill' });
    }
  }

  // Within a kind, somebody who holds what the site requires comes before somebody who does not.
  return out.sort((a, b) => a.tier - b.tier || a.lacks.length - b.lacks.length || b.freeDays - a.freeDays || a.person.name.localeCompare(b.person.name));
}

/** A stretch of approved absence of somebody who was posted to a site that is now short of their position. */
export interface AwayItem {
  person: PlanPerson;
  projectId: string;
  type: string;
  days: number[];
  from: string;
  to: string;
}

export function awayItems(plan: Plan, from: number, to: number): AwayItem[] {
  const items: AwayItem[] = [];

  for (const person of plan.people) {
    let current: AwayItem | null = null;

    for (const i of workdayIndexes(plan, from, to)) {
      const cell = person.cells[i];
      const site = cell.away ? cell.project : null;

      if (site && current && current.projectId === site) {
        current.days.push(i);
      } else {
        if (current) items.push(current);
        current = site ? { person, projectId: site, type: cell.away!, days: [i], from: '', to: '' } : null;
      }
    }

    if (current) items.push(current);
  }

  return items
    .map((it) => ({ ...it, from: plan.date(it.days[0]), to: plan.date(it.days[it.days.length - 1]) }))
    .filter((it) => {
      const site = plan.projectById.get(it.projectId);
      return !!site && it.days.some((i) => shortage(plan, site, i, it.person.position) > 0);
    })
    .sort((a, b) => a.from.localeCompare(b.from) || a.person.name.localeCompare(b.person.name));
}

export interface Segment {
  projectId: string | null;
  from: string;
  to: string;
}

/** What a person is posted to across a range, as runs of equal days. What an undo puts back. */
export function segmentsOf(plan: Plan, personId: string, from: string, to: string): Segment[] {
  const person = plan.people.find((p) => p.id === personId);
  if (!person) return [];

  const first = Math.max(0, plan.indexOf(from));
  const last = Math.min(plan.days - 1, plan.indexOf(to));
  const out: Segment[] = [];

  for (let i = first; i <= last; i++) {
    const project = person.cells[i].project;
    const tail = out[out.length - 1];

    if (tail && tail.projectId === project) {
      tail.to = plan.date(i);
    } else {
      out.push({ projectId: project, from: plan.date(i), to: plan.date(i) });
    }
  }

  return out;
}

export type RangeMode = 'W' | '2W' | 'M' | 'Q' | 'C';

export interface Column {
  from: string;
  to: string;
}

/** The first and last day of the calendar month or quarter holding `value`. */
export function periodBounds(mode: 'M' | 'Q', value: string): [string, string] {
  const year = Number(value.slice(0, 4));
  const month = Number(value.slice(5, 7)) - 1;
  const start = mode === 'Q' ? month - (month % 3) : month;
  const span = mode === 'Q' ? 3 : 1;
  const first = new Date(Date.UTC(year, start, 1)).toISOString().slice(0, 10);
  const last = new Date(Date.UTC(year, start + span, 0)).toISOString().slice(0, 10);
  return [first, last];
}

/** One column per day up to a month, one per week beyond. */
export function columnsFor(from: string, to: string): Column[] {
  const length = diffDays(from, to) + 1;
  const out: Column[] = [];

  if (length <= 31) {
    for (let i = 0; i < length; i++) out.push({ from: addDays(from, i), to: addDays(from, i) });
    return out;
  }

  for (let d = from; d <= to; d = addDays(d, 7)) {
    const end = addDays(d, 6);
    out.push({ from: d, to: end > to ? to : end });
  }

  return out;
}

/** The days of a column that people work on, as indexes into the window. */
export function workIndexesIn(plan: Plan, column: Column): number[] {
  const out: number[] = [];

  for (let d = column.from; d <= column.to; d = addDays(d, 1)) {
    const i = plan.indexOf(d);
    if (i >= 0 && i < plan.days && plan.isWork[i]) out.push(i);
  }

  return out;
}

export function workdaysIn(column: Column): string[] {
  const out: string[] = [];
  for (let d = column.from; d <= column.to; d = addDays(d, 1)) {
    if (isWorkday(d)) out.push(d);
  }
  return out;
}

export type ConflictKind = 'double' | 'afterEnd' | 'beforeStart' | 'certificate' | 'housedIdle';

export interface Conflict {
  person: PlanPerson;
  kind: ConflictKind;
  projectId: string;
  /** The second site, for a double booking. */
  otherProjectId?: string;
  /** The required certificate the person does not hold, for a certificate conflict. */
  certificate?: string;
  /** Where they are housed, for a person housed with no work. */
  place?: string;
  from: string;
  to: string;
}

/**
 * Things in the plan that look like mistakes: somebody posted to two sites on the same working day, or
 * to a site on days outside its own start and end dates. Two sites at once is allowed (a supervisor
 * covering both is real), so these are warnings to look at, never refusals.
 */
export function conflicts(plan: Plan, from: number, to: number): Conflict[] {
  const out: Conflict[] = [];

  for (const person of plan.people) {
    let open: Conflict | null = null;
    const close = () => {
      if (open) out.push(open);
      open = null;
    };

    const days = workdayIndexes(plan, from, to);

    const note = (i: number, kind: ConflictKind, projectId: string, otherProjectId?: string, certificate?: string, place?: string) => {
      const date = plan.date(i);
      if (open && open.kind === kind && open.projectId === projectId && open.otherProjectId === otherProjectId && open.certificate === certificate && open.place === place && diffDays(open.to, date) <= (weekday(open.to) === 4 ? 3 : 1)) {
        open.to = date;
      } else {
        close();
        open = { person, kind, projectId, otherProjectId, certificate, place, from: date, to: date };
      }
    };

    for (const kind of ['double', 'outside', 'certificate', 'idle'] as const) {
      close();

      for (const i of days) {
        const cell = person.cells[i];

        // Housed at the company's cost on a working day, with nothing to do and not on leave.
        if (kind === 'idle') {
          const stay = !cell.away && !cell.project ? stayOn(person, plan.date(i)) : undefined;
          if (stay) note(i, 'housedIdle', '', undefined, undefined, stay.place);
          continue;
        }

        if (cell.away || !cell.project) continue;

        if (kind === 'double') {
          for (const other of cell.others) note(i, 'double', cell.project, other);
        } else if (kind === 'outside') {
          const site = plan.projectById.get(cell.project);
          const date = plan.date(i);
          if (site?.endDate && date > site.endDate) note(i, 'afterEnd', cell.project);
          else if (site?.startDate && date < site.startDate) note(i, 'beforeStart', cell.project);
        } else {
          for (const name of missingCertificates(plan, person, cell.project, [i])) {
            note(i, 'certificate', cell.project, undefined, name);
          }
        }
      }
    }

    close();
  }

  return out.sort((a, b) => a.from.localeCompare(b.from) || a.person.name.localeCompare(b.person.name));
}

/** How many people posted to a site on a working day have not yet confirmed it. Leave days are not counted. */
export function unconfirmedCount(plan: Plan, index: number): number {
  if (!plan.isWork[index]) return 0;
  return plan.people.filter((p) => p.cells[index].project && !p.cells[index].away && !p.cells[index].acknowledged).length;
}

export type Confirmation = 'none' | 'all' | 'some' | 'nothing';

/** Whether the worker has confirmed what they are posted to across the days: all of it, part, or none; or nothing to confirm. */
export function confirmationOf(person: PlanPerson, indexes: number[]): Confirmation {
  const posted = indexes.filter((i) => person.cells[i].project && !person.cells[i].away);
  if (posted.length === 0) return 'nothing';
  const confirmed = posted.filter((i) => person.cells[i].acknowledged).length;
  return confirmed === posted.length ? 'all' : confirmed === 0 ? 'none' : 'some';
}
