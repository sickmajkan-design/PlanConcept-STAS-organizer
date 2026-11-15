import ArrowForwardIosRounded from '@mui/icons-material/ArrowForwardIosRounded';
import CleaningServicesOutlined from '@mui/icons-material/CleaningServicesOutlined';
import MonitorHeartOutlined from '@mui/icons-material/MonitorHeartOutlined';
import StorageOutlined from '@mui/icons-material/StorageOutlined';
import { Box, Button, Card, CardContent, Chip, LinearProgress, Stack, Typography, alpha } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { systemApi, type StorageStatus } from '../../api/system';
import { paths } from '../../routes/paths';
import { useT } from '../../i18n/useI18n';
import { StorageCleanupDialog } from './StorageCleanupDialog';
import { formatSize } from './formatSize';

const TONE = { 0: 'success', 1: 'warning', 2: 'error' } as const;

/** Pill buttons in the style of the server page: round, quiet, and a little press on touch (only transform, so it stays smooth). */
const pill = {
  borderRadius: 999,
  px: 1.75,
  py: 0.5,
  fontWeight: 600,
  textTransform: 'none',
  transition: 'transform 160ms cubic-bezier(0.23, 1, 0.32, 1), background-color 160ms ease',
  '&:active': { transform: 'scale(0.97)' },
  '@media (prefers-reduced-motion: reduce)': { transition: 'none', '&:active': { transform: 'none' } },
} as const;

/**
 * How much room the server has left, always on screen for the Super Admin, so a disk that is filling up is noticed
 * before it is full. The same level drives the line on the home list and the daily notice.
 */
export function StorageCard() {
  const { data } = useQuery({
    queryKey: ['system', 'storage'] as const,
    queryFn: systemApi.storage,
    refetchInterval: 5 * 60_000,
  });

  if (!data) {
    return null;
  }

  return <StorageCardView status={data} />;
}

export function StorageCardView({ status }: { status: StorageStatus }) {
  const t = useT();
  const [cleaning, setCleaning] = useState(false);
  const used = status.known && status.totalBytes > 0 ? (status.usedBytes / status.totalBytes) * 100 : 0;
  const tone = TONE[status.level];
  const levelLabel =
    status.level === 2 ? t('storage.levelCritical') : status.level === 1 ? t('storage.levelLow') : t('storage.levelOk');

  return (
    <Card variant="outlined" sx={{ mb: 2 }} data-testid="storage-card">
      <CardContent>
        <Stack direction="row" sx={{ mb: 1, alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
          <StorageOutlined fontSize="small" color="action" />
          <Typography variant="subtitle1" sx={{ flex: 1 }}>
            {t('storage.title')}
          </Typography>
          {status.known && <Chip size="small" color={tone} label={levelLabel} />}
          <Button
            size="small"
            component={RouterLink}
            to={paths.system}
            startIcon={<MonitorHeartOutlined fontSize="small" />}
            endIcon={<ArrowForwardIosRounded sx={{ fontSize: '12px !important' }} />}
            sx={(theme) => ({
              ...pill,
              color: 'primary.main',
              bgcolor: alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.18 : 0.1),
              '&:hover': { bgcolor: alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.26 : 0.16) },
            })}
          >
            {t('storage.details')}
          </Button>
          <Button
            size="small"
            variant="outlined"
            color="inherit"
            onClick={() => setCleaning(true)}
            startIcon={<CleaningServicesOutlined fontSize="small" />}
            sx={{ ...pill, borderColor: 'divider' }}
          >
            {t('storage.cleanup.open')}
          </Button>
        </Stack>

        {status.known ? (
          <>
            <LinearProgress variant="determinate" value={used} color={tone} sx={{ height: 8, borderRadius: 1, mb: 1 }} />
            <Typography variant="body2">
              {t('storage.free', {
                free: formatSize(status.freeBytes),
                total: formatSize(status.totalBytes),
                percent: Math.round(status.freePercent),
              })}
            </Typography>
          </>
        ) : (
          <Typography variant="body2" color="text.secondary">
            {t('storage.unknown')}
          </Typography>
        )}

        <Box sx={{ mt: 0.5 }}>
          <Typography variant="caption" color="text.secondary">
            {t('storage.documents', { size: formatSize(status.documentsBytes), count: status.documentsCount })}
          </Typography>
        </Box>
      </CardContent>
      <StorageCleanupDialog open={cleaning} onClose={() => setCleaning(false)} />
    </Card>
  );
}
