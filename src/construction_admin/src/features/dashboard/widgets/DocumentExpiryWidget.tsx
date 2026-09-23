import { DescriptionOutlined } from '@mui/icons-material';
import { Avatar, Box, Button, List, ListItem, ListItemAvatar, ListItemText, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { attachmentsApi } from '../../../api/attachments';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const WITHIN_DAYS = 30;
const SHOWN = 8;

/** Under a week left reads as urgent (warning), everything else as informational. */
function isUrgent(expiresAt: string | null): boolean {
  if (!expiresAt) return false;
  const days = (new Date(expiresAt).getTime() - Date.now()) / 86_400_000;
  return days <= 7;
}

export function DocumentExpiryWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();

  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard', 'attachments', 'expiring', WITHIN_DAYS] as const,
    queryFn: () => attachmentsApi.expiring(WITHIN_DAYS),
  });

  const items = (Array.isArray(data) ? [...data] : [])
    .sort((a, b) => (a.expiresAt ?? '').localeCompare(b.expiresAt ?? ''))
    .slice(0, SHOWN);

  return (
    <WidgetShell title={t('dashboard.widget.DocumentExpiry')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        {items.length === 0 ? (
          <Typography color="text.secondary" variant="body2">
            {t('dashboard.documentExpiry.empty')}
          </Typography>
        ) : (
          <List dense disablePadding sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            {items.map((attachment) => {
              const urgent = isUrgent(attachment.expiresAt);
              return (
                <ListItem key={attachment.id} disableGutters>
                  <ListItemAvatar sx={{ minWidth: 44 }}>
                    <Avatar
                      variant="rounded"
                      sx={{
                        width: 32,
                        height: 32,
                        bgcolor: urgent ? 'error.main' : 'action.selected',
                        color: urgent ? 'error.contrastText' : 'text.secondary',
                      }}
                    >
                      <DescriptionOutlined fontSize="small" />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={attachment.fileName}
                    secondary={t('dashboard.documentExpiry.expiresOn', { date: formatDate(attachment.expiresAt) })}
                    slotProps={{
                      primary: { noWrap: true, sx: { fontWeight: 600 } },
                      secondary: { sx: { color: urgent ? 'error.main' : 'text.secondary', fontWeight: urgent ? 700 : 400 } },
                    }}
                  />
                </ListItem>
              );
            })}
          </List>
        )}
        <Box sx={{ flexShrink: 0 }}>
          <Button component={Link} to={paths.expiringDocuments} size="small">
            {t('common.viewAll')}
          </Button>
        </Box>
      </Stack>
    </WidgetShell>
  );
}
