import { AttachFileOutlined, CloseOutlined } from '@mui/icons-material';
import { Button, Chip, Stack, Typography } from '@mui/material';
import { useRef, useState } from 'react';

import { ACCEPTED_EXTENSIONS, MAX_ATTACHMENT_BYTES } from '../api/attachments';
import { useT } from '../i18n/useI18n';

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
  const input = useRef<HTMLInputElement>(null);
  const [problem, setProblem] = useState<string | null>(null);

  const choose = (chosen: File | undefined) => {
    setProblem(null);
    if (!chosen) return;

    const extension = `.${chosen.name.split('.').pop()?.toLowerCase() ?? ''}`;

    if (chosen.size > MAX_ATTACHMENT_BYTES) {
      setProblem(t('invoiceFile.tooBig'));
    } else if (!ACCEPTED_EXTENSIONS.split(',').includes(extension)) {
      setProblem(t('invoiceFile.badType'));
    } else {
      onChange(chosen);
    }
  };

  return (
    <Stack spacing={0.5} sx={{ alignItems: 'flex-start' }}>
      <input
        ref={input}
        type="file"
        hidden
        accept={ACCEPTED_EXTENSIONS}
        onChange={(event) => {
          choose(event.target.files?.[0]);
          event.target.value = '';
        }}
      />
      {file ? (
        <Chip
          icon={<AttachFileOutlined />}
          label={file.name}
          onDelete={() => onChange(null)}
          deleteIcon={<CloseOutlined aria-label={t('invoiceFile.remove')} />}
          sx={{ maxWidth: '100%' }}
        />
      ) : (
        <Button
          variant="outlined"
          size="small"
          startIcon={<AttachFileOutlined />}
          onClick={() => input.current?.click()}
        >
          {t('invoiceFile.attach')}
        </Button>
      )}
      <Typography variant="caption" color={problem ? 'error' : 'text.secondary'}>
        {problem ?? t('invoiceFile.hint')}
      </Typography>
    </Stack>
  );
}
