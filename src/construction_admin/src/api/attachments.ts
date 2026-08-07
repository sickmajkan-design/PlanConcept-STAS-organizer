import { apiClient, request } from './client';
import { listParams } from './resource';
import type {
  Attachment,
  AttachmentCategory,
  AttachmentOwnerType,
} from './types';

export interface AttachmentListQuery {
  ownerType: AttachmentOwnerType;
  ownerId: string;
  category?: AttachmentCategory;
}

export interface UploadAttachmentInput {
  ownerType: AttachmentOwnerType;
  ownerId: string;
  category: AttachmentCategory;
  file: File;
  description?: string | null;
  /** `YYYY-MM-DD`. Omitted for anything that does not lapse. */
  expiresAt?: string | null;
}

/** Mirrors the API's AttachmentRules, so the picker and the limits agree. */
export const MAX_ATTACHMENT_BYTES = 20 * 1024 * 1024;

export const ACCEPTED_EXTENSIONS =
  '.pdf,.jpg,.jpeg,.png,.webp,.heic,.doc,.docx,.xls,.xlsx,.txt';

export const attachmentsApi = {
  list: (query: AttachmentListQuery) =>
    request<Attachment[]>({
      method: 'GET',
      url: '/api/v1/attachments',
      params: listParams(query),
    }),

  expiring: (withinDays: number) =>
    request<Attachment[]>({
      method: 'GET',
      url: '/api/v1/attachments/expiring',
      params: { withinDays },
    }),

  upload: (input: UploadAttachmentInput) => {
    const form = new FormData();

    form.append('file', input.file);
    form.append('ownerType', input.ownerType);
    form.append('ownerId', input.ownerId);
    form.append('category', input.category);

    if (input.description) {
      form.append('description', input.description);
    }

    if (input.expiresAt) {
      form.append('expiresAt', input.expiresAt);
    }

    // No explicit Content-Type: the browser has to set it, because only it
    // knows the multipart boundary it generated.
    return request<Attachment>({
      method: 'POST',
      url: '/api/v1/attachments',
      data: form,
    });
  },

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/attachments/${id}` }),

  /**
   * Fetches an attachment's bytes as an object URL.
   *
   * The endpoint requires a bearer token, so the file cannot be reached by
   * putting its URL in an `<img src>` or an `<a href>` — the browser would
   * send that request without the token and get a 401. Going through the
   * authenticated client and wrapping the result in a blob URL is what makes
   * a private file previewable and downloadable at all.
   *
   * The caller owns the returned URL and must revoke it; until it does, the
   * blob is held in memory.
   */
  objectUrl: async (id: string): Promise<string> => {
    const response = await apiClient.request<Blob>({
      method: 'GET',
      url: `/api/v1/attachments/${id}/content`,
      responseType: 'blob',
    });

    return URL.createObjectURL(response.data);
  },
};
