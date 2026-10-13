import { ExpandMore } from '@mui/icons-material';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
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
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useEffect, useMemo, useState, type ReactNode } from 'react';

import { ACCEPTED_EXTENSIONS, MAX_ATTACHMENT_BYTES, MAX_ATTACHMENT_TITLE_LENGTH } from '../../api/attachments';
import type { AttachmentCategory, AttachmentOwnerType } from '../../api/types';
import { useAuth } from '../../auth/useAuth';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAttachmentTagsQuery } from '../../features/attachments/useAttachments';
import { useUploadQueue } from '../../features/files/useUploadQueue';
import { validateFiles, type Rejection } from '../../features/files/validateFiles';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { ReminderDaysField } from '../ReminderDaysField';
import { FileDropzone } from './FileDropzone';
import { FileQueueList } from './FileQueueList';
import { FirstUseHint, HintLine } from './FirstUseHint';
import { rejectionText } from './rejectionText';
import { TagsField } from './TagsField';

/**
 * The one dialog behind every "add a document": the zone, the files with their progress, the details that
 * go for all of them (title when it is a single file, type, note, labels) and the optional dates. What
 * differs between callers is only which record it files against and which types it offers, so the
 * per-record dialog and the all-documents one are thin wrappers around this.
 */
export function DocumentUploadDialog({
  open,
  onClose,
  ownerType,
  ownerId,
  categories,
  ownerSlot,
  initialFiles,
}: {
  open: boolean;
  onClose: () => void;
  ownerType: AttachmentOwnerType;
  /** Empty while the owner has not been chosen yet (the all-documents dialog). */
  ownerId: string;
  categories: readonly AttachmentCategory[];
  /** Shown above the zone, for the caller that lets the person pick the record. */
  ownerSlot?: ReactNode;
  /** Files already dropped on the page behind the dialog. */
  initialFiles?: readonly File[];
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const theme = useTheme();
  const phone = useMediaQuery(theme.breakpoints.down('sm'));
  const { user } = useAuth();

  const queue = useUploadQueue();
  const tagSuggestions = useAttachmentTagsQuery(open && canAdministerAccounts(user));

  const [category, setCategory] = useState<AttachmentCategory>(categories[0] ?? 'Other');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [tags, setTags] = useState<string[]>([]);
  const [expiresAt, setExpiresAt] = useState('');
  const [retainUntil, setRetainUntil] = useState('');
  const [reminderDays, setReminderDays] = useState<number[]>([]);
  const [datesOpen, setDatesOpen] = useState(false);
  const [rejected, setRejected] = useState<Rejection[]>([]);

  const rules = useMemo(() => ({ accept: ACCEPTED_EXTENSIONS, maxBytes: MAX_ATTACHMENT_BYTES }), []);

  // A photograph has no expiry, and the API refuses one.
  const expiryAllowed = category !== 'Photo';
  const single = queue.items.length <= 1;

  const { clear, add } = queue;

  // Opening starts clean; files already dropped on the page behind are taken straight in.
  useEffect(() => {
    if (!open) return;

    clear();
    setCategory(categories[0] ?? 'Other');
    setTitle('');
    setDescription('');
    setTags([]);
    setExpiresAt('');
    setRetainUntil('');
    setReminderDays([]);
    setDatesOpen(false);
    setRejected([]);

    if (initialFiles && initialFiles.length > 0) {
      const { accepted, rejected: turnedAway } = validateFiles(initialFiles, rules);
      add(accepted);
      setRejected(turnedAway);
    }
    // Only opening should reset; the other values are read when it happens.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  // The record picker of the all-documents dialog changes the types on offer.
  useEffect(() => {
    if (!categories.includes(category)) {
      setCategory(categories[0] ?? 'Other');
    }
  }, [categories, category]);

  const close = () => {
    clear();
    onClose();
  };

  // Everything went through: nothing left to look at.
  useEffect(() => {
    if (queue.started && !queue.running && queue.items.length > 0 && queue.failed === 0 && queue.cancelled === 0) {
      close();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queue.started, queue.running, queue.items.length, queue.failed, queue.cancelled]);

  const start = () =>
    queue.start((file) => ({
      ownerType,
      ownerId,
      category,
      file,
      title: queue.items.length === 1 ? title.trim() || null : null,
      tags,
      description: description.trim() || null,
      expiresAt: expiryAllowed && expiresAt ? expiresAt : null,
      retainUntil: retainUntil || null,
      reminderDays: expiryAllowed && expiresAt ? reminderDays : [],
    }));

  const toRetry = queue.failed + queue.cancelled;
  const total = queue.items.length;

  let action: ReactNode;
  if (queue.running) {
    action = (
      <Button variant="contained" disabled>
        {t('files.summary.progress', { done: queue.done, total })}
      </Button>
    );
  } else if (queue.started && toRetry > 0) {
    action = (
      <Button variant="contained" onClick={() => queue.retry()}>
        {t('files.retryFailed', { count: toRetry })}
      </Button>
    );
  } else {
    action = (
      <Button variant="contained" disabled={total === 0 || ownerId === ''} onClick={start}>
        {t('attachments.upload')}
      </Button>
    );
  }

  return (
    <Dialog
      open={open}
      onClose={queue.running ? undefined : close}
      fullWidth
      maxWidth="sm"
      fullScreen={phone}
    >
      <DialogTitle>{t('attachments.uploadTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ mt: 1 }}>
          <FirstUseHint id="upload" title={t('files.hint.upload.title')}>
            <HintLine>{t('files.hint.upload.drop')}</HintLine>
            <HintLine>{t('files.hint.upload.many')}</HintLine>
            <HintLine>{t('files.hint.upload.details')}</HintLine>
            <HintLine>{t('files.hint.upload.phone')}</HintLine>
          </FirstUseHint>

          {ownerSlot}

          <FileDropzone
            existing={queue.items.map((i) => i.file)}
            onFiles={(files) => {
              setRejected([]);
              queue.add(files);
            }}
            onReject={setRejected}
          />

          {rejected.length > 0 && (
            <Alert severity="warning" onClose={() => setRejected([])}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {t('files.rejected.title', { count: rejected.length })}
              </Typography>
              {rejected.slice(0, 5).map((r) => (
                <Typography key={`${r.file.name}-${r.reason}`} variant="body2">
                  {rejectionText(t, r)}
                </Typography>
              ))}
              {rejected.length > 5 && (
                <Typography variant="body2">{t('files.rejected.more', { count: rejected.length - 5 })}</Typography>
              )}
            </Alert>
          )}

          <FileQueueList
            items={queue.items}
            locked={queue.running}
            onRemove={queue.remove}
            onRetry={(id) => queue.retry(id)}
          />

          <Box
            aria-live="polite"
            sx={{ position: 'absolute', width: 1, height: 1, overflow: 'hidden', clip: 'rect(0 0 0 0)' }}
          >
            {queue.running
              ? t('files.summary.progress', { done: queue.done, total })
              : total > 0
                ? t('files.added', { count: total })
                : ''}
          </Box>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={t('attachments.field.title')}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              disabled={!single}
              helperText={single ? undefined : t('attachments.field.titleMany')}
              fullWidth
              slotProps={{ htmlInput: { maxLength: MAX_ATTACHMENT_TITLE_LENGTH } }}
            />
            <FormControl fullWidth>
              <InputLabel id="document-category-label">{t('attachments.category')}</InputLabel>
              <Select
                labelId="document-category-label"
                label={t('attachments.category')}
                value={categories.includes(category) ? category : ''}
                onChange={(event) => setCategory(event.target.value as AttachmentCategory)}
              >
                {categories.map((value) => (
                  <MenuItem key={value} value={value}>
                    {enumLabel('attachmentCategory', value)}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>

          <TextField
            label={t('attachments.description')}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            multiline
            minRows={2}
            fullWidth
            slotProps={{ htmlInput: { maxLength: 1000 } }}
          />

          <TagsField value={tags} onChange={setTags} suggestions={tagSuggestions.data ?? []} />

          <Accordion
            disableGutters
            variant="outlined"
            expanded={datesOpen}
            onChange={(_, next) => setDatesOpen(next)}
            sx={{ borderRadius: 2, '&:before': { display: 'none' } }}
          >
            <AccordionSummary expandIcon={<ExpandMore />} aria-controls="document-dates" id="document-dates-header">
              <Typography sx={{ fontWeight: 700 }}>{t('attachments.dates.title')}</Typography>
              <Typography sx={{ ml: 1, fontStyle: 'italic' }} color="text.secondary">
                {t('attachments.dates.optional')}
              </Typography>
            </AccordionSummary>
            <AccordionDetails id="document-dates">
              <Stack spacing={2}>
                <TextField
                  label={t('attachments.expiresAt')}
                  type="date"
                  value={expiryAllowed ? expiresAt : ''}
                  onChange={(event) => setExpiresAt(event.target.value)}
                  disabled={!expiryAllowed}
                  fullWidth
                  slotProps={{ inputLabel: { shrink: true } }}
                  helperText={expiryAllowed ? t('attachments.expiresHint') : t('attachments.photoNoExpiry')}
                />

                {expiryAllowed && expiresAt && (
                  <ReminderDaysField value={reminderDays} onChange={setReminderDays} />
                )}

                <TextField
                  label={t('attachments.retainUntil')}
                  type="date"
                  value={retainUntil}
                  onChange={(event) => setRetainUntil(event.target.value)}
                  fullWidth
                  slotProps={{ inputLabel: { shrink: true } }}
                  helperText={t('attachments.retainUntilHint')}
                />
              </Stack>
            </AccordionDetails>
          </Accordion>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={queue.running ? queue.cancel : close}>
          {queue.running ? t('files.cancelUpload') : t('common.cancel')}
        </Button>
        {action}
      </DialogActions>
    </Dialog>
  );
}
