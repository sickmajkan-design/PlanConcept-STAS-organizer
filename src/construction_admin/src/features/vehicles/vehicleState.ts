import type { Vehicle } from '../../api/types';
import { companyNow } from '../../utils/companyTime';
import { daysUntil, EXPIRING_SOON_DAYS } from './VehicleDateChip';

export type VehicleStateTone = 'ok' | 'warning' | 'error' | 'none';

export type VehicleDateKind = 'registration' | 'inspection' | 'insurance' | 'service' | 'rentedUntil';

/** One thing that makes a vehicle need a look. */
export interface VehicleStateReason {
  tone: 'warning' | 'error';
  what: 'date' | 'tollUnpaid' | 'toll' | 'fineOverdue' | 'fineOpen';
  kind?: VehicleDateKind;
  /** `YYYY-MM-DD` of the date or the toll's end of validity. */
  date?: string;
  /** Days to the date; negative once it has passed. */
  days?: number;
  count?: number;
  amount?: number;
}

export interface VehicleState {
  tone: VehicleStateTone;
  reasons: VehicleStateReason[];
}

type Input = Pick<
  Vehicle,
  | 'registrationValidUntil'
  | 'technicalInspectionValidUntil'
  | 'insuranceValidUntil'
  | 'nextServiceDue'
  | 'rentedUntil'
  | 'ownershipType'
  | 'tollCount'
  | 'unpaidTollCount'
  | 'nextTollValidUntil'
  | 'openFineCount'
  | 'openFineTotal'
  | 'overdueFineCount'
>;

/**
 * The one sign a vehicle gets on the list: green when everything is in order, amber when something runs out within a
 * month or a fine is open, red when something has run out, a toll is unpaid or a fine is past its due date, and grey
 * when no date was ever entered (nothing is known, which is not the same as fine).
 */
export function vehicleState(v: Input, today: Date = companyNow()): VehicleState {
  const reasons: VehicleStateReason[] = [];

  const dates: [VehicleDateKind, string | null][] = [
    ['registration', v.registrationValidUntil],
    ['inspection', v.technicalInspectionValidUntil],
    ['insurance', v.insuranceValidUntil],
    ['service', v.nextServiceDue],
  ];
  if (v.ownershipType !== 'Owned') dates.push(['rentedUntil', v.rentedUntil]);

  for (const [kind, date] of dates) {
    if (!date) continue;
    const days = daysUntil(date, today);
    if (days < 0) reasons.push({ tone: 'error', what: 'date', kind, date, days });
    else if (days <= EXPIRING_SOON_DAYS) reasons.push({ tone: 'warning', what: 'date', kind, date, days });
  }

  if (v.unpaidTollCount > 0) reasons.push({ tone: 'error', what: 'tollUnpaid', count: v.unpaidTollCount });

  if (v.nextTollValidUntil) {
    const days = daysUntil(v.nextTollValidUntil, today);
    if (days < 0) reasons.push({ tone: 'error', what: 'toll', date: v.nextTollValidUntil, days });
    else if (days <= EXPIRING_SOON_DAYS) reasons.push({ tone: 'warning', what: 'toll', date: v.nextTollValidUntil, days });
  }

  if (v.overdueFineCount > 0) reasons.push({ tone: 'error', what: 'fineOverdue', count: v.overdueFineCount });

  const stillOpen = v.openFineCount - v.overdueFineCount;
  if (stillOpen > 0) reasons.push({ tone: 'warning', what: 'fineOpen', count: stillOpen, amount: v.openFineTotal });

  if (reasons.some((r) => r.tone === 'error')) return { tone: 'error', reasons };
  if (reasons.length > 0) return { tone: 'warning', reasons };

  const knowsAnything = dates.some(([, date]) => !!date) || v.tollCount > 0;

  return { tone: knowsAnything ? 'ok' : 'none', reasons };
}
