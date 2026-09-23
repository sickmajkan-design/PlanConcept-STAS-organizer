import type { ComponentType } from 'react';

import type { MessageKey } from '../../i18n/en';
import { AbsencesBalanceWidget } from './widgets/AbsencesBalanceWidget';
import { CompanyKpiWidget } from './widgets/CompanyKpiWidget';
import { CostTrendWidget } from './widgets/CostTrendWidget';
import { DocumentExpiryWidget } from './widgets/DocumentExpiryWidget';
import { FleetStatusWidget } from './widgets/FleetStatusWidget';
import { LiveMapWidget } from './widgets/LiveMapWidget';
import { NeedsAttentionWidget } from './widgets/NeedsAttentionWidget';
import { NotificationsBulletinWidget } from './widgets/NotificationsBulletinWidget';
import { ProjectsRealizationWidget } from './widgets/ProjectsRealizationWidget';
import { TodayAttendanceWidget } from './widgets/TodayAttendanceWidget';
import type { DashboardWidgetProps, DashboardWidgetType } from './widgetTypes';

interface WidgetRegistryEntry {
  component: ComponentType<DashboardWidgetProps>;
  titleKey: MessageKey;
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
  },
};
