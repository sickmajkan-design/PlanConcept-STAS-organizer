import {
  ApartmentOutlined,
  BusinessOutlined,
  CampaignOutlined,
  ChecklistOutlined,
  CreditCardOutlined,
  EventBusyOutlined,
  FolderOutlined,
  GroupsOutlined,
  LocalGasStationOutlined,
  ManageAccountsOutlined,
  HandymanOutlined,
  HomeWorkOutlined,
  Inventory2Outlined,
  LocalShippingOutlined,
  MenuBookOutlined,
  PeopleOutlined,
  ReceiptLongOutlined,
  RequestQuoteOutlined,
  ShoppingCartOutlined,
  TableChartOutlined,
} from '@mui/icons-material';
import { useEffect, useState, type ReactNode } from 'react';

import { absencesApi } from '../../api/absences';
import { accommodationsApi } from '../../api/accommodations';
import { articleOrdersApi } from '../../api/articleOrders';
import { attachmentsApi } from '../../api/attachments';
import { bookApi } from '../../api/book';
import { documentRequestsApi } from '../../api/documentRequests';
import { bulletinApi } from '../../api/bulletin';
import { customersApi } from '../../api/customers';
import { employeesApi } from '../../api/employees';
import { fuelCardsApi } from '../../api/fuelCards';
import { fuelTransactionsApi } from '../../api/fuelTransactions';
import { invoicesApi } from '../../api/invoices';
import { ledgersApi } from '../../api/ledgers';
import { materialsApi } from '../../api/materials';
import { projectsApi } from '../../api/projects';
import { refundsApi } from '../../api/refunds';
import { toolsApi } from '../../api/tools';
import { vehiclesApi } from '../../api/vehicles';
import { notificationGroupsApi } from '../../api/notificationGroups';
import { usersApi } from '../../api/users';
import { workItemsApi } from '../../api/workItems';
import {
  canAdministerAccounts,
  canManageInvoices,
  canSeeSpending,
  canViewBook,
  canViewDirectory,
  isSuperAdmin,
} from '../../auth/authHelpers';
import type { User } from '../../api/types';
import type { MessageKey } from '../../i18n/en';
import { liveT } from '../../i18n/liveT';
import { queryClient } from '../../queryClient';
import { projectCustomersQuery } from '../projects/useProjectCustomers';
import { formatDate } from '../../utils/formatting';
import { paths } from '../../routes/paths';

export interface GlobalSearchResult {
  id: string;
  label: string;
  sublabel?: string;
  path: string;
}

export interface GlobalSearchGroup {
  key: string;
  labelKey: string;
  icon: ReactNode;
  results: GlobalSearchResult[];
}

const PAGE_SIZE = 5;
const DEBOUNCE_MS = 300;
const MIN_QUERY_LENGTH = 2;

async function searchEntity<TItem>(
  call: () => Promise<{ items: TItem[] }>,
  map: (item: TItem) => GlobalSearchResult,
): Promise<GlobalSearchResult[]> {
  try {
    const page = await call();
    return page.items.map(map);
  } catch {
    return [];
  }
}

/** Which of the role-gated groups this account may search; a result is never a page the reader is refused. */
interface SearchScope {
  accounts: boolean;
  spending: boolean;
  invoices: boolean;
  ledgers: boolean;
  dkv: boolean;
  /** Documents and the requests for them: the office. */
  documents: boolean;
  book: boolean;
}

/** Bulletin posts whose title or text contains the words typed. */
async function searchBulletin(query: string): Promise<GlobalSearchResult[]> {
  const needle = query.toLowerCase();

  return searchEntity(
    async () => ({
      items: (await bulletinApi.list())
        .filter((p) => p.title.toLowerCase().includes(needle) || p.body.toLowerCase().includes(needle))
        .slice(0, PAGE_SIZE),
    }),
    (p) => ({
      id: p.id,
      label: p.title,
      sublabel: p.createdByName,
      path: `${paths.bulletin}?highlight=${p.id}`,
    }),
  );
}

/** Every word typed has to appear somewhere in the text. */
function allWords(text: string, query: string): boolean {
  const haystack = text.toLowerCase();

  return query
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean)
    .every((word) => haystack.includes(word));
}

