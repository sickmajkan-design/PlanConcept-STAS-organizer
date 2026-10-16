import { AddOutlined, VisibilityOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  Switch,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  categoriesWithExpiry,
  documentRequestStatuses,
  type DocumentRequest,
  type DocumentRequestFile,
  type DocumentRequestStatus,
} from '../../api/documentRequests';
import type { Attachment } from '../../api/types';
import { AttachmentPreviewDialog } from '../../components/AttachmentPreviewDialog';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { FirstUseHint, HintLine } from '../../components/files/FirstUseHint';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useAcceptDocumentRequest,
  useCancelDocumentRequest,
  useDeleteDocumentRequest,
  useDocumentRequestQuery,
  useDocumentRequestsQuery,
  useReturnDocumentRequest,
} from '../../features/documentRequests/useDocumentRequests';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useOpenOnParam } from '../../hooks/useOpenOnParam';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatDateTime } from '../../utils/formatting';
import { RequestDocumentDialog } from './RequestDocumentDialog';

type Tone = 'default' | 'warning' | 'success' | 'error' | 'info';

const STATUS_TONE: Record<DocumentRequestStatus, Tone> = {
  Open: 'info',
  Submitted: 'warning',
  Accepted: 'success',
  Returned: 'error',
  Cancelled: 'default',
};

const QUICK_REASONS = [
  'docRequests.reason.unreadable',
  'docRequests.reason.incomplete',
  'docRequests.reason.wrong',
  'docRequests.reason.expired',
] as const;

function isOverdue(request: DocumentRequest): boolean {
  if (!request.dueDate || (request.status !== 'Open' && request.status !== 'Returned')) {
    return false;
  }

  return new Date(`${request.dueDate}T23:59:59`).getTime() < Date.now();
}

function StatusBadge({ request }: { request: DocumentRequest }) {
  const { t } = useI18n();
  const overdue = isOverdue(request);

  return (
    <Chip
      size="small"
      color={overdue ? 'error' : STATUS_TONE[request.status]}
      variant={request.status === 'Cancelled' ? 'outlined' : 'filled'}
      label={overdue ? t('docRequests.status.Overdue') : t(`docRequests.status.${request.status}`)}
    />
  );
}

/** A file of an answer, shaped like an attachment so the same preview shows it. */
function asAttachment(file: DocumentRequestFile, request: DocumentRequest): Attachment {
  return {
    id: file.id,
    fileName: file.fileName,
    contentType: file.contentType,
    sizeBytes: file.sizeBytes,
    category: request.category,
    description: null,
    title: request.title,
    tags: [],
    expiresAt: null,
    retainUntil: null,
    reminderDays: [],
    sharedWithEmployee: false,
    ownerType: 'Employee',
    ownerId: request.employeeId,
    ownerName: request.employeeName,
    uploadedByName: null,
    createdAt: request.createdAt,
  };
}

/**
 * Looks at one request: what was asked, what came back, the history, and — while it waits to be
 * checked — accepts it, sends it back with a reason, or withdraws it. Whatever is done is confirmed on
 * the spot with the status it ends up in.
 */
