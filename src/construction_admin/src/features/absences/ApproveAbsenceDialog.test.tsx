/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import type { Absence } from '../../api/types';
import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * Granting leave to somebody who lives in company housing asks what happens to the housing: annual
 * leave takes them off it by default, any other kind leaves the choice to the office.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function absence(type: Absence['type']): Absence {
  return {
    id: 'a1',
    employeeId: 'e1',
    employeeName: 'Ana Novak',
    type,
    status: 'Requested',
    startDate: '2026-10-20',
    endDate: '2026-10-24',
    dayCount: 5,
  } as Absence;
}

const housed = {
  hasStay: true,
  stayId: 's1',
  accommodationId: 'h1',
  accommodationName: 'Stan Zagrebačka 4',
  stayStartDate: '2026-09-01',
  stayEndDate: null,
};

async function open(type: Absence['type']) {
  const { ApproveAbsenceDialog } = await import('./ApproveAbsenceDialog');

  return renderScreen(<ApproveAbsenceDialog absence={absence(type)} onClose={() => {}} />, {
    route: '/',
    path: '/',
    user: signedIn('Admin'),
  });
}

describe('ApproveAbsenceDialog', () => {
  it('asks nothing about housing for a person who is not housed', async () => {
    network.reply('/absences/housing-impact', 200, { hasStay: false });

    await open('AnnualLeave');
    await screen.findByRole('button', { name: 'Grant' });

    expect(screen.queryByText(/Take them off housing/)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('takes a housed person off housing on annual leave unless told otherwise', async () => {
    network.reply('/absences/housing-impact', 200, housed);

    await open('AnnualLeave');

    const release = await screen.findByRole('checkbox', { name: /Take them off housing from/ });
    expect((release as HTMLInputElement).checked).toBe(true);
    expect(screen.getByText(/Lives at Stan Zagrebačka 4/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('leaves the choice to the office for sick leave', async () => {
    network.reply('/absences/housing-impact', 200, housed);

    await open('SickLeave');

    const release = await screen.findByRole('checkbox', { name: /Take them off housing from/ });
    expect((release as HTMLInputElement).checked).toBe(false);
  }, SCREEN_TIMEOUT);

  it('sends the choice with the approval', async () => {
    network.reply('/absences/housing-impact', 200, housed);
    network.reply('/absences/a1/review', 200, { id: 'a1' });

    await open('SickLeave');

    await userEvent.click(await screen.findByRole('checkbox', { name: /Take them off housing from/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Grant' }));

    await waitFor(() => {
      const review = network.calls.find((c) => c.url.includes('/absences/a1/review'));
      expect(review?.body).toMatchObject({
        approve: true,
        releaseAccommodation: true,
        returnToAccommodation: false,
      });
    });
  }, SCREEN_TIMEOUT);

  it('books the person back in only when asked to', async () => {
    network.reply('/absences/housing-impact', 200, housed);
    network.reply('/absences/a1/review', 200, { id: 'a1' });

    await open('AnnualLeave');

    const comeBack = await screen.findByRole('checkbox', { name: /Book them back into the same place/ });
    expect((comeBack as HTMLInputElement).checked).toBe(false);

    await userEvent.click(comeBack);
    await userEvent.click(screen.getByRole('button', { name: 'Grant' }));

    await waitFor(() => {
      const review = network.calls.find((c) => c.url.includes('/absences/a1/review'));
      expect(review?.body).toMatchObject({ releaseAccommodation: true, returnToAccommodation: true });
    });
  }, SCREEN_TIMEOUT);

  it('sends the plain approval for a person who is not housed', async () => {
    network.reply('/absences/housing-impact', 200, { hasStay: false });
    network.reply('/absences/a1/review', 200, { id: 'a1' });

    await open('AnnualLeave');

    await userEvent.click(await screen.findByRole('button', { name: 'Grant' }));

    await waitFor(() => {
      const review = network.calls.find((c) => c.url.includes('/absences/a1/review'));
      expect(review?.body).toEqual({ approve: true });
    });
  }, SCREEN_TIMEOUT);
});
