/**
 * @vitest-environment jsdom
 */
import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import type { UseMutationResult } from '@tanstack/react-query';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { I18nProvider } from '../i18n/I18nProvider';
import { subscribeUndoQueue, undoDelete } from './undoQueue';
import { useDeleteWithConfirm } from './useDeleteWithConfirm';

const wrapper = ({ children }: { children: ReactNode }) => <I18nProvider>{children}</I18nProvider>;

function mutation(run: (id: string) => Promise<void>) {
  return { mutateAsync: run, isPending: false, isError: false, error: null } as unknown as UseMutationResult<void, unknown, string, unknown>;
}

afterEach(() => {
  // Whatever a test left waiting is cancelled, so the next one starts with an empty queue.
  const unsubscribe = subscribeUndoQueue((items) => items.forEach((item) => undoDelete(item.id)));
  unsubscribe();
  vi.useRealTimers();
});

describe('useDeleteWithConfirm', () => {
  it('asks first by default: the row is held until it is confirmed', () => {
    const run = vi.fn().mockResolvedValue(undefined);
    const { result } = renderHook(() => useDeleteWithConfirm<{ id: string }>(mutation(run)), { wrapper });

    act(() => result.current.request({ id: 'a' }));

    expect(result.current.pending).toEqual({ id: 'a' });
    expect(result.current.hiddenIds.size).toBe(0);
    expect(run).not.toHaveBeenCalled();
  });

  it('with immediate, asks nothing: the row is hidden at once, and Undo brings it back before anything runs', () => {
    vi.useFakeTimers();
    const run = vi.fn().mockResolvedValue(undefined);
    const { result } = renderHook(() => useDeleteWithConfirm<{ id: string }>(mutation(run), { immediate: true }), { wrapper });

    act(() => result.current.request({ id: 'a' }));

    expect(result.current.pending).toBeNull();
    expect(result.current.hiddenIds.has('a')).toBe(true);
    expect(run).not.toHaveBeenCalled();

    let waiting: { id: string }[] = [];
    subscribeUndoQueue((items) => {
      waiting = items;
    })();
    act(() => undoDelete(waiting[0]!.id));

    expect(result.current.hiddenIds.has('a')).toBe(false);

    act(() => vi.advanceTimersByTime(10_000));
    expect(run).not.toHaveBeenCalled();
  });

  it('with immediate, the delete runs once the undo window has passed', async () => {
    vi.useFakeTimers();
    const run = vi.fn().mockResolvedValue(undefined);
    const { result } = renderHook(() => useDeleteWithConfirm<{ id: string }>(mutation(run), { immediate: true }), { wrapper });

    act(() => result.current.request({ id: 'b' }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(7_000);
    });

    expect(run).toHaveBeenCalledWith('b');
    expect(result.current.hiddenIds.has('b')).toBe(false);
  });
});
