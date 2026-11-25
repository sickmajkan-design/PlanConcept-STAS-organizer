import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { newIdempotencyKey } from '../../api/idempotency';
import { timeEntriesApi, type ClockInInput, type ClockOutInput } from '../../api/timeEntries';
import { timeEntryKeys } from './useTimeEntries';

const shiftKey = [...timeEntryKeys.all, 'current'];

/** The signed-in person's running shift, or null. Polls, so a shift ended on the phone app shows here too. */
export function useCurrentShift() {
  return useQuery({ queryKey: shiftKey, queryFn: timeEntriesApi.current, refetchInterval: 60000 });
}

export function useClockInSites() {
  return useQuery({ queryKey: [...timeEntryKeys.all, 'clock-in-sites'], queryFn: timeEntriesApi.clockInSites, staleTime: 5 * 60000 });
}

/** The key is made per attempt, so a lost reply that is sent again is answered, not run twice. */
export function useClockIn() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ClockInInput) => timeEntriesApi.clockIn(input, newIdempotencyKey()),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: timeEntryKeys.all }),
  });
}

export function useClockOut() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ClockOutInput) => timeEntriesApi.clockOut(input, newIdempotencyKey()),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: timeEntryKeys.all }),
  });
}
