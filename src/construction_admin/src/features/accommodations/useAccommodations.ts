import { useQuery, useQueryClient } from '@tanstack/react-query';

import {
  accommodationsApi,
  type AccommodationListQuery,
  type AccommodationStayListQuery,
} from '../../api/accommodations';
import type { AccommodationInput, AccommodationStayInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const accommodationKeys = createResourceKeys<AccommodationListQuery>('accommodations');
export const accommodationStayKeys = createResourceKeys<AccommodationStayListQuery>('accommodationStays');

/** A stay changes who lives where, so the occupancy and the cost split follow. */
const stayCaches = [accommodationStayKeys.all, accommodationKeys.all, ['accommodationCosts']];

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

export function useAccommodationStaysQuery(query: AccommodationStayListQuery, enabled = true) {
  return useResourceList(accommodationStayKeys, accommodationsApi.stays.list, query, { enabled });
}

export function useAddAccommodationStay() {
  return useResourceMutation(
    (variables: { accommodationId: string; input: AccommodationStayInput }) =>
      accommodationsApi.stays.add(variables.accommodationId, variables.input),
    stayCaches,
  );
}

export function useUpdateAccommodationStay() {
  return useResourceMutation(
    (variables: { id: string; input: AccommodationStayInput }) =>
      accommodationsApi.stays.update(variables.id, variables.input),
    stayCaches,
  );
}

export function useDeleteAccommodationStay() {
  return useResourceMutation((id: string) => accommodationsApi.stays.remove(id), stayCaches);
}

/** What an accommodation cost over a period. Only for those who may see spending. */
export function useAccommodationCostsQuery(
  id: string | undefined,
  period: { from: string; to: string },
  enabled = true,
) {
  return useQuery({
    queryKey: ['accommodationCosts', id, period],
    queryFn: () => accommodationsApi.costs(id!, period),
    enabled: !!id && enabled,
  });
}

/** Refresh hook for callers that changed rates and want the cost summary to follow. */
export function useRefreshAccommodationCosts() {
  const queryClient = useQueryClient();

  return () => queryClient.invalidateQueries({ queryKey: ['accommodationCosts'] });
}
