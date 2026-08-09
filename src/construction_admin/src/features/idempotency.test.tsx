/**
 * @vitest-environment jsdom
 */
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, type FakeNetwork } from '../test/renderScreen';
import { useResourceMutation } from './resourceQueries';

/**
 * The lifetime of an idempotency key, which is the whole of the client's half
 * of the feature.
 *
 * The server can only tell a retry from a second action by the key, so a key
 * minted per call protects nothing: two presses of "Save" after a lost
 * response would carry two keys and the stock would move twice. It has to
 * survive a failure and change on a success, and both halves are easy to break
 * with a one-line edit that looks like a tidy-up.
 */
let network: FakeNetwork;

beforeEach(() => {
  network = installFakeNetwork();
});

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

/** The keys the hook handed to its mutation function, in order. */
function useRecordingMutation(seen: string[], fail: { current: boolean }) {
  return useResourceMutation<void, void>(async (_variables, key) => {
    seen.push(key);

    if (fail.current) {
      throw new Error('network');
    }
  }, []);
}

describe('the idempotency key a write carries', () => {
  it('stays the same while the write keeps failing', async () => {
    // The case the feature exists for: the request landed, the response did
    // not, and the operator presses the button again. A new key here would
    // read as a second stock movement.
    const seen: string[] = [];
    const fail = { current: true };

    const { result } = renderHook(() => useRecordingMutation(seen, fail), { wrapper });

    await act(async () => {
      await result.current.mutateAsync().catch(() => {});
      await result.current.mutateAsync().catch(() => {});
      await result.current.mutateAsync().catch(() => {});
    });

    expect(seen).toHaveLength(3);
    expect(new Set(seen).size).toBe(1);
  });

  it('changes once a write succeeds', async () => {
    // The other half. Reusing the key after a success would have the *next*
    // adjustment answered with the previous one's stored response — a movement
    // that reports success and never happens.
    const seen: string[] = [];
    const fail = { current: false };

    const { result } = renderHook(() => useRecordingMutation(seen, fail), { wrapper });

    await act(async () => {
      await result.current.mutateAsync();
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    await act(async () => {
      await result.current.mutateAsync();
    });

    expect(seen).toHaveLength(2);
    expect(seen[0]).not.toBe(seen[1]);
  });

  it('is a value the server will accept', () => {
    const seen: string[] = [];
    const fail = { current: true };

    renderHook(() => useRecordingMutation(seen, fail), { wrapper });

    // The API refuses anything shorter than 8 characters rather than ignoring
    // it, so a key that fell to a short fallback would fail loudly — but only
    // in production. Pinned here instead.
    const key = crypto.randomUUID().replace(/-/g, '');

    expect(key).toHaveLength(32);
    expect(key).toMatch(/^[0-9a-f]+$/);
  });
});

/**
 * Imported inside the test, after `renderScreen` has installed the fake
 * adapter on `axios.defaults`. `axios.create` copies the adapter at creation
 * time, and the API client is created the moment `api/client` is evaluated —
 * a top-level import here would build it against the real one.
 */
const materials = () => import('../api/materials').then((m) => m.materialsApi);

describe('the stock adjustment request', () => {
  it('puts the key on the wire', async () => {
    // Threading the key through the api function is the step that is silently
    // droppable: everything still compiles and the header simply is not there.
    network.reply('/materials/', 200, { id: 'm1', quantity: 60 });

    await (await materials()).adjust('m1', { change: -40 }, 'key-that-is-long-enough');

    const call = network.calls.at(-1)!;

    expect(call.method).toBe('POST');
    expect(call.headers['idempotency-key']).toBe('key-that-is-long-enough');
  });

  it('sends no key header when none was given', async () => {
    network.reply('/materials/', 200, { id: 'm1', quantity: 60 });

    await (await materials()).adjust('m1', { change: -40 });

    expect(network.calls.at(-1)!.headers['idempotency-key']).toBeUndefined();
  });
});
