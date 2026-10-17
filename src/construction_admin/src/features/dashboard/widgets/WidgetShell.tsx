import { CloseOutlined, DragIndicatorOutlined, OpenInFullOutlined, TuneOutlined } from '@mui/icons-material';
import { Alert, Box, IconButton, Skeleton, Stack, Tooltip, Typography } from '@mui/material';
import type { ReactNode } from 'react';

import { useT } from '../../../i18n/useI18n';

interface WidgetShellProps {
  title: string;
  /** A short second line next to the title: the period, the count, the day. */
  subtitle?: string;
  isLoading?: boolean;
  error?: unknown;
  onRemove?: () => void;
  onExpandWidth?: () => void;
  /** Opens the widget's own settings. Only widgets that have any pass it. */
  onConfigure?: () => void;
  children: ReactNode;
}

const SPRING = 'cubic-bezier(0.34, 1.3, 0.5, 1)';

/**
 * The card frame every dashboard widget shares: title and an optional second line, the controls, the loading
 * and error states. It fills 100% of whatever pixel box the free-form grid (DashboardGrid.tsx) has given it, so
 * a widget's width and height come entirely from the person's own drag and resize.
 *
 * The body is a size container: a widget lays itself out from the width it was given (`@container (min-width:
 * …)`), not from the screen's, so the same widget is a compact card on a phone, and spreads into columns when it
 * is stretched across a wide monitor.
 *
 * No outline, a soft shadow and a small lift under the pointer. The controls (move, settings, wider, remove)
 * show when the card is pointed at or has focus, and always on a touch screen, where there is no pointing.
 */
export function WidgetShell({ title, subtitle, isLoading, error, onRemove, onExpandWidth, onConfigure, children }: WidgetShellProps) {
  const t = useT();

  return (
    <Box
      component="section"
      aria-label={title}
      sx={(theme) => ({
        height: '100%',
        width: '100%',
        borderRadius: '22px',
        bgcolor: 'background.paper',
        boxShadow:
          theme.palette.mode === 'dark'
            ? 'inset 0 0 0 1px rgba(255, 255, 255, 0.06)'
            : '0 1px 2px rgba(31, 27, 22, 0.06), 0 10px 26px -12px rgba(31, 27, 22, 0.14)',
        transition: `box-shadow 0.3s ease, transform 0.45s ${SPRING}`,
        position: 'relative',
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
        '@media (hover: hover) and (pointer: fine)': {
          '&:hover': {
            transform: 'translateY(-2px)',
            boxShadow:
              theme.palette.mode === 'dark'
                ? 'inset 0 0 0 1px rgba(255, 255, 255, 0.1)'
                : '0 2px 4px rgba(31, 27, 22, 0.08), 0 18px 36px -14px rgba(31, 27, 22, 0.22)',
          },
        },
        '@media (prefers-reduced-motion: reduce)': { transition: 'none', '&:hover': { transform: 'none' } },
        // The controls: out of the way until the card is pointed at or focused; always there on touch.
        '& .widget-controls': { opacity: 0, transition: 'opacity 0.2s ease' },
        '&:hover .widget-controls, &:focus-within .widget-controls': { opacity: 1 },
        '@media (hover: none)': { '& .widget-controls': { opacity: 1 } },
      })}
    >
      <Stack direction="row" sx={{ alignItems: 'baseline', gap: 1, px: 2.5, pt: 2.25, pb: 1.25, flexShrink: 0, minHeight: 44 }}>
        <Typography component="h3" sx={{ fontSize: '0.95rem', fontWeight: 600, letterSpacing: '0.005em', minWidth: 0 }} noWrap>
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary" noWrap sx={{ flex: 1, minWidth: 0 }}>
            {subtitle}
          </Typography>
        )}
        {!subtitle && <Box sx={{ flex: 1 }} />}
        <Stack className="widget-controls" direction="row" sx={{ alignItems: 'center', alignSelf: 'center', flexShrink: 0 }}>
          {/* Matched by dragConfig.handle in DashboardGrid.tsx: react-grid-layout wires its pointer
              listeners to any element with this class. Only in the free-form grid (which passes onRemove);
              the stacked phone layout cannot drag, so a handle there would be a dead control. */}
          {onRemove && (
            <Tooltip title={t('dashboard.dragToMove')}>
              <Box
                className="widget-drag-handle"
                sx={{ display: 'flex', cursor: 'grab', color: 'text.secondary', p: 0.5, '&:active': { cursor: 'grabbing' } }}
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
      </Stack>

      {/* A flex column, not a plain block: widgets with several sections (a list plus a chart) give the
          chart `flex: 1` so it grows to fill whatever height a resize hands it. The size container makes
          the widget's own width, not the screen's, decide its layout. */}
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'auto',
          px: 2.5,
          pb: 2.5,
          containerType: 'inline-size',
          containerName: 'widget',
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
      </Box>
    </Box>
  );
}
