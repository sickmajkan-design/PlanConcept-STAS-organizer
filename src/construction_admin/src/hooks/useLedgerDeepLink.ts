import { useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';

import type { LedgerPeriod } from '../components/costs/LedgerPeriodBar';

/**
 * A cost ledger reached from a report line arrives as `?open=<id>&from=<day>&to=<day>`:
 * the period is narrowed to that day so the entry is on the first page, and the
 * entry opens by itself. Read once, when the page first renders.
 */
export function readLedgerDeepLinkPeriod(): LedgerPeriod | null {
  const params = new URLSearchParams(window.location.search);
  const from = params.get('from');
  const to = params.get('to');

  return from || to ? { from: from ?? '', to: to ?? '' } : null;
}

/** Opens the entry named by `?open=` as soon as the list that holds it has loaded, then drops the parameter. */
export function useOpenEntryFromLink<T extends { id: string }>(
  items: T[] | undefined,
  open: (item: T) => void,
) {
  const [params, setParams] = useSearchParams();
  const id = params.get('open');

  useEffect(() => {
    if (!id || !items) return;

    const item = items.find((candidate) => candidate.id === id);

    if (item) {
      open(item);
    }

    const next = new URLSearchParams(params);
    next.delete('open');
    setParams(next, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, items]);
}
