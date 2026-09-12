import { Box, Button, Chip, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { bulletinApi } from '../../../api/bulletin';
import { notificationsApi } from '../../../api/notifications';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatRelative } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const RECENT_POSTS = 3;

export function NotificationsBulletinWidget({
  instanceId: _instanceId,
  dragHandleProps,
  onRemove,
}: DashboardWidgetProps) {
  const t = useT();

  const unreadQuery = useQuery({
    queryKey: ['dashboard', 'notifications', 'unread-count'] as const,
    queryFn: () => notificationsApi.unreadCount(),
  });

  const postsQuery = useQuery({
    queryKey: ['dashboard', 'bulletin', 'latest'] as const,
    queryFn: () => bulletinApi.list(),
  });

  const isLoading = unreadQuery.isLoading || postsQuery.isLoading;
  const error = unreadQuery.error ?? postsQuery.error;
  const posts = (Array.isArray(postsQuery.data) ? postsQuery.data : []).slice(0, RECENT_POSTS);

  return (
    <WidgetShell
      title={t('dashboard.widget.NotificationsBulletin')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      dragHandleProps={dragHandleProps}
    >
      <Stack direction="row" sx={{ mb: 1.5 }}>
        <Chip
          size="small"
          color={(unreadQuery.data ?? 0) > 0 ? 'primary' : 'default'}
          component={Link}
          to={paths.notifications}
          clickable
          label={t('dashboard.notificationsBulletin.unread', { count: unreadQuery.data ?? 0 })}
        />
      </Stack>

      <Typography variant="caption" color="text.secondary">
        {t('dashboard.notificationsBulletin.latestPosts')}
      </Typography>

      {posts.length === 0 ? (
        <Typography color="text.secondary" variant="body2" sx={{ mt: 0.5 }}>
          {t('dashboard.notificationsBulletin.empty')}
        </Typography>
      ) : (
        <List dense disablePadding>
          {posts.map((post) => (
            <ListItem key={post.id} disableGutters>
              <ListItemText primary={post.title} secondary={formatRelative(post.createdAt)} />
            </ListItem>
          ))}
        </List>
      )}

      <Box>
        <Button component={Link} to={paths.bulletin} size="small">
          {t('common.viewAll')}
        </Button>
      </Box>
    </WidgetShell>
  );
}
