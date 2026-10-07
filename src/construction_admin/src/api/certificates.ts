import { request } from './client';

export interface Certificate {
  id: string;
  name: string;
  /** The last valid day. Null for one that does not expire. */
  validUntil: string | null;
  note: string | null;
}

export interface CertificateInput {
  /** Set to change an existing one; left out to add. */
  id?: string;
  name: string;
  validUntil?: string | null;
  note?: string | null;
}

export const certificatesApi = {
  list: (employeeId: string) =>
    request<Certificate[]>({ method: 'GET', url: `/api/v1/employees/${employeeId}/certificates` }),

  save: (employeeId: string, input: CertificateInput) =>
    request<Certificate>({ method: 'PUT', url: `/api/v1/employees/${employeeId}/certificates`, data: input }),

  remove: (employeeId: string, id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/employees/${employeeId}/certificates/${id}` }),
};
