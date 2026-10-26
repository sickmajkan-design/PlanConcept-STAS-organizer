import { createCrudApi } from './resource';
import type { Customer, CustomerInput, ListQuery } from './types';

export interface CustomerListQuery extends ListQuery {
  /** A two-letter country code, or `none` for the customers without one. */
  country?: string;
}

export const customersApi = createCrudApi<Customer, Customer, CustomerInput, CustomerListQuery>(
  '/api/v1/customers',
);
