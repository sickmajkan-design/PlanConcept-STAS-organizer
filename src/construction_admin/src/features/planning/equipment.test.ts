import { describe, expect, it } from 'vitest';

import type { PlanningData, PlanningEquipment } from '../../api/planning';
import { choiceRequest, equipmentOnDay, equipmentSummary, groupCounts, heldBy, personFlags, siteEquipment, suggestReceiver } from './equipment';
import { buildPlan } from './planningLogic';

const DAY = '2031-06-10';

const site = (id: string) => ({
  id,
  name: id,
  status: 'Active',
  customerName: null,
  startDate: null,
  endDate: null,
  worksSaturdays: false,
  worksSundays: false,
  needs: [],
});

const person = (id: string, postings: { projectId: string }[] = [], absent = false) => ({
  id,
  fullName: `Osoba ${id}`,
  position: 'Električar',
  postings: postings.map((p) => ({ projectId: p.projectId, startDate: DAY, endDate: DAY })),
  absences: absent ? [{ type: 'AnnualLeave', startDate: DAY, endDate: DAY }] : [],
});

const item = (id: string, group: string, employeeId: string | null, projectId: string | null, status = 'Assigned'): PlanningEquipment => ({
  id,
  kind: group === 'Vehicle' ? 'Vehicle' : 'Tool',
  group,
  label: id,
  status,
  employeeId,
  projectId,
});

const plan = (equipment: PlanningEquipment[]) => {
  const data: PlanningData = {
    from: DAY,
    to: DAY,
    today: DAY,
    projects: [site('A'), site('B')],
    employees: [person('p1', [{ projectId: 'A' }]), person('p2', [{ projectId: 'A' }]), person('p3'), person('p4', [{ projectId: 'B' }], true)],
    equipment,
    positions: ['Električar'],
  };

  return buildPlan(data);
};

describe('planning equipment', () => {
  it('sorts items into what a person holds, what sits on a site with nobody, and the store', () => {
    const p = plan([item('v1', 'Vehicle', 'p1', 'A'), item('k1', 'Kit', null, 'A'), item('m1', 'MeasuringSet', null, null)]);

    expect(heldBy(p, 'p1').map((i) => i.id)).toEqual(['v1']);
    expect(p.equipment.onSite.get('A')?.map((i) => i.id)).toEqual(['k1']);
    expect(p.equipment.store.map((i) => i.id)).toEqual(['m1']);
  });

  it('counts one icon per kind in a fixed order and leaves out the kinds with none', () => {
    const p = plan([item('a', 'CableSet', 'p1', 'A'), item('b', 'Vehicle', 'p1', 'A'), item('c', 'Vehicle', 'p2', 'A'), item('d', 'Kit', 'p1', 'A')]);

    expect(groupCounts(p.equipment.items)).toEqual([
      { group: 'vehicle', count: 2 },
      { group: 'kit', count: 1 },
      { group: 'cable', count: 1 },
    ]);
  });

  it('shows a site what its crew holds and what is on the site but held by nobody', () => {
    const p = plan([item('v1', 'Vehicle', 'p1', 'A'), item('t1', 'BatterySet', 'p2', 'A'), item('k1', 'Kit', null, 'A'), item('v9', 'Vehicle', 'p4', 'B')]);
    const crew = p.people.filter((x) => x.cells[0].project === 'A');
    const result = siteEquipment(p, p.projectById.get('A')!, crew);

    expect(result.held.map((h) => h.person.id).sort()).toEqual(['p1', 'p2']);
    expect(result.free.map((i) => i.id)).toEqual(['k1']);
    expect(result.all).toHaveLength(3);
  });

  it('flags equipment tied to another site than its holder is posted to', () => {
    const p = plan([item('v1', 'Vehicle', 'p1', 'B')]);
    const p1 = p.people.find((x) => x.id === 'p1')!;

    expect(personFlags(p, p1, 0)).toEqual({ mismatch: true, idle: false });
  });

  it('flags equipment that stays with somebody who is free or away', () => {
    const p = plan([item('k1', 'Kit', 'p3', null), item('t1', 'BatterySet', 'p4', 'B')]);

    expect(personFlags(p, p.people.find((x) => x.id === 'p3')!, 0).idle).toBe(true);
    expect(personFlags(p, p.people.find((x) => x.id === 'p4')!, 0).idle).toBe(true);
  });

  it('summarises the three things worth a look', () => {
    const p = plan([
      item('k1', 'Kit', null, 'A'),
      item('v1', 'Vehicle', 'p1', 'B'),
      item('t1', 'BatterySet', 'p3', null),
      item('t2', 'MeasuringSet', 'p4', 'B'),
      item('ok', 'Vehicle', 'p2', 'A'),
    ]);

    expect(equipmentSummary(p, 0)).toEqual({ free: 1, mismatch: 1, idle: 2 });
  });

  it('marks an item that is in service or being repaired', () => {
    const p = plan([item('v1', 'Vehicle', 'p1', 'A', 'InService'), item('t1', 'Kit', 'p1', 'A', 'UnderRepair'), item('t2', 'Kit', 'p1', 'A')]);

    expect(heldBy(p, 'p1').map((i) => i.notUsable)).toEqual([true, true, false]);
  });
});

