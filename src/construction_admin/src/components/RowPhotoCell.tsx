import { Avatar } from '@mui/material';
import type { ReactNode } from 'react';

import type { AttachmentOwnerType } from '../api/types';
import { useCoverPhoto } from '../features/attachments/useAttachments';

/** Small cover-photo thumbnail for a grid row, falling back to an icon. */
export function RowPhotoCell({
  ownerType,
  ownerId,
  icon,
}: {
  ownerType: AttachmentOwnerType;
  ownerId: string;
  icon: ReactNode;
}) {
  const photo = useCoverPhoto(ownerType, ownerId);

  return (
    <Avatar src={photo ?? undefined} variant="rounded" sx={{ width: 32, height: 32 }}>
      {icon}
    </Avatar>
  );
}
