import { useQuery } from '@tanstack/react-query';

import {
  timeEntriesApi,
  type ReviewTimeEntryInput,
  type TimeEntryListQuery,
  type TimeEntrySummaryQuery,
} from '../../api/timeEntries';
import type { TimeEntryInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const timeEntryKeys = createResourceKeys<TimeEntryListQuery>('timeEntries');

/** Kept under the same root so any write invalidates the summary too. */
const summaryKey = (query: TimeEntrySummaryQuery) => [
  ...timeEntryKeys.all,
  'summary',
  query,
];

export function useTimeEntriesQuery(query: TimeEntryListQuery) {
  return useResourceList(timeEntryKeys, timeEntriesApi.list, query);
}

export function useTimeEntryQuery(id: string | undefined) {
  return useResourceDetail(timeEntryKeys, timeEntriesApi.get, id);
}

export function useTimeEntrySummaryQuery(
  query: TimeEntrySummaryQuery,
  enabled = true,
) {
  return useQuery({
    queryKey: summaryKey(query),
    queryFn: () => timeEntriesApi.summary(query),
    enabled,
  });
}

export function useCreateTimeEntry() {
  return useResourceMutation(
    (input: TimeEntryInput) => timeEntriesApi.create(input),
    [timeEntryKeys.all],
  );
}

export function useUpdateTimeEntry(id: string) {
  return useResourceMutation(
    (input: TimeEntryInput) => timeEntriesApi.update(id, input),
    [timeEntryKeys.all],
  );
}

export function useReviewTimeEntry(id: string) {
  return useResourceMutation(
    (input: ReviewTimeEntryInput) => timeEntriesApi.review(id, input),
    [timeEntryKeys.all],
  );
}

export function useDeleteTimeEntry() {
  return useResourceMutation((id: string) => timeEntriesApi.remove(id), [
    timeEntryKeys.all,
  ]);
}
