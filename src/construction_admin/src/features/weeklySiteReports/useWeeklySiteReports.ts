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

export function useApproveWeeklySiteReport() {
  return useResourceMutation(
    (id: string) => weeklySiteReportsApi.approve(id),
    [weeklySiteReportKeys.all, WEEKLY_SUMMARY_KEY],
  );
}

export function useRequestWeeklySiteReportCorrection() {
  return useResourceMutation(
    (input: { id: string; note: string }) => weeklySiteReportsApi.requestCorrection(input.id, input.note),
    [weeklySiteReportKeys.all, WEEKLY_SUMMARY_KEY],
  );
}

const WEEKLY_SUMMARY_KEY = ['weeklySiteReports', 'summary'] as const;

/** What waits for the office: for the page's chips and the home page widget. */
export function useWeeklySiteReportsSummaryQuery(enabled = true) {
  return useQuery({
    queryKey: WEEKLY_SUMMARY_KEY,
    queryFn: () => weeklySiteReportsApi.summary(),
    enabled,
    staleTime: 30_000,
  });
}
