import { CloseOutlined, DragIndicatorOutlined, OpenInFullOutlined, TuneOutlined } from '@mui/icons-material';
import { Alert, Box, Card, CardContent, CardHeader, IconButton, Skeleton, Stack, Tooltip } from '@mui/material';
import type { ReactNode } from 'react';

import { useT } from '../../../i18n/useI18n';

interface WidgetShellProps {
  title: string;
  isLoading?: boolean;
  error?: unknown;
  onRemove?: () => void;
  onExpandWidth?: () => void;
  /** Opens the widget's own settings. Only widgets that have any pass it. */
  onConfigure?: () => void;
  children: ReactNode;
}

/**
 * The card frame every dashboard widget shares — title, drag handle, remove
 * affordance, loading/error states. Fills 100% of whatever pixel box the
 * free-form grid (DashboardGrid.tsx) has given it, so the widget's actual
 * width/height come entirely from the user's own drag/resize, not from a
 * fixed column or a preset size. A light neutral treatment (hairline border,
 * soft shadow, hover lift): the accent colour is kept for what can be pressed, not spent on every card.
 */
export function WidgetShell({ title, isLoading, error, onRemove, onExpandWidth, onConfigure, children }: WidgetShellProps) {
  const t = useT();

  return (
    <Card
      sx={(theme) => ({
        height: '100%',
        width: '100%',
        border: '1px solid',
        // A neutral line, not an orange one: the accent colour is kept for what can be pressed.
        borderColor: 'divider',
        borderRadius: 3,
        background:
          theme.palette.mode === 'dark'
            ? 'linear-gradient(165deg, #241e1a 0%, #1f1a16 55%, #1b1613 100%)'
            : 'linear-gradient(165deg, #ffffff 0%, #fbfbfa 55%, #f6f4f1 100%)',
        // Depth from a quiet neutral shadow; in the dark theme the line alone does it.
        boxShadow:
          theme.palette.mode === 'dark'
            ? 'none'
            : '0 1px 2px rgba(31, 27, 22, 0.06), 0 8px 20px -14px rgba(31, 27, 22, 0.16)',
        transition: 'box-shadow 0.22s cubic-bezier(0.16, 1, 0.3, 1)',
        position: 'relative',
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
        '&:hover': {
          boxShadow:
            theme.palette.mode === 'dark'
              ? 'none'
              : '0 2px 4px rgba(31, 27, 22, 0.08), 0 12px 26px -14px rgba(31, 27, 22, 0.2)',
        },
      })}
    >
      <CardHeader
        title={title}
        slotProps={{ title: { variant: 'h6', sx: { fontWeight: 600, fontSize: '1.05rem', letterSpacing: '-0.005em' } } }}
        action={
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            {/* Matched by dragConfig.handle in DashboardGrid.tsx — react-grid-layout
                wires pointer listeners to any element with this class, no
                per-instance props needed. */}
            {/* Only in the free-form grid (which passes onRemove); the stacked
                phone layout can't drag, so a handle there was a dead control. */}
            {onRemove && (
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
            )}
            {onConfigure && (
              <Tooltip title={t('dashboard.configureWidget')}>
                <IconButton size="small" onClick={onConfigure} aria-label={t('dashboard.configureWidget')}>
                  <TuneOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
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
          // The shape of what is about to appear, not a spinner in an empty card; still with reduced motion.
          <Stack
            spacing={1.5}
            aria-busy="true"
            sx={{ flex: 1, '& .MuiSkeleton-root': { '@media (prefers-reduced-motion: reduce)': { animation: 'none' } } }}
          >
            <Skeleton variant="rounded" height={22} width="45%" />
            <Skeleton variant="rounded" height={64} />
            <Skeleton variant="rounded" height={64} />
          </Stack>
        ) : error ? (
          <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
        ) : (
          children
        )}
      </CardContent>
    </Card>
  );
}
