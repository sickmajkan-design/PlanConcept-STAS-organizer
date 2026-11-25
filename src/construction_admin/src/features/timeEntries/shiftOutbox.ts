import type { ClockInInput, ClockOutInput } from '../../api/timeEntries';

/**
 * A start or end of a shift that could not be sent because there was no signal. It waits on the phone and is sent,
 * with the moment it really happened, when the connection is back. The API accepts such a time for up to a day.
 */
export interface OutboxOp {
  /** The idempotency key it is sent with, so a retry after a lost reply is answered rather than run twice. */
  id: string;
  kind: 'in' | 'out';
  /** When the person pressed the button, ISO. */
  occurredAt: string;
  input: ClockInInput | ClockOutInput;
}

/** An action the server refused for good, kept so the person is told instead of it vanishing. */
export interface FailedOp {
  id: string;
  kind: 'in' | 'out';
  occurredAt: string;
  message: string;
}

/** The running shift as the screen shows it: the server's, with what is still waiting applied on top. */
export interface ShiftView {
  startedAt: string;
  projectName: string | null;
  /** True when it exists only on this phone so far. */
  pending: boolean;
}

const opsKey = (userId: string) => `shiftOutbox.v1.${userId}`;
const failedKey = (userId: string) => `shiftOutboxFailed.v1.${userId}`;
const cacheKey = (userId: string) => `shiftCache.v1.${userId}`;

function read<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);

    return raw ? (JSON.parse(raw) as T) : fallback;
  } catch {
    // Private mode or damaged text: an empty queue is better than a screen that cannot open.
    return fallback;
  }
}

function write(key: string, value: unknown) {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Storage full or blocked: nothing more can be done here.
  }
}

export const loadOps = (userId: string): OutboxOp[] => {
  const list = read<OutboxOp[]>(opsKey(userId), []);

  return Array.isArray(list) ? list.filter((o) => o && (o.kind === 'in' || o.kind === 'out') && typeof o.id === 'string' && typeof o.occurredAt === 'string') : [];
};
export const saveOps = (userId: string, ops: OutboxOp[]) => write(opsKey(userId), ops);

export const loadFailed = (userId: string): FailedOp[] => {
  const list = read<FailedOp[]>(failedKey(userId), []);

  return Array.isArray(list) ? list : [];
};
export const saveFailed = (userId: string, failed: FailedOp[]) => write(failedKey(userId), failed);

/** The last shift the server reported, so a page opened without signal still knows if a shift is running. */
export const loadCachedShift = (userId: string): { startedAt: string; projectName: string | null } | null =>
  read<{ startedAt: string; projectName: string | null } | null>(cacheKey(userId), null);
export const saveCachedShift = (userId: string, shift: { startedAt: string; projectName: string | null } | null) =>
  write(cacheKey(userId), shift);

/** Applies the waiting actions, oldest first, to what the server last said. */
export function viewShift(
  server: { startedAt: string; projectName: string | null } | null,
  ops: OutboxOp[],
  siteName: (projectId: string | null | undefined) => string | null,
): ShiftView | null {
  let view: ShiftView | null = server ? { startedAt: server.startedAt, projectName: server.projectName, pending: false } : null;

  for (const op of ops) {
    view = op.kind === 'in' ? { startedAt: op.occurredAt, projectName: siteName((op.input as ClockInInput).projectId), pending: true } : null;
  }

  return view;
}
