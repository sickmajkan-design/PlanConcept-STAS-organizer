import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, IconButton, Link as MuiLink, MenuItem, Stack, Switch, TextField, Typography } from '@mui/material';
import { ArrowBackOutlined } from '@mui/icons-material';
import { useMemo, useRef, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';

import { absenceTypes, workItemPriorities, type AbsenceType, type WorkItem, type WorkItemPriority, type WorkItemStatus } from '../../api/types';
import { AttachmentList } from '../../components/AttachmentList';
import { StatusChip } from '../../components/StatusChip';
import { useAbsenceBalanceQuery, useAbsencesQuery, useBookAbsence, useDeleteAbsence } from '../../features/absences/useAbsences';
import { useMyDocumentRequests, useSubmitDocumentRequest } from '../../features/documentRequests/useDocumentRequests';
import { useRefundsQuery, useReviewRefund } from '../../features/refunds/useRefunds';
import { useMyHousing } from '../../features/accommodations/useAccommodations';
import { useUploadAttachment } from '../../features/attachments/useAttachments';
import { useClockInSites } from '../../features/timeEntries/useShift';
import { useChangeWorkItemStatus, useCreateWorkItem, useWorkItemsQuery } from '../../features/workItems/useWorkItems';
import { useTimeEntriesQuery } from '../../features/timeEntries/useTimeEntries';
import { useAuth } from '../../auth/useAuth';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { NewRefundDialog } from '../refunds/NewRefundDialog';
import { getPosition } from './MyWorkPage';
import { paths } from '../../routes/paths';
import { todayCompany } from '../../utils/companyTime';
import { dateOnlyOffset, formatDate, formatMoney, formatTimeOfDay, splitMinutes } from '../../utils/formatting';

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

/** The signed-in person's own refund requests: ask, see the decision, withdraw one that is still waiting. */
export function MyRefundsPage() {
  const t = useT();
  const { locale } = useI18n();
  const list = useRefundsQuery({ mine: true, pageSize: 50, pageNumber: 1 });
  const review = useReviewRefund();
  const [creating, setCreating] = useState(false);
  const [receipt, setReceipt] = useState<string | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const items = list.data?.items ?? [];

  const withdraw = async (id: string) => {
    setProblem(null);

    try {
      await review.mutateAsync({ id, input: { status: 'Cancelled' } });
    } catch (error) {
      setProblem((error as Error).message);
    }
  };

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Header title={t('myWork.tile.refunds')} />
      <Button variant="contained" size="large" onClick={() => setCreating(true)} sx={{ py: 1.5 }}>
        {t('refunds.new')}
      </Button>
      {problem && (
        <Alert severity="error" onClose={() => setProblem(null)}>
          {problem}
        </Alert>
      )}
      {list.isLoading && <CircularProgress size={24} />}
      {list.isError && <Alert severity="error">{t('myWork.loadError')}</Alert>}
      {!list.isLoading && !list.isError && items.length === 0 && <Typography color="text.secondary">{t('myWork.refunds.empty')}</Typography>}

      {items.map((r) => (
        <Card key={r.id} variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1 }}>
              <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                {formatMoney(r.amount, locale)} {r.currency}
              </Typography>
              <StatusChip status={r.status} kind="refundStatus" size="small" />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {formatDate(r.expenseDate)}
              {r.projectName ? ` · ${r.projectName}` : ''}
              {r.status === 'Approved' && r.payrollMonth ? ` · ${String(r.payrollMonth).padStart(2, '0')}.${r.payrollYear}` : ''}
            </Typography>
            <Typography variant="body2" sx={{ mt: 0.5 }}>
              {r.description}
            </Typography>
            {r.reviewNote && (
              <Alert severity="info" sx={{ mt: 1 }}>
                {r.reviewNote}
              </Alert>
            )}
            <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
              <Button size="small" onClick={() => setReceipt(r.id)}>
                {t('refunds.receipt')}
              </Button>
              {r.status === 'Requested' && (
                <Button size="small" color="inherit" disabled={review.isPending} onClick={() => void withdraw(r.id)}>
                  {t('refunds.action.withdraw')}
                </Button>
              )}
            </Stack>
          </CardContent>
        </Card>
      ))}

      <NewRefundDialog open={creating} onClose={() => setCreating(false)} />
      <Dialog open={!!receipt} onClose={() => setReceipt(null)} fullWidth maxWidth="sm">
        <DialogTitle>{t('refunds.receipt')}</DialogTitle>
        <DialogContent>
          {receipt && <AttachmentList ownerType="Refund" ownerId={receipt} categories={['Photo', 'Other']} canUpload={items.find((r) => r.id === receipt)?.status === 'Requested'} />}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReceipt(null)}>{t('common.close')}</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

