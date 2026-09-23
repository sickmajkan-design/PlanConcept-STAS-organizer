import { CampaignOutlined } from '@mui/icons-material';
import { Avatar, Box, Button, Chip, List, ListItem, ListItemAvatar, ListItemText, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { bulletinApi } from '../../../api/bulletin';
import { notificationsApi } from '../../../api/notifications';
import { useFormatRelative } from '../../../i18n/useFormatRelative';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const RECENT_POSTS = 5;

export function NotificationsBulletinWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const formatRelative = useFormatRelative();

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
    <WidgetShell title={t('dashboard.widget.NotificationsBulletin')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Chip
          color={(unreadQuery.data ?? 0) > 0 ? 'primary' : 'default'}
          component={Link}
          to={paths.notifications}
          clickable
          label={t('dashboard.notificationsBulletin.unread', { count: unreadQuery.data ?? 0 })}
          sx={{ fontWeight: 700, alignSelf: 'flex-start', flexShrink: 0 }}
        />

        <Typography variant="overline" color="text.secondary" sx={{ flexShrink: 0, letterSpacing: '0.06em' }}>
          {t('dashboard.notificationsBulletin.latestPosts')}
        </Typography>

        {posts.length === 0 ? (
          <Typography color="text.secondary" variant="body2">
            {t('dashboard.notificationsBulletin.empty')}
          </Typography>
        ) : (
          <List dense disablePadding sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            {posts.map((post) => (
              <ListItem key={post.id} disableGutters>
                <ListItemAvatar sx={{ minWidth: 44 }}>
                  <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}>
                    <CampaignOutlined fontSize="small" />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={post.title}
                  secondary={formatRelative(post.createdAt)}
                  slotProps={{ primary: { sx: { fontWeight: 600 } } }}
                />
              </ListItem>
            ))}
          </List>
        )}

        <Box sx={{ flexShrink: 0 }}>
          <Button component={Link} to={paths.bulletin} size="small">
            {t('common.viewAll')}
          </Button>
        </Box>
      </Stack>
    </WidgetShell>
  );
}
