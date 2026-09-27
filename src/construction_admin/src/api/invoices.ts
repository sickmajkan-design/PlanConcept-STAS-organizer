import { request } from './client';
import { listParams } from './resource';
import type { ListQuery, PagedList } from './types';

export const invoiceStatuses = ['Issued', 'Paid', 'Cancelled'] as const;
export type InvoiceStatus = (typeof invoiceStatuses)[number];

/** One company's part of an invoice. A null company is the client itself. */
export interface InvoiceShare {
  customerCompanyId: string | null;
  companyName: string | null;
  amount: number;
}

/** An invoice the firm issued to a client, recorded here. Money: only served to the finance grant. */
export interface Invoice {
  id: string;
  number: string;
  projectId: string;
  projectName: string;
  customerId: string | null;
  customerName: string | null;
  issueDate: string;
  dueDate: string | null;
  description: string | null;
  amount: number;
  payrollYear: number;
  payrollMonth: number;
  status: InvoiceStatus;
  cancelReason: string | null;
  createdAt: string;
  shares: InvoiceShare[];
}

export interface InvoiceListQuery extends ListQuery {
  projectId?: string;
  customerId?: string;
  customerCompanyId?: string;
  payrollYear?: number;
  payrollMonth?: number;
  status?: InvoiceStatus;
}

export interface CreateInvoiceInput {
  projectId: string;
  number: string;
  issueDate: string;
  dueDate?: string | null;
  description?: string | null;
  /** The whole invoice. Negative for a credit note; never zero. */
  amount: number;
  /** The payroll month it is counted in. Omitted means the month it was issued. */
  payrollYear?: number | null;
  payrollMonth?: number | null;
  /** Explicit parts. Leave empty to split evenly among `companyIds`. */
  shares?: { customerCompanyId: string | null; amount: number }[];
  companyIds?: string[];
}

/** One legal entity of a client, which an invoice can be split among. */
export interface CustomerCompany {
  id: string;
  customerId: string;
  name: string;
  address: string | null;
  isActive: boolean;
  invoiceCount: number;
}

export interface CustomerCompanyInput {
  name: string;
  address?: string | null;
  isActive?: boolean;
}

export const invoicesApi = {
  list: (query: InvoiceListQuery) =>
    request<PagedList<Invoice>>({
      method: 'GET',
      url: '/api/v1/invoices',
      params: listParams(query),
    }),

  create: (input: CreateInvoiceInput) =>
    request<Invoice>({ method: 'POST', url: '/api/v1/invoices', data: input }),

  markPaid: (id: string) =>
    request<Invoice>({ method: 'POST', url: `/api/v1/invoices/${id}/paid` }),

  cancel: (id: string, reason: string) =>
    request<Invoice>({ method: 'POST', url: `/api/v1/invoices/${id}/cancel`, data: { reason } }),

  companies: (customerId: string) =>
    request<CustomerCompany[]>({ method: 'GET', url: `/api/v1/customers/${customerId}/companies` }),

  createCompany: (customerId: string, input: CustomerCompanyInput) =>
    request<CustomerCompany>({
      method: 'POST',
      url: `/api/v1/customers/${customerId}/companies`,
      data: input,
    }),

  updateCompany: (companyId: string, input: CustomerCompanyInput) =>
    request<CustomerCompany>({
      method: 'PUT',
      url: `/api/v1/customers/companies/${companyId}`,
      data: input,
    }),
};
