import { useQuery } from '@tanstack/react-query';

import {
  attachmentsApi,
  type AttachmentListQuery,
  type UploadAttachmentInput,
} from '../../api/attachments';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

export const attachmentKeys = createResourceKeys<AttachmentListQuery>('attachments');

const expiringKey = (withinDays: number) => [
  ...attachmentKeys.all,
  'expiring',
  withinDays,
];

export function useAttachmentsQuery(query: AttachmentListQuery, enabled = true) {
  return useQuery({
    queryKey: attachmentKeys.list(query),
    queryFn: () => attachmentsApi.list(query),
    enabled: enabled && !!query.ownerId,
  });
}

export function useExpiringDocumentsQuery(withinDays = 30) {
  return useQuery({
    queryKey: expiringKey(withinDays),
    queryFn: () => attachmentsApi.expiring(withinDays),
  });
}

export function useUploadAttachment() {
  return useResourceMutation(
    (input: UploadAttachmentInput) => attachmentsApi.upload(input),
    [attachmentKeys.all],
  );
}

export function useDeleteAttachment() {
  return useResourceMutation((id: string) => attachmentsApi.remove(id), [
    attachmentKeys.all,
  ]);
}