/**
 * Documents by title, file name, note, owner or label. There is no search endpoint for them across the whole
 * company, so the list the documents page itself uses is fetched once and filtered here.
 */
async function searchDocuments(query: string): Promise<GlobalSearchResult[]> {
  return searchEntity(
    async () => ({
      items: (await attachmentsApi.expiring(null, true))
        .filter((d) =>
          allWords([d.title, d.fileName, d.description, d.ownerName, ...d.tags].filter(Boolean).join(' '), query),
        )
        .slice(0, PAGE_SIZE),
    }),
    (d) => ({
      id: d.id,
      label: d.title || d.fileName,
      sublabel: [d.ownerName, d.title ? d.fileName : null, d.tags.length ? d.tags.map((tag) => `#${tag}`).join(' ') : null]
        .filter(Boolean)
        .join(' · '),
      path: `${paths.expiringDocuments}?highlight=${d.id}`,
    }),
  );
}

/** Requests for documents by what was asked for or who was asked. */
async function searchDocumentRequests(query: string): Promise<GlobalSearchResult[]> {
  return searchEntity(
    async () => ({
      items: (await documentRequestsApi.list({ pageNumber: 1, pageSize: 100 }))
        .items.filter((r) => allWords([r.title, r.employeeName, r.note].filter(Boolean).join(' '), query))
        .slice(0, PAGE_SIZE),
    }),
    (r) => ({
      id: r.id,
      label: r.title,
      sublabel: [r.employeeName, liveT(`docRequests.status.${r.status}` as MessageKey)].filter(Boolean).join(' · '),
      path: `${paths.documentRequests}?highlight=${r.id}`,
    }),
  );
}

