import { useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';

/**
 * Runs `open` once when the page is reached with `?<name>=1` - how the command
 * palette's "create" actions land on a page that creates things in a dialog -
 * then removes the parameter so a refresh does not reopen it.
 */
export function useOpenOnParam(name: string, open: () => void) {
  const [params, setParams] = useSearchParams();
  const requested = params.get(name) === '1';

  useEffect(() => {
    if (!requested) return;

    open();
    const next = new URLSearchParams(params);
    next.delete(name);
    setParams(next, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [requested]);
}