function ReviewDialog({ requestId, onClose }: { requestId: string | null; onClose: () => void }) {
  const { t } = useI18n();
  const enumLabel = useEnumLabel();
  const { data: request, isLoading } = useDocumentRequestQuery(requestId);

  const accept = useAcceptDocumentRequest();
  const send = useReturnDocumentRequest();
  const cancel = useCancelDocumentRequest();
  const remove = useDeleteDocumentRequest();
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const [expiresAt, setExpiresAt] = useState('');
  const [returning, setReturning] = useState(false);
  const [reason, setReason] = useState('');
  const [previewing, setPreviewing] = useState<Attachment | null>(null);
  const [failure, setFailure] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  const close = () => {
    setExpiresAt('');
    setReturning(false);
    setReason('');
    setFailure(null);
    setConfirmation(null);
    onClose();
  };

  const run = async (action: () => Promise<DocumentRequest>, confirmationKey: string) => {
    setFailure(null);

    try {
      const outcome = await action();
      setConfirmation(t(confirmationKey as never, { status: t(`docRequests.status.${outcome.status}`) }));
      setReturning(false);
      setReason('');
    } catch (error) {
      setFailure(toApiError(error).message);
    }
  };

  const needsExpiry = !!request && categoriesWithExpiry.includes(request.category);
  const pending = accept.isPending || send.isPending || cancel.isPending;

  return (
    <Dialog open={!!requestId} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{request?.title ?? t('docRequests.review.title')}</DialogTitle>
      <DialogContent>
        {isLoading || !request ? (
          <Typography color="text.secondary">{t('common.loading')}</Typography>
        ) : (
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }} useFlexGap>
              <Typography sx={{ fontWeight: 600 }}>{request.employeeName}</Typography>
              <Chip size="small" variant="outlined" label={enumLabel('attachmentCategory', request.category)} />
              <StatusBadge request={request} />
              {request.dueDate && (
                <Typography variant="body2" color="text.secondary">
                  {t('docRequests.due', { date: formatDate(request.dueDate) })}
                </Typography>
              )}
              {request.attempts > 0 && (
                <Typography variant="body2" color="text.secondary">
                  {t('docRequests.attempt', { count: request.attempts })}
                </Typography>
              )}
            </Stack>

            {request.note && <Typography variant="body2">{request.note}</Typography>}

            {confirmation && <Alert severity="success">{confirmation}</Alert>}
            {failure && <Alert severity="error">{failure}</Alert>}

            {request.files.length > 0 && (
              <Stack spacing={0.5}>
                <Typography variant="subtitle2">{t('docRequests.review.files')}</Typography>
                {request.files.map((file) => (
                  <Stack key={file.id} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <Typography variant="body2" sx={{ flex: 1, overflowWrap: 'anywhere' }}>
                      {file.fileName}
                    </Typography>
                    <Button
                      size="small"
                      startIcon={<VisibilityOutlined />}
                      onClick={() => setPreviewing(asAttachment(file, request))}
                    >
                      {t('docRequests.review.open')}
                    </Button>
                  </Stack>
                ))}
              </Stack>
            )}

            {request.status === 'Submitted' && !returning && (
              <Stack spacing={1}>
                {needsExpiry && (
                  <TextField
                    type="date"
                    size="small"
                    required
                    label={t('docRequests.review.validUntil')}
                    helperText={t('docRequests.review.validUntilHint')}
                    value={expiresAt}
                    onChange={(event) => setExpiresAt(event.target.value)}
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                )}
                <Typography variant="body2" color="text.secondary">
                  {t('docRequests.review.acceptHint')}
                </Typography>
              </Stack>
            )}

            {returning && (
              <Stack spacing={1}>
                <Typography variant="subtitle2">{t('docRequests.review.whyReturn')}</Typography>
                <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                  {QUICK_REASONS.map((key) => (
                    <Chip
                      key={key}
                      size="small"
                      clickable
                      label={t(key)}
                      onClick={() => setReason(t(key))}
                    />
                  ))}
                </Stack>
                <TextField
                  autoFocus
                  multiline
                  minRows={2}
                  size="small"
                  label={t('docRequests.review.reason')}
                  helperText={t('docRequests.review.reasonHint')}
                  value={reason}
                  onChange={(event) => setReason(event.target.value)}
                  slotProps={{ htmlInput: { maxLength: 500 } }}
                />
              </Stack>
            )}

            {request.events.length > 0 && (
              <Stack spacing={0.5}>
                <Typography variant="subtitle2">{t('docRequests.history')}</Typography>
                {request.events.map((event, index) => (
                  <Typography key={index} variant="body2" color="text.secondary">
                    {formatDateTime(event.at)} ·{' '}
                    {t(`docRequests.event.${event.type}`)} ·{' '}
                    {event.byEmployee ? request.employeeName : (event.actorName ?? t('docRequests.office'))}
                    {event.reason ? ` — „${event.reason}"` : ''}
                  </Typography>
                ))}
              </Stack>
            )}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        {request && request.status !== 'Accepted' && request.status !== 'Cancelled' && !returning && (
          <Button color="error" disabled={pending} onClick={() => void run(() => cancel.mutateAsync(request.id), 'docRequests.done.cancel')}>
            {t('docRequests.action.cancel')}
          </Button>
        )}
        {request?.status === 'Cancelled' && (
          <Button color="error" disabled={pending || remove.isPending} onClick={() => setConfirmingDelete(true)}>
            {t('docRequests.action.delete')}
          </Button>
        )}
        <Box sx={{ flex: 1 }} />
        <Button onClick={close}>{t('common.close')}</Button>
        {request?.status === 'Submitted' && !returning && (
          <>
            <Button disabled={pending} onClick={() => setReturning(true)}>
              {t('docRequests.action.return')}
            </Button>
            <Button
              variant="contained"
              disabled={pending || (needsExpiry && !expiresAt)}
              onClick={() =>
                void run(
                  () => accept.mutateAsync({ id: request.id, expiresAt: expiresAt || null }),
                  'docRequests.done.accept',
                )
              }
            >
              {t('docRequests.action.accept')}
            </Button>
          </>
        )}
        {request && returning && (
          <>
            <Button disabled={pending} onClick={() => setReturning(false)}>
              {t('common.back')}
            </Button>
            <Button
              variant="contained"
              color="error"
              disabled={pending || reason.trim().length < 5}
              onClick={() =>
                void run(
                  () => send.mutateAsync({ id: request.id, reason: reason.trim() }),
                  'docRequests.done.return',
                )
              }
            >
              {t('docRequests.action.sendBack')}
            </Button>
          </>
        )}
      </DialogActions>

      <AttachmentPreviewDialog attachment={previewing} onClose={() => setPreviewing(null)} />

      <ConfirmDialog
        open={confirmingDelete}
        title={t('docRequests.delete.title')}
        description={t('docRequests.delete.body')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isPending}
        onConfirm={() => {
          if (!request) return;

          remove.mutate(request.id, {
            onSuccess: () => {
              setConfirmingDelete(false);
              close();
            },
            onError: (error) => {
              setConfirmingDelete(false);
              setFailure(toApiError(error).message);
            },
          });
        }}
        onCancel={() => setConfirmingDelete(false)}
      />
    </Dialog>
  );
}

