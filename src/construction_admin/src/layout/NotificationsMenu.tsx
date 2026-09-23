import { ArrowForwardOutlined, NotificationsNoneOutlined } from '@mui/icons-material';
import {
  Badge,
  Box,
  Button,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';

import type { Notification } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { resolveNotificationTarget } from '../features/notifications/notificationDeepLink';
import {
  useMarkNotificationRead,
  useNotificationsQuery,
  useUnreadCountQuery,
} from '../features/notifications/useNotifications';
import { useFormatRelative } from '../i18n/useFormatRelative';
import { resolveNotificationText } from '../features/notifications/notificationText';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

const PREVIEW_SIZE = 8;

/**
 * The bell in the top bar: previously just a link to the inbox page. Now the
 * whole path is a menu — open it, see the actual recent notifications, click
 * one, and land exactly where it points (same resolver the inbox page uses),
 * rather than opening the inbox first and clicking again from there.
 */
export function NotificationsMenu() {
  const t = useT();
  const navigate = useNavigate();
  const { user } = useAuth();
  const formatRelative = useFormatRelative();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const { data: unreadCount } = useUnreadCountQuery();
  const { data } = useNotificationsQuery({
    pageNumber: 1,
    pageSize: PREVIEW_SIZE,
    unreadOnly: false,
  });
  const markRead = useMarkNotificationRead();

  const items = data?.items ?? [];

  const open = (notification: Notification) => {
    setAnchor(null);
    if (!notification.isRead) markRead.mutate(notification.id);

    const target = resolveNotificationTarget(notification, user);
    if (target) navigate(target);
  };

  return (
    <>
      <IconButton
        onClick={(event) => setAnchor(event.currentTarget)}
        aria-label={t('notifications.title')}
      >
        <Badge badgeContent={unreadCount ?? 0} color="error" max={99}>
          <NotificationsNoneOutlined />
        </Badge>
      </IconButton>

      <Menu
        anchorEl={anchor}
        open={!!anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { width: 360, maxHeight: 480, overflowY: 'auto' } },
          list: { sx: { py: 0 } },
        }}
      >
        {/* "Prikaži sve" used to sit as the very last row, below up to 8
            previews — reachable only after scrolling past all of them. It now
            lives in the header instead, pinned above the scrolling list, so
            it's the first thing visible the instant the menu opens. */}
        <Box
          sx={{
            px: 2,
            py: 1,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 1,
            position: 'sticky',
            top: 0,
            bgcolor: 'background.paper',
            zIndex: 1,
          }}
        >
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            {t('notifications.title')}
          </Typography>
          <Button
            component={Link}
            to={paths.notifications}
            onClick={() => setAnchor(null)}
            size="small"
            endIcon={<ArrowForwardOutlined fontSize="small" />}
          >
            {t('notifications.viewAll')}
          </Button>
        </Box>
        <Divider />

        {items.length === 0 && (
          <Box sx={{ px: 2, py: 3 }}>
            <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
              {t('notifications.empty')}
            </Typography>
          </Box>
        )}

        {items.map((notification) => {
          const target = resolveNotificationTarget(notification, user);
          return (
            <MenuItem
              key={notification.id}
              onClick={() => open(notification)}
              sx={{
                alignItems: 'flex-start',
                whiteSpace: 'normal',
                py: 1,
                bgcolor: notification.isRead ? undefined : 'action.hover',
                // Reads as "nothing else happens" rather than a dead click,
                // for the free-text types the resolver has nowhere to send.
                opacity: target ? 1 : 0.85,
              }}
            >
              <Stack spacing={0.25} sx={{ width: '100%', minWidth: 0 }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'baseline' }}>
                  <Typography
                    variant="body2"
                    sx={{ fontWeight: notification.isRead ? 500 : 700, flex: 1, minWidth: 0 }}
                    noWrap
                  >
                    {resolveNotificationText(t, notification).title}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'nowrap' }}>
                    {formatRelative(notification.createdAt)}
                  </Typography>
                </Stack>
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden',
                  }}
                >
                  {resolveNotificationText(t, notification).body}
                </Typography>
              </Stack>
            </MenuItem>
          );
        })}
      </Menu>
    </>
  );
}