async function runGlobalSearch(query: string, scope: SearchScope): Promise<GlobalSearchGroup[]> {
  // Cached for five minutes; if it cannot be read the results simply show the project alone.
  const projectCustomers = await queryClient.fetchQuery(projectCustomersQuery).catch(() => null);
  const withCustomer = (project: string | null | undefined) =>
    project ? [project, projectCustomers?.byName.get(project)?.name].filter(Boolean).join(' · ') : undefined;

  const includeAccounts = scope.accounts;
  const listQuery = { search: query, pageNumber: 1, pageSize: PAGE_SIZE };

  const [
    employees,
    projects,
    customers,
    vehicles,
    tools,
    materials,
    workItems,
    accommodations,
    users,
    notificationGroups,
    fuelCards,
    dkvRows,
    absences,
    refunds,
    articleOrders,
    invoices,
    ledgers,
    bulletin,
    documents,
    documentRequests,
    bookEntries,
  ] =
    await Promise.all([
      searchEntity(() => employeesApi.list(listQuery), (e) => ({
        id: e.id,
        label: e.fullName,
        sublabel: e.employeeNumber,
        path: paths.employeeDetail(e.id),
      })),
      searchEntity(() => projectsApi.list(listQuery), (p) => ({
        id: p.id,
        label: p.name,
        sublabel: p.customerName ?? undefined,
        path: paths.projectDetail(p.id),
      })),
      searchEntity(() => customersApi.list(listQuery), (c) => ({
        id: c.id,
        label: c.name,
        sublabel: c.contactPerson ?? undefined,
        path: paths.customerEdit(c.id),
      })),
      searchEntity(() => vehiclesApi.list(listQuery), (v) => ({
        id: v.id,
        label: `${v.brand} ${v.model}`,
        sublabel: [v.registrationNumber, v.tdNumber ? `TD ${v.tdNumber}` : null].filter(Boolean).join(' · '),
        path: paths.vehicleDetail(v.id),
      })),
      searchEntity(() => toolsApi.list(listQuery), (tItem) => ({
        id: tItem.id,
        label: tItem.name,
        sublabel: tItem.serialNumber ?? undefined,
        path: paths.toolDetail(tItem.id),
      })),
      searchEntity(() => materialsApi.list(listQuery), (m) => ({
        id: m.id,
        label: m.name,
        sublabel: m.warehouse ?? undefined,
        path: paths.materialDetail(m.id),
      })),
      searchEntity(() => workItemsApi.list(listQuery), (w) => ({
        id: w.id,
        label: w.title,
        sublabel: withCustomer(w.projectName),
        path: paths.workItemEdit(w.id),
      })),
      searchEntity(() => accommodationsApi.list(listQuery), (a) => ({
        id: a.id,
        label: a.name || a.address,
        sublabel: a.name ? a.address : (a.city ?? undefined),
        path: paths.accommodationDetail(a.id),
      })),
      // Accounts and groups are administration: only searched for those who
      // can open them, so a result is never a page the reader is refused.
      includeAccounts
        ? searchEntity(() => usersApi.list(listQuery), (u) => ({
            id: u.id,
            label: u.employeeName ?? u.customerName ?? u.email,
            sublabel: u.email,
            path: paths.userEdit(u.id),
          }))
        : Promise.resolve([]),
      includeAccounts
        ? searchEntity(() => notificationGroupsApi.list(listQuery), (g) => ({
            id: g.id,
            label: g.name,
            path: paths.notificationGroupEdit(g.id),
          }))
        : Promise.resolve([]),
      // A fuel card opens the vehicle it is on, which lists its cards.
      scope.spending
        ? searchEntity(() => fuelCardsApi.list(listQuery), (c) => ({
            id: c.id,
            label: c.cardNumber,
            sublabel: `${c.provider} · ${c.vehicleName}`,
            path: paths.vehicleDetail(c.vehicleId),
          }))
        : Promise.resolve([]),
      // A statement row has no page of its own: it opens the statement check, filtered to what was typed.
      scope.dkv
        ? searchEntity(() => fuelTransactionsApi.list({ pageNumber: 1, pageSize: PAGE_SIZE, search: query }), (r) => ({
            id: r.id,
            label: `${r.cardNumber} · ${r.amount.toFixed(2)} ${r.currency}`,
            sublabel: [r.vehicleName, formatDate(r.occurredOn), r.productType].filter(Boolean).join(' · '),
            path: `${paths.fuelReconciliation}?search=${encodeURIComponent(query)}`,
          }))
        : Promise.resolve([]),
      searchEntity(() => absencesApi.list(listQuery), (a) => ({
        id: a.id,
        label: a.employeeName,
        sublabel: `${liveT(`absenceType.${a.type}` as MessageKey)} · ${formatDate(a.startDate)} - ${formatDate(a.endDate)}`,
        path: `${paths.absences}?highlight=${a.id}`,
      })),
      searchEntity(() => refundsApi.list(listQuery), (r) => ({
        id: r.id,
        label: r.description,
        sublabel: `${r.employeeName} · ${r.amount.toFixed(2)} ${r.currency}`,
        path: `${paths.refunds}?highlight=${r.id}`,
      })),
      searchEntity(() => articleOrdersApi.list(listQuery), (o) => ({
        id: o.id,
        label: o.items.map((i) => i.name).slice(0, 3).join(', ') || o.requestedByName,
        sublabel: [o.requestedByName, withCustomer(o.projectName)].filter(Boolean).join(' · '),
        path: `${paths.articleOrders}?highlight=${o.id}`,
      })),
      scope.invoices
        ? searchEntity(() => invoicesApi.list(listQuery), (i) => ({
            id: i.id,
            label: i.number,
            sublabel: [i.customerName, i.projectName].filter(Boolean).join(' · '),
            path: `${paths.invoices}?highlight=${i.id}`,
          }))
        : Promise.resolve([]),
      scope.ledgers
        ? searchEntity(() => ledgersApi.list(listQuery), (l) => ({
            id: l.id,
            label: l.name,
            sublabel: l.branchName ?? undefined,
            path: paths.ledgerDetail(l.id),
          }))
        : Promise.resolve([]),
      // The bulletin board is a short list, not paged, so it is filtered here instead of on the server.
      searchBulletin(query),
      scope.documents ? searchDocuments(query) : Promise.resolve([]),
      scope.documents ? searchDocumentRequests(query) : Promise.resolve([]),
      // A book line has no page of its own: it opens the book of entries, filtered to what was typed.
      scope.book
        ? searchEntity(() => bookApi.entries.list({ pageNumber: 1, pageSize: PAGE_SIZE, search: query }), (r) => ({
            id: r.id,
            label: r.description || r.counterparty || r.categoryName || formatDate(r.occurredOn),
            sublabel: [formatDate(r.occurredOn), r.categoryName, r.counterparty, r.amount.toFixed(2)].filter(Boolean).join(' · '),
            path: `${paths.bookEntries}?search=${encodeURIComponent(query)}`,
          }))
        : Promise.resolve([]),
    ]);

  const groups: GlobalSearchGroup[] = [
    { key: 'employees', labelKey: 'nav.employees', icon: <PeopleOutlined fontSize="small" />, results: employees },
    { key: 'projects', labelKey: 'nav.projects', icon: <ApartmentOutlined fontSize="small" />, results: projects },
    { key: 'customers', labelKey: 'nav.customers', icon: <BusinessOutlined fontSize="small" />, results: customers },
    { key: 'vehicles', labelKey: 'nav.vehicles', icon: <LocalShippingOutlined fontSize="small" />, results: vehicles },
    { key: 'tools', labelKey: 'nav.tools', icon: <HandymanOutlined fontSize="small" />, results: tools },
    { key: 'materials', labelKey: 'nav.materials', icon: <Inventory2Outlined fontSize="small" />, results: materials },
    { key: 'workItems', labelKey: 'nav.workItems', icon: <ChecklistOutlined fontSize="small" />, results: workItems },
    { key: 'accommodations', labelKey: 'nav.accommodations', icon: <HomeWorkOutlined fontSize="small" />, results: accommodations },
    { key: 'users', labelKey: 'nav.users', icon: <ManageAccountsOutlined fontSize="small" />, results: users },
    { key: 'notificationGroups', labelKey: 'nav.notificationGroups', icon: <GroupsOutlined fontSize="small" />, results: notificationGroups },
    { key: 'fuelCards', labelKey: 'search.fuelCards', icon: <CreditCardOutlined fontSize="small" />, results: fuelCards },
    { key: 'dkvRows', labelKey: 'search.dkvRows', icon: <LocalGasStationOutlined fontSize="small" />, results: dkvRows },
    { key: 'absences', labelKey: 'nav.absences', icon: <EventBusyOutlined fontSize="small" />, results: absences },
    { key: 'refunds', labelKey: 'nav.refunds', icon: <RequestQuoteOutlined fontSize="small" />, results: refunds },
    { key: 'articleOrders', labelKey: 'nav.articleOrders', icon: <ShoppingCartOutlined fontSize="small" />, results: articleOrders },
    { key: 'invoices', labelKey: 'nav.invoices', icon: <ReceiptLongOutlined fontSize="small" />, results: invoices },
    { key: 'ledgers', labelKey: 'nav.ledgers', icon: <TableChartOutlined fontSize="small" />, results: ledgers },
    { key: 'bulletin', labelKey: 'nav.bulletin', icon: <CampaignOutlined fontSize="small" />, results: bulletin },
    { key: 'documents', labelKey: 'search.documents', icon: <FolderOutlined fontSize="small" />, results: documents },
    { key: 'documentRequests', labelKey: 'search.documentRequests', icon: <FolderOutlined fontSize="small" />, results: documentRequests },
    { key: 'bookEntries', labelKey: 'search.bookEntries', icon: <MenuBookOutlined fontSize="small" />, results: bookEntries },
  ];

  return groups.filter((group) => group.results.length > 0);
}

/** Fans out a search term to every searchable entity's existing list endpoint (each already supports `search`), in parallel, debounced. */
export function useGlobalSearch(query: string, user: User | null | undefined) {
  const [groups, setGroups] = useState<GlobalSearchGroup[]>([]);
  const [loading, setLoading] = useState(false);
  const trimmed = query.trim();
  const enabled = canViewDirectory(user) && trimmed.length >= MIN_QUERY_LENGTH;

  useEffect(() => {
    if (!enabled) {
      setGroups([]);
      return;
    }
    let cancelled = false;
    setLoading(true);
    const timer = setTimeout(() => {
      runGlobalSearch(trimmed, {
        accounts: canAdministerAccounts(user),
        spending: canSeeSpending(user),
        invoices: canManageInvoices(user),
        ledgers: isSuperAdmin(user),
        dkv: canAdministerAccounts(user),
        documents: canAdministerAccounts(user),
        book: canViewBook(user),
      })
        .then((result) => {
          if (!cancelled) setGroups(result);
        })
        .finally(() => {
          if (!cancelled) setLoading(false);
        });
    }, DEBOUNCE_MS);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [trimmed, enabled]);

  return { groups, loading: enabled && loading };
}
