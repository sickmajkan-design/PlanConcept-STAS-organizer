/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

async function renderSummary() {
  const { TodaySummary } = await import('./TodaySummary');
  return renderScreen(<TodaySummary />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('TodaySummary', () => {
  it('says who is on a shift and what waits, each with a way to where it is dealt with', async () => {
    network.reply('/time-entries', 200, { items: [], totalCount: 12, pageNumber: 1, pageSize: 100 });
    network.reply('/attention', 200, {
      groups: [
        { key: 'dataQuality', count: 12, items: [] },
        { key: 'absenceRequests', count: 2, items: [] },
      ],
    });

    await renderSummary();

    expect(await screen.findByText('Leave requests to answer')).toBeDefined();
    expect(screen.getByText('12', { selector: 'span' })).toBeDefined();
    expect(screen.getByRole('link', { name: /Leave requests to answer/ }).getAttribute('href')).toBe('/absences');
    // What somebody is waiting on comes before housekeeping.
    const links = screen.getAllByRole('link').map((l) => l.textContent ?? '');
    expect(links.findIndex((l) => /Leave requests/.test(l))).toBeLessThan(links.findIndex((l) => /Records to tidy/.test(l)));
  }, SCREEN_TIMEOUT);

  it('says so when nothing is waiting', async () => {
    network.reply('/time-entries', 200, { items: [], totalCount: 0, pageNumber: 1, pageSize: 100 });
    network.reply('/attention', 200, { groups: [] });

    await renderSummary();

    expect(await screen.findByText('Nothing is waiting for you.')).toBeDefined();
  }, SCREEN_TIMEOUT);
});
