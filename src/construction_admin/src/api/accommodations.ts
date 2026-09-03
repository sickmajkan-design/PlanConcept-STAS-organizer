import { createCrudApi } from './resource';
import type { Accommodation, AccommodationInput, ListQuery } from './types';

export type AccommodationListQuery = ListQuery;

export const accommodationsApi = createCrudApi<
  Accommodation,
  Accommodation,
  AccommodationInput,
  AccommodationListQuery
>('/api/v1/accommodations');
