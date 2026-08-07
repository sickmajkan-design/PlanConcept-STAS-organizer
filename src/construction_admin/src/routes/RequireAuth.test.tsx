/**
 * @vitest-environment jsdom
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import type { ReactNode } from 'react';

import type { Role, User } from '../api/types';
import { AuthContext, type AuthContextValue } from '../auth/authContextInstance';
import { paths } from './paths';
import {
  RequireAccountAdmin,
  RequireAuth,
  RequireDirectoryAccess,
  RequireGuest,
  RequireLabourCostAccess,
} from './RequireAuth';

/**
 * The guards decide what each role can even reach.
 *
 * They mirror the API's policies rather than enforce them — the API refuses
 * the calls regardless. What they must not do is drift: too tight and a screen
 * vanishes for somebody entitled to it, which reads as a broken deployment;
 * too loose and they are sent to a page that answers 403.
 *
 * `user === undefined` is the third state and the one worth naming: the
 * session is still being read out of storage. Treating it as "signed out"
 * bounces a returning operator to the login screen on every reload.
 */
function signedIn(role: Role): User {
  return {
    id: '1',
    email: 'operator@example.test',
    role,
    employeeId: null,
    firstName: null,
    lastName: null,
    lastLoginAt: null,
  };
}

function withAuth(user: User | null | undefined, children: ReactNode) {
  const value: AuthContextValue = {
    user,
    isAuthenticated: !!user,
    signIn: async () => {},
    signOut: async () => {},
    refreshProfile: async () => {},
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Renders one guarded route at `/secret`, plus the two places a refusal lands.
 *
 * Home and login sit outside the guard on purpose. Nesting home inside it
 * would send a refused role to a route that refuses it again, and the test
 * would be measuring its own scaffolding.
 */
function renderGuard(
  Guard: () => ReactNode,
  user: User | null | undefined,
  startAt = '/secret',
) {
  return render(
    withAuth(
      user,
      <MemoryRouter initialEntries={[startAt]}>
        <Routes>
          <Route element={<Guard />}>
            <Route path="/secret" element={<div>the guarded screen</div>} />
          </Route>
          <Route path={paths.login} element={<div>the login screen</div>} />
          <Route path={paths.home} element={<div>the home screen</div>} />
        </Routes>
      </MemoryRouter>,
    ),
  );
}

const showing = (text: string) => screen.queryByText(text) !== null;

describe('RequireAuth', () => {
  it('lets a signed-in operator through', () => {
    renderGuard(RequireAuth, signedIn('Worker'));

    expect(showing('the guarded screen')).toBe(true);
  });

  it('sends an anonymous visitor to the login screen', () => {
    renderGuard(RequireAuth, null);

    expect(showing('the login screen')).toBe(true);
    expect(showing('the guarded screen')).toBe(false);
  });

  it('waits while the session is still being read', () => {
    // Not the login screen. The session lives in localStorage and is read in
    // an effect, so for one render nobody is signed in yet — bouncing here
    // would throw a returning operator out on every reload.
    renderGuard(RequireAuth, undefined);

    expect(showing('the login screen')).toBe(false);
    expect(showing('the guarded screen')).toBe(false);
  });
});

describe('RequireGuest', () => {
  it('lets an anonymous visitor reach the login screen', () => {
    renderGuard(RequireGuest, null);

    expect(showing('the guarded screen')).toBe(true);
  });

  it('sends a signed-in operator home instead', () => {
    renderGuard(RequireGuest, signedIn('Admin'));

    expect(showing('the home screen')).toBe(true);
  });

  it('renders nothing while the session is still being read', () => {
    renderGuard(RequireGuest, undefined);

    expect(showing('the guarded screen')).toBe(false);
    expect(showing('the home screen')).toBe(false);
  });
});

describe('the role guards', () => {
  const cases: [string, () => ReactNode, Role[], Role[]][] = [
    [
      'RequireDirectoryAccess',
      RequireDirectoryAccess,
      ['SuperAdmin', 'Admin', 'ProjectManager', 'Foreman'],
      ['Worker'],
    ],
    [
      'RequireAccountAdmin',
      RequireAccountAdmin,
      ['SuperAdmin', 'Admin'],
      ['ProjectManager', 'Foreman', 'Worker'],
    ],
    [
      'RequireLabourCostAccess',
      RequireLabourCostAccess,
      ['SuperAdmin', 'Admin', 'ProjectManager'],
      ['Foreman', 'Worker'],
    ],
  ];

  for (const [name, Guard, admitted, refused] of cases) {
    describe(name, () => {
      for (const role of admitted) {
        it(`lets ${role} through`, () => {
          renderGuard(Guard, signedIn(role));

          expect(showing('the guarded screen')).toBe(true);
        });
      }

      for (const role of refused) {
        it(`sends ${role} home`, () => {
          renderGuard(Guard, signedIn(role));

          expect(showing('the guarded screen')).toBe(false);
          expect(showing('the home screen')).toBe(true);
        });
      }

      it('refuses an anonymous visitor', () => {
        renderGuard(Guard, null);

        expect(showing('the guarded screen')).toBe(false);
      });

      it('waits while the session is still being read', () => {
        renderGuard(Guard, undefined);

        expect(showing('the guarded screen')).toBe(false);
        expect(showing('the home screen')).toBe(false);
      });
    });
  }
});
