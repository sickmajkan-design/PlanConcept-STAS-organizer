/** @vitest-environment jsdom */
import { cleanup, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { LoginAttempts } from '../../api/system';
import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';
import { ServerLoginsCard } from './ServerLoginsCard';

let network: FakeNetwork;

const days = (counts: number[]) => counts.map((count, i) => ({ day: `2026-11-${String(9 + i).padStart(2, '0')}`, count }));

const attempts = (over: Partial<LoginAttempts> = {}): LoginAttempts => ({
  last24Hours: 0,
  last7Days: 0,
  days: days([0, 0, 0, 0, 0, 0, 0]),
  reasons: [],
  topIps: [],
  topAccounts: [],
  ssh: { known: false, bannedNow: 0, bannedTotal: 0, failedTotal: 0, bannedIps: [] },
  ...over,
});

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
  network = installFakeNetwork();
});

afterEach(() => cleanup());

describe('ServerLoginsCard', () => {
  it('says plainly that nothing failed, and that the server has not reported fail2ban yet', async () => {
    network.reply('/system/login-attempts', 200, attempts());

    renderScreen(<ServerLoginsCard />);

    expect(await screen.findByText('No failed sign-ins in the last 7 days.')).toBeDefined();
    expect(screen.getByText('The server has not reported the state of fail2ban yet.')).toBeDefined();
  });

  it('lists the reasons, the addresses (flagging a guessing run) and the blocked SSH addresses', async () => {
    network.reply(
      '/system/login-attempts',
      200,
      attempts({
        last24Hours: 12,
        last7Days: 30,
        days: days([1, 2, 3, 4, 5, 6, 9]),
        reasons: [
          { reason: 'WrongPassword', count: 20 },
          { reason: 'UnknownAccount', count: 10 },
        ],
        topIps: [{ ip: '203.0.113.7', count: 25, accounts: 6, lastAtUtc: '2026-11-15T10:00:00Z' }],
        topAccounts: [{ email: 'marko@firma.test', count: 4 }],
        ssh: { known: true, bannedNow: 2, bannedTotal: 15, failedTotal: 340, bannedIps: ['1.2.3.4', '5.6.7.8'] },
      }),
    );

    renderScreen(<ServerLoginsCard />);

    expect(await screen.findByText('Wrong password: 20')).toBeDefined();
    expect(screen.getByText('Unknown account: 10')).toBeDefined();
    expect(screen.getByText('203.0.113.7')).toBeDefined();
    expect(screen.getByText('marko@firma.test')).toBeDefined();
    expect(screen.getByText('fail2ban has blocked 15 addresses in total and seen 340 failed attempts.')).toBeDefined();
    expect(screen.getByText('1.2.3.4')).toBeDefined();
  });
});
