import { LockOutlined, RefreshRounded, WifiOffRounded } from '@mui/icons-material';
import { Box, Button, Stack, Typography } from '@mui/material';

import { ApiError } from '../api/apiError';
import { useT } from '../i18n/useI18n';

/** Full-page or full-panel failure state with an optional retry action. */
export function ErrorState({
  error,
  onRetry,
}: {
  error: unknown;
  onRetry?: () => void;
}) {
  const t = useT();

  const message =
    error instanceof ApiError ? error.message : t('common.somethingWentWrong');

  const isForbidden = error instanceof ApiError && error.isForbidden;

  return (
    <Box sx={{ py: 8, display: 'flex', justifyContent: 'center' }}>
      <Stack spacing={2} sx={{ alignItems: 'center', maxWidth: 420, textAlign: 'center' }}>
        {isForbidden ? (
          <LockOutlined sx={{ fontSize: 40, color: 'text.secondary' }} />
        ) : (
          <WifiOffRounded sx={{ fontSize: 40, color: 'text.secondary' }} />
        )}
        <Typography color="text.secondary">{message}</Typography>
        {onRetry && !isForbidden && (
          <Button
            variant="outlined"
            startIcon={<RefreshRounded />}
            onClick={onRetry}
          >
            {t('common.retry')}
          </Button>
        )}
      </Stack>
    </Box>
  );
}
