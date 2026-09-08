/** @vitest-environment jsdom */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';
import { AssistantLauncher } from './AssistantLauncher';

/**
 * The assistant as it is actually used: pressed, typed into, and read.
 *
 * The two things worth holding here are the ones a demo would not catch. The
 * launcher must disappear entirely on an installation with no API key, because
 * that is the normal state rather than a fault. And a failed question must stay
 * in the thread next to the question that caused it, rather than vanishing into
 * a toast — the office is looking at a number they are about to act on.
 */
const SCREEN_TIMEOUT = 20_000;

let network: FakeNetwork;

function enabled() {
  network.reply('/assistant/status', 200, { enabled: true });
}

async function open() {
  await userEvent.click(await screen.findByRole('button', { name: 'Office assistant' }));
}

describe('the assistant panel', () => {
  beforeEach(() => {
    window.localStorage.clear();
    network = installFakeNetwork();
  });

  it(
    'shows no launcher at all when the installation has no assistant',
    async () => {
      network.reply('/assistant/status', 200, { enabled: false });

      renderScreen(<AssistantLauncher />);

      // Waited for rather than asserted immediately, so this cannot pass
      // merely because the status call had not resolved yet.
      await waitFor(() =>
        expect(network.calls.some((call) => call.url.includes('/assistant/status'))).toBe(true),
      );

      expect(screen.queryByRole('button', { name: 'Office assistant' })).toBeNull();
    },
    SCREEN_TIMEOUT,
  );

  it(
    'sends the typed question and shows the answer',
    async () => {
      enabled();
      network.reply('/assistant/chat', 200, {
        text: 'Marko je radio 168 sati.',
        toolsUsed: ['time_entry_summary'],
        truncated: false,
      });

      renderScreen(<AssistantLauncher />);
      await open();

      await userEvent.type(
        await screen.findByLabelText('Ask a question'),
        'Koliko sati je Marko radio u julu?',
      );
      await userEvent.click(screen.getByRole('button', { name: 'Send' }));

      expect(await screen.findByText('Marko je radio 168 sati.')).toBeTruthy();

      const chat = network.calls.find((call) => call.url.includes('/assistant/chat'));

      expect(chat).toBeTruthy();
      expect(chat!.body).toMatchObject({
        message: 'Koliko sati je Marko radio u julu?',
        history: [],
      });

      // Named so the reader can tell the figure was looked up, not recalled.
      expect(screen.getByText('Hours')).toBeTruthy();
    },
    SCREEN_TIMEOUT,
  );

  it(
    'carries the earlier turns into the next question',
    async () => {
      enabled();
      network.reply('/assistant/chat', 200, {
        text: '168.',
        toolsUsed: [],
        truncated: false,
      });

      renderScreen(<AssistantLauncher />);
      await open();

      const box = await screen.findByLabelText('Ask a question');

      await userEvent.type(box, 'Koliko sati u julu?');
      await userEvent.click(screen.getByRole('button', { name: 'Send' }));
      await screen.findByText('168.');

      await userEvent.type(box, 'A u avgustu?');
      await userEvent.click(screen.getByRole('button', { name: 'Send' }));

      await waitFor(() =>
        expect(
          network.calls.filter((call) => call.url.includes('/assistant/chat')),
        ).toHaveLength(2),
      );

      const second = network.calls
        .filter((call) => call.url.includes('/assistant/chat'))
        .at(-1)!;

      // Without this the assistant answers "a u avgustu?" with no idea what
      // the question is about.
      expect(second.body).toMatchObject({
        message: 'A u avgustu?',
        history: [
          { role: 'User', text: 'Koliko sati u julu?' },
          { role: 'Assistant', text: '168.' },
        ],
      });
    },
    SCREEN_TIMEOUT,
  );

  it(
    'keeps a failed question in the thread rather than losing it',
    async () => {
      enabled();
      network.reply('/assistant/chat', 503, {
        title: 'Service unavailable',
        detail: 'The assistant could not be reached. Please try again.',
        status: 503,
      });

      renderScreen(<AssistantLauncher />);
      await open();

      await userEvent.type(await screen.findByLabelText('Ask a question'), 'Koliko sati?');
      await userEvent.click(screen.getByRole('button', { name: 'Send' }));

      expect(
        await screen.findByText('The assistant could not be reached. Please try again.'),
      ).toBeTruthy();

      // And the question that caused it is still on screen above it.
      expect(screen.getByText('Koliko sati?')).toBeTruthy();
    },
    SCREEN_TIMEOUT,
  );

  it(
    'says so when the assistant ran out of lookups',
    async () => {
      enabled();
      network.reply('/assistant/chat', 200, {
        text: '',
        toolsUsed: ['list_projects'],
        truncated: true,
      });

      renderScreen(<AssistantLauncher />);
      await open();

      await userEvent.type(await screen.findByLabelText('Ask a question'), 'Sve o svemu?');
      await userEvent.click(screen.getByRole('button', { name: 'Send' }));

      // Otherwise an empty answer reads as "there is nothing", which is a
      // different and wrong statement about the company's records.
      expect(
        await screen.findByText(
          'The assistant ran out of lookups before it finished. Try asking something narrower.',
        ),
      ).toBeTruthy();
    },
    SCREEN_TIMEOUT,
  );

  it(
    'refuses to send an empty question',
    async () => {
      enabled();

      renderScreen(<AssistantLauncher />);
      await open();

      await screen.findByLabelText('Ask a question');

      expect(
        screen.getByRole('button', { name: 'Send' }).hasAttribute('disabled'),
      ).toBe(true);

      // The button being disabled is not the whole guard: Enter sends too, and
      // that path does not go past the button at all.
      await userEvent.type(screen.getByLabelText('Ask a question'), '   {Enter}');

      expect(network.calls.some((call) => call.url.includes('/assistant/chat'))).toBe(false);
    },
    SCREEN_TIMEOUT,
  );

  it(
    'clears the thread on request',
    async () => {
      enabled();
      network.reply('/assistant/chat', 200, { text: '168.', toolsUsed: [], truncated: false });

      renderScreen(<AssistantLauncher />);
      await open();

      await userEvent.type(await screen.findByLabelText('Ask a question'), 'Koliko sati?');
      await userEvent.click(screen.getByRole('button', { name: 'Send' }));
      await screen.findByText('168.');

      await userEvent.click(screen.getByRole('button', { name: 'Clear' }));

      expect(screen.queryByText('168.')).toBeNull();
      expect(screen.queryByText('Koliko sati?')).toBeNull();
    },
    SCREEN_TIMEOUT,
  );
});
