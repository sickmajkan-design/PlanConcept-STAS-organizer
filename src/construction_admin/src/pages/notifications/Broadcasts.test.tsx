/**
 * @vitest-environment jsdom
 */
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * An announcement is for the people several choices name, each once, and the office sees afterwards who has read and
 * confirmed it and can remind the others.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 20, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

function preview(overrides: Record<string, unknown> = {}) {
  return {
    people: [
      { employeeId: 'e1', userId: 'u1', name: 'Adin Čengić', role: 'Radnik', hasAccount: true, via: ['Grupa: Betoniranje'] },
      { employeeId: 'e2', userId: null, name: 'Cvijeta Test', role: 'Monter', hasAccount: false, via: ['Grupa: Betoniranje'] },
    ],
    chips: [{ kind: 'Group', id: 'g1', role: null, label: 'Grupa: Betoniranje', count: 2 }],
    total: 2,
    reachable: 1,
    withoutAccount: 1,
    ...overrides,
  };
}

async function renderNew() {
  network.reply('/notificationgroups', 200, page([{ id: 'g1', name: 'Betoniranje', memberCount: 2 }]));
  network.reply('/projects', 200, page([{ id: 'p1', name: 'Hala B' }]));
  network.reply('/employees', 200, page([{ id: 'e1', firstName: 'Adin', lastName: 'Čengić', status: 'Active' }]));
  network.reply('/branches', 200, []);
  network.reply('/notifications/audience-preview', 200, preview());
  network.reply('/notifications/broadcasts', 200, { broadcastId: 'b1', recipients: 1, withoutAccount: 1 });

  const { NewAnnouncementPage } = await import('./NewAnnouncementPage');

  return renderScreen(<NewAnnouncementPage />, { route: '/', path: '/', user: signedIn('Admin') });
}

