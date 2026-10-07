import { describe, expect, it } from 'vitest';

import type { PlanningData, PlanningEmployee, PlanningProject } from '../../api/planning';
import {
  awayItems,
  buildPlan,
  candidates,
  columnsFor,
  confirmationOf,
  conflicts,
  covered,
  freeCount,
  missingCertificates,
  missing,
  periodBounds,
  siteWorksOn,
  segmentsOf,
  shortage,
  unconfirmedCount,
} from './planningLogic';

// Monday 2026-10-05 .. Sunday 2026-10-18.
const FROM = '2026-10-05';
const TO = '2026-10-18';

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

const person = (
  id: string,
  position: string,
  postings: [string, string, string | null][] = [],
  absences: [string, string][] = [],
): PlanningEmployee => ({
  id,
  fullName: id,
  position,
  postings: postings.map(([projectId, startDate, endDate]) => ({ projectId, startDate, endDate })),
  absences: absences.map(([startDate, endDate]) => ({ type: 'AnnualLeave', startDate, endDate })),
});

const data = (employees: PlanningEmployee[], projects: PlanningProject[]): PlanningData => ({
  from: FROM,
  to: TO,
  today: '2026-10-07',
  projects,
  employees,
  positions: [],
});

describe('shortage', () => {
  it('counts what a site lacks per position, and a surplus of one position does not cover another', () => {
    const plan = buildPlan(
      data(
        [person('a', 'Zidar', [['s1', FROM, null]]), person('b', 'Zidar', [['s1', FROM, null]]), person('c', 'Zidar', [['s1', FROM, null]])],
        [site('s1', { Zidar: 2, Električar: 1 })],
      ),
    );

    expect(shortage(plan, plan.projects[0], 0)).toBe(1);
    expect(covered(plan, plan.projects[0], 0)).toBe(2);
    expect(shortage(plan, plan.projects[0], 0, 'Električar')).toBe(1);
    expect(shortage(plan, plan.projects[0], 0, 'zidar')).toBe(0);
  });

  it('counts nothing on a weekend or outside the site dates', () => {
    const plan = buildPlan(
      data([], [site('s1', { Zidar: 2 }, { startDate: '2026-10-07', endDate: '2026-10-09' })]),
    );
    const s = plan.projects[0];

    expect(shortage(plan, s, 0)).toBe(0); // Monday, before it starts
    expect(shortage(plan, s, 2)).toBe(2); // Wednesday
    expect(shortage(plan, s, 5)).toBe(0); // Saturday
    expect(shortage(plan, s, 7)).toBe(0); // after it ends
  });

  it('does not count somebody away as present', () => {
    const plan = buildPlan(
      data([person('a', 'Zidar', [['s1', FROM, null]], [['2026-10-06', '2026-10-08']])], [site('s1', { Zidar: 1 })]),
    );

    expect(shortage(plan, plan.projects[0], 0)).toBe(0);
    expect(shortage(plan, plan.projects[0], 1)).toBe(1);
    expect(shortage(plan, plan.projects[0], 3)).toBe(1);
    expect(shortage(plan, plan.projects[0], 4)).toBe(0);
  });
});

describe('free people and total missing', () => {
  it('counts free people by position on working days only', () => {
    const plan = buildPlan(data([person('a', 'Zidar'), person('b', 'Tesar'), person('c', 'Zidar', [['s1', FROM, null]])], [site('s1', { Zidar: 3 })]));

    expect(freeCount(plan, 0)).toBe(2);
    expect(freeCount(plan, 0, 'Zidar')).toBe(1);
    expect(freeCount(plan, 5)).toBe(0);
    expect(missing(plan, 0)).toBe(2);
  });
});