/**
 * The requests the office has made for documents: who was asked, what for, and where each one stands.
 * What has been sent and is waiting to be checked comes first.
 */
export function DocumentRequestsPage() {
  const { t } = useI18n();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('createdAt', 'desc');
  const { targetId, isHighlighted } = useHighlightTarget();
  const [params] = useSearchParams();

  const [status, setStatus] = useState<DocumentRequestStatus | 'All'>('Submitted');
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [creating, setCreating] = useState(false);
  const [reviewing, setReviewing] = useState<string | null>(targetId ?? null);
  useOpenOnParam('new', () => setCreating(true));

  const employeeParam = params.get('employeeId');

  const query = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      status: status === 'All' ? undefined : status,
      overdue: overdueOnly || undefined,
      employeeId: employeeParam ?? undefined,
    }),
    [list.query, status, overdueOnly, employeeParam],
  );

  const { data, isLoading, isError, error, refetch } = useDocumentRequestsQuery(query);

  const columns: GridColDef<DocumentRequest>[] = useMemo(
    () => [
      { field: 'employeeName', headerName: t('docRequests.col.person'), flex: 1, minWidth: 160, sortable: false },
      { field: 'title', headerName: t('docRequests.col.document'), flex: 1.5, minWidth: 200, sortable: false },
      {
        field: 'category',
        headerName: t('docRequests.col.type'),
        width: 170,
        sortable: false,
        valueGetter: (_value, row) => enumLabel('attachmentCategory', row.category),
      },
      {
        field: 'dueDate',
        headerName: t('docRequests.col.due'),
        width: 120,
        sortable: false,
        valueGetter: (value) => (value ? formatDate(value as string) : '—'),
      },
      {
        field: 'status',
        headerName: t('docRequests.col.status'),
        width: 190,
        sortable: false,
        renderCell: (params) => <StatusBadge request={params.row} />,
      },
      {
        field: 'attempts',
        headerName: t('docRequests.col.attempts'),
        width: 100,
        sortable: false,
        align: 'right',
        headerAlign: 'right',
      },
      {
        field: 'actions',
        headerName: '',
        width: 130,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Button size="small" onClick={() => setReviewing(params.row.id)}>
            {params.row.status === 'Submitted' ? t('docRequests.action.check') : t('docRequests.action.open')}
          </Button>
        ),
      },
    ],
    [t, enumLabel],
  );

  return (
    <Box>
      <PageHeader
        title={t('docRequests.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        description={t('docRequests.description')}
        action={{ label: t('docRequests.new'), icon: <AddOutlined />, onClick: () => setCreating(true) }}
      />

      <Box sx={{ mb: 1.5 }}>
        <FirstUseHint id="requests" title={t('docRequests.hint.title')}>
          <HintLine>{t('docRequests.hint.ask')}</HintLine>
          <HintLine>{t('docRequests.hint.check')}</HintLine>
          <HintLine>{t('docRequests.hint.notice')}</HintLine>
        </FirstUseHint>
      </Box>

      <Tabs
        value={status}
        onChange={(_, value: DocumentRequestStatus | 'All') => {
          setStatus(value);
          list.resetToFirstPage();
        }}
        variant="scrollable"
        scrollButtons="auto"
        sx={{ mb: 1 }}
      >
        {documentRequestStatuses
          .filter((value) => value !== 'Cancelled')
          .map((value) => (
            <Tab key={value} value={value} label={t(`docRequests.status.${value}`)} />
          ))}
        <Tab value="Cancelled" label={t('docRequests.status.Cancelled')} />
        <Tab value="All" label={t('docRequests.status.All')} />
      </Tabs>

      <Stack direction="row" sx={{ mb: 2 }}>
        <FormControlLabel
          control={
            <Switch
              checked={overdueOnly}
              onChange={(event) => {
                setOverdueOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('docRequests.overdueOnly')}
        />
      </Stack>

      <ResourceDataGrid
        data={data}
        columns={columns}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        paginationModel={list.paginationModel}
        onPaginationModelChange={list.setPaginationModel}
        sortModel={list.sortModel}
        onSortModelChange={list.setSortModel}
        highlightedId={targetId && isHighlighted(targetId) ? targetId : null}
      />

      <RequestDocumentDialog
        open={creating}
        onClose={() => setCreating(false)}
        initialEmployeeIds={employeeParam ? [employeeParam] : []}
      />
      <ReviewDialog requestId={reviewing} onClose={() => setReviewing(null)} />
    </Box>
  );
}
