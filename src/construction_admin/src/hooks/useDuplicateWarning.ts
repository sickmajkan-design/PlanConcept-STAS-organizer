import { useQuery } from '@tanstack/react-query';

import { useDebouncedValue } from './useDebouncedValue';

export interface DuplicateCandidate {
  id: string;
  label: string;
  path: string;
}

const MIN_LENGTH = 3;
const DEBOUNCE_MS = 400;

/**
 * Looks for existing records whose name is close to what's being typed into
 * a create/edit form, reusing that resource's own search endpoint rather
 * than a dedicated duplicate-detection query — the same fuzzy match the
 * list page's search box already does is exactly what "does this already
 * exist" needs.
 *
 * A warning, not a block: the caller decides what to do with the matches
 * (typically an `Alert` the operator can read and still save past).
 */
export function useDuplicateWarning(
  search: (term: string) => Promise<DuplicateCandidate[]>,
  value: string,
  excludeId?: string,
): { candidates: DuplicateCandidate[]; isChecking: boolean } {
  const term = value.trim();
  const debounced = useDebouncedValue(term, DEBOUNCE_MS);
  const enabled = debounced.length >= MIN_LENGTH;

  const query = useQuery({
    queryKey: ['duplicate-warning', debounced],
    queryFn: () => search(debounced),
    enabled,
    staleTime: 10_000,
  });

  const candidates = (query.data ?? []).filter((c) => c.id !== excludeId);

  return { candidates, isChecking: enabled && query.isFetching };
}
