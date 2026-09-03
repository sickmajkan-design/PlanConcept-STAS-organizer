import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import {
  attachmentsApi,
  type AttachmentListQuery,
  type UploadAttachmentInput,
} from '../../api/attachments';
import type { AttachmentOwnerType } from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

export const attachmentKeys = createResourceKeys<AttachmentListQuery>('attachments');

const expiringKey = (withinDays: number | null, includeUndated: boolean) => [
  ...attachmentKeys.all,
  'expiring',
  withinDays,
  includeUndated,
];

export function useAttachmentsQuery(query: AttachmentListQuery, enabled = true) {
  return useQuery({
    queryKey: attachmentKeys.list(query),
    queryFn: () => attachmentsApi.list(query),
    enabled: enabled && !!query.ownerId,
  });
}

export function useExpiringDocumentsQuery(withinDays: number | null = 30, includeUndated = false) {
  return useQuery({
    queryKey: expiringKey(withinDays, includeUndated),
    queryFn: () => attachmentsApi.expiring(withinDays, includeUndated),
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

/**
 * Resolves the record's cover photo: the newest `Photo`-category attachment,
 * fetched through the authenticated blob-URL path (see `attachmentsApi.objectUrl`
 * for why a plain `<img src>` cannot work). Returns `null` while loading or
 * when the record has no photo.
 */
export function useCoverPhoto(ownerType: AttachmentOwnerType, ownerId: string) {
  const { data } = useAttachmentsQuery({ ownerType, ownerId, category: 'Photo' });
  const photoId = data?.[0]?.id ?? null;
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!photoId) {
      setUrl(null);
      return;
    }

    let cancelled = false;
    let objectUrl: string | undefined;

    void attachmentsApi.objectUrl(photoId).then((resolved) => {
      if (cancelled) {
        URL.revokeObjectURL(resolved);
        return;
      }

      objectUrl = resolved;
      setUrl(resolved);
    });

    return () => {
      cancelled = true;

      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [photoId]);

  return url;
}
