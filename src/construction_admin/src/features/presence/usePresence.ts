import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';

import { presenceApi } from '../../api/presence';

/** How often an open, visible panel tab says it is still here. */
const HEARTBEAT_MS = 60_000;

/**
 * Tells the API this tab is open, once a minute while it is visible, naming the
 * screen (the API strips ids from it). A hidden tab sends nothing: leaving a
 * tab open overnight must not read as being online. Failures are ignored — the
 * ordinary API calls keep the account marked as active anyway.
 */
export function usePresenceHeartbeat(pathname: string, enabled: boolean) {
  useEffect(() => {
    if (!enabled) return undefined;

    const send = () => {
      if (document.visibilityState === 'visible') {
        presenceApi.heartbeat(pathname).catch(() => undefined);
      }
    };

    send();
    const timer = window.setInterval(send, HEARTBEAT_MS);
    document.addEventListener('visibilitychange', send);

    return () => {
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', send);
    };
  }, [pathname, enabled]);
}

export function useOnlineUsersQuery() {
  return useQuery({
    queryKey: ['presence', 'online'],
    queryFn: presenceApi.online,
    refetchInterval: 30_000,
  });
}

export function useUserSessionsQuery(days: number) {
  return useQuery({
    queryKey: ['presence', 'sessions', days],
    queryFn: () => presenceApi.sessions(days),
  });
}

export function useFailedLoginsQuery(days: number, enabled: boolean) {
  return useQuery({
    queryKey: ['presence', 'failedLogins', days],
    queryFn: () => presenceApi.failedLogins(days),
    enabled,
  });
}
