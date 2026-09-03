import { createCrudApi } from './resource';
import type { Customer, CustomerInput, ListQuery } from './types';

export type CustomerListQuery = ListQuery;

export const customersApi = createCrudApi<Customer, Customer, CustomerInput, CustomerListQuery>(
  '/api/v1/customers',
);
