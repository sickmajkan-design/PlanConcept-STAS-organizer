import { AddOutlined } from '@mui/icons-material';
import { Alert, Box, Button, CircularProgress, Grid, Stack, Typography } from '@mui/material';
import {
  DndContext,
  PointerSensor,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragOverEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  arrayMove,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useT } from '../../i18n/useI18n';
import { dashboardApi } from './api';
import { widgetRegistry } from './widgetRegistry';
import type { DashboardWidgetConfig } from './widgetTypes';
import { WidgetPicker } from './WidgetPicker';

const COLUMN_COUNT = 2;
const SAVE_DEBOUNCE_MS = 600;

// Module scope: DndContext resubscribes its listeners whenever it sees a new
// sensors array, so this must stay referentially stable across renders (same
// reasoning as AssignmentBoardPage's POINTER_ACTIVATION_CONSTRAINT).
const POINTER_ACTIVATION_CONSTRAINT = { activationConstraint: { distance: 8 } };

function columnDroppableId(column: number): string {
  return `column-${column}`;
}

function groupByColumn(widgets: DashboardWidgetConfig[]): DashboardWidgetConfig[][] {
  const columns: DashboardWidgetConfig[][] = Array.from({ length: COLUMN_COUNT }, () => []);

  for (const widget of [...widgets].sort((a, b) => a.order - b.order)) {
    (columns[widget.column] ?? columns[0]).push(widget);
  }

  return columns;
}

function flattenColumns(columns: DashboardWidgetConfig[][]): DashboardWidgetConfig[] {
  return columns.flatMap((widgets, column) =>
    widgets.map((widget, order) => ({ ...widget, column, order })),
  );
}

interface SortableWidgetProps {
  widget: DashboardWidgetConfig;
  onRemove: (id: string) => void;
}

function SortableWidget({ widget, onRemove }: SortableWidgetProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: widget.id,
  });
  const entry = widgetRegistry[widget.type];

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  if (!entry) return null;

  const Widget = entry.component;

  // The drag handle (and only the drag handle) carries the sortable
  // listeners — everything else in the widget (links, buttons, the remove
  // icon) stays clickable rather than being swallowed by a drag gesture.
  return (
    <Box ref={setNodeRef} style={style}>
      <Widget
        instanceId={widget.id}
        dragHandleProps={{ ...attributes, ...listeners }}
        onRemove={() => onRemove(widget.id)}
      />
    </Box>
  );
}

interface DashboardColumnProps {
  column: number;
  widgets: DashboardWidgetConfig[];
  onRemove: (id: string) => void;
}

function DashboardColumn({ column, widgets, onRemove }: DashboardColumnProps) {
  const { setNodeRef } = useDroppable({ id: columnDroppableId(column) });

  return (
    <Grid size={{ xs: 12, md: 6 }}>
      <Stack ref={setNodeRef} spacing={2} sx={{ minHeight: 80 }}>
        <SortableContext items={widgets.map((w) => w.id)} strategy={verticalListSortingStrategy}>
          {widgets.map((widget) => (
            <SortableWidget key={widget.id} widget={widget} onRemove={onRemove} />
          ))}
        </SortableContext>
      </Stack>
    </Grid>
  );
}

/**
 * The JIRA-style widget board Admin/SuperAdmin land on. Loads the user's
 * saved layout (or a sensible default), lets them drag widgets between two
 * columns, add from a picker, or remove one — persisting every change to
 * the backend, debounced so a flurry of drags collapses into one save.
 */
export function DashboardGrid() {
  const t = useT();
  const queryClient = useQueryClient();
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

  const columns = useMemo(() => groupByColumn(widgets ?? []), [widgets]);
  const usedTypes = new Set((widgets ?? []).map((w) => w.type));

  const sensors = useSensors(useSensor(PointerSensor, POINTER_ACTIVATION_CONSTRAINT));

  function locate(id: string): { column: number; index: number } | null {
    for (let column = 0; column < columns.length; column += 1) {
      const index = columns[column].findIndex((w) => w.id === id);
      if (index !== -1) return { column, index };
    }
    return null;
  }

  function handleDragOver(event: DragOverEvent) {
    const { active, over } = event;
    if (!over || !widgets) return;

    const from = locate(String(active.id));
    if (!from) return;

    const overId = String(over.id);
    const overColumnMatch = overId.match(/^column-(\d)$/);
    const to = overColumnMatch
      ? { column: Number(overColumnMatch[1]), index: columns[Number(overColumnMatch[1])].length }
      : locate(overId);

    if (!to || (to.column === from.column && to.index === from.index)) return;

    const next = columns.map((col) => [...col]);
    const [moved] = next[from.column].splice(from.index, 1);
    next[to.column].splice(to.index, 0, { ...moved, column: to.column });

    setWidgets(flattenColumns(next));
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || !widgets) return;

    const from = locate(String(active.id));
    const overId = String(over.id);
    const to = overId.startsWith('column-') ? null : locate(overId);

    if (from && to && from.column === to.column && from.index !== to.index) {
      const next = columns.map((col) => [...col]);
      next[from.column] = arrayMove(next[from.column], from.index, to.index);
      const flattened = flattenColumns(next);
      setWidgets(flattened);
      scheduleSave(flattened);
      return;
    }

    scheduleSave(flattenColumns(columns));
  }

  function handleRemove(id: string) {
    if (!widgets) return;
    const next = flattenColumns(columns.map((col) => col.filter((w) => w.id !== id)));
    setWidgets(next);
    scheduleSave(next);
  }

  function handleAdd(type: DashboardWidgetConfig['type']) {
    if (!widgets) return;
    const shortestColumn = columns.reduce(
      (best, col, index) => (col.length < columns[best].length ? index : best),
      0,
    );
    const next = flattenColumns(
      columns.map((col, index) =>
        index === shortestColumn
          ? [...col, { id: crypto.randomUUID(), type, column: index, order: col.length }]
          : col,
      ),
    );
    setWidgets(next);
    scheduleSave(next);
    setPickerOpen(false);
  }

  if (isLoading || !widgets) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{t('common.somethingWentWrong')}</Alert>;
  }

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'flex-end', mb: 2 }}>
        <Button
          startIcon={<AddOutlined />}
          variant="outlined"
          size="small"
          onClick={() => setPickerOpen(true)}
        >
          {t('dashboard.addWidget')}
        </Button>
      </Stack>

      {widgets.length === 0 ? (
        <Typography color="text.secondary">{t('dashboard.empty')}</Typography>
      ) : (
        <DndContext sensors={sensors} onDragOver={handleDragOver} onDragEnd={handleDragEnd}>
          <Grid container spacing={2}>
            {columns.map((columnWidgets, column) => (
              <DashboardColumn
                key={column}
                column={column}
                widgets={columnWidgets}
                onRemove={handleRemove}
              />
            ))}
          </Grid>
        </DndContext>
      )}

      <WidgetPicker
        open={pickerOpen}
        excludeTypes={usedTypes}
        onClose={() => setPickerOpen(false)}
        onPick={handleAdd}
      />
    </Box>
  );
}
