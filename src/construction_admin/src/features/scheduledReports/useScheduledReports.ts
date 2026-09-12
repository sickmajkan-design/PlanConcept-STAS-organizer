import { useQuery } from '@tanstack/react-query';

import { scheduledReportsApi } from '../../api/scheduledReports';
import type { ScheduledReportSubscriptionInput } from '../../api/types';
import { useResourceMutation } from '../resourceQueries';

const KEY = ['scheduledReports'] as const;

export function useScheduledReportsQuery() {
  return useQuery({
    queryKey: KEY,
    queryFn: () => scheduledReportsApi.list(),
  });
}

export function useCreateScheduledReport() {
  return useResourceMutation(
    (input: ScheduledReportSubscriptionInput) => scheduledReportsApi.create(input),
    [KEY],
  );
}

export function useDeleteScheduledReport() {
  return useResourceMutation((id: string) => scheduledReportsApi.remove(id), [KEY]);
}
