import { Box, Card, CardContent, Chip, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { systemApi } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';

/**
 * Unexpected server errors of the last seven days, grouped by kind. What the server keeps is only the error type, the
 * place in the code and the route pattern, so nothing here can show a person's data.
 */
export function ServerErrorsCard() {
  const t = useT();
  const { data } = useQuery({ queryKey: ['system', 'errors'] as const, queryFn: systemApi.errors, refetchInterval: 60_000 });

  if (!data) {
    return null;
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="subtitle1" sx={{ mb: 0.5 }}>
          {t('serverErrors.title')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('serverErrors.description')}
        </Typography>
        {data.length === 0 ? (
          <Chip size="small" color="success" label={t('serverErrors.none')} />
        ) : (
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
        )}
      </CardContent>
    </Card>
  );
}
