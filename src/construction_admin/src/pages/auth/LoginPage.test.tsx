/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { installFakeNetwork, renderScreen } from '../../test/renderScreen';
import { LoginPage } from './LoginPage';

/** What the deployment says the download page is; each test sets its own. */
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

/**
 * The sign-in page links to the phone app's download page — when the deployment
 * says where that is, and not otherwise. Dead links on a page every worker sees
 * are worse than none, so a development machine or an installation that does
 * not host the app shows nothing.
 */
const SCREEN_TIMEOUT = 20_000;

const DOWNLOAD_LINK = /download the phone app|preuzmi aplikaciju za telefon/i;

beforeEach(() => {
  window.localStorage.clear();
  installFakeNetwork();
});

describe('LoginPage', () => {
  it('links to the app download page when one is configured', async () => {
    deployment.appDownloadUrl = 'https://example.test/downloads/';

    renderScreen(<LoginPage />, { route: '/login', path: '/login' });

    const link = await screen.findByRole('link', { name: DOWNLOAD_LINK });

    expect(link.getAttribute('href')).toBe('https://example.test/downloads/');
  }, SCREEN_TIMEOUT);

  it('shows no download link when none is configured', async () => {
    deployment.appDownloadUrl = '';

    renderScreen(<LoginPage />, { route: '/login', path: '/login' });

    // The page is there: wait for something that is always on it.
    await screen.findByRole('link', { name: /forgot password|zaboravljena lozinka/i });

    expect(screen.queryByRole('link', { name: DOWNLOAD_LINK })).toBeNull();
  }, SCREEN_TIMEOUT);
});
