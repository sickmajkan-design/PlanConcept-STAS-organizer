import type { ComponentType } from 'react';

import type { MessageKey } from '../../i18n/en';
import { AbsencesBalanceWidget } from './widgets/AbsencesBalanceWidget';
import { DocumentExpiryWidget } from './widgets/DocumentExpiryWidget';
import { FleetStatusWidget } from './widgets/FleetStatusWidget';
import { NeedsAttentionWidget } from './widgets/NeedsAttentionWidget';
import { NotificationsBulletinWidget } from './widgets/NotificationsBulletinWidget';
import { ProjectsRealizationWidget } from './widgets/ProjectsRealizationWidget';
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
};