function Line({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography>{children}</Typography>
    </Box>
  );
}

/** Where the signed-in person lives now, or will next. Read-only, and the API leaves out every cost figure. */
export function MyHousingPage() {
  const t = useT();
  const query = useMyHousing();
  const home = query.data;

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Header title={t('myWork.tile.housing')} />
      {query.isLoading && <CircularProgress size={24} />}
      {query.isError && <Alert severity="error">{t('myWork.loadError')}</Alert>}
      {!query.isLoading && !query.isError && !home && <Typography color="text.secondary">{t('myWork.housing.none')}</Typography>}

      {home && (
        <Card variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent>
            <Stack spacing={1.5}>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  {home.name}
                </Typography>
                {home.upcoming && <Chip size="small" color="warning" variant="outlined" label={t('myWork.housing.upcoming')} sx={{ mt: 0.5 }} />}
              </Box>
              <Line label={t('myWork.housing.address')}>
                <MuiLink href={`https://maps.apple.com/?q=${encodeURIComponent(`${home.address} ${home.city ?? ''}`.trim())}`} target="_blank" rel="noopener noreferrer">
                  {home.address}
                  {home.city ? `, ${home.city}` : ''}
                </MuiLink>
                {home.floor ? ` · ${home.floor}` : ''}
              </Line>
              <Line label={t('myWork.housing.period')}>
                {formatDate(home.startDate)}
                {home.endDate ? ` – ${formatDate(home.endDate)}` : ''}
              </Line>
              {home.landlordName && (
                <Line label={t('myWork.housing.landlord')}>
                  {home.landlordName}
                  {home.landlordPhone && (
                    <>
                      {' · '}
                      <MuiLink href={`tel:${home.landlordPhone}`}>{home.landlordPhone}</MuiLink>
                    </>
                  )}
                </Line>
              )}
              {home.note && <Line label={t('myWork.housing.note')}>{home.note}</Line>}
              {home.roommateContacts.length > 0 && (
                <Line label={t('myWork.housing.roommates')}>
                  {home.roommateContacts.map((r, i) => (
                    <Box key={`${r.name}-${i}`} component="span" sx={{ display: 'block' }}>
                      {r.name}
                      {r.phone && (
                        <>
                          {' · '}
                          <MuiLink href={`tel:${r.phone}`}>{r.phone}</MuiLink>
                        </>
                      )}
                    </Box>
                  ))}
                </Line>
              )}
            </Stack>
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}

/**
 * One assigned task. A worker moves it from site: start it, or mark it done. Closing it is a check by somebody
 * else, so there is no button for that; the API refuses it for a worker as well.
 */
function TaskCard({ item, onPhotos }: { item: WorkItem; onPhotos: (item: WorkItem) => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const change = useChangeWorkItemStatus(item.id);
  const [problem, setProblem] = useState<string | null>(null);

  const move = async (status: WorkItemStatus) => {
    setProblem(null);

    try {
      await change.mutateAsync(status);
    } catch (error) {
      setProblem((error as Error).message);
    }
  };

  return (
    <Card variant="outlined" sx={{ borderRadius: 3 }}>
      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1 }}>
          <Typography sx={{ fontWeight: 600 }}>{item.title}</Typography>
          <StatusChip status={item.status} kind="workItemStatus" size="small" />
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {enumLabel('workItemKind', item.kind)} · {enumLabel('workItemPriority', item.priority)}
          {item.projectName ? ` · ${item.projectName}` : ''}
          {item.dueDate ? ` · ${t('myWork.tasks.due', { date: formatDate(item.dueDate) })}` : ''}
        </Typography>
        {item.description && (
          <Typography variant="body2" sx={{ mt: 0.5 }}>
            {item.description}
          </Typography>
        )}
        {problem && (
          <Alert severity="error" sx={{ mt: 1 }} onClose={() => setProblem(null)}>
            {problem}
          </Alert>
        )}
        <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', rowGap: 1 }}>
          {item.status === 'Open' && (
            <Button size="small" variant="contained" disabled={change.isPending} onClick={() => void move('InProgress')}>
              {t('myWork.tasks.start')}
            </Button>
          )}
          {(item.status === 'Open' || item.status === 'InProgress') && (
            <Button size="small" variant={item.status === 'InProgress' ? 'contained' : 'outlined'} disabled={change.isPending} onClick={() => void move('Resolved')}>
              {t('myWork.tasks.done')}
            </Button>
          )}
          <Button size="small" onClick={() => onPhotos(item)}>
            {t('myWork.tasks.photos')}
          </Button>
        </Stack>
      </CardContent>
    </Card>
  );
}

/** Reports a defect: what is wrong, where, and a photo. A worker may raise a defect and nothing else. */
function DefectDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const create = useCreateWorkItem();
  const upload = useUploadAttachment();
  const sites = useClockInSites();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<WorkItemPriority>('Normal');
  const [projectId, setProjectId] = useState('');
  const [photo, setPhoto] = useState<File | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const siteList = sites.data ?? [];
  // A defect belongs to a site. One site is chosen for the person; with several they pick.
  const site = projectId || (siteList.length === 1 ? siteList[0].id : '');
  const valid = title.trim().length > 0 && !!site;

  const send = async () => {
    setProblem(null);
    setBusy(true);

    try {
      const position = await getPosition();
      const item = await create.mutateAsync({
        kind: 'Defect',
        title: title.trim(),
        description: description.trim() || null,
        priority,
        projectId: site,
        latitude: position?.latitude ?? null,
        longitude: position?.longitude ?? null,
      });

      if (photo) {
        await upload.mutateAsync({ ownerType: 'WorkItem', ownerId: item.id, category: 'Photo', file: photo });
      }

      setTitle('');
      setDescription('');
      setPhoto(null);
      onClose();
    } catch (error) {
      setProblem((error as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onClose={() => !busy && onClose()} fullWidth maxWidth="xs">
      <DialogTitle>{t('myWork.tasks.reportTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {problem && <Alert severity="error">{problem}</Alert>}
          <TextField label={t('myWork.tasks.what')} value={title} onChange={(e) => setTitle(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
          <TextField label={t('myWork.tasks.details')} value={description} onChange={(e) => setDescription(e.target.value)} multiline minRows={2} slotProps={{ htmlInput: { maxLength: 2000 } }} />
          {siteList.length > 1 && (
            <TextField select label={t('myWork.shift.site')} value={projectId} onChange={(e) => setProjectId(e.target.value)}>
              {siteList.map((s) => (
                <MenuItem key={s.id} value={s.id}>
                  {s.name}
                </MenuItem>
              ))}
            </TextField>
          )}
          <TextField select label={t('workItems.priority')} value={priority} onChange={(e) => setPriority(e.target.value as WorkItemPriority)}>
            {workItemPriorities.map((p) => (
              <MenuItem key={p} value={p}>
                {enumLabel('workItemPriority', p)}
              </MenuItem>
            ))}
          </TextField>
          <Button component="label" variant="outlined">
            {photo ? photo.name : t('myWork.tasks.addPhoto')}
            <input hidden type="file" accept="image/*" onChange={(e) => setPhoto(e.target.files?.[0] ?? null)} />
          </Button>
          {!sites.isLoading && siteList.length === 0 && <Alert severity="warning">{t('myWork.tasks.noSite')}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={() => void send()} disabled={!valid || busy}>
          {t('myWork.tasks.send')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** The signed-in person's own tasks, and a way to report a defect from site. */
export function MyTasksPage() {
  const t = useT();
  const { user } = useAuth();
  const [all, setAll] = useState(false);
  const [reporting, setReporting] = useState(false);
  const [photos, setPhotos] = useState<WorkItem | null>(null);
  // A worker is limited to their own by the API. A foreman sees everything there, so the list is narrowed here.
  const list = useWorkItemsQuery({ assignedEmployeeId: user?.employeeId ?? undefined, openOnly: all ? undefined : true, pageSize: 50, pageNumber: 1 });
  const items = list.data?.items ?? [];

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Header title={t('myWork.tile.tasks')} />
      <Button variant="contained" size="large" onClick={() => setReporting(true)} sx={{ py: 1.5 }}>
        {t('myWork.tasks.report')}
      </Button>
      <FormControlLabel control={<Switch checked={all} onChange={(e) => setAll(e.target.checked)} />} label={t('myWork.tasks.showFinished')} />
      {list.isLoading && <CircularProgress size={24} />}
      {list.isError && <Alert severity="error">{t('myWork.loadError')}</Alert>}
      {!list.isLoading && !list.isError && items.length === 0 && <Typography color="text.secondary">{t('myWork.tasks.empty')}</Typography>}
      {items.map((item) => (
        <TaskCard key={item.id} item={item} onPhotos={setPhotos} />
      ))}

      <DefectDialog open={reporting} onClose={() => setReporting(false)} />
      <Dialog open={!!photos} onClose={() => setPhotos(null)} fullWidth maxWidth="sm">
        <DialogTitle>{photos?.title}</DialogTitle>
        <DialogContent>{photos && <AttachmentList ownerType="WorkItem" ownerId={photos.id} categories={['Photo', 'Other']} canUpload />}</DialogContent>
        <DialogActions>
          <Button onClick={() => setPhotos(null)}>{t('common.close')}</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
