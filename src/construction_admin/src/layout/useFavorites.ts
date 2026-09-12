import { useCallback, useState } from 'react';

const FAVORITES_STORAGE_KEY = 'nav.favorites';
const MAX_FAVORITES = 8;

function readStoredFavorites(): string[] {
  try {
    const raw = localStorage.getItem(FAVORITES_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

function writeStoredFavorites(paths: string[]) {
  try {
    localStorage.setItem(FAVORITES_STORAGE_KEY, JSON.stringify(paths));
  } catch {
    // best-effort persistence only
  }
}

/** Per-browser pinned pages, most-recently-pinned first, capped so the pinned row never crowds out the rest of the nav. */
export function useFavorites() {
  const [favorites, setFavorites] = useState<string[]>(readStoredFavorites);

  const isFavorite = useCallback((path: string) => favorites.includes(path), [favorites]);

  const toggleFavorite = useCallback((path: string) => {
    setFavorites((prev) => {
      const next = prev.includes(path)
        ? prev.filter((p) => p !== path)
        : [path, ...prev].slice(0, MAX_FAVORITES);
      writeStoredFavorites(next);
      return next;
    });
  }, []);

  return { favorites, isFavorite, toggleFavorite };
}
