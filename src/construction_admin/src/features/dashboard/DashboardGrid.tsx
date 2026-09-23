import { AddOutlined } from '@mui/icons-material';
import { Alert, Box, Button, CircularProgress, Stack, Typography, useMediaQuery, useTheme } from '@mui/material';
import { GridLayout, useContainerWidth, type Layout, type LayoutItem } from 'react-grid-layout';
import 'react-grid-layout/css/styles.css';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useT } from '../../i18n/useI18n';
import { dashboardApi } from './api';
import './dashboardGrid.css';
import { widgetRegistry } from './widgetRegistry';
import type { DashboardWidgetConfig } from './widgetTypes';
import { WidgetPicker } from './WidgetPicker';

/** Grid units, not pixels — a widget's w/h is how many of these it spans. */
const GRID_COLS = 12;
const GRID_MARGIN: readonly [number, number] = [16, 16];
const MIN_W = 3;
const MIN_H = 4;
const DEFAULT_NEW_W = 6;
const DEFAULT_NEW_H = 12;
const SAVE_DEBOUNCE_MS = 600;

/**
 * Column width already scales with the screen (it's containerWidth / cols),
 * so a 6-wide widget is already half of whatever monitor it's on. Row height
 * doesn't get that for free — it was a flat 28px, so the *height* of every
 * widget stayed identical on a 1080p laptop and a 50-inch 4K display, and the
 * extra vertical real estate on the big screen just sat empty below the
 * board. Deriving it from the viewport height instead means a taller screen
 * gets taller rows, so the same h:12 widget actually fills more of it.
 */
