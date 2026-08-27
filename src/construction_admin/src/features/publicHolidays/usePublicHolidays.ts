import { useQuery } from '@tanstack/react-query';

import { publicHolidaysApi, type PublicHolidayListQuery } from '../../api/publicHolidays';
import type { PublicHolidayInput } from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

export const publicHolidayKeys = createResourceKeys<PublicHolidayListQuery>('publicHolidays');

export function usePublicHolidaysQuery(query: PublicHolidayListQuery = {}) {
  return useQuery({
    queryKey: publicHolidayKeys.list(query),
    queryFn: () => publicHolidaysApi.list(query),
  });
}

export function useCreatePublicHoliday() {
  return useResourceMutation(
    (input: PublicHolidayInput) => publicHolidaysApi.create(input),
    [publicHolidayKeys.all],
  );
}

export function useDeletePublicHoliday() {
  return useResourceMutation((id: string) => publicHolidaysApi.remove(id), [
    publicHolidayKeys.all,
  ]);
}
