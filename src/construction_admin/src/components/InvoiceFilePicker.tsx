import { AttachFileOutlined, CloseOutlined } from '@mui/icons-material';
import { Chip, Stack, Typography } from '@mui/material';
import { useState } from 'react';

import { useAttachmentLimits } from '../features/attachments/useAttachments';
import type { Rejection } from '../features/files/validateFiles';
import { useT } from '../i18n/useI18n';
import { FileDropzone } from './files/FileDropzone';
import { rejectionText } from './files/rejectionText';

/**
 * Choose the scan or photo of an invoice to file with a delivery. The file is
 * only held here; the screen that saves the delivery uploads it once the
 * delivery exists to attach it to.
 */
export function InvoiceFilePicker({
  file,
  onChange,
}: {
  file: File | null;
  onChange: (file: File | null) => void;
}) {
  const t = useT();
  const limits = useAttachmentLimits();
  const [problem, setProblem] = useState<string | null>(null);

  const refused = (rejected: Rejection[]) => {
    const first = rejected[0];

    if (!first) return;

    setProblem(
      first.reason === 'size'
        ? t('invoiceFile.tooBig')
        : first.reason === 'type'
          ? t('invoiceFile.badType')
          : rejectionText(t, first, limits.maxBytes),
    );
  };

  return (
    <Stack spacing={0.5} sx={{ alignItems: 'stretch' }}>
      {file ? (
        <Chip
          icon={<AttachFileOutlined />}
          label={file.name}
          onDelete={() => onChange(null)}
          deleteIcon={<CloseOutlined aria-label={t('invoiceFile.remove')} />}
          sx={{ maxWidth: '100%', alignSelf: 'flex-start' }}
        />
      ) : (
        <FileDropzone
          compact
          multiple={false}
          accept={limits.accept}
          maxBytes={limits.maxBytes}
          helper={problem ?? t('invoiceFile.hint')}
          helperError={!!problem}
          onFiles={([chosen]) => {
            setProblem(null);
            onChange(chosen ?? null);
          }}
          onReject={refused}
        />
      )}
      {file && (
        <Typography variant="caption" color="text.secondary">
          {t('invoiceFile.hint')}
        </Typography>
      )}
    </Stack>
  );
}
