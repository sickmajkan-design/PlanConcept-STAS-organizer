import { Box, CircularProgress } from '@mui/material';
import { Navigate, Outlet, useLocation } from 'react-router-dom';

import { canSeeLabourCost } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { paths } from './paths';

/** Blocks anonymous access; shows a spinner while the session is restored. */
export function RequireAuth() {
  const { isAuthenticated, user } = useAuth();
  const location = useLocation();

  if (user === undefined) {
    return (
      <Box
        sx={{
          display: 'flex',
          height: '100vh',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to={paths.login} state={{ from: location }} replace />;
  }

  return <Outlet />;
}

/** Keeps a signed-in operator off the anonymous-only screens. */
export function RequireGuest() {
  const { isAuthenticated, user } = useAuth();

  if (user === undefined) {
    return null;
  }

  if (isAuthenticated) {
    return <Navigate to={paths.home} replace />;
  }

  return <Outlet />;
}

/**
 * Restricts a route to the roles allowed to administer accounts.
 *
 * Mirrors the API's AdminAndAbove policy. This only hides the screens — the
 * API refuses the calls regardless, and additionally stops an administrator
 * acting on an account senior to their own.
 */
export function RequireAccountAdmin() {
  const { user } = useAuth();

  if (user === undefined) {
    return null;
  }

  const allowed = new Set(['SuperAdmin', 'Admin']);

  if (!user || !allowed.has(user.role)) {
    return <Navigate to={paths.home} replace />;
  }

  return <Outlet />;
}

/** Restricts a route to roles the API actually serves it to. */
export function RequireDirectoryAccess() {
  const { user } = useAuth();

  if (user === undefined) {
    return null;
  }

  const allowed = new Set(['SuperAdmin', 'Admin', 'ProjectManager', 'Foreman']);

  if (!user || !allowed.has(user.role)) {
    return <Navigate to={paths.home} replace />;
  }

  return <Outlet />;
}

/**
 * Restricts a route to the roles the API lets assign and remove crew
 * (its `ProjectManagerAndAbove` policy on the assignment endpoints).
 *
 * Tighter than {@link RequireDirectoryAccess}: a foreman reads the roster,
 * but moving people between sites is a staffing decision made above them.
 */
export function RequireProjectManagerAccess() {
  const { user } = useAuth();

  if (user === undefined) {
    return null;
  }

  const allowed = new Set(['SuperAdmin', 'Admin', 'ProjectManager']);

  if (!user || !allowed.has(user.role)) {
    return <Navigate to={paths.home} replace />;
  }

  return <Outlet />;
}

/**
 * Restricts a route to the roles the API shows pay rates to.
 *
 * Tighter than {@link RequireDirectoryAccess} on purpose: a rate is
 * effectively somebody's pay, and a foreman running a site has no business
 * with it. Hiding the screen is the courtesy; the API refuses the calls
 * regardless.
 */
export function RequireLabourCostAccess() {
  const { user } = useAuth();

  if (user === undefined) {
    return null;
  }

  if (!canSeeLabourCost(user)) {
    return <Navigate to={paths.home} replace />;
  }

  return <Outlet />;
}
