import { CloseOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Chip,
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

  const [files, setFiles] = useState<File[]>([]);
  const [category, setCategory] = useState<AttachmentCategory>(categories[0]!);
  const [description, setDescription] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);
  const [progress, setProgress] = useState<{ done: number; total: number } | null>(
    null,
  );

  // A photograph has no expiry, and the API refuses one.
  const expiryAllowed = category !== 'Photo';

  const reset = () => {
    setFiles([]);
    setCategory(categories[0]!);
    setDescription('');
    setExpiresAt('');
    setLocalError(null);
    setProgress(null);
  };

  const close = () => {
    reset();
    onClose();
  };

  const pick = (chosen: FileList | null) => {
    setLocalError(null);

    if (!chosen || chosen.length === 0) {
      return;
    }

    const picked = Array.from(chosen);

    // Checked here as well as on the server so a 20 MB upload is refused
    // before it is sent rather than after.
    const tooLarge = picked.find((f) => f.size > MAX_ATTACHMENT_BYTES);

    if (tooLarge) {
      setLocalError(
        t('attachments.tooLarge', {
          limit: Math.round(MAX_ATTACHMENT_BYTES / (1024 * 1024)),
        }),
      );
      return;
    }

    setFiles((prev) => [...prev, ...picked]);
  };

  const removeFile = (index: number) => {
    setFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const submit = async () => {
    if (files.length === 0) {
      return;
    }

    setProgress({ done: 0, total: files.length });

    for (const [index, file] of files.entries()) {
      // eslint-disable-next-line no-await-in-loop -- each upload must finish before the next starts
      await upload.mutateAsync({
        ownerType,
        ownerId,
        category,
        file,
        description: description.trim() || null,
        expiresAt: expiryAllowed && expiresAt ? expiresAt : null,
      });
      setProgress({ done: index + 1, total: files.length });
    }

    close();
  };

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('attachments.uploadTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ mt: 1 }}>
          {localError && <Alert severity="error">{localError}</Alert>}
          {upload.error && <Alert severity="error">{upload.error.message}</Alert>}

          <Button variant="outlined" component="label">
            {files.length > 0
              ? t('attachments.filesChosen', { count: files.length })
              : t('attachments.chooseFiles')}
            <input
              hidden
              type="file"
              multiple
              accept={ACCEPTED_EXTENSIONS}
              onChange={(event) => {
                pick(event.target.files);
                event.target.value = '';
              }}
            />
          </Button>

          {files.length > 0 && (
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              {files.map((f, index) => (
                <Chip
                  key={`${f.name}-${index}`}
                  label={f.name}
                  size="small"
                  onDelete={() => removeFile(index)}
                  deleteIcon={<CloseOutlined />}
                />
              ))}
            </Stack>
          )}

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
          disabled={files.length === 0 || upload.isPending}
          onClick={() => void submit()}
        >
          {progress
            ? t('attachments.uploadingProgress', {
                done: progress.done,
                total: progress.total,
              })
            : t('attachments.upload')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
