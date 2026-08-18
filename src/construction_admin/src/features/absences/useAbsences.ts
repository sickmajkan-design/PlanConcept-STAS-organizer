import { useQuery } from '@tanstack/react-query';
import {
  absencesApi,
  type AbsenceListQuery,
  type ReviewAbsenceInput,
  type ScheduleQuery,
} from '../../api/absences';
import type { AbsenceInput } from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const absenceKeys = createResourceKeys<AbsenceListQuery>('absences');

export const scheduleKeys = {
  all: ['schedule'] as const,
  window: (query: ScheduleQuery) => ['schedule', query] as const,
};

export function useAbsencesQuery(query: AbsenceListQuery) {
  return useResourceList(absenceKeys, absencesApi.list, query);
}

export function useScheduleQuery(query: ScheduleQuery, enabled = true) {
  return useQuery({
    queryKey: scheduleKeys.window(query),
    queryFn: () => absencesApi.schedule(query),
    enabled,
  });
}

/** The balance line shown alongside an approve/refuse decision. */
export function useAbsenceBalanceQuery(employeeId: string | undefined, year?: number) {
  return useQuery({
    queryKey: ['absences', 'balance', employeeId, year] as const,
    queryFn: () => absencesApi.balance(employeeId!, year),
    enabled: !!employeeId,
  });
}

/**
 * Every absence write invalidates the board as well as the list. Granting
 * leave puts a bar on the schedule, so a board left on screen from before the
 * approval would show the person as available.
 */
const absenceCaches = [absenceKeys.all, scheduleKeys.all];

export function useBookAbsence() {
  return useResourceMutation(
    (input: AbsenceInput) => absencesApi.book(input),
    absenceCaches,
  );
}

export function useReviewAbsence() {
  return useResourceMutation(
    ({ id, input }: { id: string; input: ReviewAbsenceInput }) =>
      absencesApi.review(id, input),
    absenceCaches,
  );
}

export function useDeleteAbsence() {
  return useResourceMutation(
    (id: string) => absencesApi.remove(id),
    absenceCaches,
  );
}
