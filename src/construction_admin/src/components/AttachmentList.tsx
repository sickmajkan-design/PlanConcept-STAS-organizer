import {
  DeleteOutlined,
  DescriptionOutlined,
  DownloadOutlined,
  UploadFileOutlined,
} from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  IconButton,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { attachmentsApi } from '../api/attachments';
import type {
  Attachment,
  AttachmentCategory,
  AttachmentOwnerType,
} from '../api/types';
import {
  useAttachmentsQuery,
  useDeleteAttachment,
} from '../features/attachments/useAttachments';
import { useDeleteWithConfirm } from '../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../i18n/enumLabels';
import { useT } from '../i18n/useI18n';
import { formatDate } from '../utils/formatting';
import { ConfirmDialog } from './ConfirmDialog';
import { ErrorState } from './ErrorState';
import { UploadAttachmentDialog } from './UploadAttachmentDialog';

/** How soon counts as "expiring soon" on a badge. */
const SOON_DAYS = 30;

function daysUntil(date: string): number {
  const target = new Date(`${date}T00:00`).getTime();

  return Math.ceil((target - Date.now()) / 86_400_000);
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} kB`;

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * The documents on one record, with upload and delete.
 *
 * A component rather than a page: documents belong beside the thing they
 * describe, so this is embedded in the employee, project, vehicle and tool
 * detail screens instead of living somewhere a user has to navigate to.
 */
export function AttachmentList({
  ownerType,
  ownerId,
  categories,
  canUpload = true,
  canDelete = false,
}: {
  ownerType: AttachmentOwnerType;
  ownerId: string;
  /** Types offered in the upload dialog, narrowed to what suits this record. */
  categories: readonly AttachmentCategory[];
  canUpload?: boolean;
  canDelete?: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();

  const { data, isError, error, refetch } = useAttachmentsQuery({
    ownerType,
    ownerId,
  });

  const remove = useDeleteWithConfirm<Attachment>(useDeleteAttachment());
  const [uploadOpen, setUploadOpen] = useState(false);

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const attachments = data ?? [];

  return (
    <Box>
      <Stack
        direction="row"
        sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}
      >
        <Typography variant="subtitle2" color="text.secondary">
          {t('attachments.title')}
        </Typography>
        {canUpload && (
          <Button
            size="small"
            startIcon={<UploadFileOutlined />}
            onClick={() => setUploadOpen(true)}
          >
            {t('attachments.add')}
          </Button>
        )}
      </Stack>

      {attachments.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {t('attachments.empty')}
        </Typography>
      ) : (
        <List dense disablePadding>
          {attachments.map((attachment) => (
            <ListItem
              key={attachment.id}
              divider
              secondaryAction={
                <Stack direction="row" spacing={0.5}>
                  <DownloadButton attachment={attachment} />
                  {canDelete && (
                    <Tooltip title={t('common.delete')}>
                      <IconButton
                        size="small"
                        onClick={() => remove.request(attachment)}
                      >
                        <DeleteOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </Stack>
              }
            >
              <ListItemIcon sx={{ minWidth: 36 }}>
                <DescriptionOutlined fontSize="small" />
              </ListItemIcon>
              <ListItemText
                primary={
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <span>{attachment.fileName}</span>
                    <ExpiryChip expiresAt={attachment.expiresAt} />
                  </Stack>
                }
                secondary={[
                  enumLabel('attachmentCategory', attachment.category),
                  formatSize(attachment.sizeBytes),
                  attachment.description,
                ]
                  .filter(Boolean)
                  .join(' · ')}
              />
            </ListItem>
          ))}
        </List>
      )}

      <UploadAttachmentDialog
        open={uploadOpen}
        ownerType={ownerType}
        ownerId={ownerId}
        categories={categories}
        onClose={() => setUploadOpen(false)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('attachments.deleteTitle')}
        description={
          remove.pending
            ? t('attachments.deleteBody', { name: remove.pending.fileName })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Typography variant="body2" color="error" sx={{ mt: 1 }}>
          {remove.error.message}
        </Typography>
      )}
    </Box>
  );
}

/** Red once lapsed, amber while it is close, nothing otherwise. */
function ExpiryChip({ expiresAt }: { expiresAt: string | null }) {
  const t = useT();

  if (!expiresAt) {
    return null;
  }

  const remaining = daysUntil(expiresAt);

  if (remaining < 0) {
    return <Chip size="small" color="error" label={t('attachments.expired')} />;
  }

  if (remaining <= SOON_DAYS) {
    return (
      <Chip
        size="small"
        color="warning"
        label={t('attachments.expiresOn', { date: formatDate(expiresAt) })}
      />
    );
  }

  return (
    <Chip
      size="small"
      variant="outlined"
      label={t('attachments.expiresOn', { date: formatDate(expiresAt) })}
    />
  );
}

/**
 * Downloads through the authenticated client.
 *
 * A plain link cannot work: the endpoint needs a bearer token and a browser
 * navigation does not carry one. So the bytes are fetched, wrapped in a blob
 * URL, handed to a synthetic anchor, and the URL revoked — otherwise the blob
 * stays in memory for the life of the page.
 */
function DownloadButton({ attachment }: { attachment: Attachment }) {
  const t = useT();
  const [busy, setBusy] = useState(false);

  const download = async () => {
    setBusy(true);

    let url: string | undefined;

    try {
      url = await attachmentsApi.objectUrl(attachment.id);

      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = attachment.fileName;
      anchor.click();
    } finally {
      if (url) {
        URL.revokeObjectURL(url);
      }

      setBusy(false);
    }
  };

  return (
    <Tooltip title={t('attachments.download')}>
      <span>
        <IconButton size="small" disabled={busy} onClick={() => void download()}>
          <DownloadOutlined fontSize="small" />
        </IconButton>
      </span>
    </Tooltip>
  );
}
