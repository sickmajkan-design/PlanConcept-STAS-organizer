import { AddOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  MenuItem,
  Stack,
  Switch,
  TextField,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { useCreateArticleOrder } from '../../features/articleOrders/useArticleOrders';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useT } from '../../i18n/useI18n';

interface Line {
  name: string;
  quantity: string;
  unit: string;
  note: string;
}

const emptyLine = (): Line => ({ name: '', quantity: '1', unit: '', note: '' });

/** Asks for articles: what is needed, how many, for which site, and whether it is urgent. */
export function NewArticleOrderDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const create = useCreateArticleOrder();
  const projects = useAllProjectsQuery();
  const [lines, setLines] = useState<Line[]>([emptyLine()]);
  const [projectId, setProjectId] = useState('');
  const [urgent, setUrgent] = useState(false);
  const [note, setNote] = useState('');
  const [error, setError] = useState<string | null>(null);

  const update = (index: number, patch: Partial<Line>) =>
    setLines((current) => current.map((line, i) => (i === index ? { ...line, ...patch } : line)));

  const valid = lines.length > 0 && lines.every((l) => l.name.trim() && Number(l.quantity) > 0);

  const reset = () => {
    setLines([emptyLine()]);
    setProjectId('');
    setUrgent(false);
    setNote('');
    setError(null);
  };

  const submit = async () => {
    setError(null);

    try {
      await create.mutateAsync({
        projectId: projectId || null,
        urgent,
        note: note.trim() || null,
        items: lines.map((l) => ({
          name: l.name.trim(),
          quantity: Number(l.quantity),
          unit: l.unit.trim() || null,
          note: l.note.trim() || null,
        })),
      });
      reset();
      onClose();
    } catch (failure) {
      setError(toApiError(failure).message);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('articleOrders.newTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {lines.map((line, index) => (
            <Stack key={index} direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'flex-start' }}>
              <TextField
                label={t('articleOrders.itemName')}
                value={line.name}
                onChange={(event) => update(index, { name: event.target.value })}
                sx={{ flex: '2 1 200px' }}
                slotProps={{ htmlInput: { maxLength: 200 } }}
              />
              <TextField
                label={t('articleOrders.quantity')}
                type="number"
                value={line.quantity}
                onChange={(event) => update(index, { quantity: event.target.value })}
                sx={{ flex: '0 1 90px' }}
                slotProps={{ htmlInput: { min: 0, step: 'any' } }}
              />
              <TextField
                label={t('articleOrders.unit')}
                value={line.unit}
                onChange={(event) => update(index, { unit: event.target.value })}
                sx={{ flex: '0 1 90px' }}
                slotProps={{ htmlInput: { maxLength: 30 } }}
              />
              <TextField
                label={t('articleOrders.itemNote')}
                value={line.note}
                onChange={(event) => update(index, { note: event.target.value })}
                sx={{ flex: '1 1 100%' }}
                slotProps={{ htmlInput: { maxLength: 300 } }}
              />
              {lines.length > 1 && (
                <IconButton
                  aria-label={t('articleOrders.removeItem')}
                  onClick={() => setLines((current) => current.filter((_, i) => i !== index))}
                >
                  <DeleteOutlined />
                </IconButton>
              )}
            </Stack>
          ))}
          <Button startIcon={<AddOutlined />} onClick={() => setLines((current) => [...current, emptyLine()])} sx={{ alignSelf: 'flex-start' }}>
            {t('articleOrders.addItem')}
          </Button>

          <TextField
            select
            label={t('articleOrders.project')}
            helperText={t('articleOrders.projectHint')}
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
          >
            <MenuItem value="">{t('articleOrders.noProject')}</MenuItem>
            {(projects.data?.items ?? []).map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {project.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label={t('articleOrders.note')}
            value={note}
            multiline
            minRows={2}
            onChange={(event) => setNote(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 1000 } }}
          />
          <FormControlLabel
            control={<Switch checked={urgent} onChange={(event) => setUrgent(event.target.checked)} />}
            label={t('articleOrders.urgent')}
          />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid || create.isPending} onClick={() => void submit()}>
          {t('articleOrders.send')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
