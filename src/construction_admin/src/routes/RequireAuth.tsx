import { Box, CircularProgress } from '@mui/material';
import { Navigate, Outlet, useLocation } from 'react-router-dom';

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
