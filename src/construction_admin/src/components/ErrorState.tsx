import { LockOutlined, RefreshRounded, WifiOffRounded } from '@mui/icons-material';
import { Box, Button, Stack, Typography } from '@mui/material';

import { ApiError } from '../api/apiError';

/** Full-page or full-panel failure state with an optional retry action. */
export function ErrorState({
  error,
  onRetry,
}: {
  error: unknown;
  onRetry?: () => void;
}) {
  const message =
    error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';

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
            Try again
          </Button>
        )}
      </Stack>
    </Box>
  );
}
