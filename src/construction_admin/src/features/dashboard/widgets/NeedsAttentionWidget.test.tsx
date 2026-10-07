/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../../test/renderScreen';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

async function renderWidget() {
  const { NeedsAttentionWidget } = await import('./NeedsAttentionWidget');
  return renderScreen(<NeedsAttentionWidget instanceId="w1" onRemove={() => {}} onExpandWidth={() => {}} />, {
    route: '/',
    path: '/',
    user: signedIn('Admin'),
  });
}

describe('NeedsAttentionWidget', () => {
  it('lists each kind of work waiting with how much there is, and where it is done', async () => {
    network.reply('/attention', 200, {
      groups: [
        { key: 'dataQuality', count: 12, items: [] },
        { key: 'absenceRequests', count: 2, items: [{ label: 'Ana Novak', kind: null, date: '2026-10-20' }] },
        { key: 'vehicleDates', count: 1, items: [{ label: 'Iveco Daily (ZG-1)', kind: 'registration', date: '2026-10-12' }] },
      ],
    });

    await renderWidget();

    expect(await screen.findByText('Leave requests to answer')).toBeDefined();
    expect(screen.getByText('2')).toBeDefined();
    expect(screen.getByText(/Ana Novak/)).toBeDefined();
    expect(screen.getByText(/Iveco Daily \(ZG-1\) · registration/)).toBeDefined();
    expect(screen.getByRole('link', { name: /Leave requests to answer/ }).getAttribute('href')).toBe('/absences');

    // Requests somebody is waiting on come before housekeeping, whatever order the API sent them in.
    const titles = screen.getAllByRole('link').map((l) => l.textContent ?? '');
    expect(titles[0]).toMatch(/Leave requests/);
    expect(titles[titles.length - 1]).toMatch(/Records to tidy/);
  }, SCREEN_TIMEOUT);

  it('says so when nothing is waiting', async () => {
    network.reply('/attention', 200, { groups: [] });

    await renderWidget();

    expect(await screen.findByText('Nothing needs your attention right now.')).toBeDefined();
  }, SCREEN_TIMEOUT);
});
