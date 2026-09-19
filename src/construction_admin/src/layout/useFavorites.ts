import { useCallback, useEffect, useState } from 'react';

import { useAuth } from '../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../hooks/userScopedStorage';

const FAVORITES_KEY = 'nav.favorites';
const MAX_FAVORITES = 8;

/** Pinned pages for the signed-in account, most-recently-pinned first, capped so the pinned row never crowds out the rest of the nav. */
export function useFavorites() {
  const { user } = useAuth();
  const scope = storageScope(user);
  const [favorites, setFavorites] = useState<string[]>(() =>
    readScoped<string[]>(scope, FAVORITES_KEY, []),
  );

  // A different account (or role) on the same mounted layout must not keep
  // showing the previous one's pins.
  useEffect(() => {
    setFavorites(readScoped<string[]>(scope, FAVORITES_KEY, []));
  }, [scope]);

  const isFavorite = useCallback((path: string) => favorites.includes(path), [favorites]);

  const toggleFavorite = useCallback(
    (path: string) => {
      setFavorites((prev) => {
        const next = prev.includes(path)
          ? prev.filter((p) => p !== path)
          : [path, ...prev].slice(0, MAX_FAVORITES);
        writeScoped(scope, FAVORITES_KEY, next);
        return next;
      });
    },
    [scope],
  );

  return { favorites, isFavorite, toggleFavorite };
}
