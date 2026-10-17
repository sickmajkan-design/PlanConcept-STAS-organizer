import { CampaignOutlined } from '@mui/icons-material';
import { Avatar, Box, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { bulletinApi } from '../../../api/bulletin';
import { notificationsApi } from '../../../api/notifications';
import { useFormatRelative } from '../../../i18n/useFormatRelative';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, InsetList, InsetRow, MoreLink, Pill } from './widgetUi';

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

  const unread = unreadQuery.data ?? 0;

  return (
    <WidgetShell title={t('dashboard.widget.NotificationsBulletin')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        <Pill
          tone={unread > 0 ? 'accent' : 'neutral'}
          sx={{ alignSelf: 'flex-start', fontSize: '0.85rem', px: 1.5, py: 0.25, textDecoration: 'none', cursor: 'pointer' }}
        >
          <Link to={paths.notifications} style={{ color: 'inherit', textDecoration: 'none' }}>
            {t('dashboard.notificationsBulletin.unread', { count: unread })}
          </Link>
        </Pill>

        <Typography variant="overline" color="text.secondary" sx={{ flexShrink: 0, letterSpacing: '0.06em' }}>
          {t('dashboard.notificationsBulletin.latestPosts')}
        </Typography>

        {posts.length === 0 ? (
          <EmptyNote>{t('dashboard.notificationsBulletin.empty')}</EmptyNote>
        ) : (
          <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            <InsetList>
              {posts.map((post) => (
                <InsetRow
                  key={post.id}
                  to={paths.bulletin}
                  leading={
                    <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}>
                      <CampaignOutlined fontSize="small" />
                    </Avatar>
                  }
                  primary={post.title}
                  secondary={formatRelative(post.createdAt)}
                />
              ))}
            </InsetList>
          </Box>
        )}

        <MoreLink to={paths.bulletin}>{t('common.viewAll')}</MoreLink>
      </Stack>
    </WidgetShell>
  );
}
