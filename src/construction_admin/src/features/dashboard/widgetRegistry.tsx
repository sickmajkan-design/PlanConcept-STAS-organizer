import type { ComponentType } from 'react';

import {
  canManageArticleOrders,
  canReviewAbsences,
  canViewDirectory,
  canViewFinance,
  canViewFinanceStatistics,
} from '../../auth/authHelpers';
import type { User } from '../../api/types';

import type { MessageKey } from '../../i18n/en';
import { AbsenceRequestsWidget } from './widgets/AbsenceRequestsWidget';
import { ArticleOrdersWidget } from './widgets/ArticleOrdersWidget';
import { AbsencesBalanceWidget } from './widgets/AbsencesBalanceWidget';
import { ActiveProjectsWidget } from './widgets/ActiveProjectsWidget';
import { BranchOverviewWidget } from './widgets/BranchOverviewWidget';
import { CompanyKpiWidget } from './widgets/CompanyKpiWidget';
import { DocumentExpiryWidget } from './widgets/DocumentExpiryWidget';
import { WeeklyReportsWidget } from './widgets/WeeklyReportsWidget';
import { FinanceWidget } from './widgets/FinanceWidget';
import { FleetStatusWidget } from './widgets/FleetStatusWidget';
import { IncomeVsExpenseWidget } from './widgets/IncomeVsExpenseWidget';
import { LiveMapWidget } from './widgets/LiveMapWidget';
import { NeedsAttentionWidget } from './widgets/NeedsAttentionWidget';
import { NotificationsBulletinWidget } from './widgets/NotificationsBulletinWidget';
import { ProfitByProjectWidget } from './widgets/ProfitByProjectWidget';
import { ProjectFocusWidget } from './widgets/ProjectFocusWidget';
import { ProjectsOverBudgetWidget } from './widgets/ProjectsOverBudgetWidget';
import { ProjectsRealizationWidget } from './widgets/ProjectsRealizationWidget';
import { TodayAttendanceWidget } from './widgets/TodayAttendanceWidget';
import { TopProjectsByExpenseWidget } from './widgets/TopProjectsByExpenseWidget';
import type { DashboardWidgetProps, DashboardWidgetType } from './widgetTypes';

