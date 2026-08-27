import { request } from './client';
import { listParams } from './resource';
import type { PublicHoliday, PublicHolidayInput } from './types';

export interface PublicHolidayListQuery {
  year?: number;
}

export const publicHolidaysApi = {
  list: (query: PublicHolidayListQuery = {}) =>
    request<PublicHoliday[]>({
      method: 'GET',
      url: '/api/v1/public-holidays',
      params: listParams(query),
    }),

  create: (input: PublicHolidayInput) =>
    request<PublicHoliday>({
      method: 'POST',
      url: '/api/v1/public-holidays',
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/public-holidays/${id}` }),
};
