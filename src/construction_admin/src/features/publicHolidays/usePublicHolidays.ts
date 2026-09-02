import { useMutation, useQuery } from '@tanstack/react-query';

import {
  publicHolidaysApi,
  type HolidaySyncPreviewQuery,
  type ImportPublicHolidaysInput,
  type PublicHolidayListQuery,
} from '../../api/publicHolidays';
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

/**
 * A mutation rather than a query: it's triggered by a "search" button, not
 * derived from state that should keep the result in sync, and nothing about
 * it should be cached or refetched automatically.
 */
export function usePreviewHolidaySync() {
  return useMutation({
    mutationFn: (query: HolidaySyncPreviewQuery) => publicHolidaysApi.syncPreview(query),
  });
}

export function useImportPublicHolidays() {
  return useResourceMutation(
    (input: ImportPublicHolidaysInput) => publicHolidaysApi.import(input),
    [publicHolidayKeys.all],
  );
}
