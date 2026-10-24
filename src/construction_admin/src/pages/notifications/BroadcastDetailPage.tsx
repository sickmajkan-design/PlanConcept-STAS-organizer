import { NotificationsActiveOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  LinearProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { BroadcastRecipient } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { useBroadcastQuery, useRemindBroadcast } from '../../features/notifications/useNotifications';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDateTime } from '../../utils/formatting';

type Tab = 'all' | 'unread' | 'waiting' | 'acked';

function Stat({ label, value, note, color, percent }: { label: string; value: number; note?: string; color?: string; percent?: number }) {
  return (
    <Card variant="outlined" sx={{ flex: '1 1 170px' }}>
      <CardContent sx={{ py: 1.5 }}>
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="h5" sx={{ fontWeight: 700, color }}>
          {value}
        </Typography>
        {percent != null && <LinearProgress variant="determinate" value={percent} color={color === 'warning.main' ? 'warning' : 'success'} sx={{ height: 6, borderRadius: 3, mt: 0.5 }} />}
        {note && (
          <Typography variant="caption" color="text.secondary">
            {note}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

/** One sent announcement: who has read it, who has confirmed it, and who still has not, with a reminder for them. */
export function BroadcastDetailPage() {
  const t = useT();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useBroadcastQuery(id);
  const remind = useRemindBroadcast();
  const [tab, setTab] = useState<Tab>('all');
  const [reminded, setReminded] = useState<number | null>(null);

  if (isLoading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (isError || !data) {
    return <Alert severity="error">{t('notifications.loadFailed')}</Alert>;
  }

  const people = data.recipients;
  const total = people.length;
  const read = people.filter((p) => p.isRead).length;
  const acked = people.filter((p) => p.acknowledgedAt).length;
  const unread = total - read;
  const waiting = data.requiresAcknowledgment ? people.filter((p) => p.isRead && !p.acknowledgedAt).length : 0;
  const toRemind = unread + waiting;

  const shown = people.filter(
    (p) =>
      tab === 'all' ||
      (tab === 'unread' && !p.isRead) ||
      (tab === 'waiting' && p.isRead && !p.acknowledgedAt) ||
      (tab === 'acked' && !!p.acknowledgedAt),
  );

  const tabs: { id: Tab; label: string; show: boolean }[] = [
    { id: 'all', label: t('notifications.tab.all', { count: total }), show: true },
    { id: 'unread', label: t('notifications.tab.unread', { count: unread }), show: true },
    { id: 'waiting', label: t('notifications.tab.waiting', { count: waiting }), show: data.requiresAcknowledgment },
    { id: 'acked', label: t('notifications.tab.acked', { count: acked }), show: data.requiresAcknowledgment },
  ];

  const sendReminder = () =>
    remind.mutate(data.id, { onSuccess: (count) => setReminded(count) });

  const cell = (person: BroadcastRecipient) => ({
    delivered: person.hasDevice ? t('notifications.delivery.push') : t('notifications.delivery.bell'),
    read: person.isRead && person.readAt ? formatDateTime(person.readAt) : t('notifications.notRead'),
    acked: person.acknowledgedAt
      ? formatDateTime(person.acknowledgedAt)
      : data.requiresAcknowledgment
        ? person.isRead
          ? t('notifications.ackWaiting')
          : t('notifications.notAcked')
        : '—',
  });

  return (
    <Box>
      <PageHeader
        title={data.title}
        description={t('notifications.sentLine', { when: formatDateTime(data.sentAt), who: data.sentBy ?? '—' })}
        secondaryActions={<Button onClick={() => navigate(paths.notificationsSent)}>{t('notifications.sentTitle')}</Button>}
      />

      <Card variant="outlined" sx={{ mb: 2 }}>
        <CardContent>
          <Box sx={{ display: 'grid', gap: 3, gridTemplateColumns: { xs: '1fr', md: '2fr 1fr' } }}>
            <Box>
              <Typography variant="caption" color="text.secondary">
                {t('notifications.message')}
              </Typography>
              <Typography sx={{ whiteSpace: 'pre-wrap' }}>{data.body}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                {t('notifications.sentToLabel')}
              </Typography>
              <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {data.audience.map((chip) => (
                  <Chip key={chip.label} size="small" variant="outlined" label={`${chip.label} (${chip.count})`} />
                ))}
              </Stack>
              {data.requiresAcknowledgment && (
                <Chip size="small" color="warning" variant="outlined" label={t('notifications.requiresAcknowledgmentShort')} sx={{ mt: 1 }} />
              )}
            </Box>
          </Box>
        </CardContent>
      </Card>

      <Stack direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap', mb: 2 }}>
        <Stat label={t('notifications.stat.recipients')} value={total} note={data.withoutAccount > 0 ? t('notifications.stat.withoutAccount', { count: data.withoutAccount }) : undefined} />
        <Stat label={t('notifications.stat.read')} value={read} color="success.main" percent={total ? Math.round((100 * read) / total) : 0} />
        {data.requiresAcknowledgment && (
          <Stat label={t('notifications.stat.acked')} value={acked} color="warning.main" percent={total ? Math.round((100 * acked) / total) : 0} />
        )}
        <Stat label={t('notifications.stat.unread')} value={unread} color={unread > 0 ? 'error.main' : undefined} />
      </Stack>

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 2, alignItems: 'center' }}>
        {tabs
          .filter((x) => x.show)
          .map((x) => (
            <Chip
              key={x.id}
              clickable
              color={tab === x.id ? 'primary' : 'default'}
              variant={tab === x.id ? 'filled' : 'outlined'}
              label={x.label}
              onClick={() => setTab(x.id)}
            />
          ))}
        <Box sx={{ flex: 1 }} />
        <Button
          variant="contained"
          startIcon={<NotificationsActiveOutlined />}
          disabled={toRemind === 0 || remind.isPending}
          onClick={sendReminder}
        >
          {t('notifications.remind', { count: toRemind })}
        </Button>
      </Stack>

      {reminded != null && (
        <Alert severity={reminded > 0 ? 'success' : 'info'} onClose={() => setReminded(null)} sx={{ mb: 2 }}>
          {reminded > 0 ? t('notifications.reminded', { count: reminded }) : t('notifications.remindedNone')}
        </Alert>
      )}
      {remind.isError && <Alert severity="error" sx={{ mb: 2 }}>{toApiError(remind.error).message}</Alert>}

      <Card variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('notifications.col.recipient')}</TableCell>
              <TableCell>{t('notifications.col.via')}</TableCell>
              <TableCell>{t('notifications.col.delivered')}</TableCell>
              <TableCell>{t('notifications.col.readAt')}</TableCell>
              {data.requiresAcknowledgment && <TableCell>{t('notifications.col.ackAt')}</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {shown.map((person) => {
              const c = cell(person);
              return (
                <TableRow key={person.userId}>
                  <TableCell>
                    <Typography sx={{ fontWeight: 600 }}>{person.name}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {person.role}
                      {person.reminders > 0 && ` · ${t('notifications.remindersCount', { count: person.reminders })}`}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {person.via ?? '—'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip size="small" variant="outlined" color={person.hasDevice ? 'success' : 'warning'} label={c.delivered} />
                  </TableCell>
                  <TableCell>
                    <Chip size="small" variant="outlined" color={person.isRead ? 'success' : 'error'} label={c.read} />
                  </TableCell>
                  {data.requiresAcknowledgment && (
                    <TableCell>
                      <Chip
                        size="small"
                        variant="outlined"
                        color={person.acknowledgedAt ? 'success' : person.isRead ? 'warning' : 'error'}
                        label={c.acked}
                      />
                    </TableCell>
                  )}
                </TableRow>
              );
            })}
            {shown.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>
                  <Typography color="text.secondary">{t('notifications.noneInTab')}</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Card>

      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
        {t('notifications.remindNote')}
      </Typography>
    </Box>
  );
}
