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
  /** The business unit of the project, which issues the invoice. */
  branchId: string | null;
  branchName: string | null;
  branchColor: string | null;
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

/** Who stands behind the document: the unit's data, or the company's where the unit has none. */
export interface InvoiceIssuer {
  name: string;
  branchId: string | null;
  branchName: string | null;
  kind: 'LegalEntity' | 'RepresentativeOffice' | null;
  address: string | null;
  city: string | null;
  postalCode: string | null;
  countryCode: string | null;
  /** Tax numbers are null unless the account may see them. */
  taxId: string | null;
  registrationNumber: string | null;
  vatNumber: string | null;
  ownerName: string | null;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  /** The numbers shown are the company's, because the unit is an office with none of its own. */
  usesCompanyNumbers: boolean;
}

export interface InvoiceDocumentRecipient {
  name: string;
  address: string | null;
  taxId: string | null;
  registrationNumber: string | null;
  vatNumber: string | null;
  amount: number;
}

/** Everything a printed copy of a recorded invoice shows. */
export interface InvoiceDocument {
  invoiceId: string;
  number: string;
  issueDate: string;
  dueDate: string | null;
  description: string | null;
  amount: number;
  status: InvoiceStatus;
  cancelReason: string | null;
  payrollYear: number;
  payrollMonth: number;
  projectName: string;
  projectAddress: string | null;
  customerName: string | null;
  customerContactPerson: string | null;
  issuer: InvoiceIssuer;
  recipients: InvoiceDocumentRecipient[];
}

export interface InvoiceListQuery extends ListQuery {
  branchId?: string;
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

  document: (id: string) =>
    request<InvoiceDocument>({ method: 'GET', url: `/api/v1/invoices/${id}/document` }),

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
