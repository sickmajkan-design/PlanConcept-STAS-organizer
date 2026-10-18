import { ChevronRightOutlined } from '@mui/icons-material';
import { Box, Card, Skeleton, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { useTimeEntriesQuery } from '../timeEntries/useTimeEntries';
import type { SinceNote } from './useSinceYesterday';
import { DESTINATIONS, SUMMARY_GROUPS, useAttentionGroups } from './widgets/NeedsAttentionWidget';
import { SinceText } from './widgets/widgetUi';

/** The same page size the attendance widget uses, so both share one request. */
const ON_SITE_PAGE_SIZE = 100;

interface RowProps {
  count: number;
  label: string;
  to: string;
  /** Something is waiting on this person; the number takes the warning colour. */
  waiting?: boolean;
  since?: SinceNote | null;
}

function Row({ count, label, to, waiting, since }: RowProps) {
  return (
    <Box
      component={Link}
      to={to}
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        py: 1.25,
        color: 'inherit',
        textDecoration: 'none',
        borderBottom: '1px solid',
        borderColor: 'divider',
        '&:last-of-type': { borderBottom: 0 },
      }}
    >
      <Typography
        component="span"
        sx={{
          width: 32,
          flexShrink: 0,
          fontSize: '1.4rem',
          fontWeight: 700,
          fontVariantNumeric: 'tabular-nums',
          lineHeight: 1.1,
          color: waiting ? 'warning.main' : 'text.primary',
        }}
      >
        {count}
      </Typography>
      <Typography component="span" variant="body2" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
        {label}
      </Typography>
      <SinceText note={since ?? null} />
      <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled', flexShrink: 0 }} />
    </Box>
  );
}

/**
 * The first thing on a phone's home screen: who is on a shift and what is waiting for an answer, one line each,
 * every line a way to the list where it is dealt with. The cards below say the same things in more detail, but on a
 * phone they sit a screen further down.
 */
export function TodaySummary() {
  const t = useT();
  const onSite = useTimeEntriesQuery({ pageNumber: 1, pageSize: ON_SITE_PAGE_SIZE, openOnly: true });
  const { groups, sinceFor, isLoading } = useAttentionGroups();

  const onShift = onSite.data?.totalCount ?? 0;
  const waiting = groups.filter((g) => g.count > 0).slice(0, SUMMARY_GROUPS);
  const loading = isLoading || onSite.isLoading;

  return (
    <Card variant="outlined" sx={{ mb: 2, px: 2, py: 0.5 }} aria-label={t('home.today')}>
      {loading ? (
        <Stack spacing={1} sx={{ py: 1.5 }}>
          <Skeleton variant="text" height={28} />
          <Skeleton variant="text" height={28} />
        </Stack>
      ) : (
        <>
          <Row count={onShift} label={t('home.summary.onShift')} to={paths.timeEntries} />
          {waiting.map((group) => (
            <Row
              key={group.key}
              count={group.count}
              label={t(`attention.${group.key}` as MessageKey)}
              to={DESTINATIONS[group.key] ?? paths.home}
              waiting
              since={sinceFor(group.key)}
            />
          ))}
          {waiting.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 1.25 }}>
              {t('home.summary.nothingWaiting')}
            </Typography>
          )}
        </>
      )}
    </Card>
  );
}
