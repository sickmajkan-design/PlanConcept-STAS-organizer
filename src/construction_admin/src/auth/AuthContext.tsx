import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';

import { authApi } from '../api/auth';
import { setSessionLostHandler } from '../api/client';
import {
  sessionFromAuthResponse,
  sessionStore,
  type Session,
} from '../api/session';
import { AuthContext, type AuthContextValue } from './authContextInstance';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null | undefined>(undefined);

  useEffect(() => {
    setSession(sessionStore.read());
    setSessionLostHandler(() => setSession(null));
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const response = await authApi.login(email, password);
    const next = sessionFromAuthResponse(response);
    sessionStore.write(next);
    setSession(next);
  }, []);

  const signOut = useCallback(async () => {
    try {
      // No token to pass: the API reads the refresh cookie and clears it.
      // Called unconditionally, because a cookie can outlive the stored
      // access token — a tab left open past fifteen minutes has no session
      // here and still has a live credential at the API.
      await authApi.logout();
    } catch {
      // Local sign-out must always succeed; the token expires on its own.
    }

    sessionStore.clear();
    setSession(null);
  }, []);

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