interface WidgetRegistryEntry {
  component: ComponentType<DashboardWidgetProps>;
  titleKey: MessageKey;
  /** An extra condition on who may have this widget, beyond what it shows of the company's money. */
  allowed?: (user: User | null | undefined) => boolean;
  /**
   * What the widget shows of the company's money, and so what an account needs to see it:
   * `'full'` for amounts, `'statistics'` for percentages only. Absent for a widget that shows none.
   */
  finance?: 'full' | 'statistics';
  /**
   * The card is as tall as its content on the desktop board, unless the person has resized it. Left off for a
   * widget that draws a chart or a map to fill the box it is given: those need a box to fill.
   */
  fit?: boolean;
  /** The help topic that says what this widget is for and how each of its numbers is counted. */
  help: string;
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
    help: 'widgeti-pregled',
    fit: true,
  },
  ProjectsRealization: {
    component: ProjectsRealizationWidget,
    titleKey: 'dashboard.widget.ProjectsRealization',
    help: 'widgeti-projekti',
    finance: 'full',
  },
  AbsencesBalance: {
    component: AbsencesBalanceWidget,
    titleKey: 'dashboard.widget.AbsencesBalance',
    help: 'widgeti-ljudi',
    fit: true,
  },
  NotificationsBulletin: {
    component: NotificationsBulletinWidget,
    titleKey: 'dashboard.widget.NotificationsBulletin',
    help: 'widgeti-pregled',
    fit: true,
  },
  DocumentExpiry: {
    component: DocumentExpiryWidget,
    titleKey: 'dashboard.widget.DocumentExpiry',
    help: 'widgeti-pregled',
    fit: true,
  },
  FleetStatus: {
    component: FleetStatusWidget,
    titleKey: 'dashboard.widget.FleetStatus',
    help: 'widgeti-projekti',
    fit: true,
  },
  NeedsAttention: {
    component: NeedsAttentionWidget,
    titleKey: 'dashboard.widget.NeedsAttention',
    help: 'widgeti-pregled',
    fit: true,
  },
  LiveMap: {
    component: LiveMapWidget,
    titleKey: 'dashboard.widget.LiveMap',
    help: 'widgeti-projekti',
  },
  TodayAttendance: {
    component: TodayAttendanceWidget,
    titleKey: 'dashboard.widget.TodayAttendance',
    help: 'widgeti-ljudi',
    fit: true,
  },
  Finance: {
    component: FinanceWidget,
    titleKey: 'dashboard.widget.Finance',
    help: 'widgeti-novac',
    // Statistics are the least it shows; the amounts, trend and structure tabs need the full right.
    finance: 'statistics',
    // Its charts have a height of their own, so the card is as tall as the tab that is open.
    fit: true,
  },
  IncomeVsExpense: {
    component: IncomeVsExpenseWidget,
    titleKey: 'dashboard.widget.IncomeVsExpense',
    help: 'widgeti-novac',
    finance: 'full',
  },
  TopProjectsByExpense: {
    component: TopProjectsByExpenseWidget,
    titleKey: 'dashboard.widget.TopProjectsByExpense',
    help: 'widgeti-novac',
    fit: true,
    finance: 'full',
  },
  ProfitByProject: {
    component: ProfitByProjectWidget,
    titleKey: 'dashboard.widget.ProfitByProject',
    help: 'widgeti-novac',
    fit: true,
    finance: 'full',
  },
  ProjectFocus: {
    component: ProjectFocusWidget,
    titleKey: 'dashboard.widget.ProjectFocus',
    help: 'widgeti-projekti',
    fit: true,
    finance: 'full',
  },
  ProjectsOverBudget: {
    component: ProjectsOverBudgetWidget,
    titleKey: 'dashboard.widget.ProjectsOverBudget',
    help: 'widgeti-projekti',
    fit: true,
    finance: 'full',
  },
  AbsenceRequests: {
    component: AbsenceRequestsWidget,
    titleKey: 'dashboard.widget.AbsenceRequests',
    help: 'widgeti-ljudi',
    fit: true,
    // Answering a request is the API's AdminAndAbove.
    allowed: canReviewAbsences,
  },
  WeeklyReports: {
    component: WeeklyReportsWidget,
    titleKey: 'dashboard.widget.WeeklyReports',
    help: 'widgeti-ljudi',
    fit: true,
    // Reviewing a report is the API's AdminAndAbove.
    allowed: canReviewAbsences,
  },
  ActiveProjects: {
    component: ActiveProjectsWidget,
    titleKey: 'dashboard.widget.ActiveProjects',
    help: 'widgeti-projekti',
    fit: true,
    allowed: canViewDirectory,
  },
  ArticleOrders: {
    component: ArticleOrdersWidget,
    titleKey: 'dashboard.widget.ArticleOrders',
    help: 'widgeti-ljudi',
    fit: true,
    // Ordering and sending are the API's office roles.
    allowed: canManageArticleOrders,
  },
  BranchOverview: {
    component: BranchOverviewWidget,
    titleKey: 'dashboard.widget.BranchOverview',
    help: 'widgeti-pregled',
    fit: true,
    // The units, their heads and their head counts: the same people who may open the directory.
    allowed: canViewDirectory,
  },
};

/**
 * Whether the account may have this widget on its board: one that shows amounts needs
 * the full finance right, one that shows only percentages needs at least the statistics right.
 * Server-side the calls are refused all the same; this only decides what is drawn and offered.
 */
export function widgetAllowed(type: DashboardWidgetType, user: User | null | undefined): boolean {
  const entry = widgetRegistry[type];

  if (entry?.allowed && !entry.allowed(user)) {
    return false;
  }

  switch (entry?.finance) {
    case 'full':
      return canViewFinance(user);
    case 'statistics':
      return canViewFinanceStatistics(user);
    default:
      return true;
  }
}
