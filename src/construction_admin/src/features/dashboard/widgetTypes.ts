/** Mirrors `DashboardWidgetTypes` in Construction.Application — keep in sync. */
export const dashboardWidgetTypes = [
  'CompanyKpi',
  'ProjectsRealization',
  'AbsencesBalance',
  'NotificationsBulletin',
  'DocumentExpiry',
  'FleetStatus',
  'NeedsAttention',
  'LiveMap',
  'TodayAttendance',
  'CostTrend',
  'FinanceOverview',
  'IncomeVsExpense',
  'TopProjectsByExpense',
  'ProfitByProject',
  'CostBreakdown',
  'ProjectFocus',
  'ProjectsOverBudget',
  'SpendingTrend',
  'FinanceStatistics',
  'AbsenceRequests',
  'ActiveProjects',
  'ArticleOrders',
] as const;

export type DashboardWidgetType = (typeof dashboardWidgetTypes)[number];

/**
 * Position and size on the free-form board, in grid units (see GRID_COLS /
 * ROW_HEIGHT_PX in DashboardGrid.tsx) — not fixed columns. The user drags to
 * (x, y) and resizes to (w, h) directly; nothing here is a preset bucket.
 */
export interface DashboardWidgetConfig {
  id: string;
  type: DashboardWidgetType;
  x: number;
  y: number;
  w: number;
  h: number;
  /** Choices made on this one widget — which project it shows, how many rows — as plain text. */
  settings?: Record<string, string>;
}

export interface DashboardLayout {
  widgets: DashboardWidgetConfig[];
}

/** Every widget component fetches its own data — the grid only positions it. */
export interface DashboardWidgetProps {
  instanceId: string;
  /** This instance's own choices. Absent when it has made none. */
  settings?: Record<string, string>;
  /** Replaces the instance's choices. Absent where the board cannot be edited. */
  onSettingsChange?: (settings: Record<string, string>) => void;
  onRemove?: () => void;
  /** Snaps the widget to x:0 and the full column count in one click — the reliable alternative to dragging a resize handle exactly to the grid's edge. */
  onExpandWidth?: () => void;
}
