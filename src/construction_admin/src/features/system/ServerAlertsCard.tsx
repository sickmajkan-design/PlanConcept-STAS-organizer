import { Alert, Box, Button, Stack, Typography } from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { systemApi, type ServerAlertKey, type ServerAlertSetting } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { IosSwitch, Panel } from './serverUi';

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
    <Panel
      title={t('serverAlerts.title')}
      description={t('serverAlerts.description')}
      action={
        <Stack direction="row" sx={{ alignItems: 'center', gap: 1.5 }}>
          {test.isSuccess && (
            <Typography variant="caption" color="text.secondary">
              {t('serverAlerts.testSent')}
            </Typography>
          )}
          <Button variant="outlined" onClick={() => test.mutate()} disabled={test.isPending} sx={{ borderRadius: 999, px: 2.5 }}>
            {t('serverAlerts.test')}
          </Button>
        </Stack>
      }
    >
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 360px), 1fr))', columnGap: 6 }}>
        {data.map((row) => (
          <Stack key={row.key} data-row direction="row" sx={{ alignItems: 'center', gap: 2, py: 1.75, flexWrap: 'wrap' }}>
            <Box sx={{ flex: '1 1 180px', minWidth: 0 }}>
              <Typography sx={{ fontSize: 16, fontWeight: 500 }}>{t(`serverAlerts.kind.${row.key}`)}</Typography>
              <Typography variant="caption" color="text.secondary" component="div">
                {t(`serverAlerts.when.${row.key}`)}
                {row.lastSentAtUtc ? ` · ${t('serverAlerts.lastSent')}: ${formatDateTime(row.lastSentAtUtc)}` : ''}
              </Typography>
            </Box>
            <Stack direction="row" sx={{ alignItems: 'center', gap: 1.25 }}>
              <Typography variant="caption" sx={{ fontWeight: 600, color: 'text.secondary' }}>
                {t('serverAlerts.email')}
              </Typography>
              <IosSwitch
                checked={row.emailEnabled}
                onChange={() => toggle(row, 'emailEnabled')}
                slotProps={{ input: { 'aria-label': `${t(`serverAlerts.kind.${row.key}`)}: ${t('serverAlerts.email')}` } }}
              />
              <Typography variant="caption" sx={{ fontWeight: 600, color: 'text.secondary', ml: 0.5 }}>
                {t('serverAlerts.push')}
              </Typography>
              <IosSwitch
                checked={row.pushEnabled}
                onChange={() => toggle(row, 'pushEnabled')}
                slotProps={{ input: { 'aria-label': `${t(`serverAlerts.kind.${row.key}`)}: ${t('serverAlerts.push')}` } }}
              />
            </Stack>
          </Stack>
        ))}
      </Box>
      {(save.isError || test.isError) && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {t('serverAlerts.failed')}
        </Alert>
      )}
    </Panel>
  );
}