describe('NewAnnouncementPage', () => {
  it('lists who a choice names and says who cannot be reached', async () => {
    await renderNew();

    await userEvent.click(await screen.findByRole('button', { name: /^(Grupa|Group)$/ }));
    await userEvent.click(await screen.findByRole('menuitem', { name: /Betoniranje/ }));

    expect(await screen.findByText('Adin Čengić')).toBeDefined();
    expect(screen.getAllByText(/(bez naloga|no account)/).length).toBeGreaterThan(0);
    expect(screen.getByText(/(nema korisnički nalog|nemaju korisnički nalog|has no account|have no account)/)).toBeDefined();
    // Only the one who has an account counts as a recipient.
    expect(await screen.findByRole('button', { name: /(Pošalji 1 osoba|Send to 1)/ })).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('will not send without a title, a message and a recipient', async () => {
    await renderNew();

    const send = await screen.findByRole('button', { name: /(Pošalji|Send to)/ });
    expect((send as HTMLButtonElement).disabled).toBe(true);
  }, SCREEN_TIMEOUT);

  it('sends the chosen targets with the message', async () => {
    await renderNew();

    await userEvent.click(await screen.findByRole('button', { name: /^(Grupa|Group)$/ }));
    await userEvent.click(await screen.findByRole('menuitem', { name: /Betoniranje/ }));
    await screen.findByText('Adin Čengić');

    await userEvent.type(screen.getByRole('textbox', { name: /(Naslov|Subject|Title)/ }), 'Sutra u 6:30');
    await userEvent.type(screen.getByRole('textbox', { name: /(Poruka|Message)/ }), 'Ispred kontejnera.');
    await userEvent.click(await screen.findByRole('button', { name: /(Pošalji 1 osoba|Send to 1)/ }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/notifications/broadcasts'));
    expect(posts).toHaveLength(1);
    const body = JSON.stringify(posts[0].body);
    expect(body).toContain('Sutra u 6:30');
    expect(body).toContain('"kind":"Group"');
    expect(body).toContain('"id":"g1"');
  }, SCREEN_TIMEOUT);

  it('asks before sending to everyone', async () => {
    await renderNew();

    await userEvent.click(await screen.findByRole('button', { name: /^(Svi|Everyone)$/ }));
    await screen.findByText('Adin Čengić');
    await userEvent.type(screen.getByRole('textbox', { name: /(Naslov|Subject|Title)/ }), 'Važno');
    await userEvent.type(screen.getByRole('textbox', { name: /(Poruka|Message)/ }), 'Svima.');
    await userEvent.click(await screen.findByRole('button', { name: /(Pošalji 1 osoba|Send to 1)/ }));

    expect(await screen.findByRole('dialog')).toBeDefined();
    expect(network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/notifications/broadcasts'))).toHaveLength(0);
  }, SCREEN_TIMEOUT);
});

function item(overrides: Record<string, unknown> = {}) {
  return {
    id: 'b1',
    title: 'Sutra se počinje u 6:30',
    preview: 'Zbog betoniranja sastanak je u 6:30.',
    audience: [{ kind: 'Project', id: 'p1', role: null, label: 'Projekat: Hala B', count: 5 }],
    sentAt: '2026-10-24T12:05:00Z',
    sentBy: 'darijo@firma.test',
    requiresAcknowledgment: true,
    recipients: 5,
    read: 4,
    acknowledged: 3,
    ...overrides,
  };
}

describe('BroadcastsListPage', () => {
  it('shows each announcement with how many have read and confirmed it', async () => {
    network.reply('/notifications/broadcasts', 200, page([item()]));
    const { BroadcastsListPage } = await import('./BroadcastsListPage');
    renderScreen(<BroadcastsListPage />, { route: '/', path: '/', user: signedIn('Admin') });

    expect(await screen.findByText('Sutra se počinje u 6:30')).toBeDefined();
    expect(screen.getByText('Projekat: Hala B')).toBeDefined();
    expect(screen.getByText(/4 (od|of) 5/)).toBeDefined();
    expect(screen.getByText(/3 (od|of) 5/)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('asks the server for only the announcements that need a confirmation', async () => {
    network.reply('/notifications/broadcasts', 200, page([item()]));
    const { BroadcastsListPage } = await import('./BroadcastsListPage');
    renderScreen(<BroadcastsListPage />, { route: '/', path: '/', user: signedIn('Admin') });
    await screen.findByText('Sutra se počinje u 6:30');

    await userEvent.click(screen.getByRole('button', { name: /^(Traže potvrdu|Ask for confirmation)$/ }));

    const asked = network.calls.filter((call) => call.method === 'GET' && call.url.endsWith('/notifications/broadcasts'));
    expect(asked.some((call) => String(call.params.requiresAcknowledgment) === 'true')).toBe(true);
  }, SCREEN_TIMEOUT);
});

function detail() {
  const person = (name: string, extra: Record<string, unknown> = {}) => ({
    userId: name,
    name,
    role: 'Radnik',
    via: 'Projekat: Hala B',
    hasDevice: true,
    isRead: false,
    readAt: null,
    acknowledgedAt: null,
    reminders: 0,
    ...extra,
  });

  return {
    ...item(),
    body: 'Zbog betoniranja sastanak je u 6:30 ispred kontejnera.',
    withoutAccount: 0,
    lastReminderAt: null,
    recipients: [
      person('Adin Čengić', { isRead: true, readAt: '2026-10-24T12:21:00Z', acknowledgedAt: '2026-10-24T12:22:00Z' }),
      person('Jovica Krstić', { isRead: true, readAt: '2026-10-24T13:02:00Z' }),
      person('Vladimir Radojčić', { hasDevice: false }),
    ],
  };
}

describe('BroadcastDetailPage', () => {
  async function renderDetail() {
    network.reply('/notifications/broadcasts/b1', 200, detail());
    network.reply('/notifications/broadcasts/b1/remind', 200, 2);
    const { BroadcastDetailPage } = await import('./BroadcastDetailPage');

    return renderScreen(<BroadcastDetailPage />, { route: '/notifications/sent/b1', path: '/notifications/sent/:id', user: signedIn('Admin') });
  }

  it('says per person whether they read and confirmed, and who only gets the bell', async () => {
    await renderDetail();

    expect(await screen.findByText('Adin Čengić')).toBeDefined();
    expect(screen.getByText('Jovica Krstić')).toBeDefined();
    expect(screen.getAllByText(/(Samo zvonce|Bell only)/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/(čeka se potvrda|waiting for confirmation)/)).toHaveLength(1);
  }, SCREEN_TIMEOUT);

  it('narrows to those who have not read', async () => {
    await renderDetail();
    await screen.findByText('Adin Čengić');

    await userEvent.click(screen.getByRole('button', { name: /(Nisu pročitali|Not read) \(1\)/ }));

    expect(screen.queryByText('Adin Čengić')).toBeNull();
    expect(screen.getByText('Vladimir Radojčić')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('reminds those who have not read or not confirmed, and says how many', async () => {
    await renderDetail();
    await screen.findByText('Adin Čengić');

    // One has not read and one has read without confirming.
    const remind = screen.getByRole('button', { name: /(Podsjeti|Remind) \(2\)/ });
    await userEvent.click(remind);

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.endsWith('/b1/remind'));
    expect(posts).toHaveLength(1);
    expect(await screen.findByText(/(poslat 2|sent to 2)/)).toBeDefined();
    expect(within(document.body).getAllByText(/Hala B/).length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);
});