describe('candidates', () => {
  const projects = [site('s1', { Tesar: 1 }), site('s2', { Tesar: 1 })];

  it('ranks free, then partly free, then surplus, then another skill', () => {
    const plan = buildPlan(
      data(
        [
          person('away', 'Tesar', [['s1', '2026-10-06', '2026-10-08']], [['2026-10-06', '2026-10-08']]),
          person('freeTesar', 'Tesar'),
          person('partTesar', 'Tesar', [['s2', '2026-10-06', '2026-10-06']]),
          person('surplusTesar', 'Tesar', [['s2', FROM, null]]),
          person('other', 'Zidar'),
        ],
        [site('s1', { Tesar: 1 }), site('s2', { Tesar: 0 }, { needs: [] })],
      ),
    );

    const list = candidates(plan, 'away', 's1', [1, 2, 3]);

    expect(list.map((c) => [c.person.id, c.tier])).toEqual([
      ['freeTesar', 1],
      ['partTesar', 2],
      ['surplusTesar', 3],
      ['other', 4],
    ]);
    expect(projects.length).toBe(2);
  });

  it('leaves out anybody who is away on one of the days and a same-position person on a site that needs them', () => {
    const plan = buildPlan(
      data(
        [
          person('away', 'Tesar', [['s1', FROM, null]], [['2026-10-06', '2026-10-06']]),
          person('alsoAway', 'Tesar', [], [['2026-10-06', '2026-10-06']]),
          person('busy', 'Tesar', [['s2', FROM, null]]),
        ],
        [site('s1', { Tesar: 1 }), site('s2', { Tesar: 1 })],
      ),
    );

    expect(candidates(plan, 'away', 's1', [1])).toEqual([]);
  });
});

describe('awayItems', () => {
  it('lists an absence only when it leaves the site short, as one item over the days it spans', () => {
    const plan = buildPlan(
      data(
        [
          person('a', 'Zidar', [['s1', FROM, null]], [['2026-10-06', '2026-10-09']]),
          person('b', 'Zidar', [['s1', FROM, null]], [['2026-10-06', '2026-10-06']]),
        ],
        [site('s1', { Zidar: 1 })],
      ),
    );

    // b's single day is covered by a; once a is away too the site is short on that day and both show.
    const items = awayItems(plan, 0, plan.days - 1);
    expect(items.map((i) => i.person.id).sort()).toEqual(['a', 'b']);
    expect(items.find((i) => i.person.id === 'a')).toMatchObject({ from: '2026-10-06', to: '2026-10-09' });
  });

  it('is empty once somebody covers the days', () => {
    const plan = buildPlan(
      data(
        [
          person('a', 'Zidar', [['s1', FROM, null]], [['2026-10-06', '2026-10-09']]),
          person('stand-in', 'Zidar', [['s1', '2026-10-06', '2026-10-09']]),
        ],
        [site('s1', { Zidar: 1 })],
      ),
    );

    expect(awayItems(plan, 0, plan.days - 1)).toEqual([]);
  });
});

describe('segmentsOf', () => {
  it('describes what a person does across a range, for an undo', () => {
    const plan = buildPlan(
      data([person('a', 'Zidar', [['s1', '2026-10-06', '2026-10-08'], ['s2', '2026-10-09', null]])], [site('s1', {}), site('s2', {})]),
    );

    expect(segmentsOf(plan, 'a', '2026-10-05', '2026-10-10')).toEqual([
      { projectId: null, from: '2026-10-05', to: '2026-10-05' },
      { projectId: 's1', from: '2026-10-06', to: '2026-10-08' },
      { projectId: 's2', from: '2026-10-09', to: '2026-10-10' },
    ]);
  });
});

describe('periods', () => {
  it('gives the calendar month and quarter around a day', () => {
    expect(periodBounds('M', '2026-11-17')).toEqual(['2026-11-01', '2026-11-30']);
    expect(periodBounds('Q', '2026-11-17')).toEqual(['2026-10-01', '2026-12-31']);
    expect(periodBounds('M', '2028-02-10')).toEqual(['2028-02-01', '2028-02-29']);
  });

  it('uses a column per day up to a month and per week beyond', () => {
    expect(columnsFor('2026-10-01', '2026-10-31')).toHaveLength(31);

    const weeks = columnsFor('2026-10-01', '2026-12-31');
    expect(weeks[0]).toEqual({ from: '2026-10-01', to: '2026-10-07' });
    expect(weeks[weeks.length - 1].to).toBe('2026-12-31');
  });
});

