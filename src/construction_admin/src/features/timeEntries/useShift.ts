import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import { ApiError } from '../../api/apiError';
import { newIdempotencyKey } from '../../api/idempotency';
import { timeEntriesApi, type ClockInInput, type ClockOutInput } from '../../api/timeEntries';
import { useAuth } from '../../auth/useAuth';
import {
  loadCachedShift,
  loadFailed,
  loadOps,
  saveCachedShift,
  saveFailed,
  saveOps,
  viewShift,
  type FailedOp,
  type OutboxOp,
} from './shiftOutbox';
import { timeEntryKeys } from './useTimeEntries';

const shiftKey = [...timeEntryKeys.all, 'current'];

/** The signed-in person's running shift, or null. Polls, so a shift ended on the phone app shows here too. */
export function useCurrentShift() {
  return useQuery({ queryKey: shiftKey, queryFn: timeEntriesApi.current, refetchInterval: 60000 });
}

export function useClockInSites() {
  return useQuery({ queryKey: [...timeEntryKeys.all, 'clock-in-sites'], queryFn: timeEntriesApi.clockInSites, staleTime: 5 * 60000 });
}

/** True when the request never got an answer, so it may or may not have arrived: no signal, a lost reply, a timeout. */
function noAnswer(error: unknown): boolean {
  return !(error instanceof ApiError) || error.status === undefined || error.status === 408 || error.status >= 500 || error.status === 401 || error.status === 429;
}

/**
 * The shift, with a queue for when there is no signal: a start or an end pressed offline is kept on the phone with the
 * moment it happened and sent when the connection returns. Safari cannot send in the background, so sending happens
 * while the page is open, when it comes back online, and on a short timer.
 */
export function useShiftState() {
  const { user } = useAuth();
  const userId = user?.id ?? '';
  const queryClient = useQueryClient();
  const current = useCurrentShift();
  const sites = useClockInSites();
  const [ops, setOps] = useState<OutboxOp[]>(() => loadOps(userId));
  const [failed, setFailed] = useState<FailedOp[]>(() => loadFailed(userId));
  const [busy, setBusy] = useState(false);
  const flushing = useRef(false);

  useEffect(() => {
    if (userId && current.data !== undefined) saveCachedShift(userId, current.data);
  }, [userId, current.data]);

  // With no answer from the server the last known state stands, so a page reopened in a basement still knows a shift runs.
  const server = current.isError ? loadCachedShift(userId) : (current.data ?? null);
  const siteName = useCallback((id: string | null | undefined) => (id ? (sites.data?.find((s) => s.id === id)?.name ?? null) : null), [sites.data]);
  const shift = useMemo(() => viewShift(server, ops, siteName), [server, ops, siteName]);

  const queue = useCallback(
    (op: OutboxOp) => {
      const next = [...loadOps(userId), op];

      saveOps(userId, next);
      setOps(next);
    },
    [userId],
  );

  const flush = useCallback(async () => {
    if (!userId || flushing.current) return;

    flushing.current = true;

    try {
      let list = loadOps(userId);
      const lost: FailedOp[] = [];

      while (list.length > 0) {
        const op = list[0];

        try {
          if (op.kind === 'in') await timeEntriesApi.clockIn({ ...(op.input as ClockInInput), occurredAt: op.occurredAt }, op.id);
          else await timeEntriesApi.clockOut({ ...(op.input as ClockOutInput), occurredAt: op.occurredAt }, op.id);
        } catch (error) {
          if (noAnswer(error)) break;

          // 409: already in the state the action asked for (a shift is running / none is), so there is nothing to tell.
          if ((error as ApiError).status !== 409) {
            lost.push({ id: op.id, kind: op.kind, occurredAt: op.occurredAt, message: (error as Error).message });
          }
        }

        list = list.slice(1);
        saveOps(userId, list);
        setOps(list);
      }

      if (lost.length > 0) {
        const all = [...loadFailed(userId), ...lost];

        saveFailed(userId, all);
        setFailed(all);
      }

      await queryClient.invalidateQueries({ queryKey: timeEntryKeys.all });
    } finally {
      flushing.current = false;
    }
  }, [userId, queryClient]);

  useEffect(() => {
    if (ops.length === 0) return;

    void flush();
    const id = window.setInterval(() => void flush(), 20000);
    const again = () => void flush();

    window.addEventListener('online', again);
    document.addEventListener('visibilitychange', again);

    return () => {
      window.clearInterval(id);
      window.removeEventListener('online', again);
      document.removeEventListener('visibilitychange', again);
    };
  }, [ops.length, flush]);

  /** Sends now when there is signal; otherwise keeps it on the phone. Anything but "no answer" is the server's refusal and is thrown. */
  const run = async (kind: 'in' | 'out', input: ClockInInput | ClockOutInput) => {
    setBusy(true);

    try {
      // Anything still waiting goes first, or an end could reach the server before its start.
      if (ops.length === 0 && navigator.onLine !== false) {
        try {
          if (kind === 'in') await timeEntriesApi.clockIn(input as ClockInInput, newIdempotencyKey());
          else await timeEntriesApi.clockOut(input as ClockOutInput, newIdempotencyKey());
          await queryClient.invalidateQueries({ queryKey: timeEntryKeys.all });

          return;
        } catch (error) {
          if (!noAnswer(error)) throw error;
        }
      }

      queue({ id: newIdempotencyKey(), kind, occurredAt: new Date().toISOString(), input });
    } finally {
      setBusy(false);
    }
  };

  return {
    shift,
    loading: current.isLoading && ops.length === 0,
    loadError: current.isError && !loadCachedShift(userId) && ops.length === 0,
    queued: ops.length,
    failed,
    dismissFailed: (id: string) => {
      const rest = failed.filter((f) => f.id !== id);

      saveFailed(userId, rest);
      setFailed(rest);
    },
    busy,
    flush,
    start: (input: ClockInInput) => run('in', input),
    end: (input: ClockOutInput) => run('out', input),
  };
}
