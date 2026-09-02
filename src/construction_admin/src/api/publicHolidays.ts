import { request } from './client';
import { listParams } from './resource';
import type { PublicHoliday, PublicHolidayCandidate, PublicHolidayInput } from './types';

export interface PublicHolidayListQuery {
  year?: number;
}

export interface HolidaySyncPreviewQuery {
  countryCode: string;
  year: number;
}

export interface ImportPublicHolidaysInput {
  items: { date: string; name: string }[];
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

  /** Fetches a country's public holidays for one year from the internet. Nothing is written yet. */
  syncPreview: (query: HolidaySyncPreviewQuery) =>
    request<PublicHolidayCandidate[]>({
      method: 'GET',
      url: '/api/v1/public-holidays/sync-preview',
      params: listParams(query),
    }),

  import: (input: ImportPublicHolidaysInput) =>
    request<PublicHoliday[]>({
      method: 'POST',
      url: '/api/v1/public-holidays/import',
      data: input,
    }),
};
