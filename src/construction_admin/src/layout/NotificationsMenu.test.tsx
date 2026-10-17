/**
 * @vitest-environment jsdom
 */
import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../test/renderScreen';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const notification = (id: string, over: Record<string, unknown> = {}) => ({
  id,
  type: 'GeneralAnnouncement',
  title: `Title ${id}`,
  body: `Body ${id}`,
  dataJson: null,
  isRead: true,
  readAt: null,
  requiresAcknowledgment: false,
  acknowledgedAt: null,
  createdAt: '2026-10-16T08:00:00Z',
  ...over,
});

const page = (items: unknown[], pageNumber: number, totalPages: number) => ({
  items,
  pageNumber,
  pageSize: 15,
  totalCount: 16,
  totalPages,
});

async function renderMenu() {
  const { NotificationsMenu } = await import('./NotificationsMenu');
  return renderScreen(<NotificationsMenu />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('NotificationsMenu', () => {
  it('goes through every notification to the last one, and says it is the last', async () => {
    network.reply('/notifications/unread-count', 200, 0);
    network.reply('/notifications', 200, page([notification('first')], 1, 2));
    network.reply('/notifications', 200, page([notification('last')], 2, 2));

    await renderMenu();
    await userEvent.click(await screen.findByRole('button', { name: 'Notifications' }));

    expect(await screen.findByText('Title first')).toBeDefined();
    expect(screen.queryByText('That is all of them.')).toBeNull();

    // Reaching the bottom of the list brings in the next page.
    fireEvent.scroll(document.querySelector('.MuiMenu-paper') as HTMLElement);

    expect(await screen.findByText('Title last')).toBeDefined();
    expect(await screen.findByText('That is all of them.')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('opens a notification that points nowhere in full, so it can be read to its end', async () => {
    const long = 'First line.\nSecond line.\nThe very last line of the announcement.';
    network.reply('/notifications/unread-count', 200, 1);
    network.reply('/notifications', 200, page([notification('a', { title: 'Site closed', body: long, isRead: false })], 1, 1));
    network.reply('/notifications/a/read', 204);

    await renderMenu();
    await userEvent.click(await screen.findByRole('button', { name: 'Notifications' }));
    await userEvent.click(await screen.findByText('Site closed'));

    const dialog = await screen.findByRole('dialog');
    await waitFor(() => expect(dialog.textContent).toContain('The very last line of the announcement.'));
    expect(dialog.textContent).toContain('Site closed');
  }, SCREEN_TIMEOUT);
});
