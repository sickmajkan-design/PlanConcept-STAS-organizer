import { useCallback, useRef, useState } from 'react';

/**
 * Measures an element's actual rendered pixel box via ResizeObserver. Used
 * to hand MUI X Charts an explicit width/height instead of letting it infer
 * one from a flex ancestor — inside these widgets' nested flex-1 sections
 * (a chart's box height comes from how much room the user's resize left it),
 * the chart's own internal auto-sizing sometimes measures a stale 0x0 on
 * first paint and corrupts its geometry, so charts here are always given a
 * real number instead of `undefined`/auto.
 *
 * A callback ref, not `useRef` + a one-time `useEffect([])`: several of these
 * chart boxes only mount once their data finishes loading (an earlier render
 * shows "no data yet" text in that same spot instead). A `useEffect([])`
 * attaches its ResizeObserver to whatever the ref points to on the very
 * first commit only — if that first commit is the text branch, the ref is
 * null forever and the effect never runs again to notice the chart box
 * mounting later. A callback ref re-fires on every attach/detach, including
 * that first appearance, so it can't miss it.
 */
export function useElementSize<T extends HTMLElement>() {
  const [size, setSize] = useState({ width: 0, height: 0 });
  const observerRef = useRef<ResizeObserver | null>(null);

  const ref = useCallback((node: T | null) => {
    observerRef.current?.disconnect();
    observerRef.current = null;

    if (!node) return;

    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (!entry) return;
      const { width, height } = entry.contentRect;
      setSize({ width: Math.round(width), height: Math.round(height) });
    });
    observer.observe(node);
    observerRef.current = observer;
  }, []);

  return { ref, width: size.width, height: size.height };
}
