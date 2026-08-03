import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import {
  ACCEPTED_EXTENSIONS,
  MAX_ATTACHMENT_BYTES,
} from '../api/attachments';
import type {
  AttachmentCategory,
  AttachmentOwnerType,
} from '../api/types';
import { useUploadAttachment } from '../features/attachments/useAttachments';
import { useEnumLabel } from '../i18n/enumLabels';
import { useT } from '../i18n/useI18n';

export function UploadAttachmentDialog({
  open,
  ownerType,
  ownerId,
  categories,
  onClose,
}: {
  open: boolean;
  ownerType: AttachmentOwnerType;
  ownerId: string;
  categories: readonly AttachmentCategory[];
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const upload = useUploadAttachment();

  const [file, setFile] = useState<File | null>(null);
  const [category, setCategory] = useState<AttachmentCategory>(categories[0]!);
  const [description, setDescription] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  // A photograph has no expiry, and the API refuses one.
  const expiryAllowed = category !== 'Photo';

  const reset = () => {
    setFile(null);
    setCategory(categories[0]!);
    setDescription('');
    setExpiresAt('');
    setLocalError(null);
  };

  const close = () => {
    reset();
    onClose();
  };

  const pick = (chosen: File | null) => {
    setLocalError(null);

    // Checked here as well as on the server so a 20 MB upload is refused
    // before it is sent rather than after.
    if (chosen && chosen.size > MAX_ATTACHMENT_BYTES) {
      setLocalError(
        t('attachments.tooLarge', {
          limit: Math.round(MAX_ATTACHMENT_BYTES / (1024 * 1024)),
        }),
      );
      setFile(null);
      return;
    }

    setFile(chosen);
  };

  const submit = async () => {
    if (!file) {
      return;
    }

    await upload.mutateAsync(
      {
        ownerType,
        ownerId,
        category,
        file,
        description: description.trim() || null,
        expiresAt: expiryAllowed && expiresAt ? expiresAt : null,
      },
      { onSuccess: close },
    );
  };

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('attachments.uploadTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ mt: 1 }}>
          {localError && <Alert severity="error">{localError}</Alert>}
          {upload.error && <Alert severity="error">{upload.error.message}</Alert>}

          <Button variant="outlined" component="label">
            {file ? file.name : t('attachments.chooseFile')}
            <input
              hidden
              type="file"
              accept={ACCEPTED_EXTENSIONS}
              onChange={(event) => pick(event.target.files?.[0] ?? null)}
            />
          </Button>

          <FormControl fullWidth>
            <InputLabel id="attachment-category-label">
              {t('attachments.category')}
            </InputLabel>
            <Select
              labelId="attachment-category-label"
              label={t('attachments.category')}
              value={category}
              onChange={(event) =>
                setCategory(event.target.value as AttachmentCategory)
              }
            >
              {categories.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('attachmentCategory', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label={t('attachments.description')}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            fullWidth
          />

          <TextField
            label={t('attachments.expiresAt')}
            type="date"
            value={expiresAt}
            onChange={(event) => setExpiresAt(event.target.value)}
            disabled={!expiryAllowed}
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
            helperText={
              expiryAllowed
                ? t('attachments.expiresHint')
                : t('attachments.photoNoExpiry')
            }
          />

          <Typography variant="caption" color="text.secondary">
            {ACCEPTED_EXTENSIONS.replaceAll(',', ' ')} ·{' '}
            {Math.round(MAX_ATTACHMENT_BYTES / (1024 * 1024))} MB
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!file || upload.isPending}
          onClick={() => void submit()}
        >
          {t('attachments.upload')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