describe('conflicts', () => {
  it('flags somebody posted to two sites on the same working days, as one stretch', () => {
    const plan = buildPlan(
      data(
        [person('a', 'Zidar', [['s1', FROM, null], ['s2', '2026-10-07', '2026-10-09']])],
        [site('s1', {}), site('s2', {})],
      ),
    );

    const found = conflicts(plan, 0, plan.days - 1);

    expect(found).toHaveLength(1);
    expect(found[0]).toMatchObject({ kind: 'double', projectId: 's1', otherProjectId: 's2', from: '2026-10-07', to: '2026-10-09' });
  });

  it('keeps one stretch running across a weekend', () => {
    const plan = buildPlan(
      data([person('a', 'Zidar', [['s1', FROM, null], ['s2', FROM, null]])], [site('s1', {}), site('s2', {})]),
    );

    expect(conflicts(plan, 0, plan.days - 1)).toHaveLength(1);
  });

  it('flags days outside the dates of the site, and says which side', () => {
    const plan = buildPlan(
      data(
        [person('a', 'Zidar', [['s1', FROM, null]])],
        [site('s1', {}, { startDate: '2026-10-07', endDate: '2026-10-09' })],
      ),
    );

    const found = conflicts(plan, 0, plan.days - 1);

    expect(found.map((c) => [c.kind, c.from, c.to])).toEqual([
      ['beforeStart', '2026-10-05', '2026-10-06'],
      ['afterEnd', '2026-10-12', '2026-10-16'],
    ]);
  });

  it('ignores days of leave and weekends', () => {
    const plan = buildPlan(
      data(
        [person('a', 'Zidar', [['s1', FROM, null], ['s2', FROM, null]], [['2026-10-05', '2026-10-18']])],
        [site('s1', {}), site('s2', {})],
      ),
    );

    expect(conflicts(plan, 0, plan.days - 1)).toEqual([]);
  });

  it('says nothing about an ordinary plan', () => {
    const plan = buildPlan(data([person('a', 'Zidar', [['s1', FROM, null]])], [site('s1', {})]));

    expect(conflicts(plan, 0, plan.days - 1)).toEqual([]);
  });
});

describe('confirmation', () => {
  const posted = (acknowledgedAt: string | null) => ({
    id: 'a',
    fullName: 'a',
    position: 'Zidar',
    postings: [{ projectId: 's1', startDate: FROM, endDate: null, acknowledgedAt }],
    absences: [],
  });

  it('counts the people posted on a working day who have not confirmed, and none on a weekend', () => {
    const plan = buildPlan(data([posted(null), { ...posted('2026-10-01T08:00:00Z'), id: 'b' }], [site('s1', {})]));

    expect(unconfirmedCount(plan, 0)).toBe(1);
    expect(unconfirmedCount(plan, 5)).toBe(0);
  });

  it('says whether a person has confirmed all, some or none of the days they are posted', () => {
    const none = buildPlan(data([posted(null)], [site('s1', {})]));
    const all = buildPlan(data([posted('2026-10-01T08:00:00Z')], [site('s1', {})]));
    const days = [0, 1, 2];

    expect(confirmationOf(none.people[0], days)).toBe('none');
    expect(confirmationOf(all.people[0], days)).toBe('all');

    const split = buildPlan(
      data(
        [{ ...posted(null), postings: [
          { projectId: 's1', startDate: '2026-10-05', endDate: '2026-10-06', acknowledgedAt: '2026-10-01T08:00:00Z' },
          { projectId: 's1', startDate: '2026-10-07', endDate: null, acknowledgedAt: null },
        ] }],
        [site('s1', {})],
      ),
    );
    expect(confirmationOf(split.people[0], days)).toBe('some');
  });

  it('has nothing to confirm for somebody posted nowhere', () => {
    const plan = buildPlan(data([{ ...posted(null), postings: [] }], [site('s1', {})]));

    expect(confirmationOf(plan.people[0], [0, 1])).toBe('nothing');
  });
});

