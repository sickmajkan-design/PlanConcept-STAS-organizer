import { request } from '../../api/client';
import type { DashboardLayout } from './widgetTypes';

/** What the board showed today and, for "since yesterday", yesterday: figure name to number. */
export interface DashboardSnapshots {
  today: Record<string, number>;
  yesterday: Record<string, number>;
}

export const dashboardApi = {
  getLayout: () =>
    request<DashboardLayout>({ method: 'GET', url: '/api/v1/dashboard-layout' }),

  saveLayout: (layout: DashboardLayout) =>
    request<DashboardLayout>({
      method: 'PUT',
      url: '/api/v1/dashboard-layout',
      data: layout,
    }),

  getSnapshots: () =>
    request<DashboardSnapshots>({ method: 'GET', url: '/api/v1/dashboard-snapshots' }),

  recordSnapshots: (values: Record<string, number>) =>
    request<void>({ method: 'PUT', url: '/api/v1/dashboard-snapshots', data: { values } }),
};
