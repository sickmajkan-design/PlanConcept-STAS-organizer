import { useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { useEffect, useMemo } from 'react';

import { useT } from '../../i18n/useI18n';
import { useBranchFilter } from '../branches/BranchContext';
import { dashboardApi, type DashboardSnapshots } from './api';

const QUERY_KEY = ['dashboard', 'snapshots'] as const;

/** How soon after a figure changes it is recorded; everything that changed in that time goes in one request. */
const RECORD_DELAY_MS = 2_000;

/** Which way a change is welcome: more is worse (things waiting), more is better (vehicles free), or neither. */
export type Better = 'up' | 'down' | 'none';

export interface SinceNote {
  text: string;
  tone: 'good' | 'bad' | 'neutral';
}

let queued: Record<string, number> = {};
let timer: ReturnType<typeof setTimeout> | undefined;

/**
 * Records figures the board is showing so tomorrow's board can say how they changed. Collected across every
 * widget on the page and sent together; a failed send is dropped, since the next change or visit records it
 * again and a missing baseline only means no note.
 */
function record(client: QueryClient, values: Record<string, number>) {
  queued = { ...queued, ...values };

  if (timer) {
    clearTimeout(timer);
  }

  timer = setTimeout(() => {
    const batch = queued;
    queued = {};
    timer = undefined;

    dashboardApi
      .recordSnapshots(batch)
      .then(() => {
        client.setQueryData<DashboardSnapshots>(QUERY_KEY, (current) =>
          current ? { ...current, today: { ...current.today, ...batch } } : current,
        );
      })
      .catch(() => undefined);
  }, RECORD_DELAY_MS);
}

/**
 * "Since yesterday" for counts on the board. Pass the figures as they are now, keyed by a name of your own
 * (undefined while they are still loading); the hook records them as today's and returns, per name, how much
 * they differ from what this account last saw yesterday.
 *
 * The scope is part of the key: with a business unit chosen in the header the figures are that unit's, and
 * compared only with the same unit's. No note means nothing to compare with (the first day, or a day the board
 * was not opened yesterday) or no change, never an error.
 */
export function useSinceYesterday(values: Record<string, number | undefined>) {
  const t = useT();
  const client = useQueryClient();
  const { branchId } = useBranchFilter();
  const scope = branchId ?? 'all';

  const snapshots = useQuery({ queryKey: QUERY_KEY, queryFn: dashboardApi.getSnapshots, staleTime: 5 * 60_000, retry: false });
  const today = snapshots.data?.today;

  // A stable signature, so the effect runs when a figure changes rather than on every render.
  const signature = Object.entries(values)
    .map(([key, value]) => `${key}=${value ?? ''}`)
    .join('&');

  useEffect(() => {
    if (!today) {
      return;
    }

    const changed: Record<string, number> = {};

    for (const [key, value] of Object.entries(values)) {
      const scoped = `${key}|${scope}`;

      if (value !== undefined && today[scoped] !== value) {
        changed[scoped] = value;
      }
    }

    if (Object.keys(changed).length > 0) {
      record(client, changed);
    }
    // `values` is covered by `signature`.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [today, signature, scope, client]);

  const yesterday = snapshots.data?.yesterday;

  return useMemo(
    () =>
      (key: string, better: Better = 'none'): SinceNote | null => {
        const now = values[key];
        const before = yesterday?.[`${key}|${scope}`];

        if (now === undefined || before === undefined || now === before) {
          return null;
        }

        const change = now - before;
        const sign = change > 0 ? '+' : '−';
        const good = better === 'none' ? false : (change > 0) === (better === 'up');

        return {
          text: t('dashboard.since', { change: `${sign}${Math.abs(change)}` }),
          tone: better === 'none' ? 'neutral' : good ? 'good' : 'bad',
        };
      },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [yesterday, scope, signature, t],
  );
}
