import AttachFileIcon from '@mui/icons-material/AttachFile';
import { Stack, Tooltip, Typography } from '@mui/material';

import type { AttachmentOwnerType } from '../api/types';
import { useAttachmentCountsQuery } from '../features/attachments/useAttachments';
import { useT } from '../i18n/useI18n';

/**
 * How many documents a row's record has, shown among the row's action buttons.
 *
 * Every row of a list reads the same query, so a list of hundreds costs one request. Nothing is shown
 * until it has loaded, or for an account that may not see documents, rather than a wrong zero.
 */
export function DocumentCountBadge({
  ownerType,
  ownerId,
}: {
  ownerType: AttachmentOwnerType;
  ownerId: string;
}) {
  const t = useT();
  const { data } = useAttachmentCountsQuery(ownerType);

  if (!data) return null;

  const row = data.get(ownerId);
  const count = row?.count ?? 0;
  const expired = row?.expired ?? 0;

  return (
    <Tooltip
      title={
        expired > 0
          ? `${t('attachments.countChip', { count })} · ${t('attachments.countExpired', { count: expired })}`
          : t('attachments.countChip', { count })
      }
    >
      <Stack
        direction="row"
        spacing={0.25}
        data-testid="document-count"
        sx={{
          alignItems: 'center',
          px: 0.5,
          minWidth: 36,
          color: expired > 0 ? 'error.main' : count > 0 ? 'text.primary' : 'text.disabled',
        }}
      >
        <AttachFileIcon sx={{ fontSize: 16 }} />
        <Typography variant="body2" sx={{ fontWeight: expired > 0 ? 700 : 500 }}>
          {count}
        </Typography>
      </Stack>
    </Tooltip>
  );
}
