import { useQuery } from '@tanstack/react-query';

import {
  timeEntriesApi,
  type ReviewTimeEntryInput,
  type TimeEntryListQuery,
  type TimeEntrySummaryQuery,
} from '../../api/timeEntries';
import type { TimeEntryInput } from '../../api/types';
import { config } from '../../config';
import { navBadgeKeys } from '../../layout/useNavBadgeCounts';
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

/**
 * Every time-entry write also invalidates the nav badge: reviewing one
 * resolves the "Submitted" backlog it counts, and editing or deleting one
 * can just as easily change which entries are still in that state — so the
 * count on "Radno vreme" updates the instant the write succeeds.
 */
const timeEntryCaches = [timeEntryKeys.all, navBadgeKeys.timeEntriesSubmitted];

/**
 * Polls rather than loading once: the Work Time board is how the office
 * finds out someone clocked in or out, and a screen that only updates on a
 * manual refresh would defeat that — the same reasoning behind the live
 * map's own `refetchInterval`.
 */
export function useTimeEntriesQuery(query: TimeEntryListQuery, enabled = true) {
  return useResourceList(timeEntryKeys, timeEntriesApi.list, query, {
    refetchInterval: config.workTimeRefreshMs,
    enabled,
  });
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
    timeEntryCaches,
  );
}

export function useUpdateTimeEntry(id: string) {
  return useResourceMutation(
    (input: TimeEntryInput) => timeEntriesApi.update(id, input),
    timeEntryCaches,
  );
}

export function useReviewTimeEntry(id: string) {
  return useResourceMutation(
    (input: ReviewTimeEntryInput) => timeEntriesApi.review(id, input),
    timeEntryCaches,
  );
}

export function useDeleteTimeEntry() {
  return useResourceMutation((id: string) => timeEntriesApi.remove(id), timeEntryCaches);
}
