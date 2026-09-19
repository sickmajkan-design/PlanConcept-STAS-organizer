import { useEffect, useState } from 'react';

import { useAuth } from '../auth/useAuth';
import { readScoped, storageScope, writeScoped } from './userScopedStorage';

export interface SavedView<TState> {
  id: string;
  name: string;
  state: TState;
}

function baseKey(pageKey: string): string {
  return `savedViews.${pageKey}`;
}

/**
 * Named snapshots of "whatever filters this list page currently has set",
 * per signed-in account, per page. The page decides what `TState` holds —
 * search text, a status/type filter, a sort model, anything it wants to be
 * able to jump straight back to — this hook only names, stores, and lists the
 * snapshots.
 *
 * Per account rather than per browser: a saved filter can carry a person's
 * name or a project, and on a shared machine the next sign-in must not see it.
 */
export function useSavedViews<TState>(pageKey: string) {
  const { user } = useAuth();
  const scope = storageScope(user);
  const [views, setViews] = useState<SavedView<TState>[]>(() =>
    readScoped<SavedView<TState>[]>(scope, baseKey(pageKey), []),
  );

  useEffect(() => {
    setViews(readScoped<SavedView<TState>[]>(scope, baseKey(pageKey), []));
  }, [scope, pageKey]);

  const saveView = (name: string, state: TState) => {
    setViews((prev) => {
      const next = [...prev, { id: crypto.randomUUID(), name, state }];
      writeScoped(scope, baseKey(pageKey), next);
      return next;
    });
  };

  const deleteView = (id: string) => {
    setViews((prev) => {
      const next = prev.filter((v) => v.id !== id);
      writeScoped(scope, baseKey(pageKey), next);
      return next;
    });
  };

  return { views, saveView, deleteView };
}
