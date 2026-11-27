import { describe, expect, it } from 'vitest';

import type { PlanningData, PlanningEmployee, PlanningProject } from '../../api/planning';
import { attendanceFor } from './attendance';
import { capacity, dayCounts, decisionsFor, load, shortfall, siteLines, weekMatrix, withExtraNeed } from './planner';
import { buildPlan } from './planningLogic';

// Monday 2026-10-05 .. Sunday 2026-10-25, three weeks, today is Wednesday the 7th.
const FROM = '2026-10-05';
const TO = '2026-10-25';

const site = (id: string, needs: Record<string, number>, extra: Partial<PlanningProject> = {}): PlanningProject => ({
  id,
  name: id,
  status: 'Active',
  customerName: null,
  startDate: null,
  endDate: null,
  worksSaturdays: false,
  worksSundays: false,
  needs: Object.entries(needs).map(([position, count]) => ({ position, count })),
  ...extra,
});

const person = (id: string, position: string, postings: [string, string, string | null][] = [], absences: [string, string][] = []): PlanningEmployee => ({
  id,
  fullName: id,
  position,
  postings: postings.map(([projectId, startDate, endDate]) => ({ projectId, startDate, endDate })),
  absences: absences.map(([startDate, endDate]) => ({ type: 'AnnualLeave', startDate, endDate })),
});

const build = (employees: PlanningEmployee[], projects: PlanningProject[]) =>
  buildPlan({ from: FROM, to: TO, today: '2026-10-07', projects, employees, positions: [] } satisfies PlanningData);

describe('dayCounts and siteLines', () => {
  const plan = build(
    [person('a', 'Zidar', [['s1', FROM, null]]), person('b', 'Zidar', [['s1', FROM, null]], [['2026-10-07', '2026-10-07']]), person('c', 'Zidar')],
    [site('s1', { Zidar: 3 })],
  );

  it('counts assigned, free, away and the places still open', () => {
    expect(dayCounts(plan, 2)).toEqual({ assigned: 1, free: 1, away: 1, short: 2, unconfirmed: 1 });
  });

  it('lists the site with who is there, who is on leave and what is missing', () => {
    const [line] = siteLines(plan, 2);

    expect(line.people.map((p) => p.id)).toEqual(['a']);
    expect(line.away.map((p) => p.id)).toEqual(['b']);
    expect(line.gaps).toEqual([{ position: 'Zidar', count: 2 }]);
  });

  it('shows nothing on a weekend', () => {
    expect(siteLines(plan, 5)).toEqual([]);
  });
});

describe('decisionsFor', () => {
  it('names who is on leave behind a missing place, and counts what is unconfirmed', () => {
    const plan = build([person('a', 'Zidar', [['s1', FROM, null]], [['2026-10-07', '2026-10-07']])], [site('s1', { Zidar: 1 })]);
    const found = decisionsFor(plan, 2, undefined);

    expect(found.find((d) => d.kind === 'short')).toMatchObject({ position: 'Zidar', count: 1, awayNames: ['a'] });
    expect(found.some((d) => d.kind === 'unconfirmed')).toBe(false);
  });

  it('adds who is late and who is on the wrong site when the attendance is known', () => {
    const plan = build([person('a', 'Zidar', [['s1', FROM, null]])], [site('s1', { Zidar: 1 }, { shiftStartTime: '07:00:00', latitude: 44.1, longitude: 17.9 })]);
    const attendance = new Map([['a', attendanceFor(undefined, plan.projects[0], '08:00')]]);
    const found = decisionsFor(plan, 2, attendance);

    expect(found.find((d) => d.kind === 'late')).toMatchObject({ minutes: 60 });
  });

  it('lists leave that starts within a week as "soon", and never on a past day', () => {
    const plan = build([person('a', 'Zidar', [['s1', FROM, null]], [['2026-10-09', '2026-10-09']])], [site('s1', { Zidar: 1 })]);

    expect(decisionsFor(plan, 2, undefined).some((d) => d.kind === 'soon')).toBe(true);
    expect(decisionsFor(plan, 0, undefined).some((d) => d.kind === 'soon')).toBe(false);
  });
});

describe('weekMatrix', () => {
  it('gives people and shortage per site and day, and leaves out sites nobody works at', () => {
    const plan = build([person('a', 'Zidar', [['s1', FROM, null]])], [site('s1', { Zidar: 2 }), site('s2', {})]);
    const matrix = weekMatrix(plan, FROM);

    expect(matrix.rows.map((r) => r.project.id)).toEqual(['s1']);
    expect(matrix.rows[0].days[0]).toEqual({ need: 2, people: 1, short: 1 });
    expect(matrix.free).toEqual([0, 0, 0, 0, 0]);
  });
});

describe('capacity', () => {
  const plan = build(
    [person('a', 'Zidar', [['s1', FROM, null]]), person('b', 'Zidar', [['s1', FROM, null]], [['2026-10-12', '2026-10-16']])],
    [site('s1', { Zidar: 3 }), site('s2', { Električar: 1 }, { startDate: '2026-10-12' })],
  );

  it('compares needs with people who are not on leave, week by week, the tightest position first', () => {
    const rows = capacity(plan, FROM, 3);

    expect(rows.map((r) => r.position)).toEqual(['Električar', 'Zidar']);

    const zidar = rows[1].weeks;
    expect(zidar.map((w) => [w.need, w.available])).toEqual([[3, 2], [3, 1], [3, 2]]);
    expect(load(zidar[1])).toBe(3);
    expect(shortfall(zidar[1])).toBe(2);
  });

  it('treats needs with nobody to meet them as infinitely full', () => {
    const rows = capacity(plan, FROM, 3);
    const el = rows[0].weeks;

    expect(load(el[0])).toBe(0);
    expect(load(el[1])).toBe(Infinity);
  });

  it('adds the needs of a new project from its week, for as many weeks as it lasts, and changes nothing else', () => {
    const rows = capacity(plan, FROM, 3);
    const extra = withExtraNeed(rows, 'zidar', 2, '2026-10-14', 1);

    expect(extra[1].weeks.map((w) => w.need)).toEqual([3, 5, 3]);
    expect(extra[0]).toBe(rows[0]);
    expect(rows[1].weeks.map((w) => w.need)).toEqual([3, 3, 3]);
  });
});
