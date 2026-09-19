import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';

import { authApi } from '../api/auth';
import { restoreSessionFromCookie, setSessionLostHandler } from '../api/client';
import {
  activityClock,
  purgeLegacySession,
  sessionFromAuthResponse,
  sessionStore,
  type Session,
} from '../api/session';
import { purgeLegacyUiStorage } from '../hooks/userScopedStorage';
import { queryClient } from '../queryClient';
import { AuthContext, type AuthContextValue } from './authContextInstance';

/** How often an open panel checks whether it has been left alone too long. */
const IDLE_CHECK_INTERVAL_MS = 30_000;

/** Interaction is recorded at most this often; every keystroke would be waste. */
const TOUCH_THROTTLE_MS = 15_000;

const ACTIVITY_EVENTS = ['pointerdown', 'keydown', 'wheel', 'touchstart'] as const;

/**
 * Tells the other tabs of this browser that who is signed in has changed.
 *
 * Each tab keeps its own copy of the session, so without this a sign-out in
 * one tab left the others working for up to fifteen minutes on the access token
 * they still held — and a sign-in as somebody else left them showing the
 * previous account until their next refresh quietly swapped the user under them.
 */
const AUTH_CHANNEL = 'construction.admin.auth';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null | undefined>(undefined);
  const channel = useRef<BroadcastChannel | null>(null);

  const signIn = useCallback(async (email: string, password: string) => {
    const response = await authApi.login(email, password);
    const next = sessionFromAuthResponse(response);
    sessionStore.write(next);
    activityClock.start();
    // React Query's cache keys don't carry a user id, so anything left over
    // from a previous account would otherwise render for this one until it
    // happened to refetch — a real cross-account data leak, not just a stale
    // UI flash.
    queryClient.clear();
    setSession(next);
    channel.current?.postMessage('changed');
  }, []);

  const signOut = useCallback(async () => {
    try {
      // No token to pass: the API reads the refresh cookie, revokes it and
      // clears it. Called unconditionally, because the cookie can outlive the
      // stored session — an idle timeout, or a browser that restored its tabs.
      await authApi.logout();
    } catch {
      // Local sign-out must always succeed; the token expires on its own.
    }

    sessionStore.clear();
    queryClient.clear();
    setSession(null);
    channel.current?.postMessage('changed');
  }, []);

  useEffect(() => {
    purgeLegacySession();
    purgeLegacyUiStorage();

    // A forced sign-out (refresh rejected) leaves cached queries for whoever
    // was signed in — clear them so the next person on this machine never
    // sees a screen still holding the previous account's data.
    setSessionLostHandler(() => {
      queryClient.clear();
      setSession(null);
    });

    let active = true;

    // Asks the refresh cookie — which every tab shares — who is signed in.
    const adopt = async () => {
      const restored = await restoreSessionFromCookie();

      if (active) {
        setSession(restored);
      }
    };

    if (typeof BroadcastChannel !== 'undefined') {
      channel.current = new BroadcastChannel(AUTH_CHANNEL);
      channel.current.onmessage = () => {
        // A tab with no session has nothing that could belong to the wrong
        // person — and may be a sign-in form somebody is halfway through.
        if (!sessionStore.read()) {
          return;
        }

        // Whatever this tab holds may now belong to the wrong person. Hide it
        // at once — `undefined` is the loading state — then re-ask the cookie.
        sessionStore.clear();
        queryClient.clear();
        setSession(undefined);
        void adopt();
      };
    }

    if (activityClock.isExpired()) {
      // Nobody has touched the panel in any tab for the idle limit, or ever —
      // the browser restored an old session, or the previous person walked
      // away. Sign out properly rather than just locally, so the refresh
      // cookie is revoked and no later tab can revive it.
      void signOut();
    } else {
      const stored = sessionStore.read();

      if (stored) {
        setSession(stored);
      } else {
        void adopt();
      }
    }

    return () => {
      active = false;
      channel.current?.close();
      channel.current = null;
    };
  }, [signOut]);

  const signedIn = !!session;

  useEffect(() => {
    if (!signedIn) {
      return;
    }

    let lastTouch = 0;

    const onActivity = () => {
      const now = Date.now();

      if (now - lastTouch >= TOUCH_THROTTLE_MS) {
        lastTouch = now;
        activityClock.touch();
      }
    };

    for (const event of ACTIVITY_EVENTS) {
      window.addEventListener(event, onActivity, { passive: true });
    }

    const timer = window.setInterval(() => {
      if (activityClock.isExpired()) {
        void signOut();
      }
    }, IDLE_CHECK_INTERVAL_MS);

    return () => {
      for (const event of ACTIVITY_EVENTS) {
        window.removeEventListener(event, onActivity);
      }

      window.clearInterval(timer);
    };
  }, [signedIn, signOut]);

  const refreshProfile = useCallback(async () => {
    const user = await authApi.currentUser();
    const current = sessionStore.read();

    if (current) {
      const updated = { ...current, user };
      sessionStore.write(updated);
      setSession(updated);
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session === undefined ? undefined : (session?.user ?? null),
      isAuthenticated: !!session,
      signIn,
      signOut,
      refreshProfile,
    }),
    [session, signIn, signOut, refreshProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
