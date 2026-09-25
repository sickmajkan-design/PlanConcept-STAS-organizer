/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn } from '../../test/renderScreen';
import { ReleaseNotesDialog } from './ReleaseNotesDialog';
import { RELEASE_ID } from './releaseNotes';

/** Where this installation says the phone app is downloaded; each test sets its own. */
const deployment = vi.hoisted(() => ({ appDownloadUrl: '' }));

vi.mock('../../config', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../config')>();

  return {
    ...actual,
    config: new Proxy(actual.config, {
      get: (target, key, receiver) =>
        key === 'appDownloadUrl' ? deployment.appDownloadUrl : Reflect.get(target, key, receiver),
    }),
  };
});

beforeEach(() => {
  deployment.appDownloadUrl = '';
  window.localStorage.clear();
  installFakeNetwork();
});

const SCREEN_TIMEOUT = 20_000;

describe('ReleaseNotesDialog', () => {
  it('tells an account what is new, once, and remembers it was told', async () => {
    const user = userEvent.setup();
    const operator = signedIn('Worker');

    const { unmount } = renderScreen(<ReleaseNotesDialog />, { user: operator });

    expect(await screen.findByText(/What's new/)).toBeDefined();
    expect(screen.getByText(/the cards no longer jitter/)).toBeDefined();

    await user.click(screen.getByRole('button', { name: 'Got it' }));
    await waitFor(() => expect(screen.queryByText(/What's new/)).toBeNull());
    expect(window.localStorage.getItem(`releaseNotes.seen.${operator.id}`)).toBe(RELEASE_ID);

    // The next sign-in in this browser: nothing to show.
    unmount();
    renderScreen(<ReleaseNotesDialog />, { user: operator });
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(screen.queryByText(/What's new/)).toBeNull();
  }, SCREEN_TIMEOUT);

  it('shows only the parts that concern the account', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('Worker') });

    await screen.findByText(/What's new/);
    expect(screen.queryByText('Important for administrators and managers')).toBeNull();
    expect(screen.queryByText('New in finances')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('warns an administrator that finance moved behind a right, without the new finance features they cannot use', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('Admin') });

    await screen.findByText('Important for administrators and managers');
    expect(screen.getByText(/ask a SuperAdmin/)).toBeDefined();
    expect(screen.queryByText('New in finances')).toBeNull();
  }, SCREEN_TIMEOUT);

  it('tells an account that can see finances what is new there', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('SuperAdmin') });

    await screen.findByText('New in finances');
    expect(screen.getByText('Important for administrators and managers')).toBeDefined();
    expect(screen.getByText('Fixed')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('is shown again for a new release', async () => {
    const operator = signedIn('Worker');
    window.localStorage.setItem(`releaseNotes.seen.${operator.id}`, 'an-older-release');

    renderScreen(<ReleaseNotesDialog />, { user: operator });

    expect(await screen.findByText(/What's new/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('names the address the phone app is downloaded from, as a link, to an administrator', async () => {
    deployment.appDownloadUrl = 'https://example.test/downloads/';

    renderScreen(<ReleaseNotesDialog />, { user: signedIn('SuperAdmin') });

    const link = await screen.findByRole('link', { name: 'https://example.test/downloads/' });

    expect(link.getAttribute('href')).toBe('https://example.test/downloads/');
  }, SCREEN_TIMEOUT);

  it('says nothing about a download address this installation does not have', async () => {
    renderScreen(<ReleaseNotesDialog />, { user: signedIn('SuperAdmin') });

    await screen.findByText('Reminders');

    expect(screen.queryByText(/Download the phone app/)).toBeNull();
  }, SCREEN_TIMEOUT);
});
