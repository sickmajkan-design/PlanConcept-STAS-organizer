import {
  ApartmentOutlined,
  BuildCircleOutlined,
  CampaignOutlined,
  HandymanOutlined,
  Inventory2Outlined,
  LocalShippingOutlined,
  ManageAccountsOutlined,
  ChecklistOutlined,
  FolderOutlined,
  MapOutlined,
  ScheduleOutlined,
  CalendarMonthOutlined,
  EventBusyOutlined,
  PaidOutlined,
  LocalGasStationOutlined,
  SwapVertOutlined,
  RequestQuoteOutlined,
  EventOutlined,
  ReceiptLongOutlined,
  TrendingUpOutlined,
  DashboardCustomizeOutlined,
  GroupsOutlined,
  PeopleOutlined,
  BusinessOutlined,
  PaymentsOutlined,
  HomeOutlined,
  HomeWorkOutlined,
  TableChartOutlined,
  DomainOutlined,
  FolderSharedOutlined,
  ScheduleSendOutlined,
  WorkOutlined,
  AdminPanelSettingsOutlined,
  SecurityOutlined,
} from '@mui/icons-material';
import type { ReactNode } from 'react';

import {
  canAdministerAccounts,
  canManageAssignments,
  canSeeLabourCost,
  isSuperAdmin,
  canViewDirectory,
} from '../auth/authHelpers';
import type { User } from '../api/types';
import type { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

export interface NavItem {
  label: string;
  path: string;
  icon: ReactNode;
}

export interface NavGroup {
  key: string;
  label: string;
  icon: ReactNode;
  items: NavItem[];
}

export type NavEntry = NavItem | NavGroup;

export function isNavGroup(entry: NavEntry): entry is NavGroup {
  return 'items' in entry;
}

/** Builds the role-gated nav tree. Shared by the drawer and the command palette so both always list the same pages. */
export function buildNavEntries(user: User, t: ReturnType<typeof useT>): NavEntry[] {
  return [
    { label: t('nav.home'), path: paths.home, icon: <HomeOutlined /> },
    { label: t('nav.liveMap'), path: paths.map, icon: <MapOutlined /> },
    { label: t('nav.bulletin'), path: paths.bulletin, icon: <CampaignOutlined /> },
    ...(canViewDirectory(user)
      ? [
          {
            key: 'directory',
            label: t('nav.group.directory'),
            icon: <FolderSharedOutlined />,
            items: [
              { label: t('nav.employees'), path: paths.employees, icon: <PeopleOutlined /> },
              { label: t('nav.projects'), path: paths.projects, icon: <ApartmentOutlined /> },
              { label: t('nav.customers'), path: paths.customers, icon: <BusinessOutlined /> },
              {
                label: t('nav.vehicles'),
                path: paths.vehicles,
                icon: <LocalShippingOutlined />,
              },
              { label: t('nav.tools'), path: paths.tools, icon: <HandymanOutlined /> },
              {
                label: t('nav.materials'),
                path: paths.materials,
                icon: <Inventory2Outlined />,
              },
            ],
          } satisfies NavGroup,
          {
            key: 'work',
            label: t('nav.group.work'),
            icon: <WorkOutlined />,
            items: [
              {
                label: t('nav.timeEntries'),
                path: paths.timeEntries,
                icon: <ScheduleOutlined />,
              },
              {
                label: t('nav.workItems'),
                path: paths.workItems,
                icon: <ChecklistOutlined />,
              },
              {
                label: t('nav.schedule'),
                path: paths.schedule,
                icon: <CalendarMonthOutlined />,
              },
              { label: t('nav.absences'), path: paths.absences, icon: <EventBusyOutlined /> },
              {
                label: t('nav.weeklyReports'),
                path: paths.weeklyReports,
                icon: <ScheduleSendOutlined />,
              },
              ...(canManageAssignments(user)
                ? [
                    {
                      label: t('nav.assignmentBoard'),
                      path: paths.assignmentBoard,
                      icon: <DashboardCustomizeOutlined />,
                    },
                  ]
                : []),
            ],
          } satisfies NavGroup,
          {
            key: 'costs',
            label: t('nav.group.costs'),
            icon: <PaidOutlined />,
            items: [
              { label: t('nav.costs'), path: paths.costs, icon: <PaidOutlined /> },
              {
                label: t('nav.stockMovements'),
                path: paths.stockMovements,
                icon: <SwapVertOutlined />,
              },
              {
                label: t('nav.vehicleExpenses'),
                path: paths.vehicleExpenses,
                icon: <LocalGasStationOutlined />,
              },
              {
                label: t('nav.toolExpenses'),
                path: paths.toolExpenses,
                icon: <BuildCircleOutlined />,
              },
              {
                label: t('nav.generalExpenses'),
                path: paths.generalExpenses,
                icon: <PaymentsOutlined />,
              },
              {
                label: t('nav.accommodations'),
                path: paths.accommodations,
                icon: <HomeWorkOutlined />,
              },
              ...(canSeeLabourCost(user)
                ? [
                    {
                      label: t('nav.rates'),
                      path: paths.rates,
                      icon: <RequestQuoteOutlined />,
                    },
                    {
                      label: t('nav.publicHolidays'),
                      path: paths.publicHolidays,
                      icon: <EventOutlined />,
                    },
                    {
                      label: t('nav.financeEntries'),
                      path: paths.financeEntries,
                      icon: <ReceiptLongOutlined />,
                    },
                    {
                      label: t('nav.annualRealization'),
                      path: paths.annualRealization,
                      icon: <TrendingUpOutlined />,
                    },
                  ]
                : []),
            ],
          } satisfies NavGroup,
        ]
      : []),
    ...(canAdministerAccounts(user)
      ? [
          {
            key: 'admin',
            label: t('nav.group.admin'),
            icon: <AdminPanelSettingsOutlined />,
            items: [
              {
                label: t('nav.documents'),
                path: paths.expiringDocuments,
                icon: <FolderOutlined />,
              },
              { label: t('nav.users'), path: paths.users, icon: <ManageAccountsOutlined /> },
              {
                label: t('nav.notificationGroups'),
                path: paths.notificationGroups,
                icon: <GroupsOutlined />,
              },
              {
                label: t('nav.scheduledReports'),
                path: paths.scheduledReports,
                icon: <ScheduleSendOutlined />,
              },
            ],
          } satisfies NavGroup,
        ]
      : []),
    ...(isSuperAdmin(user)
      ? [
          {
            key: 'superadmin',
            label: t('nav.group.superadmin'),
            icon: <SecurityOutlined />,
            items: [
              { label: t('nav.ledgers'), path: paths.ledgers, icon: <TableChartOutlined /> },
              {
                label: t('nav.companySettings'),
                path: paths.companySettings,
                icon: <DomainOutlined />,
              },
            ],
          } satisfies NavGroup,
        ]
      : []),
  ];
}

/** Flattens groups into a single leaf-item list, e.g. for search or favorites. */
export function flattenNavEntries(entries: NavEntry[]): NavItem[] {
  return entries.flatMap((entry) => (isNavGroup(entry) ? entry.items : [entry]));
}

export interface BreadcrumbSegment {
  label: string;
  /** Omitted for the trail's last segment — the current page never links to itself. */
  path?: string;
}

/**
 * Home / group / page trail for the current path. A pathname that exactly
 * matches a nav entry is a primary page; anything else (a detail, edit, or
 * "new" route) is treated as living one level under the longest nav path
 * that prefixes it, mirroring the back-button logic in `AppLayout`.
 */
export function getBreadcrumbTrail(
  navEntries: NavEntry[],
  pathname: string,
  homeLabel: string,
): BreadcrumbSegment[] {
  if (pathname === paths.home) return [];

  let matched: NavItem | null = null;
  let group: NavGroup | null = null;

  for (const entry of navEntries) {
    if (isNavGroup(entry)) {
      const found = entry.items.find((item) => item.path === pathname);
      if (found) {
        matched = found;
        group = entry;
        break;
      }
    } else if (entry.path === pathname) {
      matched = entry;
      break;
    }
  }

  let isSubPage = false;

  if (!matched) {
    isSubPage = true;
    for (const entry of navEntries) {
      const items = isNavGroup(entry) ? entry.items : [entry];
      for (const item of items) {
        if (
          pathname.startsWith(`${item.path}/`) &&
          (!matched || item.path.length > matched.path.length)
        ) {
          matched = item;
          group = isNavGroup(entry) ? entry : null;
        }
      }
    }
  }

  if (!matched) return [];

  const trail: BreadcrumbSegment[] = [{ label: homeLabel, path: paths.home }];
  if (group) trail.push({ label: group.label });
  trail.push({ label: matched.label, path: isSubPage ? matched.path : undefined });
  return trail;
}
