import type { ComponentType } from 'react';

import { canViewFinance, canViewFinanceStatistics } from '../../auth/authHelpers';
import type { User } from '../../api/types';

import type { MessageKey } from '../../i18n/en';
import { AbsencesBalanceWidget } from './widgets/AbsencesBalanceWidget';
import { CompanyKpiWidget } from './widgets/CompanyKpiWidget';
import { CostTrendWidget } from './widgets/CostTrendWidget';
import { CostBreakdownWidget } from './widgets/CostBreakdownWidget';
import { DocumentExpiryWidget } from './widgets/DocumentExpiryWidget';
import { FinanceOverviewWidget } from './widgets/FinanceOverviewWidget';
import { FleetStatusWidget } from './widgets/FleetStatusWidget';
import { IncomeVsExpenseWidget } from './widgets/IncomeVsExpenseWidget';
import { LiveMapWidget } from './widgets/LiveMapWidget';
import { NeedsAttentionWidget } from './widgets/NeedsAttentionWidget';
import { NotificationsBulletinWidget } from './widgets/NotificationsBulletinWidget';
import { ProfitByProjectWidget } from './widgets/ProfitByProjectWidget';
import { FinanceStatisticsWidget } from './widgets/FinanceStatisticsWidget';
import { ProjectFocusWidget } from './widgets/ProjectFocusWidget';
import { ProjectsOverBudgetWidget } from './widgets/ProjectsOverBudgetWidget';
import { ProjectsRealizationWidget } from './widgets/ProjectsRealizationWidget';
import { SpendingTrendWidget } from './widgets/SpendingTrendWidget';
import { TodayAttendanceWidget } from './widgets/TodayAttendanceWidget';
import { TopProjectsByExpenseWidget } from './widgets/TopProjectsByExpenseWidget';
import type { DashboardWidgetProps, DashboardWidgetType } from './widgetTypes';

interface WidgetRegistryEntry {
  component: ComponentType<DashboardWidgetProps>;
  titleKey: MessageKey;
  /**
   * What the widget shows of the company's money, and so what an account needs to see it:
   * `'full'` for amounts, `'statistics'` for percentages only. Absent for a widget that shows none.
   */
  finance?: 'full' | 'statistics';
}

/**
 * The catalog of widgets a dashboard can host. Adding a fifth widget is:
 * write the component, add one entry here — the grid and the picker both
 * read off this map and need no other change.
 */
export const widgetRegistry: Record<DashboardWidgetType, WidgetRegistryEntry> = {
  CompanyKpi: {
    component: CompanyKpiWidget,
    titleKey: 'dashboard.widget.CompanyKpi',
  },
  ProjectsRealization: {
    component: ProjectsRealizationWidget,
    titleKey: 'dashboard.widget.ProjectsRealization',
    finance: 'full',
  },
  AbsencesBalance: {
    component: AbsencesBalanceWidget,
    titleKey: 'dashboard.widget.AbsencesBalance',
  },
  NotificationsBulletin: {
    component: NotificationsBulletinWidget,
    titleKey: 'dashboard.widget.NotificationsBulletin',
  },
  DocumentExpiry: {
    component: DocumentExpiryWidget,
    titleKey: 'dashboard.widget.DocumentExpiry',
  },
  FleetStatus: {
    component: FleetStatusWidget,
    titleKey: 'dashboard.widget.FleetStatus',
  },
  NeedsAttention: {
    component: NeedsAttentionWidget,
    titleKey: 'dashboard.widget.NeedsAttention',
  },
  LiveMap: {
    component: LiveMapWidget,
    titleKey: 'dashboard.widget.LiveMap',
  },
  TodayAttendance: {
    component: TodayAttendanceWidget,
    titleKey: 'dashboard.widget.TodayAttendance',
  },
  CostTrend: {
    component: CostTrendWidget,
    titleKey: 'dashboard.widget.CostTrend',
    finance: 'full',
  },
  FinanceOverview: {
    component: FinanceOverviewWidget,
    titleKey: 'dashboard.widget.FinanceOverview',
    finance: 'full',
  },
  IncomeVsExpense: {
    component: IncomeVsExpenseWidget,
    titleKey: 'dashboard.widget.IncomeVsExpense',
    finance: 'full',
  },
  TopProjectsByExpense: {
    component: TopProjectsByExpenseWidget,
    titleKey: 'dashboard.widget.TopProjectsByExpense',
    finance: 'full',
  },
  ProfitByProject: {
    component: ProfitByProjectWidget,
    titleKey: 'dashboard.widget.ProfitByProject',
    finance: 'full',
  },
  CostBreakdown: {
    component: CostBreakdownWidget,
    titleKey: 'dashboard.widget.CostBreakdown',
    finance: 'full',
  },
  ProjectFocus: {
    component: ProjectFocusWidget,
    titleKey: 'dashboard.widget.ProjectFocus',
    finance: 'full',
  },
  ProjectsOverBudget: {
    component: ProjectsOverBudgetWidget,
    titleKey: 'dashboard.widget.ProjectsOverBudget',
    finance: 'full',
  },
  SpendingTrend: {
    component: SpendingTrendWidget,
    titleKey: 'dashboard.widget.SpendingTrend',
    finance: 'full',
  },
  FinanceStatistics: {
    component: FinanceStatisticsWidget,
    titleKey: 'dashboard.widget.FinanceStatistics',
    finance: 'statistics',
  },
};

/**
 * Whether the account may have this widget on its board: one that shows amounts needs
 * the full finance right, one that shows only percentages needs at least the statistics right.
 * Server-side the calls are refused all the same; this only decides what is drawn and offered.
 */
export function widgetAllowed(type: DashboardWidgetType, user: User | null | undefined): boolean {
  switch (widgetRegistry[type]?.finance) {
    case 'full':
      return canViewFinance(user);
    case 'statistics':
      return canViewFinanceStatistics(user);
    default:
      return true;
  }
}
