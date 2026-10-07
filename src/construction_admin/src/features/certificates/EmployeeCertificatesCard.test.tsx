/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

async function renderCard() {
  const { EmployeeCertificatesCard } = await import('./EmployeeCertificatesCard');
  return renderScreen(<EmployeeCertificatesCard employeeId="e1" />, { route: '/', path: '/', user: signedIn('Admin') });
}

const daysFromToday = (n: number) => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);

describe('EmployeeCertificatesCard', () => {
  it('lists the certificates and marks one that has run out or is about to', async () => {
    network.reply('/employees/e1/certificates', 200, [
      { id: 'c1', name: 'Work at height', validUntil: daysFromToday(-3), note: null },
      { id: 'c2', name: 'Forklift', validUntil: daysFromToday(10), note: 'Class B' },
      { id: 'c3', name: 'First aid', validUntil: null, note: null },
    ]);

    await renderCard();

    expect(await screen.findByText('Work at height')).toBeDefined();
    expect(screen.getByText('Expired')).toBeDefined();
    expect(screen.getByText('Runs out soon')).toBeDefined();
    expect(screen.getByText(/Does not expire/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('says so when there are none', async () => {
    network.reply('/employees/e1/certificates', 200, []);

    await renderCard();

    expect(await screen.findByText('No certificates entered.')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('adds a certificate with the name and date entered', async () => {
    network.reply('/employees/e1/certificates', 200, []);

    await renderCard();
    await userEvent.click(await screen.findByRole('button', { name: 'Add a certificate' }));

    const dialog = await screen.findByRole('dialog');
    await userEvent.type(dialog.querySelector('input[type="text"]') as HTMLInputElement, 'Welding');
    await userEvent.type(dialog.querySelector('input[type="date"]') as HTMLInputElement, '2027-03-01');
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => {
      const put = network.calls.find((c) => c.method?.toLowerCase() === 'put' && c.url.includes('/employees/e1/certificates'));
      expect(put?.body).toMatchObject({ name: 'Welding', validUntil: '2027-03-01' });
    });
  }, SCREEN_TIMEOUT);
});
