import { useBranchScoped } from '../branches/BranchContext';
import { useQuery } from '@tanstack/react-query';
import {
  absencesApi,
  type AbsenceListQuery,
  type ConfirmAbsenceEditInput,
  type ProposeAbsenceEditInput,
  type ReviewAbsenceInput,
  type ScheduleQuery,
} from '../../api/absences';
import type { AbsenceInput, LeaveAdjustmentInput, LeaveSettings } from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const absenceKeys = createResourceKeys<AbsenceListQuery>('absences');

export const scheduleKeys = {
  all: ['schedule'] as const,
  window: (query: ScheduleQuery) => ['schedule', query] as const,
};

export function useAbsencesQuery(query: AbsenceListQuery, enabled = true) {
  const scoped = useBranchScoped(query);
  return useResourceList(absenceKeys, absencesApi.list, scoped, { enabled });
}

export function useScheduleQuery(query: ScheduleQuery, enabled = true) {
  const scoped = useBranchScoped(query);
  return useQuery({
    queryKey: scheduleKeys.window(scoped),
    queryFn: () => absencesApi.schedule(scoped),
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

/** The history of manual corrections of somebody's leave. */
export function useLeaveAdjustmentsQuery(employeeId: string | undefined, year?: number) {
  return useQuery({
    queryKey: ['absences', 'adjustments', employeeId, year] as const,
    queryFn: () => absencesApi.adjustments(employeeId!, year),
    enabled: !!employeeId,
  });
}

/** Writing a correction changes the balance and the history, so both are refreshed. */
export function useCreateLeaveAdjustment() {
  return useResourceMutation(
    (input: LeaveAdjustmentInput) => absencesApi.createAdjustment(input),
    [['absences', 'balance'], ['absences', 'adjustments']],
  );
}

/** What a day of leave pays and whose holidays count. Only asked for by those who may see amounts. */
export function useLeaveSettingsQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['leave-settings'] as const,
    queryFn: () => absencesApi.leaveSettings(),
    enabled,
  });
}

export function useUpdateLeaveSettings() {
  return useResourceMutation(
    (input: LeaveSettings) => absencesApi.updateLeaveSettings(input),
    [['leave-settings']],
  );
}

/**
 * Every absence write invalidates the board as well as the list. Granting
 * leave puts a bar on the schedule, so a board left on screen from before the
 * approval would show the person as available. `absenceKeys.all` is a prefix
 * of every `useAbsencesQuery` call, including the nav badge's and the
 * dashboard's own — so approving or refusing a request drops both of those
 * counts the instant it succeeds too, with nothing extra to list here.
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

export function useProposeAbsenceEdit() {
  return useResourceMutation(
    ({ id, input }: { id: string; input: ProposeAbsenceEditInput }) =>
      absencesApi.proposeEdit(id, input),
    absenceCaches,
  );
}

export function useConfirmAbsenceEdit() {
  return useResourceMutation(
    ({ id, input }: { id: string; input: ConfirmAbsenceEditInput }) =>
      absencesApi.confirmEdit(id, input),
    absenceCaches,
  );
}

export function useDeleteAbsence() {
  return useResourceMutation(
    (id: string) => absencesApi.remove(id),
    absenceCaches,
  );
}
