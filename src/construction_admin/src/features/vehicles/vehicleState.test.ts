import { describe, expect, it } from 'vitest';

import { vehicleState } from './vehicleState';

/** The sign on the vehicles list: what turns it amber, red or grey. */
const TODAY = new Date(2026, 9, 26); // 26 October 2026

const none = {
  registrationValidUntil: null,
  technicalInspectionValidUntil: null,
  insuranceValidUntil: null,
  nextServiceDue: null,
  rentedUntil: null,
  ownershipType: 'Owned' as const,
  tollCount: 0,
  unpaidTollCount: 0,
  nextTollValidUntil: null,
  openFineCount: 0,
  openFineTotal: 0,
  overdueFineCount: 0,
};

describe('vehicleState', () => {
  it('is grey when nothing was ever entered, and green when dates are far away', () => {
    expect(vehicleState(none, TODAY).tone).toBe('none');
    expect(vehicleState({ ...none, registrationValidUntil: '2027-06-01' }, TODAY).tone).toBe('ok');
  });

  it('is amber for a date inside a month and red once it has passed', () => {
    const soon = vehicleState({ ...none, insuranceValidUntil: '2026-11-10' }, TODAY);
    expect(soon.tone).toBe('warning');
    expect(soon.reasons[0]).toMatchObject({ what: 'date', kind: 'insurance', days: 15 });

    const past = vehicleState({ ...none, insuranceValidUntil: '2026-10-12', registrationValidUntil: '2026-11-10' }, TODAY);
    expect(past.tone).toBe('error');
    expect(past.reasons).toHaveLength(2);
  });

  it('counts the end of a rental only for a vehicle that is not owned', () => {
    expect(vehicleState({ ...none, rentedUntil: '2026-10-01', registrationValidUntil: '2027-06-01' }, TODAY).tone).toBe('ok');
    expect(vehicleState({ ...none, ownershipType: 'Rented', rentedUntil: '2026-10-01' }, TODAY).tone).toBe('error');
  });

  it('is red for an unpaid toll and follows the nearest end of validity', () => {
    expect(vehicleState({ ...none, tollCount: 1, unpaidTollCount: 1 }, TODAY).tone).toBe('error');
    expect(vehicleState({ ...none, tollCount: 2, nextTollValidUntil: '2026-11-06' }, TODAY).tone).toBe('warning');
    expect(vehicleState({ ...none, tollCount: 2, nextTollValidUntil: '2026-10-20' }, TODAY).tone).toBe('error');
  });

  it('is amber for an open fine and red for one past its due date', () => {
    const open = vehicleState({ ...none, registrationValidUntil: '2027-06-01', openFineCount: 2, openFineTotal: 180 }, TODAY);
    expect(open.tone).toBe('warning');
    expect(open.reasons[0]).toMatchObject({ what: 'fineOpen', count: 2, amount: 180 });

    const late = vehicleState({ ...none, registrationValidUntil: '2027-06-01', openFineCount: 2, openFineTotal: 180, overdueFineCount: 1 }, TODAY);
    expect(late.tone).toBe('error');
    expect(late.reasons.map((r) => r.what)).toEqual(['fineOverdue', 'fineOpen']);
  });
});
