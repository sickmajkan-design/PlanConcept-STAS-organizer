import { useBranchScoped, useUnitParams } from '../branches/BranchContext';
import { useQuery } from '@tanstack/react-query';

import {
  weeklySiteReportsApi,
  type CreateWeeklySiteReportInput,
} from '../../api/weeklySiteReports';
import type { WeeklyReportLineInput, WeeklySiteReportListQuery } from '../../api/types';
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
    (input: { id: string; lines?: WeeklyReportLineInput[]; keepExisting?: string[] }) =>
      weeklySiteReportsApi.approve(input.id, { lines: input.lines, keepExisting: input.keepExisting }),
    // The sheets change too: what was entered shows in the monthly sheet and its checks.
    [weeklySiteReportKeys.all, WEEKLY_SUMMARY_KEY, ['ledgers']],
  );
}

/** One report with its pictures, workers and what the monthly sheet holds for them. */
export function useWeeklySiteReportQuery(id: string | null) {
  return useQuery({
    queryKey: ['weeklySiteReports', 'detail', id],
    queryFn: () => weeklySiteReportsApi.detail(id!),
    enabled: !!id,
  });
}

/** Saves the corrections to the workers and hours without approving the report. */
export function useUpdateWeeklySiteReportLines() {
  return useResourceMutation(
    (input: { id: string; lines: WeeklyReportLineInput[] }) => weeklySiteReportsApi.updateLines(input.id, input.lines),
    [weeklySiteReportKeys.all, ['weeklySiteReports', 'detail']],
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
  // Narrowed to the business unit chosen in the header, like the other widgets.
  const { branchId } = useUnitParams();

  return useQuery({
    queryKey: [...WEEKLY_SUMMARY_KEY, branchId ?? null] as const,
    queryFn: () => weeklySiteReportsApi.summary(branchId),
    enabled,
    staleTime: 30_000,
  });
}
