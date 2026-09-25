/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';
import { SetupChecklistCard } from './SetupChecklistCard';

let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const SCREEN_TIMEOUT = 20_000;

const reply = (items: { key: string; count: number }[]) => network.reply('/setup/checklist', 200, { items });

describe('SetupChecklistCard', () => {
  it('shows nothing when nothing is missing', async () => {
    reply([]);

    renderScreen(<SetupChecklistCard />, { user: signedIn('SuperAdmin') });
    await new Promise((resolve) => setTimeout(resolve, 100));

    expect(screen.queryByText('Still to set up')).toBeNull();
    expect(screen.queryByText('Welcome — let us set things up')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('walks an empty system through its first step and marks the finished ones', async () => {
    reply([
      { key: 'noProjects', count: 1 },
      { key: 'noEmployees', count: 1 },
    ]);

    renderScreen(<SetupChecklistCard />, { user: signedIn('SuperAdmin') });

    await screen.findByText('Welcome — let us set things up');
    // The company is done (its gap is gone), so the guide is at the first project.
    expect(screen.getByText('The site your people will work on.')).toBeDefined();
    expect(screen.queryByText('Name and contact details, shown on documents and reports.')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('moves a skipped step down into the list, where it stays until fixed', async () => {
    const user = userEvent.setup();
    reply([
      { key: 'noProjects', count: 1 },
      { key: 'noEmployees', count: 1 },
    ]);

    renderScreen(<SetupChecklistCard />, { user: signedIn('SuperAdmin') });

    await screen.findByText('The site your people will work on.');
    await user.click(screen.getByRole('button', { name: 'Skip for now' }));

    // The guide moves on to the people, and the project gap is still listed.
    await screen.findByText('Add them one by one or import a spreadsheet.');
    expect(screen.getByText('Add your first project')).toBeDefined();
    await waitFor(() => expect(window.localStorage.getItem('onboarding.wizard.skipped')).toContain('project'));
  }, SCREEN_TIMEOUT);

  it('lists what only the server owner can fix, without a button to fix it', async () => {
    reply([
      { key: 'emailNotConfigured', count: 1 },
      { key: 'pushNotConfigured', count: 1 },
    ]);

    renderScreen(<SetupChecklistCard />, { user: signedIn('SuperAdmin') });

    await screen.findByText(/E-mail is not set up on the server/);
    expect(screen.getByText(/Push notifications are not set up on the server/)).toBeDefined();
    expect(screen.getByText(/contact your provider/)).toBeDefined();
    // A first-run guide has no business on a system that already has its data.
    expect(screen.queryByText('Welcome — let us set things up')).toBeNull();
    expect(screen.queryByRole('button', { name: 'Fix' })).toBeNull();
  }, SCREEN_TIMEOUT);

  it('keeps the ordinary list, with fix buttons, once the system has its data', async () => {
    reply([{ key: 'employeesWithoutProject', count: 3 }]);

    renderScreen(<SetupChecklistCard />, { user: signedIn('Admin') });

    await screen.findByText('Employees not assigned to any project: 3');
    expect(screen.getByRole('button', { name: 'Fix' })).toBeDefined();
    expect(screen.queryByText('Welcome — let us set things up')).toBeNull();
  }, SCREEN_TIMEOUT);
});
