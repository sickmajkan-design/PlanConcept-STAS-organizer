/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { paths } from '../routes/paths';
import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../test/renderScreen';
import { useNavBadgeCounts } from './useNavBadgeCounts';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function Probe() {
  const counts = useNavBadgeCounts(signedIn('SuperAdmin'));

  return <pre data-testid="counts">{JSON.stringify(counts)}</pre>;
}

const read = () => JSON.parse(screen.getByTestId('counts').textContent ?? '{}') as Record<string, number>;

describe('useNavBadgeCounts', () => {
  it('counts the notices not yet opened, and what the work queue already counts', async () => {
    network.reply('/bulletin', 200, [
      { id: 'a', viewed: false },
      { id: 'b', viewed: true },
      { id: 'c', viewed: false },
    ]);
    network.reply('/attention', 200, {
      groups: [
        { key: 'refunds', count: 3, items: [] },
        { key: 'vehicleDates', count: 2, items: [] },
        { key: 'dkvRows', count: 4, items: [] },
        { key: 'lateInvoices', count: 1, items: [] },
        { key: 'lateCosts', count: 5, items: [] },
        { key: 'dataQuality', count: 7, items: [] },
      ],
    });

    renderScreen(<Probe />, { route: '/', path: '/', user: signedIn('SuperAdmin') });

    await waitFor(() => expect(read()[paths.bulletin]).toBe(2));
    await waitFor(() => expect(read()[paths.refunds]).toBe(3));

    const counts = read();
    expect(counts[paths.vehicles]).toBe(2);
    // A statement that does not match is reached from the vehicle costs, so its hub carries it.
    expect(counts[paths.costRecords]).toBe(4);
    expect(counts[paths.bookReceivables]).toBe(6);
    expect(counts[paths.dataQuality]).toBe(7);
  }, SCREEN_TIMEOUT);
});
