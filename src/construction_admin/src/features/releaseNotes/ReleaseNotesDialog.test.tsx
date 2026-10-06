/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn } from '../../test/renderScreen';
import { ReleaseNotesDialog } from './ReleaseNotesDialog';
import { RELEASE_ID, releaseSections } from './releaseNotes';

beforeEach(() => {
  window.localStorage.clear();
  installFakeNetwork();
});

const SCREEN_TIMEOUT = 20_000;

describe('ReleaseNotesDialog', () => {
  it('tells an account what is new, once, and remembers it was told', async () => {
    const user = userEvent.setup();
    const operator = signedIn('Admin');

    const { unmount } = renderScreen(<ReleaseNotesDialog />, { user: operator });

    expect(await screen.findByText(/What's new/)).toBeDefined();
    expect(screen.getByText('Security improvements')).toBeDefined();

    await user.click(screen.getByRole('button', { name: 'Got it' }));
    await waitFor(() => expect(screen.queryByText(/What's new/)).toBeNull());
    expect(window.localStorage.getItem(`releaseNotes.seen.${operator.id}`)).toBe(RELEASE_ID);

    // The next sign-in in this browser: nothing to show.
    unmount();
    renderScreen(<ReleaseNotesDialog />, { user: operator });
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(screen.queryByText(/What's new/)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('shows an account nothing that is meant only for administrators', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('Foreman') });

    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(screen.queryByText('Security improvements')).toBeNull();
    expect(screen.queryByText('Business units')).toBeNull();
    expect(screen.queryByText('Who is online')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('shows nothing to an account none of the sections concern', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('Worker') });

    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(screen.queryByText(/What's new/)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('tells an administrator about everything meant for administrators', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('SuperAdmin') });

    await screen.findByText('Security improvements');
    expect(screen.getByText('Business units')).toBeDefined();
    expect(screen.getByText('Documents, reminders and housing')).toBeDefined();
    expect(screen.getByText('Who is online')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('is shown again for a new release', async () => {
    const operator = signedIn('Admin');
    window.localStorage.setItem(`releaseNotes.seen.${operator.id}`, 'an-older-release');

    renderScreen(<ReleaseNotesDialog />, { user: operator });

    expect(await screen.findByText(/What's new/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('keeps the notes of the last five releases only', () => {
    // A summary of what changed recently, not a changelog. Adding a release
    // means dropping the oldest section — see the comment on RELEASE_ID.
    expect(releaseSections.length).toBeLessThanOrEqual(5);
  });
});
