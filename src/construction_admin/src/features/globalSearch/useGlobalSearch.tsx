import {
  ApartmentOutlined,
  BusinessOutlined,
  ChecklistOutlined,
  HandymanOutlined,
  HomeWorkOutlined,
  Inventory2Outlined,
  LocalShippingOutlined,
  PeopleOutlined,
} from '@mui/icons-material';
import { useEffect, useState, type ReactNode } from 'react';

import { accommodationsApi } from '../../api/accommodations';
import { customersApi } from '../../api/customers';
import { employeesApi } from '../../api/employees';
import { materialsApi } from '../../api/materials';
import { projectsApi } from '../../api/projects';
import { toolsApi } from '../../api/tools';
import { vehiclesApi } from '../../api/vehicles';
import { workItemsApi } from '../../api/workItems';
import { canViewDirectory } from '../../auth/authHelpers';
import type { User } from '../../api/types';
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

async function runGlobalSearch(query: string): Promise<GlobalSearchGroup[]> {
  const listQuery = { search: query, pageNumber: 1, pageSize: PAGE_SIZE };

  const [employees, projects, customers, vehicles, tools, materials, workItems, accommodations] =
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
        sublabel: v.registrationNumber,
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
        sublabel: w.projectName ?? undefined,
        path: paths.workItemEdit(w.id),
      })),
      searchEntity(() => accommodationsApi.list(listQuery), (a) => ({
        id: a.id,
        label: a.address,
        path: paths.accommodationDetail(a.id),
      })),
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
      runGlobalSearch(trimmed)
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
