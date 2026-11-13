import StorageOutlined from '@mui/icons-material/StorageOutlined';
import { Box, Card, CardContent, Chip, LinearProgress, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { systemApi, type StorageStatus } from '../../api/system';
import { useT } from '../../i18n/useI18n';

const GIGABYTE = 1024 * 1024 * 1024;
const MEGABYTE = 1024 * 1024;

/** `7.6 GB` for a disk, `52 MB` for a folder's worth: one decimal above a gigabyte, none below. */
export function formatSize(bytes: number): string {
  return bytes >= GIGABYTE ? `${(bytes / GIGABYTE).toFixed(1)} GB` : `${Math.round(bytes / MEGABYTE)} MB`;
}

const TONE = { 0: 'success', 1: 'warning', 2: 'error' } as const;

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
  const used = status.known && status.totalBytes > 0 ? (status.usedBytes / status.totalBytes) * 100 : 0;
  const tone = TONE[status.level];
  const levelLabel =
    status.level === 2 ? t('storage.levelCritical') : status.level === 1 ? t('storage.levelLow') : t('storage.levelOk');

  return (
    <Card variant="outlined" sx={{ mb: 2 }} data-testid="storage-card">
      <CardContent>
        <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
          <StorageOutlined fontSize="small" color="action" />
          <Typography variant="subtitle1" sx={{ flex: 1 }}>
            {t('storage.title')}
          </Typography>
          {status.known && <Chip size="small" color={tone} label={levelLabel} />}
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
    </Card>
  );
}
