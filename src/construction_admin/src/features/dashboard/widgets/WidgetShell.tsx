import { CloseOutlined, DragIndicatorOutlined, OpenInFullOutlined } from '@mui/icons-material';
import { Alert, Box, Card, CardContent, CardHeader, CircularProgress, IconButton, Stack, Tooltip } from '@mui/material';
import type { ReactNode } from 'react';

import { useT } from '../../../i18n/useI18n';

interface WidgetShellProps {
  title: string;
  isLoading?: boolean;
  error?: unknown;
  onRemove?: () => void;
  onExpandWidth?: () => void;
  children: ReactNode;
}

/**
 * The card frame every dashboard widget shares — title, drag handle, remove
 * affordance, loading/error states. Fills 100% of whatever pixel box the
 * free-form grid (DashboardGrid.tsx) has given it, so the widget's actual
 * width/height come entirely from the user's own drag/resize, not from a
 * fixed column or a preset size. Full 3D treatment (layered gradient, tinted
 * elevation, hover lift) so the board reads as tactile rather than flat.
 */
export function WidgetShell({ title, isLoading, error, onRemove, onExpandWidth, children }: WidgetShellProps) {
  const t = useT();

  return (
    <Card
      sx={{
        height: '100%',
        width: '100%',
        border: '1px solid',
        borderColor: 'rgba(230, 81, 0, 0.10)',
        borderRadius: 3,
        background: 'linear-gradient(165deg, #ffffff 0%, #fbfbfa 55%, #f6f4f1 100%)',
        boxShadow:
          '0 1px 2px rgba(31, 27, 22, 0.06), 0 10px 24px -12px rgba(31, 27, 22, 0.18), 0 20px 44px -26px rgba(230, 81, 0, 0.16)',
        transition: 'box-shadow 0.22s cubic-bezier(0.16, 1, 0.3, 1)',
        position: 'relative',
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
        '&:hover': {
          boxShadow:
            '0 2px 4px rgba(31, 27, 22, 0.09), 0 18px 36px -16px rgba(31, 27, 22, 0.24), 0 30px 60px -28px rgba(230, 81, 0, 0.22)',
        },
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          height: 3,
          background: 'linear-gradient(90deg, #e65100 0%, #f9a825 55%, transparent 100%)',
          opacity: 0.85,
        },
      }}
    >
      <CardHeader
        title={title}
        slotProps={{ title: { variant: 'h6', sx: { fontWeight: 800, fontSize: '1.05rem', letterSpacing: '-0.01em' } } }}
        action={
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            {/* Matched by dragConfig.handle in DashboardGrid.tsx — react-grid-layout
                wires pointer listeners to any element with this class, no
                per-instance props needed. */}
            <Tooltip title={t('dashboard.dragToMove')}>
              <Box
                className="widget-drag-handle"
                sx={{
                  display: 'flex',
                  cursor: 'grab',
                  color: 'text.disabled',
                  '&:active': { cursor: 'grabbing' },
                }}
              >
                <DragIndicatorOutlined fontSize="small" />
              </Box>
            </Tooltip>
            {onExpandWidth && (
              <Tooltip title={t('dashboard.expandWidth')}>
                <IconButton size="small" onClick={onExpandWidth} aria-label={t('dashboard.expandWidth')}>
                  <OpenInFullOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            {onRemove && (
              <Tooltip title={t('dashboard.removeWidget')}>
                <IconButton size="small" onClick={onRemove} aria-label={t('dashboard.removeWidget')}>
                  <CloseOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        }
        sx={{ pb: 0, pt: 2, px: 2.5, flexShrink: 0 }}
      />
      {/* A flex column, not a plain block: widgets with multiple sections
          (a list plus one or more charts) give the chart sections `flex: 1`
          so they grow to fill whatever extra height a resize hands them,
          instead of staying pinned at a fixed pixel height while the card
          around them just gets taller. */}
      <CardContent
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'auto',
          px: 2.5,
          pb: 2.5,
          '&:last-child': { pb: 2.5 },
        }}
      >
        {isLoading ? (
          <Box sx={{ display: 'flex', flex: 1, alignItems: 'center', justifyContent: 'center' }}>
            <CircularProgress size={28} />
          </Box>
        ) : error ? (
          <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
        ) : (
          children
        )}
      </CardContent>
    </Card>
  );
}
