import { useQuery } from '@tanstack/react-query';

import {
  documentRequestsApi,
  type CreateDocumentRequestsInput,
  type DocumentRequestListQuery,
} from '../../api/documentRequests';
import { attachmentKeys } from '../attachments/useAttachments';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const documentRequestKeys = createResourceKeys<DocumentRequestListQuery>('documentRequests');

/** Accepting puts a document on the person's record, so the document lists are dropped as well. */
const caches = [documentRequestKeys.all, attachmentKeys.all];

export function useDocumentRequestsQuery(query: DocumentRequestListQuery, enabled = true) {
  // Refreshed on a short interval and when the tab regains focus: the office is told by a notice when
  // a person answers, and the list should already agree when they arrive.
  return useResourceList(documentRequestKeys, documentRequestsApi.list, query, {
    refetchInterval: 30_000,
    enabled,
  });
}

export function useDocumentRequestQuery(id: string | null) {
  return useQuery({
    queryKey: [...documentRequestKeys.all, 'detail', id],
    queryFn: () => documentRequestsApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateDocumentRequests() {
  return useResourceMutation((input: CreateDocumentRequestsInput) => documentRequestsApi.create(input), caches);
}

export function useAcceptDocumentRequest() {
  return useResourceMutation(
    ({ id, expiresAt }: { id: string; expiresAt: string | null }) => documentRequestsApi.accept(id, expiresAt),
    caches,
  );
}

export function useReturnDocumentRequest() {
  return useResourceMutation(
    ({ id, reason }: { id: string; reason: string }) => documentRequestsApi.return(id, reason),
    caches,
  );
}

export function useDeleteDocumentRequest() {
  return useResourceMutation((id: string) => documentRequestsApi.remove(id), caches);
}

export function useCancelDocumentRequest() {
  return useResourceMutation((id: string) => documentRequestsApi.cancel(id), caches);
}
