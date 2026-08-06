import { CampaignOutlined, DoneAllOutlined, NotificationsNoneOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControlLabel,
  ListItemButton,
  Pagination,
  Paper,
  Snackbar,
  Stack,
  Switch,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import type { NotificationListQuery } from '../../api/notifications';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotificationsQuery,
  useUnreadCountQuery,
} from '../../features/notifications/useNotifications';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { useFormatRelative } from '../../i18n/useFormatRelative';
import { AnnounceDialog } from './AnnounceDialog';

const PAGE_SIZE = 20;

/**
 * The operator's own inbox.
 *
 * The same notifications the phone receives, for the people who work at a
 * desk. Everything that reaches here reached a device too — this is the record
 * of it, not a second channel, which is why nothing can be deleted from it.
 */
export function NotificationsPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const formatRelative = useFormatRelative();
  const { user } = useAuth();

  const [page, setPage] = useState(1);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [announcing, setAnnouncing] = useState(false);
  const [sentTo, setSentTo] = useState<number | null>(null);

  const query: NotificationListQuery = useMemo(
    () => ({ pageNumber: page, pageSize: PAGE_SIZE, unreadOnly }),
    [page, unreadOnly],
  );

  const { data, isLoading, isError, error, refetch } = useNotificationsQuery(query);
  const { data: unreadCount } = useUnreadCountQuery();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const showFilter = (value: boolean) => {
    setUnreadOnly(value);
    // Page 3 of "everything" is rarely a page of "unread" at all, and an empty
    // grid reads as "nothing to see" rather than "wrong page".
    setPage(1);
  };

  return (
    <Box>
      <PageHeader
        title={t('notifications.title')}
        subtitle={t('notifications.subtitle')}
        action={
          canAdministerAccounts(user)
            ? {
                label: t('notifications.announce'),
                icon: <CampaignOutlined />,
                onClick: () => setAnnouncing(true),
              }
            : undefined
        }
      />

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <FormControlLabel
          control={
            <Switch
              checked={unreadOnly}
              onChange={(event) => showFilter(event.target.checked)}
            />
          }
          label={t('notifications.unreadOnly')}
        />
        <Box sx={{ flex: 1 }} />
        <Button
          startIcon={<DoneAllOutlined />}
          disabled={!unreadCount || markAllRead.isPending}
          onClick={() => markAllRead.mutate()}
        >
          {t('notifications.markAllRead')}
        </Button>
      </Stack>

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {isLoading && (
        <Box sx={{ py: 8, display: 'flex', justifyContent: 'center' }}>
          <CircularProgress />
        </Box>
      )}

      {data && data.items.length === 0 && !isLoading && (
        <EmptyState
          icon={NotificationsNoneOutlined}
          message={
            unreadOnly ? t('notifications.noUnread') : t('notifications.empty')
          }
        />
      )}

      {data && data.items.length > 0 && (
        <Paper>
          {data.items.map((notification) => (
            <ListItemButton
              key={notification.id}
              divider
              // Opening it is what marks it read; there is no separate button
              // because there is nothing else to do with one.
              onClick={() =>
                !notification.isRead && markRead.mutate(notification.id)
              }
              sx={{
                alignItems: 'flex-start',
                bgcolor: notification.isRead ? undefined : 'action.hover',
                py: 1.5,
              }}
            >
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Stack
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: 'center', mb: 0.5, flexWrap: 'wrap' }}
                >
                  <Typography
                    variant="subtitle2"
                    sx={{ fontWeight: notification.isRead ? 500 : 700 }}
                  >
                    {notification.title}
                  </Typography>
                  <Chip
                    size="small"
                    variant="outlined"
                    label={enumLabel('notificationType', notification.type)}
                  />
                </Stack>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ whiteSpace: 'pre-line' }}
                >
                  {notification.body}
                </Typography>
              </Box>
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ ml: 2, whiteSpace: 'nowrap', pt: 0.5 }}
              >
                {formatRelative(notification.createdAt)}
              </Typography>
            </ListItemButton>
          ))}
        </Paper>
      )}

      {data && data.totalPages > 1 && (
        <Stack sx={{ mt: 2, alignItems: 'center' }}>
          <Pagination
            count={data.totalPages}
            page={data.pageNumber}
            onChange={(_, value) => setPage(value)}
          />
        </Stack>
      )}

      <AnnounceDialog
        open={announcing}
        onClose={() => setAnnouncing(false)}
        onSent={setSentTo}
      />

      {/* The recipient count is the only confirmation there is that the
          audience filter did what was meant — a message sent to nobody looks
          exactly like one sent to everybody otherwise. */}
      <Snackbar
        open={sentTo !== null}
        autoHideDuration={6000}
        onClose={() => setSentTo(null)}
        message={t('notifications.sentTo', { count: sentTo ?? 0 })}
      />
    </Box>
  );
}
