import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';

const FADE_MS = 2200;

/**
 * Reads `?highlight=<id>` from the URL (as a notification deep-link lands a
 * user here) and briefly flags the matching row so `isHighlighted` can drive
 * a fading `bgcolor`, plus scrolls it into view once on arrival — the same
 * "flash and fade" ProjectDetailPage already does for `justCreated`, just
 * keyed off the URL instead of navigation state.
 */
export function useHighlightTarget(paramName = 'highlight') {
  const [searchParams] = useSearchParams();
  const targetId = searchParams.get(paramName);
  const [active, setActive] = useState(Boolean(targetId));
  const scrolledRef = useRef(false);

  useEffect(() => {
    if (!targetId) return;
    setActive(true);
    const timer = setTimeout(() => setActive(false), FADE_MS);
    return () => clearTimeout(timer);
  }, [targetId]);

  const scrollIntoViewOnce = (element: HTMLElement | null) => {
    if (!element || scrolledRef.current || !targetId) return;
    scrolledRef.current = true;
    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
  };

  return {
    targetId,
    isHighlighted: (id: string) => active && id === targetId,
    scrollIntoViewOnce,
  };
}
