import { Alert, Box, Button, Card, CardContent, Stack, Switch, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { systemApi, type ServerAlertKey, type ServerAlertSetting } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';

const KEY = ['system', 'alerts'] as const;

/**
 * Where the Super Admin hears about trouble on the server, by kind: e-mail, push (the in-app notice and the phone), both
 * or neither. The server checks every few minutes and repeats the same problem at most every six hours.
 */
export function ServerAlertsCard() {
  const t = useT();
  const queryClient = useQueryClient();
  const { data } = useQuery({ queryKey: KEY, queryFn: systemApi.alerts });

  const save = useMutation({
    mutationFn: ({ key, ...channels }: { key: ServerAlertKey; emailEnabled: boolean; pushEnabled: boolean }) => systemApi.setAlert(key, channels),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: KEY }),
  });
  const test = useMutation({ mutationFn: systemApi.testAlert });

  if (!data) {
    return null;
  }

  const toggle = (row: ServerAlertSetting, channel: 'emailEnabled' | 'pushEnabled') =>
    save.mutate({ key: row.key, emailEnabled: row.emailEnabled, pushEnabled: row.pushEnabled, [channel]: !row[channel] });

  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="subtitle1" sx={{ mb: 0.5 }}>
          {t('serverAlerts.title')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('serverAlerts.description')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" aria-label={t('serverAlerts.title')}>
            <TableHead>
              <TableRow>
                <TableCell>{t('serverAlerts.event')}</TableCell>
                <TableCell align="center">{t('serverAlerts.email')}</TableCell>
                <TableCell align="center">{t('serverAlerts.push')}</TableCell>
                <TableCell>{t('serverAlerts.lastSent')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.map((row) => (
                <TableRow key={row.key}>
                  <TableCell>
                    {t(`serverAlerts.kind.${row.key}`)}
                    <Typography variant="caption" color="text.secondary" component="div">
                      {t(`serverAlerts.when.${row.key}`)}
                    </Typography>
                  </TableCell>
                  <TableCell align="center">
                    <Switch
                      size="small"
                      checked={row.emailEnabled}
                      onChange={() => toggle(row, 'emailEnabled')}
                      slotProps={{ input: { 'aria-label': `${t(`serverAlerts.kind.${row.key}`)}: ${t('serverAlerts.email')}` } }}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Switch
                      size="small"
                      checked={row.pushEnabled}
                      onChange={() => toggle(row, 'pushEnabled')}
                      slotProps={{ input: { 'aria-label': `${t(`serverAlerts.kind.${row.key}`)}: ${t('serverAlerts.push')}` } }}
                    />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.lastSentAtUtc ? formatDateTime(row.lastSentAtUtc) : '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
        <Stack direction="row" sx={{ mt: 1.5, gap: 1.5, alignItems: 'center', flexWrap: 'wrap' }}>
          <Button size="small" variant="outlined" onClick={() => test.mutate()} disabled={test.isPending}>
            {t('serverAlerts.test')}
          </Button>
          {test.isSuccess && (
            <Typography variant="caption" color="text.secondary">
              {t('serverAlerts.testSent')}
            </Typography>
          )}
        </Stack>
        {(save.isError || test.isError) && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {t('serverAlerts.failed')}
          </Alert>
        )}
      </CardContent>
    </Card>
  );
}
