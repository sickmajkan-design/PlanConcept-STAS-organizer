import { CampaignOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  LinearProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { PageHeader } from '../../components/PageHeader';
import { useBroadcastsQuery } from '../../features/notifications/useNotifications';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDateTime } from '../../utils/formatting';

type Filter = 'all' | 'ack' | 'unread';

const PAGE_SIZE = 20;

/** How far a count has come, as a bar with the figures under it. */
function Progress({ done, total, color }: { done: number; total: number; color: 'success' | 'warning' }) {
  const t = useT();
  const percent = total === 0 ? 0 : Math.round((100 * done) / total);
  const complete = total > 0 && done === total;

  return (
    <Box sx={{ minWidth: 120 }}>
      <LinearProgress variant="determinate" value={percent} color={color} sx={{ height: 8, borderRadius: 4 }} />
      <Typography variant="caption" color={complete ? 'success.main' : 'text.secondary'}>
        {t('notifications.progress', { done, total })}
        {complete && ` · ${t('notifications.everyone')}`}
      </Typography>
    </Box>
  );
}

/** The announcements that were sent, newest first, with how many people have read and confirmed each. */
export function BroadcastsListPage() {
  const t = useT();
  const navigate = useNavigate();
  const [filter, setFilter] = useState<Filter>('all');
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);

  const { data, isLoading, isError } = useBroadcastsQuery({
    pageNumber,
    pageSize: PAGE_SIZE,
    search: search.trim() || undefined,
    requiresAcknowledgment: filter === 'ack' ? true : undefined,
    notAllRead: filter === 'unread' ? true : undefined,
  });

  const choose = (next: Filter) => {
    setFilter(next);
    setPageNumber(1);
  };

  const filters: { id: Filter; label: string }[] = [
    { id: 'all', label: t('notifications.filter.all') },
    { id: 'ack', label: t('notifications.filter.ack') },
    { id: 'unread', label: t('notifications.filter.unread') },
  ];

  return (
    <Box>
      <PageHeader
        title={t('notifications.sentTitle')}
        description={t('notifications.sentDescription')}
        action={{ label: t('notifications.newTitle'), icon: <CampaignOutlined />, onClick: () => navigate(paths.notificationsNew) }}
      />

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 2, alignItems: 'center' }}>
        {filters.map((f) => (
          <Chip
            key={f.id}
            clickable
            color={filter === f.id ? 'primary' : 'default'}
            variant={filter === f.id ? 'filled' : 'outlined'}
            label={f.label}
            onClick={() => choose(f.id)}
          />
        ))}
        <Box sx={{ flex: 1 }} />
        <TextField
          size="small"
          label={t('notifications.searchSent')}
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPageNumber(1);
          }}
          sx={{ minWidth: 260 }}
        />
      </Stack>

      {isError && <Alert severity="error">{t('notifications.loadFailed')}</Alert>}

      <Card variant="outlined" sx={{ overflowX: 'auto' }}>
        {isLoading ? (
          <Stack sx={{ alignItems: 'center', py: 6 }}>
            <CircularProgress />
          </Stack>
        ) : (data?.items.length ?? 0) === 0 ? (
          <Typography color="text.secondary" sx={{ p: 3 }}>
            {t('notifications.sentEmpty')}
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('notifications.col.notice')}</TableCell>
                <TableCell>{t('notifications.col.audience')}</TableCell>
                <TableCell>{t('notifications.col.sent')}</TableCell>
                <TableCell>{t('notifications.col.sentBy')}</TableCell>
                <TableCell>{t('notifications.col.read')}</TableCell>
                <TableCell>{t('notifications.col.acknowledged')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data!.items.map((item) => (
                <TableRow
                  key={item.id}
                  hover
                  sx={{ cursor: 'pointer' }}
                  onClick={() => navigate(paths.notificationBroadcast(item.id))}
                >
                  <TableCell sx={{ maxWidth: 360 }}>
                    <Typography sx={{ fontWeight: 600 }} noWrap>
                      {item.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                      {item.preview}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                      {item.audience.map((chip) => (
                        <Chip key={chip.label} size="small" variant="outlined" label={chip.label} />
                      ))}
                    </Stack>
                  </TableCell>
                  <TableCell>{formatDateTime(item.sentAt)}</TableCell>
                  <TableCell>{item.sentBy ?? '—'}</TableCell>
                  <TableCell>
                    <Progress done={item.read} total={item.recipients} color="success" />
                  </TableCell>
                  <TableCell>
                    {item.requiresAcknowledgment ? (
                      <Progress done={item.acknowledged} total={item.recipients} color="warning" />
                    ) : (
                      <Typography variant="caption" color="text.secondary">
                        {t('notifications.noAckRequired')}
                      </Typography>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      {data && data.totalPages > 1 && (
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'center', alignItems: 'center', mt: 2 }}>
          <Button disabled={!data.hasPreviousPage} onClick={() => setPageNumber((p) => p - 1)}>
            {t('common.previous')}
          </Button>
          <Typography variant="body2">
            {data.pageNumber} / {data.totalPages}
          </Typography>
          <Button disabled={!data.hasNextPage} onClick={() => setPageNumber((p) => p + 1)}>
            {t('common.next')}
          </Button>
        </Stack>
      )}
    </Box>
  );
}
