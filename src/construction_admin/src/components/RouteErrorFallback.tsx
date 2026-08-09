import { HomeOutlined, RefreshRounded, ReportProblemOutlined } from '@mui/icons-material';
import { Box, Button, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import type { FallbackProps } from './ErrorBoundary';

/**
 * Shown in place of a screen that threw while rendering.
 *
 * It renders inside the layout, so the navigation drawer and the app bar are
 * still there — the failure is scoped to the one page, and the operator can
 * leave it without reloading. "Try again" re-renders the same screen, which is
 * worth offering because a fair share of these are a transient bad shape in
 * one response rather than a deterministic bug.
 *
 * The message itself is deliberately not the exception's. `error.message` on a
 * production build is minified React internals, it is never in the operator's
 * language, and it occasionally carries data from the record being rendered.
 * It goes to the console, where the person who can act on it will look.
 */
export function RouteErrorFallback({ reset }: FallbackProps) {
  const t = useT();

  return (
    <Box sx={{ py: 8, display: 'flex', justifyContent: 'center' }}>
      <Stack spacing={2} sx={{ alignItems: 'center', maxWidth: 460, textAlign: 'center' }}>
        <ReportProblemOutlined sx={{ fontSize: 44, color: 'warning.main' }} />
        <Typography variant="h6">{t('error.pageTitle')}</Typography>
        <Typography color="text.secondary">{t('error.pageBody')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="contained" startIcon={<RefreshRounded />} onClick={reset}>
            {t('common.retry')}
          </Button>
          <Button
            variant="outlined"
            component={Link}
            to={paths.home}
            startIcon={<HomeOutlined />}
          >
            {t('error.backHome')}
          </Button>
        </Stack>
      </Stack>
    </Box>
  );
}
