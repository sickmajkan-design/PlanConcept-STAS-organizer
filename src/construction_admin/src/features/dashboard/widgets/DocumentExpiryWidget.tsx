import { Box, Button, List, ListItem, ListItemText, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { attachmentsApi } from '../../../api/attachments';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const WITHIN_DAYS = 30;
const SHOWN = 5;

export function DocumentExpiryWidget({
  instanceId: _instanceId,
  dragHandleProps,
  onRemove,
}: DashboardWidgetProps) {
  const t = useT();

  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard', 'attachments', 'expiring', WITHIN_DAYS] as const,
    queryFn: () => attachmentsApi.expiring(WITHIN_DAYS),
  });

  const items = (Array.isArray(data) ? [...data] : [])
    .sort((a, b) => (a.expiresAt ?? '').localeCompare(b.expiresAt ?? ''))
    .slice(0, SHOWN);

  return (
    <WidgetShell
      title={t('dashboard.widget.DocumentExpiry')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      dragHandleProps={dragHandleProps}
    >
      {items.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.documentExpiry.empty')}
        </Typography>
      ) : (
        <List dense disablePadding>
          {items.map((attachment) => (
            <ListItem key={attachment.id} disableGutters>
              <ListItemText
                primary={attachment.fileName}
                secondary={t('dashboard.documentExpiry.expiresOn', {
                  date: formatDate(attachment.expiresAt),
                })}
              />
            </ListItem>
          ))}
        </List>
      )}
      <Box>
        <Button component={Link} to={paths.expiringDocuments} size="small">
          {t('common.viewAll')}
        </Button>
      </Box>
    </WidgetShell>
  );
}
