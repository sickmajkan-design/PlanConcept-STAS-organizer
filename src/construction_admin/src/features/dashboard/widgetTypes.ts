/** Mirrors `DashboardWidgetTypes` in Construction.Application — keep in sync. */
export const dashboardWidgetTypes = [
  'ProjectsRealization',
  'AbsencesBalance',
  'NotificationsBulletin',
  'DocumentExpiry',
  'FleetStatus',
  'NeedsAttention',
] as const;

export type DashboardWidgetType = (typeof dashboardWidgetTypes)[number];

export interface DashboardWidgetConfig {
  id: string;
  type: DashboardWidgetType;
  column: number;
  order: number;
}

export interface DashboardLayout {
  widgets: DashboardWidgetConfig[];
}

/** Every widget component fetches its own data — the grid only positions it. */
export interface DashboardWidgetProps {
  instanceId: string;
  /** Spread onto the drag-handle element; undefined when the widget isn't draggable (e.g. a static preview). */
  dragHandleProps?: Record<string, unknown>;
  onRemove?: () => void;
}
