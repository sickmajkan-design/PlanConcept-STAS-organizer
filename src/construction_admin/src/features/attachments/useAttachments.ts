import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';

import {
  ACCEPTED_EXTENSIONS,
  attachmentsApi,
  MAX_ATTACHMENT_BYTES,
  MAX_ATTACHMENT_TAG_LENGTH,
  MAX_ATTACHMENT_TAGS,
  MAX_ATTACHMENT_TITLE_LENGTH,
  type AttachmentListQuery,
  type UpdateAttachmentInput,
  type UploadAttachmentInput,
} from '../../api/attachments';
import type { AttachmentOwnerType } from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

export const attachmentKeys = createResourceKeys<AttachmentListQuery>('attachments');

/**
 * Every attachment write invalidates the whole collection. `attachmentKeys.all`
 * is a prefix of `expiringKey(...)` too, so uploading a renewal or deleting an
 * expiring document drops the nav badge's and the dashboard's counts the
 * instant the write succeeds — nothing extra to list here for either of them.
 */
const attachmentCaches = [attachmentKeys.all];

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

/**
 * How many documents each record of one kind has, as a map by id. One request however many rows the
 * list shows; every attachment write invalidates it with the rest, so the numbers stay current.
 * Undefined while loading or when the account may not see them, so a row simply shows nothing.
 */
export function useAttachmentCountsQuery(ownerType: AttachmentOwnerType) {
  return useQuery({
    queryKey: [...attachmentKeys.all, 'counts', ownerType],
    queryFn: () => attachmentsApi.counts(ownerType),
    retry: false,
    staleTime: 15_000,
    select: (rows) => new Map((Array.isArray(rows) ? rows : []).map((row) => [row.ownerId, row])),
  });
}

export function useExpiringDocumentsQuery(
  withinDays: number | null = 30,
  includeUndated = false,
  enabled = true,
) {
  return useQuery({
    queryKey: expiringKey(withinDays, includeUndated),
    queryFn: () => attachmentsApi.expiring(withinDays, includeUndated),
    enabled,
  });
}

/**
 * The labels already in use, for suggestions. Only admins may ask (the API refuses others), so the
 * caller passes whether to; a refusal just means no suggestions.
 */
export function useAttachmentTagsQuery(enabled: boolean) {
  return useQuery({
    queryKey: [...attachmentKeys.all, 'tags'],
    queryFn: () => attachmentsApi.tags(),
    enabled,
    retry: false,
    staleTime: 60_000,
  });
}

/**
 * The limits the server enforces, so the web never disagrees with it after a change there. Until the
 * answer arrives (or if it cannot be had) the built-in values stand in; the server judges anyway.
 */
export function useAttachmentLimits() {
  const { data } = useQuery({
    queryKey: [...attachmentKeys.all, 'limits'],
    queryFn: () => attachmentsApi.limits(),
    retry: false,
    staleTime: Infinity,
  });

  return useMemo(() => {
    // Anything but the expected shape (an old server, a failed call) leaves the built-in values.
    const number = (value: unknown, fallback: number) =>
      typeof value === 'number' && value > 0 ? value : fallback;
    const extensions = Array.isArray(data?.extensions) ? data.extensions : [];

    return {
      maxBytes: number(data?.maxSizeBytes, MAX_ATTACHMENT_BYTES),
      accept: extensions.length > 0 ? extensions.join(',') : ACCEPTED_EXTENSIONS,
      maxTags: number(data?.maxTags, MAX_ATTACHMENT_TAGS),
      maxTagLength: number(data?.maxTagLength, MAX_ATTACHMENT_TAG_LENGTH),
      maxTitleLength: number(data?.maxTitleLength, MAX_ATTACHMENT_TITLE_LENGTH),
    };
  }, [data]);
}

export function useUploadAttachment() {
  return useResourceMutation(
    (input: UploadAttachmentInput) => attachmentsApi.upload(input),
    attachmentCaches,
  );
}

export function useSetAttachmentSharing() {
  return useResourceMutation(
    (input: { ids: readonly string[]; shared: boolean }) =>
      attachmentsApi.setSharing(input.ids, input.shared),
    attachmentCaches,
  );
}

export function useUpdateAttachment() {
  return useResourceMutation(
    (input: UpdateAttachmentInput) => attachmentsApi.update(input),
    attachmentCaches,
  );
}

export function useDeleteAttachment() {
  return useResourceMutation((id: string) => attachmentsApi.remove(id), attachmentCaches);
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
