import { useQuery } from '@tanstack/react-query';

import { absencesApi } from '../api/absences';
import { attachmentsApi } from '../api/attachments';
import type { User } from '../api/types';
import { canAdministerAccounts, canViewDirectory } from '../auth/authHelpers';

const DOCUMENT_EXPIRY_WINDOW_DAYS = 30;
/** These are convenience counters on a nav icon, not a live dashboard — a minute of staleness is fine. */
const STALE_TIME_MS = 60_000;

/** Small counts shown as a badge on a rail group icon, keyed by {@link NavGroup.key}. */
export function useNavBadgeCounts(user: User | null | undefined): Record<string, number> {
  const showDocuments = canAdministerAccounts(user);
  const showAbsences = canViewDirectory(user);

  const documentsQuery = useQuery({
    queryKey: ['nav-badge', 'documents-expiring'] as const,
    queryFn: () => attachmentsApi.expiring(DOCUMENT_EXPIRY_WINDOW_DAYS),
    enabled: showDocuments,
    staleTime: STALE_TIME_MS,
  });

  const absencesQuery = useQuery({
    queryKey: ['nav-badge', 'absences-pending'] as const,
    queryFn: () => absencesApi.list({ pageNumber: 1, pageSize: 1, status: 'Requested' }),
    enabled: showAbsences,
    staleTime: STALE_TIME_MS,
  });

  return {
    admin: showDocuments ? (documentsQuery.data?.length ?? 0) : 0,
    work: showAbsences ? (absencesQuery.data?.totalCount ?? 0) : 0,
  };
}