describe('certificates', () => {
  const holder = (id: string, certificates: { name: string; validUntil: string | null }[]) => ({
    ...person(id, 'Zidar', [['s1', FROM, null]]),
    certificates,
  });

  const heights = site('s1', {}, { requiredCertificates: ['Work at height'] });

  it('flags somebody posted to a site that requires a certificate they do not hold', () => {
    const plan = buildPlan(data([holder('a', [])], [heights]));

    const found = conflicts(plan, 0, plan.days - 1);

    expect(found).toHaveLength(1);
    expect(found[0]).toMatchObject({ kind: 'certificate', certificate: 'Work at height', from: '2026-10-05' });
  });

  it('accepts a valid one, whatever the spelling, and one that never runs out', () => {
    const spelled = buildPlan(data([holder('a', [{ name: ' work AT height ', validUntil: '2027-01-01' }])], [heights]));
    const forever = buildPlan(data([holder('a', [{ name: 'Work at height', validUntil: null }])], [heights]));

    expect(conflicts(spelled, 0, spelled.days - 1)).toEqual([]);
    expect(conflicts(forever, 0, forever.days - 1)).toEqual([]);
  });

  it('flags only the days after the certificate ran out', () => {
    const plan = buildPlan(data([holder('a', [{ name: 'Work at height', validUntil: '2026-10-08' }])], [heights]));

    const found = conflicts(plan, 0, plan.days - 1);

    expect(found.map((c) => [c.kind, c.from, c.to])).toEqual([['certificate', '2026-10-09', '2026-10-16']]);
  });

  it('says which certificates a person lacks for the days, and ranks those who hold them first among stand-ins', () => {
    const plan = buildPlan(
      data(
        [
          person('away', 'Zidar', [['s1', FROM, null]], [['2026-10-06', '2026-10-06']]),
          { ...person('without', 'Zidar'), certificates: [] },
          { ...person('with', 'Zidar'), certificates: [{ name: 'Work at height', validUntil: null }] },
        ],
        [heights],
      ),
    );

    expect(missingCertificates(plan, plan.people[1], 's1', [1])).toEqual(['Work at height']);
    expect(missingCertificates(plan, plan.people[2], 's1', [1])).toEqual([]);

    const list = candidates(plan, 'away', 's1', [1]);

    expect(list.map((c) => c.person.id)).toEqual(['with', 'without']);
    expect(list[1].lacks).toEqual(['Work at height']);
  });
});

describe('working days', () => {
  const withHolidays = (employees: PlanningEmployee[], projects: PlanningProject[], holidays: { date: string; name: string; countryCode: string }[] = []) => ({
    ...data(employees, projects),
    holidays,
  });

  it('counts Saturday for a site that works it, and only for that site', () => {
    const saturday = site('s1', { Zidar: 2 }, { worksSaturdays: true });
    const plan = buildPlan(withHolidays([person('a', 'Zidar', [['s1', FROM, null]])], [saturday]));

    // 2026-10-10 is a Saturday: index 5.
    expect(plan.isWork[5]).toBe(true);
    expect(shortage(plan, plan.projects[0], 5)).toBe(1);
    expect(plan.isWork[6]).toBe(false);
  });

  it('is not a working day when every site is off', () => {
    const plan = buildPlan(withHolidays([], [site('s1', { Zidar: 1 })]));

    expect(plan.isWork[5]).toBe(false);
    expect(shortage(plan, plan.projects[0], 5)).toBe(0);
  });

  it('does not count a public holiday of the country the site keeps, and says what it is', () => {
    const plan = buildPlan(
      withHolidays([], [site('s1', { Zidar: 1 }, { countryCode: 'HR' })], [{ date: '2026-10-08', name: 'Independence Day', countryCode: 'HR' }]),
    );

    // Thursday 2026-10-08: index 3.
    expect(plan.isWork[3]).toBe(false);
    expect(shortage(plan, plan.projects[0], 3)).toBe(0);
    expect(shortage(plan, plan.projects[0], 2)).toBe(1);
    expect(plan.holidayNames.get('2026-10-08')).toBe('Independence Day');
  });

  it('keeps working on a holiday of another country, and where the site names no country', () => {
    const holidays = [{ date: '2026-10-08', name: 'Holiday', countryCode: 'HR' }];
    const elsewhere = buildPlan(withHolidays([], [site('s1', { Zidar: 1 }, { countryCode: 'SI' })], holidays));
    const none = buildPlan(withHolidays([], [site('s1', { Zidar: 1 })], holidays));

    expect(elsewhere.isWork[3]).toBe(true);
    expect(none.isWork[3]).toBe(true);
  });

  it('works out a site day by day', () => {
    const holidays = new Set(['HR|2026-10-08']);
    const hr = site('s1', {}, { countryCode: 'HR', worksSaturdays: true });

    expect(siteWorksOn(hr, '2026-10-07', holidays)).toBe(true);
    expect(siteWorksOn(hr, '2026-10-08', holidays)).toBe(false);
    expect(siteWorksOn(hr, '2026-10-10', holidays)).toBe(true);
    expect(siteWorksOn(hr, '2026-10-11', holidays)).toBe(false);
  });
});
