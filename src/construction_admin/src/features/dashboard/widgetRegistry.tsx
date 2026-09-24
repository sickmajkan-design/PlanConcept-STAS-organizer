import type { ComponentType } from 'react';

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
import { ProjectFocusWidget } from './widgets/ProjectFocusWidget';
import { ProjectsRealizationWidget } from './widgets/ProjectsRealizationWidget';
import { TodayAttendanceWidget } from './widgets/TodayAttendanceWidget';
import { TopProjectsByExpenseWidget } from './widgets/TopProjectsByExpenseWidget';
import type { DashboardWidgetProps, DashboardWidgetType } from './widgetTypes';

interface WidgetRegistryEntry {
  component: ComponentType<DashboardWidgetProps>;
  titleKey: MessageKey;
  /** Shows amounts of the company's money — hidden from anyone without the finance right. */
  requiresFinance?: boolean;
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
    requiresFinance: true,
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
    requiresFinance: true,
  },
  FinanceOverview: {
    component: FinanceOverviewWidget,
    titleKey: 'dashboard.widget.FinanceOverview',
    requiresFinance: true,
  },
  IncomeVsExpense: {
    component: IncomeVsExpenseWidget,
    titleKey: 'dashboard.widget.IncomeVsExpense',
    requiresFinance: true,
  },
  TopProjectsByExpense: {
    component: TopProjectsByExpenseWidget,
    titleKey: 'dashboard.widget.TopProjectsByExpense',
    requiresFinance: true,
  },
  ProfitByProject: {
    component: ProfitByProjectWidget,
    titleKey: 'dashboard.widget.ProfitByProject',
    requiresFinance: true,
  },
  CostBreakdown: {
    component: CostBreakdownWidget,
    titleKey: 'dashboard.widget.CostBreakdown',
    requiresFinance: true,
  },
  ProjectFocus: {
    component: ProjectFocusWidget,
    titleKey: 'dashboard.widget.ProjectFocus',
    requiresFinance: true,
  },
};
