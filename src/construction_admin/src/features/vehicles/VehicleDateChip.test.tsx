/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { renderScreen, signedIn } from '../../test/renderScreen';
import { VehicleDateChip, dateTone, daysUntil } from './VehicleDateChip';

/** A date `days` from today, as the API sends it. */
function inDays(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);

  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

describe('the date a vehicle runs out', () => {
  it('counts whole days from today, negative once passed', () => {
    expect(daysUntil(inDays(0))).toBe(0);
    expect(daysUntil(inDays(10))).toBe(10);
    expect(daysUntil(inDays(-3))).toBe(-3);
  });

  it('is plain far ahead, amber in the last month and red once it has passed', () => {
    expect(dateTone(inDays(120))).toBe('default');
    expect(dateTone(inDays(30))).toBe('warning');
    expect(dateTone(inDays(0))).toBe('warning');
    expect(dateTone(inDays(-1))).toBe('error');
    expect(dateTone(null)).toBe('default');
  });

  it('says plainly when the date was never entered', async () => {
    renderScreen(<VehicleDateChip label="Registered until" date={null} />, {
      route: '/',
      path: '/',
      user: signedIn('Admin'),
    });

    expect(await screen.findByText('Registered until: not entered')).toBeDefined();
  }, 40_000);

  it('says how many days are left when the date is close', async () => {
    renderScreen(<VehicleDateChip label="Registered until" date={inDays(12)} />, {
      route: '/',
      path: '/',
      user: signedIn('Admin'),
    });

    expect(await screen.findByText(/12 days left/)).toBeDefined();
  }, 40_000);

  it('says so when it has expired', async () => {
    renderScreen(<VehicleDateChip label="Registered until" date={inDays(-5)} />, {
      route: '/',
      path: '/',
      user: signedIn('Admin'),
    });

    expect(await screen.findByText(/expired/)).toBeDefined();
  }, 40_000);
});
