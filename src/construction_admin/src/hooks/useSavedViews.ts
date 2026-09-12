import { useState } from 'react';

export interface SavedView<TState> {
  id: string;
  name: string;
  state: TState;
}

function storageKey(pageKey: string): string {
  return `savedViews.${pageKey}`;
}

function readViews<TState>(pageKey: string): SavedView<TState>[] {
  try {
    const raw = localStorage.getItem(storageKey(pageKey));
    return raw ? (JSON.parse(raw) as SavedView<TState>[]) : [];
  } catch {
    return [];
  }
}

function writeViews<TState>(pageKey: string, views: SavedView<TState>[]) {
  try {
    localStorage.setItem(storageKey(pageKey), JSON.stringify(views));
  } catch {
    // best-effort persistence only
  }
}

/**
 * Named snapshots of "whatever filters this list page currently has set",
 * per browser, per page. The page decides what `TState` holds — search text,
 * a status/type filter, a sort model, anything it wants to be able to jump
 * straight back to — this hook only names, stores, and lists the snapshots.
 */
export function useSavedViews<TState>(pageKey: string) {
  const [views, setViews] = useState<SavedView<TState>[]>(() => readViews<TState>(pageKey));

  const saveView = (name: string, state: TState) => {
    setViews((prev) => {
      const next = [...prev, { id: crypto.randomUUID(), name, state }];
      writeViews(pageKey, next);
      return next;
    });
  };

  const deleteView = (id: string) => {
    setViews((prev) => {
      const next = prev.filter((v) => v.id !== id);
      writeViews(pageKey, next);
      return next;
    });
  };

  return { views, saveView, deleteView };
}
