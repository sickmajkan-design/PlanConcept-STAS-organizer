import { useQuery } from '@tanstack/react-query';

import { accommodationsApi, type AccommodationListQuery } from '../../api/accommodations';
import type { AccommodationInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const accommodationKeys = createResourceKeys<AccommodationListQuery>('accommodations');

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: AccommodationListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'address',
};

export function useAccommodationsQuery(query: AccommodationListQuery) {
  return useResourceList(accommodationKeys, accommodationsApi.list, query);
}

export function useAccommodationQuery(id: string | undefined) {
  return useResourceDetail(accommodationKeys, accommodationsApi.get, id);
}

/** All accommodations for a picker. Cached for a minute rather than paged. */
export function useAllAccommodationsQuery() {
  return useQuery({
    queryKey: accommodationKeys.list(PICKER_QUERY),
    queryFn: () => accommodationsApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateAccommodation() {
  return useResourceMutation(
    (input: AccommodationInput) => accommodationsApi.create(input),
    [accommodationKeys.all],
  );
}

export function useUpdateAccommodation(id: string) {
  return useResourceMutation(
    (input: AccommodationInput) => accommodationsApi.update(id, input),
    [accommodationKeys.all],
  );
}

export function useDeleteAccommodation() {
  return useResourceMutation((id: string) => accommodationsApi.remove(id), [
    accommodationKeys.all,
  ]);
}
