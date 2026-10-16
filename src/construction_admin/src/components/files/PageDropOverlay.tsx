import { CloudUploadOutlined } from '@mui/icons-material';
import { alpha, Box, Typography } from '@mui/material';

import { useT } from '../../i18n/useI18n';

/** Covers the page while a file is dragged over it, so it is clear that dropping anywhere will take it. */
export function PageDropOverlay({ show }: { show: boolean }) {
  const t = useT();

  if (!show) return null;

  return (
    <Box
      data-testid="page-drop-overlay"
      aria-hidden
      sx={(theme) => ({
        position: 'fixed',
        inset: 12,
        zIndex: theme.zIndex.modal + 1,
        display: 'grid',
        placeItems: 'center',
        pointerEvents: 'none',
        border: '3px dashed',
        borderColor: 'primary.main',
        borderRadius: 3,
        bgcolor: alpha(theme.palette.background.paper, 0.88),
      })}
    >
      <Box sx={{ textAlign: 'center', color: 'primary.main' }}>
        <CloudUploadOutlined sx={{ fontSize: 56 }} />
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          {t('files.drop.release')}
        </Typography>
      </Box>
    </Box>
  );
}
