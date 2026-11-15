import { Box, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { systemApi } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';
import { formatDateTime } from '../../utils/formatting';
import { BigNumber, Panel, StatusDot } from './serverUi';

const REASONS: Record<string, MessageKey> = {
  UnknownAccount: 'serverLogins.reason.UnknownAccount',
  WrongPassword: 'serverLogins.reason.WrongPassword',
  LockedOut: 'serverLogins.reason.LockedOut',
  Deactivated: 'serverLogins.reason.Deactivated',
  WrongMfaCode: 'serverLogins.reason.WrongMfaCode',
};

/** An address that tried this many different accounts is guessing, not a person who forgot a password. */
const GUESSING_ACCOUNTS = 3;

/**
 * Failed sign-ins of the last seven days and what the host's fail2ban did to the SSH door. Shown to the Super Admin on the
 * server page, so that an attack is seen without reading logs. Only accounts that exist are named; a name typed by a
 * stranger that matches nobody is counted under its reason, never listed.
 */
export function ServerLoginsCard() {
  const t = useT();
  const { data } = useQuery({ queryKey: ['system', 'login-attempts'] as const, queryFn: systemApi.loginAttempts, refetchInterval: 60_000 });

  if (!data) {
    return null;
  }

  const peak = Math.max(1, ...data.days.map((day) => day.count));
  const quiet = data.last7Days === 0;

  return (
    <Panel title={t('serverLogins.title')} description={t('serverLogins.description')}>
      <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: { xs: 2, sm: 5 }, mb: 2.5, flexWrap: 'wrap' }}>
        <Box>
          <BigNumber size={34}>{data.last24Hours}</BigNumber>
          <Typography variant="body2" color="text.secondary">
            {t('serverLogins.last24')}
          </Typography>
        </Box>
        <Box>
          <BigNumber size={34}>{data.last7Days}</BigNumber>
          <Typography variant="body2" color="text.secondary">
            {t('serverLogins.last7')}
          </Typography>
        </Box>
        <Box>
          <BigNumber size={34}>{data.ssh.known ? data.ssh.bannedNow : '–'}</BigNumber>
          <Typography variant="body2" color="text.secondary">
            {t('serverLogins.sshBanned')}
          </Typography>
        </Box>
      </Stack>

      <Stack direction="row" role="img" aria-label={t('serverLogins.perDay')} sx={{ alignItems: 'flex-end', gap: 1, height: 64, mb: 0.5 }}>
        {data.days.map((day) => (
          <Box key={day.day} sx={{ flex: 1, display: 'flex', alignItems: 'flex-end', height: '100%' }} title={`${day.day}: ${day.count}`}>
            <Box sx={{ width: '100%', height: `${Math.max(4, (day.count / peak) * 100)}%`, borderRadius: '6px', bgcolor: day.count > 0 ? 'warning.main' : 'action.disabledBackground', opacity: day.count > 0 ? 0.85 : 0.6 }} />
          </Box>
        ))}
      </Stack>
      <Typography variant="caption" color="text.secondary" component="div" sx={{ mb: 2.5 }}>
        {t('serverLogins.perDay')}
      </Typography>

      {quiet ? (
        <Stack direction="row" sx={{ alignItems: 'center', gap: 1, mb: 2 }}>
          <StatusDot tone="success" />
          <Typography variant="body2">{t('serverLogins.none')}</Typography>
        </Stack>
      ) : (
        <>
          <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap', mb: 2.5 }}>
            {data.reasons.map((reason) => (
              <Chip key={reason.reason} size="small" label={`${REASONS[reason.reason] ? t(REASONS[reason.reason]) : reason.reason}: ${reason.count}`} />
            ))}
          </Stack>

          {data.topIps.length > 0 && (
            <Box sx={{ overflowX: 'auto', mb: 2.5 }}>
              <Table size="small" aria-label={t('serverLogins.addresses')}>
                <TableHead>
                  <TableRow>
                    <TableCell>{t('serverLogins.address')}</TableCell>
                    <TableCell align="right">{t('serverLogins.attempts')}</TableCell>
                    <TableCell align="right">{t('serverLogins.accounts')}</TableCell>
                    <TableCell>{t('serverLogins.last')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.topIps.map((row) => (
                    <TableRow key={row.ip}>
                      <TableCell sx={{ fontFamily: 'monospace', fontSize: 12 }}>{row.ip}</TableCell>
                      <TableCell align="right">{row.count}</TableCell>
                      <TableCell align="right">
                        <Chip size="small" color={row.accounts >= GUESSING_ACCOUNTS ? 'warning' : 'default'} label={row.accounts} />
                      </TableCell>
                      <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(row.lastAtUtc)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          )}

          {data.topAccounts.length > 0 && (
            <>
              <Typography sx={{ fontSize: 15, fontWeight: 600, mb: 0.5 }}>{t('serverLogins.targeted')}</Typography>
              <Stack sx={{ mb: 2.5 }}>
                {data.topAccounts.map((account) => (
                  <Stack key={account.email} data-row direction="row" sx={{ justifyContent: 'space-between', gap: 2, py: 0.75 }}>
                    <Typography variant="body2" sx={{ overflowWrap: 'anywhere' }}>
                      {account.email}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {account.count}
                    </Typography>
                  </Stack>
                ))}
              </Stack>
            </>
          )}
        </>
      )}

      <Typography sx={{ fontSize: 15, fontWeight: 600, mb: 0.5 }}>{t('serverLogins.ssh')}</Typography>
      {data.ssh.known ? (
        <>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            {t('serverLogins.sshSummary', { banned: data.ssh.bannedTotal, failed: data.ssh.failedTotal })}
          </Typography>
          {data.ssh.bannedIps.length > 0 && (
            <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
              {data.ssh.bannedIps.map((ip) => (
                <Chip key={ip} size="small" variant="outlined" label={ip} sx={{ fontFamily: 'monospace' }} />
              ))}
            </Stack>
          )}
        </>
      ) : (
        <Typography variant="body2" color="text.secondary">
          {t('serverLogins.sshUnknown')}
        </Typography>
      )}
    </Panel>
  );
}
