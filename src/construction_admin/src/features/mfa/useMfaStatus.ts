import { useQuery } from '@tanstack/react-query';

import { authApi } from '../../api/auth';
import { useAuth } from '../../auth/useAuth';

/**
 * Whether this server can hold a second factor at all. Until it can, the panel
 * does not offer one: a menu entry that only ever answers "not set up" is worse
 * than none. Staff only — a portal customer has no such page.
 */
export function useMfaStatusQuery() {
  const { user } = useAuth();
  const isStaff = !!user && user.role !== 'Customer';

  return useQuery({
    queryKey: ['auth', 'mfa-status', user?.id],
    queryFn: () => authApi.mfaStatus(),
    enabled: isStaff,
    staleTime: 5 * 60_000,
  });
}
