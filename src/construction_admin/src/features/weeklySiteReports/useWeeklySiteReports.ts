import { useBranchScoped } from '../branches/BranchContext';
import { useQuery } from '@tanstack/react-query';

import {
  weeklySiteReportsApi,
  type CreateWeeklySiteReportInput,
} from '../../api/weeklySiteReports';
import type { WeeklySiteReportListQuery } from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const weeklySiteReportKeys = createResourceKeys<WeeklySiteReportListQuery>('weeklySiteReports');

export function useWeeklySiteReportsQuery(query: WeeklySiteReportListQuery) {
  const scoped = useBranchScoped(query);
  return useResourceList(weeklySiteReportKeys, weeklySiteReportsApi.list, scoped);
}

export function useReportableProjectsQuery() {
  return useQuery({
    queryKey: ['weeklySiteReports', 'reportable-projects'],
    queryFn: () => weeklySiteReportsApi.reportableProjects(),
    staleTime: 60_000,
  });
}

export function useCreateWeeklySiteReport() {
  return useResourceMutation(
    (input: CreateWeeklySiteReportInput) => weeklySiteReportsApi.create(input),
    [weeklySiteReportKeys.all],
  );
}

export function useMarkWeeklySiteReportProcessed() {
  return useResourceMutation(
    (id: string) => weeklySiteReportsApi.markProcessed(id),
    [weeklySiteReportKeys.all],
  );
}