describe('the decision about equipment', () => {
  it('suggests a free person with the same position, else any free person, else nobody', () => {
    const p = plan([item('v1', 'Vehicle', 'p4', 'B')]);
    const absent = p.people.find((x) => x.id === 'p4')!;

    // p3 is free that day and has the same position
    expect(suggestReceiver(p, absent, 0)?.id).toBe('p3');

    const none = buildPlan({ ...({} as PlanningData), from: DAY, to: DAY, today: DAY, projects: [site('A')], employees: [person('x', [{ projectId: 'A' }]), person('y', [{ projectId: 'A' }], true)], positions: [] });
    expect(suggestReceiver(none, none.people.find((x) => x.id === 'y')!, 0)).toBeNull();
  });

  it('sends only what changes, and notices a hand-over with nobody to receive it', () => {
    const p = plan([item('v1', 'Vehicle', 'p1', 'A'), item('t1', 'Kit', 'p1', 'A'), item('t2', 'BatterySet', 'p1', 'A')]);
    const items = heldBy(p, 'p1');

    const request = choiceRequest(items, {
      v1: { choice: 'HandOver', to: 'p3' },
      t1: { choice: 'ToStore', to: null },
      t2: { choice: 'Stay', to: null },
    });

    expect(request.items).toEqual([
      { kind: 'Vehicle', id: 'v1', choice: 'HandOver', toEmployeeId: 'p3' },
      { kind: 'Tool', id: 't1', choice: 'ToStore', toEmployeeId: null },
    ]);
    expect(request.missingReceiver).toBe(false);
    expect(choiceRequest(items, { v1: { choice: 'HandOver', to: null } }).missingReceiver).toBe(true);
    expect(choiceRequest(items, {}).items).toEqual([]);
  });
});

describe('equipment on another day', () => {
  const base = () => plan([item('car', 'Vehicle', 'ana', 'A'), item('kit', 'Kit', null, null)]).equipment;

  it('moves items to the holders of a later day and keeps every item', () => {
    const later = equipmentOnDay(base(), { date: '2031-06-12', items: [{ kind: 'Vehicle', id: 'car', employeeId: 'ivo', projectId: 'B' }], planned: [] }, false);

    expect(later.byEmployee.get('ivo')?.map((i) => i.id)).toEqual(['car']);
    expect(later.byEmployee.get('ana')).toBeUndefined();
    expect(later.store.map((i) => i.id)).toEqual(['kit']);
  });

  it('leaves out, for an earlier day, the items the history does not know', () => {
    const past = equipmentOnDay(base(), { date: '2031-06-01', items: [{ kind: 'Vehicle', id: 'car', employeeId: null, projectId: null }], planned: [] }, true);

    expect(past.items.map((i) => i.id)).toEqual(['car']);
    expect(past.store.map((i) => i.id)).toEqual(['car']);
  });
});
