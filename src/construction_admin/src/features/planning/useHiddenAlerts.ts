import { useCallback, useMemo, useState } from 'react';

import type { AwayItem, Conflict } from './planningLogic';

const MAX_REMEMBERED = 200;

/**
 * An alert is identified by what it says — who, what, which days — so a new problem is never hidden by the dismissal
 * of an old one. The last part of every key is the final day, which is how a dismissal that can no longer matter is dropped.
 */
export const conflictKey = (c: Conflict): string =>
  ['c', c.kind, c.person.id, c.projectId, c.otherProjectId ?? '', c.from, c.to].join('|');

export const awayKey = (i: AwayItem): string => ['a', i.person.id, i.projectId, i.type, i.from, i.to].join('|');

function read(storageKey: string): string[] {
  try {
    const parsed: unknown = JSON.parse(window.localStorage.getItem(storageKey) ?? '[]');
    return Array.isArray(parsed) ? parsed.filter((k): k is string => typeof k === 'string') : [];
  } catch {
    return [];
  }
}

/** The planning alerts this person has dismissed, remembered in this browser only. */
export function useHiddenAlerts(userId: string | undefined, today: string) {
  const storageKey = `planning.alerts.hidden.${userId ?? ''}`;
  const [keys, setKeys] = useState<string[]>(() => read(storageKey));

  const save = useCallback(
    (next: string[]) => {
      setKeys(next);
      try {
        window.localStorage.setItem(storageKey, JSON.stringify(next));
      } catch {
        /* the alert simply comes back next time */
      }
    },
    [storageKey],
  );

  const hide = (key: string) =>
    save([...keys.filter((k) => k !== key), key].filter((k) => (k.split('|').pop() ?? '') >= today).slice(-MAX_REMEMBERED));

  const restore = () => save([]);

  return { hidden: useMemo(() => new Set(keys), [keys]), hide, restore };
}
