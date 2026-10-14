import { createContext } from 'react';

import type { User } from '../api/types';

/**
 * What the password step led to: a finished sign-in, or a challenge that still
 * wants a code from the authenticator.
 */
export type SignInOutcome = { status: 'signedIn' } | { status: 'mfa'; mfaToken: string };

export interface AuthContextValue {
  /** `undefined` while the stored session is still being validated. */
  user: User | null | undefined;
  isAuthenticated: boolean;
  /** This account's role is expected to use a second factor and it has none yet. */
  mfaEnrollmentRequired: boolean;
  signIn: (email: string, password: string) => Promise<SignInOutcome>;
  /** Finishes a sign-in that stopped at a challenge. */
  completeMfa: (mfaToken: string, code: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
