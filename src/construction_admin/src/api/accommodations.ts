import { request } from './client';
import { createCrudApi, listParams } from './resource';
import type {
  Accommodation,
  AccommodationCostSummary,
  AccommodationInput,
  AccommodationStay,
  AccommodationStayInput,
  AccommodationType,
  ListQuery,
  PagedList,
} from './types';

export interface AccommodationListQuery extends ListQuery {
  type?: AccommodationType;
  /** True: only the ones still rented. False: only the ones given up. Omit for both. */
  isActive?: boolean;
}

export interface AccommodationStayListQuery extends ListQuery {
  accommodationId?: string;
  employeeId?: string;
  /** Only the stays that cover today. */
  currentOnly?: boolean;
}

const crud = createCrudApi<Accommodation, Accommodation, AccommodationInput, AccommodationListQuery>(
  '/api/v1/accommodations',
);

export const accommodationsApi = {
  ...crud,

  stays: {
    list: (query: AccommodationStayListQuery) =>
      request<PagedList<AccommodationStay>>({
        method: 'GET',
        url: '/api/v1/accommodations/stays',
        params: listParams(query),
      }),

    add: (accommodationId: string, input: AccommodationStayInput) =>
      request<AccommodationStay>({
        method: 'POST',
        url: `/api/v1/accommodations/${accommodationId}/stays`,
        data: input,
      }),

    update: (id: string, input: AccommodationStayInput) =>
      request<AccommodationStay>({
        method: 'PUT',
        url: `/api/v1/accommodations/stays/${id}`,
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/accommodations/stays/${id}` }),
  },

  costs: (id: string, period: { from: string; to: string }) =>
    request<AccommodationCostSummary>({
      method: 'GET',
      url: `/api/v1/accommodations/${id}/costs`,
      params: period,
    }),
};
