import { useQuery } from '@tanstack/react-query';

import { customersApi, type CustomerListQuery } from '../../api/customers';
import type { CustomerInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const customerKeys = createResourceKeys<CustomerListQuery>('customers');

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: CustomerListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'name',
};

export function useCustomersQuery(query: CustomerListQuery) {
  return useResourceList(customerKeys, customersApi.list, query);
}

export function useCustomerQuery(id: string | undefined) {
  return useResourceDetail(customerKeys, customersApi.get, id);
}

/**
 * All customers for the project form's picker. Cached for a minute rather
 * than paged, like the other pickers: it is opened repeatedly and its
 * contents rarely change mid-session.
 */
export function useAllCustomersQuery() {
  return useQuery({
    queryKey: customerKeys.list(PICKER_QUERY),
    queryFn: () => customersApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateCustomer() {
  return useResourceMutation(
    (input: CustomerInput) => customersApi.create(input),
    [customerKeys.all],
  );
}

export function useUpdateCustomer(id: string) {
  return useResourceMutation(
    (input: CustomerInput) => customersApi.update(id, input),
    [customerKeys.all],
  );
}

export function useDeleteCustomer() {
  return useResourceMutation((id: string) => customersApi.remove(id), [
    customerKeys.all,
  ]);
}
