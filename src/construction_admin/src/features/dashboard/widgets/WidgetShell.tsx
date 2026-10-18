import {
  CheckCircleOutlined,
  CloseOutlined,
  DragIndicatorOutlined,
  HelpOutlineOutlined,
  HorizontalRuleOutlined,
  OpenInFullOutlined,
  TuneOutlined,
} from '@mui/icons-material';
import { Alert, Box, IconButton, Skeleton, Stack, Tooltip, Typography } from '@mui/material';
import { useEffect, useRef, type ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { useWidgetHeight, useWidgetHelp } from '../widgetHeight';

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
  /**
   * Nothing to show: the card becomes one line, the title and a sentence saying so, instead of a card with a
   * sentence in a large empty space. `ok` is the good kind of nothing (nothing is late, nothing is expiring) and
   * takes a green tick; `none` is nothing yet. The widget still mounts its body's data, so the strip becomes the
   * full card on its own as soon as there is something to show.
   */
  empty?: { text: ReactNode; tone?: 'ok' | 'none'; action?: ReactNode };
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
export function WidgetShell({ title, subtitle, isLoading, error, onRemove, onExpandWidth, onConfigure, empty, children }: WidgetShellProps) {
  const t = useT();
  const { fit, report, maxPixels } = useWidgetHeight();
  const helpTopic = useWidgetHelp();
  const strip = !!empty && !isLoading && !error;

  // The content's own height, for the grid: measured on the element that is as tall as its content, never on
  // the card, whose height the grid decides from this very number.
  const measured = useRef<HTMLDivElement | null>(null);
  useEffect(() => {
    const node = measured.current;
    if (!fit || !report || !node || typeof ResizeObserver === 'undefined') return;

    const observer = new ResizeObserver(() => report(Math.ceil(node.getBoundingClientRect().height)));
    observer.observe(node);
    report(Math.ceil(node.getBoundingClientRect().height));

    return () => observer.disconnect();
  }, [fit, report]);

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
        // A one-line strip sits in the middle of the rows the grid gave it, not stuck to the top of them.
        justifyContent: strip && fit ? 'center' : 'flex-start',
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
      <Box
        ref={measured}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          minHeight: 0,
          width: '100%',
          // As tall as its content (up to a limit, past which the body scrolls), or filling the box it was given.
          ...(fit ? { flex: '0 0 auto', maxHeight: maxPixels } : { flex: 1 }),
        }}
      >
      <Stack
        direction="row"
        sx={{
          alignItems: strip ? 'center' : 'baseline',
          // A strip may take two lines on a narrow card (the sentence, then the link) instead of squeezing its
          // sentence into a column a letter wide.
          flexWrap: strip ? 'wrap' : 'nowrap',
          columnGap: 1,
          rowGap: 0.25,
          px: 2.5,
          pt: strip ? 1.5 : 2.25,
          pb: strip ? 1.5 : 1.25,
          flexShrink: 0,
          minHeight: strip ? 48 : 44,
        }}
      >
        {strip && (
          <Box
            component="span"
            aria-hidden="true"
            sx={{ display: 'inline-flex', flexShrink: 0, color: empty?.tone === 'none' ? 'text.disabled' : 'success.main' }}
          >
            {empty?.tone === 'none' ? <HorizontalRuleOutlined fontSize="small" /> : <CheckCircleOutlined fontSize="small" />}
          </Box>
        )}
        <Typography
          component="h3"
          title={title}
          sx={{ fontSize: '0.95rem', fontWeight: 600, letterSpacing: '0.005em', minWidth: 0, ...(strip ? { maxWidth: '100%' } : {}) }}
          noWrap
        >
          {title}
        </Typography>
        {strip && (
          <Typography variant="body2" color="text.secondary" sx={{ flex: '1 1 150px', minWidth: 0 }}>
            {empty?.text}
          </Typography>
        )}
        {strip && empty?.action}
        {!strip && subtitle && (
          <Typography variant="body2" color="text.secondary" noWrap sx={{ flex: 1, minWidth: 0 }}>
            {subtitle}
          </Typography>
        )}
        {!strip && !subtitle && <Box sx={{ flex: 1 }} />}
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
          {helpTopic && (
            <Tooltip title={t('dashboard.widgetHelp')}>
              <IconButton
                size="small"
                component={RouterLink}
                to={paths.helpTopic(helpTopic)}
                aria-label={t('dashboard.widgetHelp')}
              >
                <HelpOutlineOutlined fontSize="small" />
              </IconButton>
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
      {!strip && (
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'auto',
          // Never a sideways scrollbar inside a card: whatever is wider than the card wraps or is cut by its own
          // element, it does not push the card's content out.
          overflowX: 'hidden',
          minWidth: 0,
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
      )}
      </Box>
    </Box>
  );
}
