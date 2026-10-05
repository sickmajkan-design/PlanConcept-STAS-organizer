import AttachFileIcon from '@mui/icons-material/AttachFile';
import { Chip } from '@mui/material';

import type { AttachmentOwnerType } from '../api/types';
import { useAttachmentsQuery } from '../features/attachments/useAttachments';
import { useT } from '../i18n/useI18n';

/**
 * How many documents a record has, for its header. It reads the same query as the document
 * list, which every upload, edit and delete invalidates, so the number changes the moment
 * the change is saved and never drifts from what the list shows.
 */
export function DocumentCountChip({
  ownerType,
  ownerId,
}: {
  ownerType: AttachmentOwnerType;
  ownerId: string;
}) {
  const t = useT();
  const { data } = useAttachmentsQuery({ ownerType, ownerId });

  if (!data) return null;

  const today = new Date().toISOString().slice(0, 10);
  const expired = data.filter((a) => a.expiresAt !== null && a.expiresAt < today).length;
  const label =
    t('attachments.countChip', { count: data.length }) +
    (expired > 0 ? ` · ${t('attachments.countExpired', { count: expired })}` : '');

  return (
    <Chip
      size="small"
      variant="outlined"
      color={expired > 0 ? 'error' : 'default'}
      icon={<AttachFileIcon />}
      label={label}
      data-testid="document-count"
    />
  );
}
