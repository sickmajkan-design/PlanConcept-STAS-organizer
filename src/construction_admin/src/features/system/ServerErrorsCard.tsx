import { Box, Chip, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { systemApi } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { BigNumber, Panel } from './serverUi';

function useServerErrors() {
  return useQuery({ queryKey: ['system', 'errors'] as const, queryFn: systemApi.errors, refetchInterval: 60_000 });
}

/**
 * A short answer for the side of the page: "no errors", or how many today. The table below it (when there is
 * anything to show) has the detail.
 */
export function ServerErrorsTile() {
  const t = useT();
  const { data } = useServerErrors();

  if (!data) {
    return null;
  }

  const today = data.reduce((sum, group) => sum + group.today, 0);

  return (
    <Panel fill={false}>
      <Typography sx={{ fontSize: 15, fontWeight: 600, color: 'text.secondary', mb: 1 }}>{t('serverErrors.title')}</Typography>
      <BigNumber size={34}>{data.length === 0 ? t('serverErrors.noneShort') : t('serverErrors.todayCount', { count: today })}</BigNumber>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        {t('serverErrors.sevenDays')}
      </Typography>
    </Panel>
  );
}

/**
 * Unexpected server errors of the last seven days, grouped by kind. What the server keeps is only the error type, the
 * place in the code and the route pattern, so nothing here can show a person's data. Shown only when there is something.
 */
export function ServerErrorsCard() {
  const t = useT();
  const { data } = useServerErrors();

  if (!data || data.length === 0) {
    return null;
  }

  return (
    <Panel title={t('serverErrors.title')} description={t('serverErrors.description')}>
      <Box sx={{ overflowX: 'auto' }}>
        <Table size="small" aria-label={t('serverErrors.title')}>
          <TableHead>
            <TableRow>
              <TableCell>{t('serverErrors.error')}</TableCell>
              <TableCell>{t('serverErrors.route')}</TableCell>
              <TableCell align="right">{t('serverErrors.today')}</TableCell>
              <TableCell align="right">{t('serverErrors.week')}</TableCell>
              <TableCell>{t('serverErrors.last')}</TableCell>
              <TableCell>{t('serverErrors.since')}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data.map((group) => (
              <TableRow key={`${group.exceptionType}|${group.location}|${group.route}`}>
                <TableCell>
                  {group.exceptionType}
                  <Typography variant="caption" color="text.secondary" component="div">
                    {group.location}
                  </Typography>
                </TableCell>
                <TableCell sx={{ fontFamily: 'monospace', fontSize: 12 }}>{group.route}</TableCell>
                <TableCell align="right">
                  <Chip size="small" color={group.today > 0 ? 'error' : 'default'} label={group.today} />
                </TableCell>
                <TableCell align="right">{group.total}</TableCell>
                <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(group.lastSeenAtUtc)}</TableCell>
                <TableCell sx={{ fontFamily: 'monospace', fontSize: 12 }}>{group.firstVersion}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Box>
    </Panel>
  );
}
