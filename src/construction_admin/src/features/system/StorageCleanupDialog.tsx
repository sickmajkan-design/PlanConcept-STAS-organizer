import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { systemApi, type StorageCleanupCategory, type StorageCleanupKind, type StorageCleanupResult } from '../../api/system';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { formatSize } from './formatSize';

const TITLES: Record<StorageCleanupKind, MessageKey> = {
  OrphanFiles: 'storage.cleanup.orphanFiles.title',
  DeletedRecordDocuments: 'storage.cleanup.deletedRecordDocuments.title',
  DeletionTraces: 'storage.cleanup.deletionTraces.title',
};

const DESCRIPTIONS: Record<StorageCleanupKind, MessageKey> = {
  OrphanFiles: 'storage.cleanup.orphanFiles.description',
  DeletedRecordDocuments: 'storage.cleanup.deletedRecordDocuments.description',
  DeletionTraces: 'storage.cleanup.deletionTraces.description',
};

/**
 * What the Super Admin can clean off the server by hand, with how much each would free, and a confirmation before
 * anything is removed for good. Documents under a retention date are never offered.
 */
export function StorageCleanupDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const queryClient = useQueryClient();
  const [confirming, setConfirming] = useState<StorageCleanupCategory | null>(null);
  const [result, setResult] = useState<StorageCleanupResult | null>(null);

  const scan = useQuery({
    queryKey: ['system', 'storage', 'cleanup'] as const,
    queryFn: systemApi.cleanupScan,
    enabled: open,
  });

  const run = useMutation({
    mutationFn: (kind: StorageCleanupKind) => systemApi.cleanupRun(kind),
    onSuccess: async (done) => {
      setResult(done);
      await queryClient.invalidateQueries({ queryKey: ['system'] });
      await queryClient.invalidateQueries({ queryKey: ['attention'] });
    },
  });

  const categories = scan.data?.categories ?? [];

  return (
    <>
      <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
        <DialogTitle>{t('storage.cleanup.title')}</DialogTitle>
        <DialogContent>
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t('storage.cleanup.intro')}
          </Alert>

          {result && (
            <Alert severity="success" sx={{ mb: 2 }} data-testid="cleanup-result">
              {t('storage.cleanup.done', {
                files: result.removedFiles,
                rows: result.removedRows,
                size: formatSize(result.freedBytes),
              })}
            </Alert>
          )}

          {scan.isLoading && <Typography color="text.secondary">{t('common.loading')}</Typography>}
          {scan.error && <Alert severity="error">{t('storage.cleanup.failed')}</Alert>}

          <Stack divider={<Divider flexItem />} spacing={2}>
            {categories.map((category) => (
              <Box key={category.kind} data-testid={`cleanup-${category.kind}`}>
                <Typography variant="subtitle2">{t(TITLES[category.kind])}</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                  {t(DESCRIPTIONS[category.kind], { hours: scan.data?.orphanGraceHours ?? 24 })}
                </Typography>
                <Stack direction="row" sx={{ alignItems: 'center', gap: 2 }}>
                  <Typography variant="body2" sx={{ flex: 1 }}>
                    {category.count === 0
                      ? t('storage.cleanup.nothing')
                      : t('storage.cleanup.count', { count: category.count, size: formatSize(category.bytes) })}
                  </Typography>
                  <Button
                    size="small"
                    color="error"
                    variant="outlined"
                    disabled={category.count === 0 || run.isPending}
                    onClick={() => setConfirming(category)}
                  >
                    {t('storage.cleanup.remove')}
                  </Button>
                </Stack>
                {category.examples.length > 0 && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5, wordBreak: 'break-all' }}>
                    {category.examples.join(' · ')}
                  </Typography>
                )}
                {category.keptForRetention > 0 && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    {t('storage.cleanup.keptForRetention', { count: category.keptForRetention })}
                  </Typography>
                )}
              </Box>
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>{t('storage.cleanup.close')}</Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={confirming !== null}
        destructive
        title={t('storage.cleanup.confirmTitle')}
        description={
          confirming
            ? t('storage.cleanup.confirmText', {
                what: t(TITLES[confirming.kind]),
                count: confirming.count,
                size: formatSize(confirming.bytes),
              })
            : ''
        }
        confirmLabel={t('storage.cleanup.remove')}
        onConfirm={async () => {
          if (confirming) {
            await run.mutateAsync(confirming.kind);
            setConfirming(null);
          }
        }}
        onCancel={() => setConfirming(null)}
      />
    </>
  );
}
