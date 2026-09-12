import { useLocation } from 'react-router-dom';

interface SiblingNavState {
  siblingIds?: string[];
}

/**
 * Reads the id list a list page passed via `navigate(path, { state })` when
 * the operator opened this record, and works out what "previous" and "next"
 * mean from it.
 *
 * Router state rather than a fetch: the list page already has these ids in
 * memory in the order it is showing them, and re-fetching the whole list here
 * just to recover an order the caller already knew would repeat the same
 * "load everything to get one thing" mistake the Ledger crash taught. The
 * cost is that arrows only appear when a record was reached by clicking a
 * list row — not via a direct link, a refresh, or global search — which is
 * an acceptable gap for a convenience feature.
 */
export function useSiblingNavigation(currentId: string | undefined): {
  prevId: string | null;
  nextId: string | null;
  siblingIds: string[] | null;
} {
  const location = useLocation();
  const siblingIds = (location.state as SiblingNavState | null)?.siblingIds ?? null;

  if (!siblingIds || !currentId) {
    return { prevId: null, nextId: null, siblingIds: null };
  }

  const index = siblingIds.indexOf(currentId);

  if (index === -1) {
    return { prevId: null, nextId: null, siblingIds: null };
  }

  return {
    prevId: index > 0 ? siblingIds[index - 1] : null,
    nextId: index < siblingIds.length - 1 ? siblingIds[index + 1] : null,
    siblingIds,
  };
}
