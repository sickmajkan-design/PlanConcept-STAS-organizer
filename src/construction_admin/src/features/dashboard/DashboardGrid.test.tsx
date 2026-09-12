/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';
import { HomePage } from '../../pages/HomePage';

let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

/**
 * The dashboard is Admin/SuperAdmin only — every other role keeps the plain
 * static home page (`HomePage`'s own fallback branch, unchanged by this
 * feature). What matters here is the fork: the right role sees widgets, the
 * wrong one never even calls the layout endpoint.
 */
describe('HomePage dashboard', () => {
  it('shows the default widget catalog the API returns for a first-time layout', async () => {
    // The backend (not this screen) is responsible for synthesizing the
    // default catalog when a user has no saved layout — see
    // GetDashboardLayoutQueryHandler. This screen only has to render
    // whatever the endpoint sends back.
    network.reply('/dashboard-layout', 200, {
      widgets: [
        { id: '1', type: 'ProjectsRealization', column: 0, order: 0 },
        { id: '2', type: 'AbsencesBalance', column: 0, order: 1 },
        { id: '3', type: 'NotificationsBulletin', column: 0, order: 2 },
        { id: '4', type: 'DocumentExpiry', column: 0, order: 3 },
        { id: '5', type: 'FleetStatus', column: 1, order: 0 },
      ],
    });

    renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

    await waitFor(() => {
      expect(screen.getByText('Projects — realization')).toBeDefined();
    });

    expect(screen.getByText('Workforce')).toBeDefined();
    expect(screen.getByText('Notifications & bulletin')).toBeDefined();
    expect(screen.getByText('Documents expiring soon')).toBeDefined();
    expect(screen.getByText('Vehicles & tools')).toBeDefined();
  });

  it('never mounts the dashboard for a role below Admin', async () => {
    renderScreen(<HomePage />, { user: signedIn('Foreman') });

    await waitFor(() => {
      expect(screen.getByText('Pick a section from the menu to get started.')).toBeDefined();
    });

    expect(screen.queryByRole('button', { name: 'Add widget' })).toBeNull();
    expect(network.calls.some((call) => call.url.includes('/dashboard-layout'))).toBe(false);
  });
});
