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

const group = (key: string, items: { id: string; label: string; detail?: string }[], count = items.length) => ({
  key,
  count,
  items: items.map((i) => ({ detail: null, ...i })),
});

async function renderPage() {
  const { DataQualityPage } = await import('./DataQualityPage');
  return renderScreen(<DataQualityPage />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('DataQualityPage', () => {
  it('lists each problem with its records, and names the checks that came out clean', async () => {
    network.reply('/data-quality', 200, {
      groups: [
        group('employeesNoPosition', [{ id: 'e1', label: 'Ana Novak', detail: 'P-1' }]),
        group('vehiclesNoDates', [{ id: 'v1', label: 'Iveco Daily (ZG-1)', detail: 'registration,insurance,' }]),
        group('postingsAfterEnd', []),
        group('projectsNoNeeds', []),
        group('projectsNoDates', []),
        group('vehiclesNoTd', []),
        group('unknownCards', []),
      ],
    });

    await renderPage();

    expect(await screen.findByText('Workers without a position')).toBeDefined();
    expect(screen.getByText('Ana Novak')).toBeDefined();
    expect(screen.getByText(/registration, insurance/)).toBeDefined();
    expect(screen.getByRole('link', { name: /Ana Novak/ }).getAttribute('href')).toBe('/employees/e1/edit');
    expect(screen.getByText('In order:')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('says so when nothing needs tidying', async () => {
    network.reply('/data-quality', 200, { groups: [group('employeesNoPosition', []), group('vehiclesNoTd', [])] });

    await renderPage();

    expect(await screen.findByText('Everything checked is in order.')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('says how many are shown when the list is cut short', async () => {
    network.reply('/data-quality', 200, {
      groups: [group('vehiclesNoTd', [{ id: 'v1', label: 'Iveco Daily', detail: 'ZG-1' }], 120)],
    });

    await renderPage();

    expect(await screen.findByText('Showing 1 of 120.')).toBeDefined();
  }, SCREEN_TIMEOUT);
});