function useViewportHeight(): number {
  const [height, setHeight] = useState(() => (typeof window === 'undefined' ? 900 : window.innerHeight));
  useEffect(() => {
    const onResize = () => setHeight(window.innerHeight);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);
  return height;
}

function toLayoutItem(widget: DashboardWidgetConfig): LayoutItem {
  return { i: widget.id, x: widget.x, y: widget.y, w: widget.w, h: widget.h, minW: MIN_W, minH: MIN_H };
}

function applyLayout(widgets: DashboardWidgetConfig[], layout: Layout): DashboardWidgetConfig[] {
  const byId = new Map(layout.map((item) => [item.i, item]));
  return widgets.map((widget) => {
    const item = byId.get(widget.id);
    return item ? { ...widget, x: item.x, y: item.y, w: item.w, h: item.h } : widget;
  });
}

/**
 * react-grid-layout re-derives its own internal layout from the `layout`
 * prop and the rendered children on every render, and calls onLayoutChange
 * whenever that internal state changes — including reference-only changes
 * that carry the same x/y/w/h values. Feeding every one of those back into
 * React state (a brand-new `widgets` array, a brand-new `layout` prop, a
 * brand-new render) can retrigger the same recompute indefinitely. Comparing
 * by value here, and only touching state when something actually moved or
 * resized, is what breaks that cycle.
 */
function layoutsEqual(a: DashboardWidgetConfig[], b: DashboardWidgetConfig[]): boolean {
  if (a.length !== b.length) return false;
  const byId = new Map(b.map((w) => [w.id, w]));
  return a.every((w) => {
    const other = byId.get(w.id);
    return !!other && w.x === other.x && w.y === other.y && w.w === other.w && w.h === other.h;
  });
}

/**
 * The dashboard's widget board. Loads the user's saved layout (or a sensible
 * default), and lets them drag any widget to any position and resize it to
 * any width/height on a free-form grid — not two fixed columns — persisting
 * every change to the backend, debounced so a drag or resize collapses into
 * one save. Below `md` there's no room for dragging or resizing precisely,
 * so widgets fall back to a plain read-only stack in saved order.
 */
export function DashboardGrid() {
  const t = useT();
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const queryClient = useQueryClient();
  const { width, containerRef, mounted } = useContainerWidth();
  const viewportHeight = useViewportHeight();
  const rowHeight = useMemo(() => Math.round(Math.min(120, Math.max(28, viewportHeight / 22))), [viewportHeight]);
  const [widgets, setWidgets] = useState<DashboardWidgetConfig[] | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const saveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard', 'layout'] as const,
    queryFn: () => dashboardApi.getLayout(),
  });

  useEffect(() => {
    if (data) setWidgets(data.widgets);
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: (next: DashboardWidgetConfig[]) => dashboardApi.saveLayout({ widgets: next }),
    onSuccess: (saved) => {
      queryClient.setQueryData(['dashboard', 'layout'], saved);
    },
  });

  const scheduleSave = (next: DashboardWidgetConfig[]) => {
    if (saveTimer.current) clearTimeout(saveTimer.current);
    saveTimer.current = setTimeout(() => saveMutation.mutate(next), SAVE_DEBOUNCE_MS);
  };

  useEffect(() => {
    return () => {
      if (saveTimer.current) clearTimeout(saveTimer.current);
    };
  }, []);

  const layout = useMemo(() => (widgets ?? []).map(toLayoutItem), [widgets]);
  const usedTypes = new Set((widgets ?? []).map((w) => w.type));

  const gridConfig = useMemo(
    () => ({ cols: GRID_COLS, rowHeight, margin: GRID_MARGIN, containerPadding: [0, 0] as const, maxRows: Infinity }),
    [rowHeight],
  );
  const dragConfig = useMemo(() => ({ enabled: true, handle: '.widget-drag-handle' }), []);
  // All eight handles, not just the bottom-right corner: a widget that isn't
  // already sitting at the left edge can only reach the grid's right edge by
  // growing from its own left side too (growing from 'se' alone is capped at
  // `cols - x`, which reads as "stuck" no matter how far the user drags, and
  // no amount of browser zoom changes that — it's a column-count limit, not
  // a pixel one).
  const resizeConfig = useMemo(
    () => ({ enabled: true, handles: ['se', 'sw', 'ne', 'nw', 'e', 'w', 'n', 's'] as const }),
    [],
  );

  const handleLayoutChange = useCallback((next: Layout) => {
    setWidgets((prev) => {
      if (!prev) return prev;
      const updated = applyLayout(prev, next);
      if (layoutsEqual(prev, updated)) return prev;
      scheduleSave(updated);
      return updated;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleRemove(id: string) {
    if (!widgets) return;
    const next = widgets.filter((w) => w.id !== id);
    setWidgets(next);
    scheduleSave(next);
  }

  /**
   * The reliable alternative to dragging a resize handle exactly to the
   * grid's edge: from any x position, one click snaps a widget to x:0 and
   * the full column count, so "as wide as the board allows" never depends on
   * how precisely the user can grab a corner.
   */
  function handleExpandWidth(id: string) {
    if (!widgets) return;
    const next = widgets.map((w) => (w.id === id ? { ...w, x: 0, w: GRID_COLS } : w));
    setWidgets(next);
    scheduleSave(next);
  }

  function handleAdd(type: DashboardWidgetConfig['type']) {
    if (!widgets) return;
    const next = [
      ...widgets,
      { id: crypto.randomUUID(), type, x: 0, y: Infinity, w: DEFAULT_NEW_W, h: DEFAULT_NEW_H },
    ];
    setWidgets(next);
    scheduleSave(next);
    setPickerOpen(false);
  }

  // `containerRef` (from useContainerWidth) has to be attached on the very
  // first render for its mount effect to ever measure anything — the effect
  // re-runs only when the hook's own memoized callback changes, which it
  // never does, so a ref that shows up for the first time on a *later*
  // render (e.g. behind an `if (isLoading) return <Spinner />` guard above
  // this element) never gets measured at all, leaving `width` stuck at the
  // hook's 1280px fallback forever. Every state below therefore renders
  // inside this same always-mounted Box instead of behind an early return.
  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'flex-end', mb: 2 }}>
        <Button startIcon={<AddOutlined />} variant="outlined" size="small" onClick={() => setPickerOpen(true)}>
          {t('dashboard.addWidget')}
        </Button>
      </Stack>

      <Box ref={containerRef}>
        {isLoading || !widgets ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        ) : error ? (
          <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
        ) : widgets.length === 0 ? (
          <Typography color="text.secondary">{t('dashboard.empty')}</Typography>
        ) : !isDesktop ? (
          // Narrow viewport: no room to drag or resize precisely, so widgets
          // just stack in their saved order, full width, no editing affordances.
          <Stack spacing={2}>
            {[...widgets]
              .sort((a, b) => a.y - b.y || a.x - b.x)
              .map((widget) => {
                const entry = widgetRegistry[widget.type];
                if (!entry) return null;
                const Widget = entry.component;
                return (
                  <Box key={widget.id} sx={{ minHeight: 200 }}>
                    <Widget instanceId={widget.id} />
                  </Box>
                );
              })}
          </Stack>
        ) : (
          mounted && (
            <GridLayout
              width={width}
              layout={layout}
              gridConfig={gridConfig}
              dragConfig={dragConfig}
              resizeConfig={resizeConfig}
              onLayoutChange={handleLayoutChange}
            >
              {widgets.map((widget) => {
                const entry = widgetRegistry[widget.type];
                if (!entry) return null;
                const Widget = entry.component;
                return (
                  <div key={widget.id}>
                    <Widget
                      instanceId={widget.id}
                      onRemove={() => handleRemove(widget.id)}
                      onExpandWidth={widget.x === 0 && widget.w === GRID_COLS ? undefined : () => handleExpandWidth(widget.id)}
                    />
                  </div>
                );
              })}
            </GridLayout>
          )
        )}
      </Box>

      <WidgetPicker open={pickerOpen} excludeTypes={usedTypes} onClose={() => setPickerOpen(false)} onPick={handleAdd} />
    </Box>
  );
}
