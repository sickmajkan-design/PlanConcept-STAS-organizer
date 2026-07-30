import { createContext } from 'react';

import type { User } from '../api/types';

export interface AuthContextValue {
  /** `undefined` while the stored session is still being validated. */
  user: User | null | undefined;
  isAuthenticated: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
