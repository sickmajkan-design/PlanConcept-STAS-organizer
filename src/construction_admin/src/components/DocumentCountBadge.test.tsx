/** @vitest-environment jsdom */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, type FakeNetwork } from '../test/renderScreen';
import { DocumentCountBadge } from './DocumentCountBadge';

/** The number beside a list row's buttons comes from one request for the whole list. */
let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

describe('DocumentCountBadge', () => {
  it('shows each row its own number, from a single request', async () => {
    network.reply('/attachments/counts', 200, [
      { ownerId: 'a', count: 3, expired: 0 },
      { ownerId: 'b', count: 1, expired: 1 },
    ]);

    renderScreen(
      <>
        <DocumentCountBadge ownerType="Employee" ownerId="a" />
        <DocumentCountBadge ownerType="Employee" ownerId="b" />
        <DocumentCountBadge ownerType="Employee" ownerId="c" />
      </>,
    );

    const badges = await screen.findAllByTestId('document-count');

    expect(badges.map((b) => b.textContent)).toEqual(['3', '1', '0']);
    expect(network.calls.filter((c) => c.url.includes('/attachments/counts'))).toHaveLength(1);
  });

  it('shows nothing when the account may not see documents', async () => {
    network.reply('/attachments/counts', 403, { title: 'Forbidden' });

    renderScreen(<DocumentCountBadge ownerType="Employee" ownerId="a" />);

    await new Promise((resolve) => setTimeout(resolve, 200));

    expect(screen.queryByTestId('document-count')).toBeNull();
  });
});
