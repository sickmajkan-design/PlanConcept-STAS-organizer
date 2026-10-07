import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { certificatesApi, type Certificate } from '../../api/certificates';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

const SOON_DAYS = 30;

function standing(validUntil: string | null): 'none' | 'expired' | 'soon' | 'ok' {
  if (!validUntil) return 'none';
  const today = new Date().toISOString().slice(0, 10);
  if (validUntil < today) return 'expired';
  const soon = new Date(Date.now() + SOON_DAYS * 864e5).toISOString().slice(0, 10);
  return validUntil <= soon ? 'soon' : 'ok';
}

/**
 * What a worker is certified for and until when. The schedule reads these: a site can require a
 * certificate, and posting somebody who does not hold a valid one is flagged there.
 */
export function EmployeeCertificatesCard({ employeeId, names }: { employeeId: string; names?: string[] }) {
  const t = useT();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<Certificate | 'new' | null>(null);
  const [removing, setRemoving] = useState<Certificate | null>(null);
  const [failure, setFailure] = useState<string | null>(null);

  const list = useQuery({ queryKey: ['certificates', employeeId], queryFn: () => certificatesApi.list(employeeId) });

  // The plan and the attention list both read these, so they follow.
  const changed = () => {
    for (const key of ['certificates', 'planning', 'attention']) {
      void queryClient.invalidateQueries({ queryKey: [key] });
    }
  };

  const remove = useMutation({
    mutationFn: (id: string) => certificatesApi.remove(employeeId, id),
    onSuccess: () => {
      setRemoving(null);
      changed();
    },
    onError: (error) => setFailure(toApiError(error).message),
  });

  const items = list.data ?? [];

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 0.5 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('certificates.title')}
          </Typography>
          <Button size="small" startIcon={<AddOutlined />} onClick={() => { setFailure(null); setEditing('new'); }}>
            {t('certificates.add')}
          </Button>
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('certificates.help')}
        </Typography>

        {failure && <Alert severity="error" sx={{ mb: 1 }}>{failure}</Alert>}

        {items.length === 0 && !list.isLoading ? (
          <Typography variant="body2" color="text.secondary">
            {t('certificates.empty')}
          </Typography>
        ) : (
          <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>
            {items.map((c) => {
              const state = standing(c.validUntil);

              return (
                <Stack key={c.id} direction="row" spacing={1} sx={{ py: 0.75, alignItems: 'center' }}>
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {c.name}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {c.validUntil ? `${t('certificates.validUntil')} ${formatDate(c.validUntil)}` : t('certificates.noExpiry')}
                      {c.note ? ` · ${c.note}` : ''}
                    </Typography>
                  </Box>
                  {state === 'expired' && <Chip size="small" color="error" label={t('certificates.expired')} />}
                  {state === 'soon' && <Chip size="small" color="warning" label={t('certificates.expiresSoon')} />}
                  <Tooltip title={t('common.edit')}>
                    <IconButton size="small" aria-label={t('common.edit')} onClick={() => { setFailure(null); setEditing(c); }}>
                      <EditOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={t('common.delete')}>
                    <IconButton size="small" aria-label={t('common.delete')} onClick={() => setRemoving(c)}>
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Stack>
              );
            })}
          </Stack>
        )}
      </CardContent>

      {editing && (
        <CertificateDialog
          key={editing === 'new' ? 'new' : editing.id}
          employeeId={employeeId}
          certificate={editing === 'new' ? null : editing}
          names={names ?? []}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            changed();
          }}
        />
      )}

      <Dialog open={!!removing} onClose={() => setRemoving(null)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('certificates.deleteConfirm', { name: removing?.name ?? '' })}</DialogTitle>
        <DialogActions>
          <Button onClick={() => setRemoving(null)}>{t('common.cancel')}</Button>
          <Button color="error" variant="contained" onClick={() => removing && remove.mutate(removing.id)} disabled={remove.isPending}>
            {t('common.delete')}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
}

function CertificateDialog({
  employeeId,
  certificate,
  names,
  onClose,
  onSaved,
}: {
  employeeId: string;
  certificate: Certificate | null;
  names: string[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const t = useT();
  const [name, setName] = useState(certificate?.name ?? '');
  const [validUntil, setValidUntil] = useState(certificate?.validUntil ?? '');
  const [note, setNote] = useState(certificate?.note ?? '');
  const [failure, setFailure] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () =>
      certificatesApi.save(employeeId, {
        id: certificate?.id,
        name: name.trim(),
        validUntil: validUntil || null,
        note: note.trim() || null,
      }),
    onSuccess: onSaved,
    onError: (error) => setFailure(toApiError(error).message),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('certificates.editTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Autocomplete
            freeSolo
            size="small"
            options={names}
            value={name}
            onInputChange={(_, value) => setName(value)}
            renderInput={(params) => <TextField {...params} label={t('certificates.name')} autoFocus required />}
          />
          <TextField
            type="date"
            size="small"
            label={t('certificates.validUntil')}
            value={validUntil}
            onChange={(e) => setValidUntil(e.target.value)}
            helperText={t('certificates.noExpiry')}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField size="small" label={t('certificates.note')} value={note} onChange={(e) => setNote(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
          {failure && <Alert severity="error">{failure}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={name.trim() === '' || save.isPending} onClick={() => save.mutate()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
