import { useBranchScoped } from '../branches/BranchContext';
import { useQuery } from '@tanstack/react-query';

import {
  invoicesApi,
  type CreateInvoiceInput,
  type CustomerCompanyInput,
  type InvoiceListQuery,
} from '../../api/invoices';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const invoiceKeys = createResourceKeys<InvoiceListQuery>('invoices');

export function useInvoicesQuery(query: InvoiceListQuery, enabled = true) {
  const scoped = useBranchScoped(query);
  return useResourceList(invoiceKeys, invoicesApi.list, scoped, { enabled });
}

/** The data of a printed copy of one invoice. Idle until an invoice is chosen. */
export function useInvoiceDocumentQuery(id: string | undefined) {
  return useQuery({
    queryKey: [...invoiceKeys.all, 'document', id] as const,
    queryFn: () => invoicesApi.document(id!),
    enabled: !!id,
  });
}

/** An invoice changes what a fixed-sum site is billed in the payroll, so its ledgers are dropped too. */
const caches = [invoiceKeys.all, ['ledgers'], ['customer-companies']];

export function useCreateInvoice() {
  return useResourceMutation((input: CreateInvoiceInput) => invoicesApi.create(input), caches);
}

export function useMarkInvoicePaid() {
  return useResourceMutation((id: string) => invoicesApi.markPaid(id), caches);
}

export function useCancelInvoice() {
  return useResourceMutation(
    ({ id, reason }: { id: string; reason: string }) => invoicesApi.cancel(id, reason),
    caches,
  );
}

/** The companies of a client. Empty means the client is invoiced as a whole. */
export function useCustomerCompaniesQuery(customerId: string | null | undefined) {
  return useQuery({
    queryKey: ['customer-companies', customerId] as const,
    queryFn: () => invoicesApi.companies(customerId!),
    enabled: !!customerId,
  });
}

export function useCreateCustomerCompany(customerId: string) {
  return useResourceMutation(
    (input: CustomerCompanyInput) => invoicesApi.createCompany(customerId, input),
    [['customer-companies']],
  );
}

export function useUpdateCustomerCompany() {
  return useResourceMutation(
    ({ id, input }: { id: string; input: CustomerCompanyInput }) => invoicesApi.updateCompany(id, input),
    [['customer-companies']],
  );
}
