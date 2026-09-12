import { request } from '../../api/client';
import type { DashboardLayout } from './widgetTypes';

export const dashboardApi = {
  getLayout: () =>
    request<DashboardLayout>({ method: 'GET', url: '/api/v1/dashboard-layout' }),

  saveLayout: (layout: DashboardLayout) =>
    request<DashboardLayout>({
      method: 'PUT',
      url: '/api/v1/dashboard-layout',
      data: layout,
    }),
};
