import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { ACCEPTED_EXTENSIONS, MAX_ATTACHMENT_BYTES } from '../../api/attachments';
import { useUploadAttachment } from '../../features/attachments/useAttachments';
import { useCreateRefund } from '../../features/refunds/useRefunds';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useT } from '../../i18n/useI18n';

const today = () => new Date().toISOString().slice(0, 10);

/** Asks to be paid back: how much, when, why, and the receipt. The reason is required; it is the justification. */
export function NewRefundDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const create = useCreateRefund();
  const upload = useUploadAttachment();
  const projects = useAllProjectsQuery();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('EUR');
  const [date, setDate] = useState(today());
  const [description, setDescription] = useState('');
  const [projectId, setProjectId] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  const tooBig = !!file && file.size > MAX_ATTACHMENT_BYTES;
  const valid = Number(amount) > 0 && description.trim().length > 0 && /^[A-Za-z]{3}$/.test(currency) && !tooBig;

  const submit = async () => {
    setError(null);

    try {
      const refund = await create.mutateAsync({
        amount: Number(amount),
        currency: currency.toUpperCase(),
        expenseDate: date,
        description: description.trim(),
        projectId: projectId || null,
      });

      if (file) {
        await upload.mutateAsync({ ownerType: 'Refund', ownerId: refund.id, category: 'Photo', file });
      }

      setAmount('');
      setDescription('');
      setProjectId('');
      setFile(null);
      onClose();
    } catch (failure) {
      setError(toApiError(failure).message);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('refunds.newTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Stack direction="row" spacing={1}>
            <TextField
              label={t('refunds.amount')}
              type="number"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              sx={{ flex: 2 }}
              slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
            />
            <TextField
              label={t('refunds.currency')}
              value={currency}
              onChange={(event) => setCurrency(event.target.value)}
              sx={{ flex: 1 }}
              slotProps={{ htmlInput: { maxLength: 3 } }}
            />
          </Stack>
          <TextField
            label={t('refunds.expenseDate')}
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label={t('refunds.reason')}
            helperText={t('refunds.reasonHint')}
            value={description}
            multiline
            minRows={2}
            onChange={(event) => setDescription(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 1000 } }}
          />
          <TextField select label={t('refunds.project')} value={projectId} onChange={(event) => setProjectId(event.target.value)}>
            <MenuItem value="">{t('refunds.noProject')}</MenuItem>
            {(projects.data?.items ?? []).map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {project.name}
              </MenuItem>
            ))}
          </TextField>
          <Stack spacing={0.5}>
            <Button variant="outlined" component="label" sx={{ alignSelf: 'flex-start' }}>
              {file ? file.name : t('refunds.attachReceipt')}
              <input
                hidden
                type="file"
                accept={ACCEPTED_EXTENSIONS}
                onChange={(event) => setFile(event.target.files?.[0] ?? null)}
              />
            </Button>
            <Typography variant="caption" color={tooBig ? 'error' : 'text.secondary'}>
              {tooBig ? t('refunds.receiptTooBig') : t('refunds.receiptHint')}
            </Typography>
          </Stack>
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid || create.isPending || upload.isPending} onClick={() => void submit()}>
          {t('refunds.send')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
