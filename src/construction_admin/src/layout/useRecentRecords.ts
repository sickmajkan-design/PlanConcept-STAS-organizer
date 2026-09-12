import { useEffect } from 'react';

const RECENT_STORAGE_KEY = 'nav.recentRecords';
const MAX_RECENT = 6;

export interface RecentRecord {
  path: string;
  label: string;
}

export function readRecentRecords(): RecentRecord[] {
  try {
    const raw = localStorage.getItem(RECENT_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as RecentRecord[]) : [];
  } catch {
    return [];
  }
}

function writeRecentRecords(records: RecentRecord[]) {
  try {
    localStorage.setItem(RECENT_STORAGE_KEY, JSON.stringify(records));
  } catch {
    // best-effort persistence only
  }
}

/**
 * Call from a detail page once its record has loaded, to add it to the
 * global "recently viewed" list shown in the command palette.
 *
 * Reads/writes localStorage directly rather than shared React state: the
 * palette lives in `AppLayout`, an entirely different part of the tree, and
 * re-reads the list itself on every route change — there is no render this
 * hook needs to trigger, only a value that needs to be there by the time the
 * palette next opens.
 */
export function useRecordVisit(path: string, label: string | undefined): void {
  useEffect(() => {
    if (!label) return;
    const existing = readRecentRecords().filter((r) => r.path !== path);
    writeRecentRecords([{ path, label }, ...existing].slice(0, MAX_RECENT));
  }, [path, label]);
}
