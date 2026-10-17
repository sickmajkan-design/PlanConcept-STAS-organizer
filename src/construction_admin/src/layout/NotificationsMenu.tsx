import { ArrowForwardOutlined, NotificationsNoneOutlined } from '@mui/icons-material';
import {
  Badge,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import { keepPreviousData, useQueries } from '@tanstack/react-query';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';

import { notificationsApi } from '../api/notifications';
import type { Notification } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { resolveNotificationTarget } from '../features/notifications/notificationDeepLink';
import {
  notificationKeys,
  useMarkNotificationRead,
  useUnreadCountQuery,
} from '../features/notifications/useNotifications';
import { useFormatRelative } from '../i18n/useFormatRelative';
import { resolveNotificationText } from '../features/notifications/notificationText';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { formatDateTime } from '../utils/formatting';

/** How many are fetched at a time; the rest arrive as the list is scrolled to its end. */
const PAGE_SIZE = 15;

/**
 * The bell in the top bar: previously just a link to the inbox page. Now the
 * whole path is a menu — open it, see the actual recent notifications, click
 * one, and land exactly where it points (same resolver the inbox page uses),
 * rather than opening the inbox first and clicking again from there.
 *
 * It goes all the way through: scrolling to the bottom brings in the next page, until the last
 * notification, which says it is the last. A notification that points at nothing (an announcement, a
 * message) opens in full, so it can be read to its end instead of being cut at two lines.
 */
export function NotificationsMenu() {
  const t = useT();
  const navigate = useNavigate();
  const { user } = useAuth();
  const formatRelative = useFormatRelative();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const [pageCount, setPageCount] = useState(1);
  const [reading, setReading] = useState<Notification | null>(null);

  const { data: unreadCount } = useUnreadCountQuery();
  const pages = useQueries({
    queries: Array.from({ length: pageCount }, (_, index) => {
      const query = { pageNumber: index + 1, pageSize: PAGE_SIZE, unreadOnly: false };

      return {
        queryKey: notificationKeys.list(query),
        queryFn: () => notificationsApi.list(query),
        placeholderData: keepPreviousData,
      };
    }),
  });
  const markRead = useMarkNotificationRead();

  const items = pages.flatMap((page) => page.data?.items ?? []);
  const totalPages = pages[0]?.data?.totalPages ?? 1;
  const hasMore = pageCount < totalPages;
  const loadingMore = pages.some((page) => page.isFetching && !page.data);

  const close = () => {
    setAnchor(null);
    // Next time it opens it starts from the newest again, instead of holding every page ever scrolled to.
    setPageCount(1);
  };

  const loadMore = () => {
    if (hasMore && !loadingMore) setPageCount((count) => count + 1);
  };

  const open = (notification: Notification) => {
    close();
    if (!notification.isRead) markRead.mutate(notification.id);

    const target = resolveNotificationTarget(notification, user);
    if (target) navigate(target);
    else setReading(notification);
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
        onClose={close}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: {
            sx: { width: 360, maxHeight: 480, overflowY: 'auto' },
            onScroll: (event: React.UIEvent<HTMLElement>) => {
              const element = event.currentTarget;
              if (element.scrollTop + element.clientHeight >= element.scrollHeight - 64) loadMore();
            },
          },
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
            onClick={close}
            size="small"
            endIcon={<ArrowForwardOutlined fontSize="small" />}
          >
            {t('notifications.viewAll')}
          </Button>
        </Box>
        <Divider />

        {items.length === 0 && !loadingMore && (
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

        {hasMore && (
          <Box sx={{ px: 2, py: 1.5 }}>
            {loadingMore ? (
              <Stack spacing={1} aria-busy="true">
                <Skeleton variant="rounded" height={14} width="60%" />
                <Skeleton variant="rounded" height={12} />
              </Stack>
            ) : (
              <Button size="small" fullWidth onClick={loadMore}>
                {t('notifications.loadMore')}
              </Button>
            )}
          </Box>
        )}

        {!hasMore && items.length > 0 && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', textAlign: 'center', py: 1.5 }}>
            {t('notifications.endOfList')}
          </Typography>
        )}
      </Menu>

      <Dialog open={!!reading} onClose={() => setReading(null)} fullWidth maxWidth="sm">
        {reading && (
          <>
            <DialogTitle sx={{ overflowWrap: 'anywhere' }}>{resolveNotificationText(t, reading).title}</DialogTitle>
            <DialogContent>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5 }}>
                {formatDateTime(reading.createdAt)}
              </Typography>
              <Typography variant="body1" sx={{ whiteSpace: 'pre-line', overflowWrap: 'anywhere' }}>
                {resolveNotificationText(t, reading).body}
              </Typography>
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setReading(null)}>{t('common.close')}</Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </>
  );
}
