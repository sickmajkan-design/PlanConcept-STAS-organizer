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
  ShoppingCartOutlined,
  PaidOutlined,
  LocalGasStationOutlined,
  SwapVertOutlined,
  RequestQuoteOutlined,
  EventOutlined,
  ReceiptLongOutlined,
  TrendingUpOutlined,
  GroupsOutlined,
  PeopleOutlined,
  BusinessOutlined,
  PaymentsOutlined,
  HomeOutlined,
  EventNoteOutlined,
  HomeWorkOutlined,
  TableChartOutlined,
  ScheduleSendOutlined,
  AdminPanelSettingsOutlined,
  SecurityOutlined,
  FactCheckOutlined,
  HistoryOutlined,
  WifiTetheringOutlined,
  AccountTreeOutlined,
} from '@mui/icons-material';
import type { ReactNode } from 'react';

import {
  canAdministerAccounts,
  canManageInvoices,
  canSeeLabourCost,
  isSuperAdmin,
  canViewDirectory,
  canViewFinance,
} from '../auth/authHelpers';
import type { User } from '../api/types';
import type { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

export interface NavItem {
  label: string;
  path: string;
  icon: ReactNode;
  /**
   * Reached through a tab strip on its section's page rather than the drawer.
   * Still searchable, favouritable and part of the breadcrumb trail; only the
   * drawer and the collapsed rail leave it out.
   */
  inTabs?: boolean;
  /** Other paths this entry is "current" for, so a hub stays highlighted on its tabs. */
  alsoActiveOn?: string[];
  /** Heading the flyout panel prints above this item (and the ones after it, until the next heading). */
  section?: string;
}

export interface NavGroup {
  key: string;
  label: string;
  icon: ReactNode;
  /** One line under the group's name in the flyout panel. */
  sub?: string;
  /** The group people plan with: drawn as a filled tile on the rail. */
  emphasized?: boolean;
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
            key: 'planning',
            label: t('nav.group.planning'),
            sub: t('nav.group.planning.sub'),
            icon: <EventNoteOutlined />,
            emphasized: true,
            items: [
              {
                label: t('nav.schedule'),
                path: paths.schedule,
                icon: <CalendarMonthOutlined />,
                section: t('nav.section.people'),
              },
              {
                label: t('nav.absences'),
                path: paths.absences,
                icon: <EventBusyOutlined />,
                section: t('nav.section.people'),
              },
              {
                label: t('nav.timeEntries'),
                path: paths.timeEntries,
                icon: <ScheduleOutlined />,
                section: t('nav.section.people'),
              },
              {
                label: t('nav.workItems'),
                path: paths.workItems,
                icon: <ChecklistOutlined />,
                section: t('nav.section.work'),
              },
              {
                label: t('nav.articleOrders'),
                path: paths.articleOrders,
                icon: <ShoppingCartOutlined />,
                section: t('nav.section.work'),
              },
              {
                label: t('nav.weeklyReports'),
                path: paths.weeklyReports,
                icon: <ScheduleSendOutlined />,
                section: t('nav.section.work'),
              },
            ],
          } satisfies NavGroup,
          {
            key: 'people',
            label: t('nav.group.people'),
            sub: t('nav.group.people.sub'),
            icon: <PeopleOutlined />,
            items: [
              { label: t('nav.employees'), path: paths.employees, icon: <PeopleOutlined /> },
              { label: t('nav.hierarchy'), path: paths.hierarchy, icon: <AccountTreeOutlined /> },
              {
                label: t('nav.accommodations'),
                path: paths.accommodations,
                icon: <HomeWorkOutlined />,
              },
            ],
          } satisfies NavGroup,
          {
            key: 'projects',
            label: t('nav.group.projects'),
            sub: t('nav.group.projects.sub'),
            icon: <ApartmentOutlined />,
            items: [
              { label: t('nav.projects'), path: paths.projects, icon: <ApartmentOutlined /> },
              { label: t('nav.customers'), path: paths.customers, icon: <BusinessOutlined /> },
            ],
          } satisfies NavGroup,
          {
            key: 'equipment',
            label: t('nav.group.equipment'),
            sub: t('nav.group.equipment.sub'),
            icon: <LocalShippingOutlined />,
            items: [
              { label: t('nav.vehicles'), path: paths.vehicles, icon: <LocalShippingOutlined /> },
              { label: t('nav.tools'), path: paths.tools, icon: <HandymanOutlined /> },
              { label: t('nav.materials'), path: paths.materials, icon: <Inventory2Outlined /> },
            ],
          } satisfies NavGroup,
          {
            key: 'costs',
            label: t('nav.group.costs'),
            sub: t('nav.group.costs.sub'),
            icon: <PaidOutlined />,
            items: [
              ...(canViewFinance(user)
                ? [
                    { label: t('nav.costs'), path: paths.costs, icon: <PaidOutlined /> },
                    { label: t('nav.companyRevenues'), path: paths.companyRevenues, icon: <TrendingUpOutlined /> },
                  ]
                : []),
              { label: t('nav.refunds'), path: paths.refunds, icon: <RequestQuoteOutlined /> },
              ...(canManageInvoices(user)
                ? [{ label: t('nav.invoices'), path: paths.invoices, icon: <ReceiptLongOutlined /> }]
                : []),
              // Reading any of these back is money now (`FinanceRules.CanSeeSpendingAsync`),
              // so the whole tab strip needs the finance grant, same as the routes.
              ...(canViewFinance(user)
                ? [
                    {
                      label: t('nav.costRecords'),
                      path: paths.costRecords,
                      icon: <ReceiptLongOutlined />,
                      alsoActiveOn: [
                        paths.vehicleExpenses,
                        paths.toolExpenses,
                        paths.generalExpenses,
                        paths.stockMovements,
                        paths.accommodationCosts,
                        paths.financeEntries,
                      ],
                    },
                    {
                      label: t('nav.vehicleExpenses'),
                      path: paths.vehicleExpenses,
                      icon: <LocalGasStationOutlined />,
                      inTabs: true,
                    },
                    {
                      label: t('nav.toolExpenses'),
                      path: paths.toolExpenses,
                      icon: <BuildCircleOutlined />,
                      inTabs: true,
                    },
                  ]
                : []),
              ...(canViewFinance(user)
                ? [
                    {
                      label: t('nav.generalExpenses'),
                      path: paths.generalExpenses,
                      icon: <PaymentsOutlined />,
                      inTabs: true,
                    },
                  ]
                : []),
              ...(canViewFinance(user)
                ? [
                    {
                      label: t('nav.stockMovements'),
                      path: paths.stockMovements,
                      icon: <SwapVertOutlined />,
                      inTabs: true,
                    },
                    {
                      label: t('nav.accommodationCosts'),
                      path: paths.accommodationCosts,
                      icon: <HomeWorkOutlined />,
                      inTabs: true,
                    },
                  ]
                : []),
              ...(canSeeLabourCost(user)
                ? [
                    ...(canViewFinance(user)
                      ? [
                          {
                            label: t('nav.financeEntries'),
                            path: paths.financeEntries,
                            icon: <ReceiptLongOutlined />,
                            inTabs: true,
                          },
                        ]
                      : []),
                    {
                      label: t('nav.billingSettings'),
                      path: paths.billingSettings,
                      icon: <RequestQuoteOutlined />,
                      alsoActiveOn: [paths.rates, paths.publicHolidays, paths.annualRealization],
                    },
                    ...(canViewFinance(user)
                      ? [
                          {
                            label: t('nav.rates'),
                            path: paths.rates,
                            icon: <RequestQuoteOutlined />,
                            inTabs: true,
                          },
                        ]
                      : []),
                    {
                      label: t('nav.publicHolidays'),
                      path: paths.publicHolidays,
                      icon: <EventOutlined />,
                      inTabs: true,
                    },
                    ...(canViewFinance(user)
                      ? [
                          {
                            label: t('nav.annualRealization'),
                            path: paths.annualRealization,
                            icon: <TrendingUpOutlined />,
                            inTabs: true,
                          },
                        ]
                      : []),
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
            sub: t('nav.group.admin.sub'),
            icon: <AdminPanelSettingsOutlined />,
            items: [
              {
                label: t('nav.documents'),
                path: paths.expiringDocuments,
                icon: <FolderOutlined />,
              },
              { label: t('nav.users'), path: paths.users, icon: <ManageAccountsOutlined /> },
              { label: t('nav.organization'), path: paths.organization, icon: <AccountTreeOutlined /> },
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
              {
                label: t('nav.dataQuality'),
                path: paths.dataQuality,
                icon: <FactCheckOutlined />,
              },
              {
                label: t('nav.audit'),
                path: paths.audit,
                icon: <HistoryOutlined />,
              },
              {
                label: t('nav.presence'),
                path: paths.presence,
                icon: <WifiTetheringOutlined />,
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
            sub: t('nav.group.superadmin.sub'),
            icon: <SecurityOutlined />,
            items: [
              { label: t('nav.ledgers'), path: paths.ledgers, icon: <TableChartOutlined /> },
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
