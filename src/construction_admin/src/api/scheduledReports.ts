import { request } from './client';
import type { ScheduledReportSubscription, ScheduledReportSubscriptionInput } from './types';

export const scheduledReportsApi = {
  list: () => request<ScheduledReportSubscription[]>({ method: 'GET', url: '/api/v1/scheduledreports' }),

  create: (input: ScheduledReportSubscriptionInput) =>
    request<ScheduledReportSubscription>({
      method: 'POST',
      url: '/api/v1/scheduledreports',
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/scheduledreports/${id}` }),
};
