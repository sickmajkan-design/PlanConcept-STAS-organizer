/**
 * A small global queue of "about to happen" deletes, each cancelable for a
 * few seconds before it actually runs — the client-side half of undo, for a
 * backend with no soft-delete or restore endpoint to lean on instead.
 *
 * Deliberately a plain module, not a hook or context: `useDeleteWithConfirm`
 * (called from ~20 unrelated list pages) needs to push into this queue, and
 * exactly one `UndoSnackbarHost`, mounted once in `AppLayout`, needs to read
 * it — a subscribe/notify singleton is the smallest thing that connects a
 * caller with no shared parent to a single UI surface, without wiring every
 * page through a context provider it does not otherwise need.
 */

export interface PendingUndo {
  id: string;
  message: string;
  execute: () => Promise<void>;
}

type Listener = (items: PendingUndo[]) => void;

const UNDO_WINDOW_MS = 6000;

let items: PendingUndo[] = [];
const listeners = new Set<Listener>();
const timers = new Map<string, ReturnType<typeof setTimeout>>();

function notify() {
  listeners.forEach((listener) => listener(items));
}

export function subscribeUndoQueue(listener: Listener): () => void {
  listeners.add(listener);
  listener(items);
  return () => listeners.delete(listener);
}

async function commit(id: string) {
  const item = items.find((i) => i.id === id);
  if (!item) return;

  items = items.filter((i) => i.id !== id);
  timers.delete(id);
  notify();

  try {
    await item.execute();
  } catch {
    // The mutation's own error state (surfaced by `useDeleteWithConfirm`'s
    // `error` return value, which every caller already renders) is what
    // reports this — nothing further to do with the rejection here.
  }
}

/** Queues a delete; returns the undo handle a caller could use to cancel it directly. */
export function scheduleUndoableDelete(message: string, execute: () => Promise<void>): string {
  const id = crypto.randomUUID();
  items = [...items, { id, message, execute }];
  notify();

  timers.set(
    id,
    setTimeout(() => {
      void commit(id);
    }, UNDO_WINDOW_MS),
  );

  return id;
}

export function undoDelete(id: string): void {
  const timer = timers.get(id);
  if (timer) clearTimeout(timer);
  timers.delete(id);
  items = items.filter((i) => i.id !== id);
  notify();
}
