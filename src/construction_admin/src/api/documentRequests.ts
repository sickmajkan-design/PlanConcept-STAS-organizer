import { request } from './client';
import { listParams } from './resource';
import type { AttachmentCategory, ListQuery, PagedList } from './types';

export const documentRequestStatuses = ['Open', 'Submitted', 'Accepted', 'Returned', 'Cancelled'] as const;

export type DocumentRequestStatus = (typeof documentRequestStatuses)[number];

export interface DocumentRequestFile {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface DocumentRequestEvent {
  type: 'Created' | 'Submitted' | 'Accepted' | 'Returned' | 'Cancelled';
  at: string;
  /** Whether it was the person themselves, not the office. */
  byEmployee: boolean;
  actorName: string | null;
  reason: string | null;
  files: DocumentRequestFile[];
}

export interface DocumentRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  category: AttachmentCategory;
  title: string;
  note: string | null;
  /** `YYYY-MM-DD`. */
  dueDate: string | null;
  status: DocumentRequestStatus;
  attempts: number;
  requestedByName: string | null;
  createdAt: string;
  reviewedAt: string | null;
  returnReason: string | null;
  /** The files of the latest answer. Only on a single request. */
  files: DocumentRequestFile[];
  /** Oldest first. Only on a single request. */
  events: DocumentRequestEvent[];
}

export interface DocumentRequestListQuery extends ListQuery {
  status?: DocumentRequestStatus;
  employeeId?: string;
  overdue?: boolean;
}

export interface DocumentRequestItem {
  category: AttachmentCategory;
  title: string;
  note?: string | null;
  /** `YYYY-MM-DD`. */
  dueDate?: string | null;
}

export interface CreateDocumentRequestsInput {
  employeeIds: string[];
  items: DocumentRequestItem[];
}

export interface CreateDocumentRequestsResult {
  created: DocumentRequest[];
  /** How many were left out because the same one was already waiting. */
  skipped: number;
}

const BASE = '/api/v1/documentrequests';

export const documentRequestsApi = {
  list: (query: DocumentRequestListQuery) =>
    request<PagedList<DocumentRequest>>({ method: 'GET', url: BASE, params: listParams(query) }),

  get: (id: string) => request<DocumentRequest>({ method: 'GET', url: `${BASE}/${id}` }),

  create: (input: CreateDocumentRequestsInput) =>
    request<CreateDocumentRequestsResult>({ method: 'POST', url: BASE, data: input }),

  accept: (id: string, expiresAt: string | null) =>
    request<DocumentRequest>({ method: 'POST', url: `${BASE}/${id}/accept`, data: { expiresAt } }),

  return: (id: string, reason: string) =>
    request<DocumentRequest>({ method: 'POST', url: `${BASE}/${id}/return`, data: { reason } }),

  cancel: (id: string) => request<DocumentRequest>({ method: 'POST', url: `${BASE}/${id}/cancel` }),

  /** Removes a withdrawn request for good. */
  remove: (id: string) => request<void>({ method: 'DELETE', url: `${BASE}/${id}` }),
};

/** Categories whose documents lapse, so accepting one needs the date it is valid until. Mirrors the API. */
export const categoriesWithExpiry: readonly AttachmentCategory[] = [
  'Identity',
  'Visa',
  'Licence',
  'Certificate',
  'MedicalCheck',
];

/** The "new worker" package: what is normally asked for when somebody starts. */
export const newWorkerPackage: readonly { category: AttachmentCategory; titleKey: string }[] = [
  { category: 'Identity', titleKey: 'docRequests.package.idCard' },
  { category: 'Identity', titleKey: 'docRequests.package.passport' },
  { category: 'Visa', titleKey: 'docRequests.package.visa' },
  { category: 'Contract', titleKey: 'docRequests.package.contract' },
  { category: 'Licence', titleKey: 'docRequests.package.licence' },
  { category: 'Diploma', titleKey: 'docRequests.package.diploma' },
  { category: 'Certificate', titleKey: 'docRequests.package.certificates' },
];
