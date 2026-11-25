import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { ArrowBackOutlined } from '@mui/icons-material';
import { useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';

import { absenceTypes, type AbsenceType } from '../../api/types';
import { StatusChip } from '../../components/StatusChip';
import { useAbsenceBalanceQuery, useAbsencesQuery, useBookAbsence, useDeleteAbsence } from '../../features/absences/useAbsences';
import { useMyDocumentRequests, useSubmitDocumentRequest } from '../../features/documentRequests/useDocumentRequests';
import { useTimeEntriesQuery } from '../../features/timeEntries/useTimeEntries';
import { useAuth } from '../../auth/useAuth';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { todayCompany } from '../../utils/companyTime';
import { dateOnlyOffset, formatDate, formatTimeOfDay, splitMinutes } from '../../utils/formatting';

function Header({ title }: { title: string }) {
  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
      <IconButton component={Link} to={paths.myWork} aria-label={title} edge="start">
        <ArrowBackOutlined />
      </IconButton>
      <Typography variant="h5" sx={{ fontWeight: 600 }}>
        {title}
      </Typography>
    </Stack>
  );
}

/** The signed-in person's own shifts, the last two weeks. The API serves a worker only their own rows. */
export function MyTimePage() {
  const t = useT();
  const from = `${dateOnlyOffset(-14)}T00:00:00.000Z`;
  const query = useTimeEntriesQuery({ from, pageSize: 100, pageNumber: 1 });
  const items = query.data?.items ?? [];

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Header title={t('myWork.tile.time')} />
      {query.isLoading && <CircularProgress size={24} />}
      {query.isError && <Alert severity="error">{t('myWork.loadError')}</Alert>}
      {!query.isLoading && !query.isError && items.length === 0 && <Typography color="text.secondary">{t('myWork.time.empty')}</Typography>}
      {items.map((e) => (
        <Card key={e.id} variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1 }}>
              <Typography sx={{ fontWeight: 600 }}>{formatDate(e.startedAt)}</Typography>
              <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                {e.workedMinutes === null ? t('myWork.time.running') : t('timeEntries.hoursShort', splitMinutes(e.workedMinutes))}
              </Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {formatTimeOfDay(e.startedAt)} – {e.endedAt ? formatTimeOfDay(e.endedAt) : '…'}
              {e.projectName ? ` · ${e.projectName}` : ''}
            </Typography>
            <Stack direction="row" sx={{ gap: 0.5, mt: 0.75, flexWrap: 'wrap' }}>
              <StatusChip status={e.status} kind="timeEntryStatus" size="small" />
              {e.viaWeb && <Chip size="small" variant="outlined" label={t('timeEntries.viaWeb')} />}
              {e.reviewNote && <Chip size="small" color="warning" variant="outlined" label={e.reviewNote} />}
            </Stack>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}

function RequestDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const book = useBookAbsence();
  const today = todayCompany();
  const [type, setType] = useState<AbsenceType>('AnnualLeave');
  const [startDate, setStart] = useState(today);
  const [endDate, setEnd] = useState(today);
  const [reason, setReason] = useState('');
  const [problem, setProblem] = useState<string | null>(null);
  const invalid = !startDate || !endDate || endDate < startDate;

  const send = async () => {
    setProblem(null);

    try {
      await book.mutateAsync({ type, startDate, endDate, reason: reason.trim() || null });
      setReason('');
      onClose();
    } catch (error) {
      setProblem((error as Error).message);
    }
  };

  return (
    <Dialog open={open} onClose={() => !book.isPending && onClose()} fullWidth maxWidth="xs">
      <DialogTitle>{t('myWork.absences.request')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {problem && <Alert severity="error">{problem}</Alert>}
          <TextField select label={t('myWork.absences.type')} value={type} onChange={(e) => setType(e.target.value as AbsenceType)}>
            {absenceTypes.map((a) => (
              <MenuItem key={a} value={a}>
                {enumLabel('absenceType', a)}
              </MenuItem>
            ))}
          </TextField>
          <TextField type="date" label={t('myWork.absences.from')} value={startDate} onChange={(e) => setStart(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField type="date" label={t('myWork.absences.to')} value={endDate} onChange={(e) => setEnd(e.target.value)} error={!!endDate && endDate < startDate} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField label={t('myWork.absences.reason')} value={reason} onChange={(e) => setReason(e.target.value)} multiline minRows={2} slotProps={{ htmlInput: { maxLength: 1000 } }} />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={book.isPending}>
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={send} disabled={invalid || book.isPending}>
          {t('myWork.absences.send')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** The signed-in person's own leave: what is left this year, what was asked for, and a new request. */
export function MyAbsencesPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [problem, setProblem] = useState<string | null>(null);
  const year = useMemo(() => new Date().getFullYear(), []);
  const balance = useAbsenceBalanceQuery(user?.employeeId ?? undefined, year);
  const list = useAbsencesQuery({ pageSize: 50, pageNumber: 1 });
  const remove = useDeleteAbsence();
  const items = list.data?.items ?? [];

  const withdraw = async (id: string) => {
    setProblem(null);

    try {
      await remove.mutateAsync(id);
    } catch (error) {
      setProblem((error as Error).message);
    }
  };

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Header title={t('myWork.tile.absences')} />

      {balance.data && (
        <Card variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent>
            <Typography variant="overline" color="text.secondary">
              {t('myWork.absences.balance', { year })}
            </Typography>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              {t('myWork.absences.daysLeft', { days: balance.data.remainingDays })}
            </Typography>
            <Typography color="text.secondary">{t('myWork.absences.used', { used: balance.data.usedDays, total: balance.data.allowanceDays })}</Typography>
          </CardContent>
        </Card>
      )}

      <Button variant="contained" size="large" onClick={() => setOpen(true)} sx={{ py: 1.5 }}>
        {t('myWork.absences.request')}
      </Button>

      {problem && (
        <Alert severity="error" onClose={() => setProblem(null)}>
          {problem}
        </Alert>
      )}
      {list.isLoading && <CircularProgress size={24} />}
      {list.isError && <Alert severity="error">{t('myWork.loadError')}</Alert>}
      {!list.isLoading && !list.isError && items.length === 0 && <Typography color="text.secondary">{t('myWork.absences.empty')}</Typography>}

      {items.map((a) => (
        <Card key={a.id} variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1 }}>
              <Typography sx={{ fontWeight: 600 }}>{enumLabel('absenceType', a.type)}</Typography>
              <StatusChip status={a.status} kind="absenceStatus" size="small" />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {formatDate(a.startDate)} – {formatDate(a.endDate)} · {t('myWork.absences.days', { count: a.dayCount })}
            </Typography>
            {a.reviewNote && (
              <Typography variant="body2" sx={{ mt: 0.5 }}>
                {a.reviewNote}
              </Typography>
            )}
            {a.status === 'Requested' && (
              <Box sx={{ mt: 1 }}>
                <Button size="small" color="inherit" onClick={() => withdraw(a.id)} disabled={remove.isPending}>
                  {t('myWork.absences.withdraw')}
                </Button>
              </Box>
            )}
          </CardContent>
        </Card>
      ))}

      <RequestDialog open={open} onClose={() => setOpen(false)} />
    </Stack>
  );
}

const MAX_FILES = 5;

/** What the office has asked this person for. A photo from the phone camera or a PDF answers a request. */
export function MyDocumentsPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useMyDocumentRequests();
  const submit = useSubmitDocumentRequest();
  const input = useRef<HTMLInputElement>(null);
  const [target, setTarget] = useState<string | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const items = list.data ?? [];

  const picked = async (files: FileList | null) => {
    const id = target;
    setTarget(null);
    if (!id || !files || files.length === 0) return;

    setProblem(null);
    setSent(false);

    try {
      await submit.mutateAsync({ id, files: Array.from(files).slice(0, MAX_FILES) });
      setSent(true);
    } catch (error) {
      setProblem((error as Error).message);
    }
  };

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Header title={t('myWork.tile.documents')} />
      <input
        ref={input}
        type="file"
        multiple
        hidden
        accept="image/*,application/pdf"
        onChange={(e) => {
          void picked(e.target.files);
          e.target.value = '';
        }}
      />
      {sent && (
        <Alert severity="success" onClose={() => setSent(false)}>
          {t('myWork.documents.sent')}
        </Alert>
      )}
      {problem && (
        <Alert severity="error" onClose={() => setProblem(null)}>
          {problem}
        </Alert>
      )}
      {list.isLoading && <CircularProgress size={24} />}
      {list.isError && <Alert severity="error">{t('myWork.loadError')}</Alert>}
      {!list.isLoading && !list.isError && items.length === 0 && <Typography color="text.secondary">{t('myWork.documents.empty')}</Typography>}

      {items.map((d) => {
        const canAnswer = d.status === 'Open' || d.status === 'Returned';

        return (
          <Card key={d.id} variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1 }}>
                <Typography sx={{ fontWeight: 600 }}>{d.title}</Typography>
                <Chip
                  size="small"
                  variant="outlined"
                  color={d.status === 'Accepted' ? 'success' : canAnswer ? 'warning' : 'default'}
                  label={t(`docRequests.status.${d.status}`)}
                />
              </Stack>
              <Typography variant="body2" color="text.secondary">
                {enumLabel('attachmentCategory', d.category)}
                {d.dueDate ? ` · ${t('docRequests.due', { date: formatDate(d.dueDate) })}` : ''}
              </Typography>
              {d.note && (
                <Typography variant="body2" sx={{ mt: 0.5 }}>
                  {d.note}
                </Typography>
              )}
              {d.status === 'Returned' && d.returnReason && (
                <Alert severity="warning" sx={{ mt: 1 }}>
                  {d.returnReason}
                </Alert>
              )}
              {canAnswer && (
                <Box sx={{ mt: 1 }}>
                  <Button
                    variant="contained"
                    size="small"
                    disabled={submit.isPending}
                    onClick={() => {
                      setTarget(d.id);
                      input.current?.click();
                    }}
                  >
                    {t('myWork.documents.send')}
                  </Button>
                </Box>
              )}
            </CardContent>
          </Card>
        );
      })}
    </Stack>
  );
}
