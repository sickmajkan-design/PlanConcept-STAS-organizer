import { DownloadOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { attachmentsApi } from '../api/attachments';
import type { Attachment } from '../api/types';
import { useT } from '../i18n/useI18n';

/**
 * A large in-place look at one document, opened on double-click instead of
 * making every look a download.
 *
 * Only images and PDFs render inline — a browser has no built-in way to show
 * a `.docx` or `.xlsx`, and pulling in a library to fake it is more than this
 * is worth. Everything else falls back to the download link it already had.
 */
export function AttachmentPreviewDialog({
  attachment,
  onClose,
}: {
  attachment: Attachment | null;
  onClose: () => void;
}) {
  const t = useT();
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!attachment) {
      setUrl(null);
      return;
    }

    let cancelled = false;
    let objectUrl: string | undefined;

    void attachmentsApi.objectUrl(attachment.id).then((resolved) => {
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
  }, [attachment]);

  const isImage = attachment?.contentType.startsWith('image/') ?? false;
  const isPdf = attachment?.contentType === 'application/pdf';

  return (
    <Dialog open={!!attachment} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{attachment?.fileName}</DialogTitle>
      <DialogContent>
        {!url ? (
          <Stack sx={{ alignItems: 'center', py: 6 }}>
            <CircularProgress size={28} />
          </Stack>
        ) : isImage ? (
          <Box
            component="img"
            src={url}
            alt={attachment?.fileName}
            sx={{ display: 'block', maxWidth: '100%', maxHeight: '75vh', mx: 'auto' }}
          />
        ) : isPdf ? (
          <Box
            component="iframe"
            src={url}
            title={attachment?.fileName}
            sx={{ width: '100%', height: '75vh', border: 0 }}
          />
        ) : (
          <Stack spacing={1} sx={{ py: 4, alignItems: 'center' }}>
            <Typography color="text.secondary">{t('attachments.noPreview')}</Typography>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        {url && attachment && (
          <Button
            component="a"
            href={url}
            download={attachment.fileName}
            startIcon={<DownloadOutlined />}
          >
            {t('attachments.download')}
          </Button>
        )}
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
