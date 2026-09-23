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
import { useEffect, useState } from 'react';

import type { Attachment, AttachmentCategory } from '../api/types';
import { attachmentCategories } from '../api/types';
import { useUpdateAttachment } from '../features/attachments/useAttachments';
import { useEnumLabel } from '../i18n/enumLabels';
import { useT } from '../i18n/useI18n';

/**
 * Edits a document's details in place — mainly the expiry date, which is how a
 * renewed certificate stops showing up as expiring. The file itself is not
 * touched.
 */
export function EditDocumentDialog({
  attachment,
  onClose,
}: {
  attachment: Attachment | null;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const update = useUpdateAttachment();

  const [category, setCategory] = useState<AttachmentCategory>('Other');
  const [description, setDescription] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [retainUntil, setRetainUntil] = useState('');

  const resetUpdate = update.reset;

  useEffect(() => {
    if (attachment) {
      setCategory(attachment.category);
      setDescription(attachment.description ?? '');
      setExpiresAt(attachment.expiresAt ?? '');
      setRetainUntil(attachment.retainUntil ?? '');
      resetUpdate();
    }
  }, [attachment, resetUpdate]);

  // A photograph does not lapse, and the API refuses an expiry on one.
  const expiryAllowed = category !== 'Photo';

  const submit = () => {
    if (!attachment) return;

    update.mutate(
      {
        id: attachment.id,
        category,
        description: description.trim() || null,
        expiresAt: expiryAllowed && expiresAt ? expiresAt : null,
        retainUntil: retainUntil || null,
      },
      { onSuccess: onClose },
    );
  };

  return (
    <Dialog open={!!attachment} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('attachments.editTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {attachment?.fileName}
          </Typography>

          <FormControl size="small" fullWidth>
            <InputLabel id="edit-doc-category">{t('attachments.category')}</InputLabel>
            <Select
              labelId="edit-doc-category"
              label={t('attachments.category')}
              value={category}
              onChange={(event) => setCategory(event.target.value as AttachmentCategory)}
            >
              {attachmentCategories.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('attachmentCategory', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            size="small"
            type="date"
            label={t('attachments.expiresAt')}
            value={expiryAllowed ? expiresAt : ''}
            disabled={!expiryAllowed}
            onChange={(event) => setExpiresAt(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />

          <TextField
            size="small"
            type="date"
            label={t('attachments.retainUntil')}
            value={retainUntil}
            onChange={(event) => setRetainUntil(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />

          <TextField
            size="small"
            label={t('attachments.description')}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            multiline
            minRows={2}
            slotProps={{ htmlInput: { maxLength: 1000 } }}
          />

          {update.error && <Alert severity="error">{update.error.message}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" onClick={submit} disabled={update.isPending}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
