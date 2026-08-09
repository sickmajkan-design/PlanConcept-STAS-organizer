import { useSyncExternalStore } from 'react';

function subscribe(onChange: () => void): () => void {
  window.addEventListener('online', onChange);
  window.addEventListener('offline', onChange);

  return () => {
    window.removeEventListener('online', onChange);
    window.removeEventListener('offline', onChange);
  };
}

function getSnapshot(): boolean {
  return window.navigator.onLine;
}

/**
 * Whether the browser thinks it has a network.
 *
 * `useSyncExternalStore` rather than `useState` + `useEffect`: the flag can
 * flip between the first render and the effect that subscribes, and that gap
 * is exactly when it flips — the tab was restored from the background, or the
 * page finished loading over a connection that then dropped. The store form
 * re-reads the value on subscribe, so a change in the gap is not lost.
 *
 * What it means is asymmetric, and the UI is written around that: `false` is
 * reliable — the machine has no route out and nothing will succeed. `true`
 * only means an interface is up, which on a site with a captive-portal
 * wireless or a phone showing one bar can still be a connection to nowhere.
 * So `false` drives a banner, and `true` is never used to claim things work.
 */
export function useOnlineStatus(): boolean {
  // The server snapshot is only reached if this is ever server-rendered; the
  // honest answer there is "no idea", and optimistic is the less alarming
  // wrong answer for one paint.
  return useSyncExternalStore(subscribe, getSnapshot, () => true);
}
