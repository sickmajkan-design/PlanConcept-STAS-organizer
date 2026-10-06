import {
  Box,
  Chip,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useAuth } from '../../auth/useAuth';
import { isSuperAdmin } from '../../auth/authHelpers';
import {
  useFailedLoginsQuery,
  useOnlineUsersQuery,
  useUserSessionsQuery,
} from '../../features/presence/usePresence';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';

const DAY_OPTIONS = [1, 7, 30];

/**
 * Who is using the platform right now, and who signed in recently.
 *
 * The live list comes from the API's memory, not the database, and refreshes
 * itself every 30 seconds. SuperAdmin accounts never appear in it; a SuperAdmin
 * sees other SuperAdmins' sign-ins only in the history below.
 */
export function PresencePage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const [days, setDays] = useState(7);

  const online = useOnlineUsersQuery();
  const sessions = useUserSessionsQuery(days);
  const { user } = useAuth();
  const canSeeFailures = isSuperAdmin(user);
  const failures = useFailedLoginsQuery(days, canSeeFailures);

  const clientLabel = (client: string) =>
    client === 'app' ? t('presence.clientApp') : t('presence.clientWeb');

  return (
    <Box>
      <PageHeader
        title={t('presence.title')}
        description={t('presence.description')}
        subtitle={online.data ? t('presence.onlineCount', { count: online.data.length }) : undefined}
      />

      {online.isError ? (
        <ErrorState error={online.error} onRetry={() => void online.refetch()} />
      ) : (
        <TableContainer component={Paper} variant="outlined" sx={{ mb: 4 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('presence.user')}</TableCell>
                <TableCell>{t('presence.role')}</TableCell>
                <TableCell>{t('presence.client')}</TableCell>
                <TableCell>{t('presence.screen')}</TableCell>
                <TableCell>{t('presence.signedInAt')}</TableCell>
                <TableCell>{t('presence.lastSeen')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(online.data ?? []).map((row) => (
                <TableRow key={row.userId}>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {row.name}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {row.email}
                    </Typography>
                  </TableCell>
                  <TableCell>{enumLabel('role', row.role)}</TableCell>
                  <TableCell>
                    <Chip size="small" label={clientLabel(row.client)} />
                  </TableCell>
                  <TableCell>{row.screen ?? '—'}</TableCell>
                  <TableCell>{row.signedInAt ? formatDateTime(row.signedInAt) : '—'}</TableCell>
                  <TableCell>{formatDateTime(row.lastSeenAt)}</TableCell>
                </TableRow>
              ))}
              {online.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                      {t('presence.nobodyOnline')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Stack direction="row" sx={{ mb: 2, alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6">{t('presence.sessionsTitle')}</Typography>
        <TextField
          select
          size="small"
          label={t('presence.period')}
          value={days}
          onChange={(event) => setDays(Number(event.target.value))}
          sx={{ minWidth: 160 }}
        >
          {DAY_OPTIONS.map((option) => (
            <MenuItem key={option} value={option}>
              {t('presence.lastDays', { count: option })}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {sessions.isError ? (
        <ErrorState error={sessions.error} onRetry={() => void sessions.refetch()} />
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('presence.user')}</TableCell>
                <TableCell>{t('presence.role')}</TableCell>
                <TableCell>{t('presence.client')}</TableCell>
                <TableCell>{t('presence.ip')}</TableCell>
                <TableCell>{t('presence.signedInAt')}</TableCell>
                <TableCell>{t('presence.lastSeen')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(sessions.data ?? []).map((row) => (
                <TableRow key={row.id}>
                  <TableCell>{row.name}</TableCell>
                  <TableCell>{enumLabel('role', row.role)}</TableCell>
                  <TableCell>{clientLabel(row.client)}</TableCell>
                  <TableCell>{row.ipAddress ?? '—'}</TableCell>
                  <TableCell>{formatDateTime(row.startedAt)}</TableCell>
                  <TableCell>{formatDateTime(row.lastSeenAt)}</TableCell>
                </TableRow>
              ))}
              {sessions.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                      {t('presence.noSessions')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {canSeeFailures && (
        <>
          <Typography variant="h6" sx={{ mt: 4, mb: 2 }}>
            {t('presence.failedTitle')}
          </Typography>
          {failures.isError ? (
            <ErrorState error={failures.error} onRetry={() => void failures.refetch()} />
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{t('presence.failedWhen')}</TableCell>
                    <TableCell>{t('presence.failedEmail')}</TableCell>
                    <TableCell>{t('presence.failedReason')}</TableCell>
                    <TableCell>{t('presence.client')}</TableCell>
                    <TableCell>{t('presence.ip')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {(failures.data ?? []).map((row) => (
                    <TableRow key={row.id}>
                      <TableCell>{formatDateTime(row.occurredAt)}</TableCell>
                      <TableCell>{row.email}</TableCell>
                      <TableCell>{t(`presence.reason.${row.reason}`)}</TableCell>
                      <TableCell>{clientLabel(row.client)}</TableCell>
                      <TableCell>{row.ipAddress ?? '—'}</TableCell>
                    </TableRow>
                  ))}
                  {failures.data?.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5}>
                        <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                          {t('presence.noFailures')}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </>
      )}
    </Box>
  );
}
