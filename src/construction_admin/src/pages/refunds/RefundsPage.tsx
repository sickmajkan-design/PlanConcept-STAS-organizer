import { AddOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Refund } from '../../api/types';
import { AttachmentList } from '../../components/AttachmentList';
import { PageHeader } from '../../components/PageHeader';
import { ReasonDialog } from '../../components/ReasonDialog';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { useAuth } from '../../auth/useAuth';
import { canManageArticleOrders } from '../../auth/authHelpers';
import { useRefundsQuery, useReviewRefund } from '../../features/refunds/useRefunds';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useOpenOnParam } from '../../hooks/useOpenOnParam';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';
import { NewRefundDialog } from './NewRefundDialog';

/** The months around today, since that is what gets picked as the payroll to pay a refund with. */
function monthChoices(): { year: number; month: number }[] {
  const now = new Date();
  const choices: { year: number; month: number }[] = [];

  for (let offset = -1; offset <= 3; offset++) {
    const date = new Date(now.getFullYear(), now.getMonth() + offset, 1);
    choices.push({ year: date.getFullYear(), month: date.getMonth() + 1 });
  }

  return choices;
}

function ApproveDialog({ refund, onClose }: { refund: Refund | null; onClose: () => void }) {
  const { t } = useI18n();
  const review = useReviewRefund();
  const now = new Date();
  const [value, setValue] = useState(`${now.getFullYear()}-${now.getMonth() + 1}`);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    if (!refund) return;
    const [year, month] = value.split('-').map(Number);

    try {
      await review.mutateAsync({ id: refund.id, input: { status: 'Approved', payrollYear: year, payrollMonth: month } });
      onClose();
    } catch (failure) {
      setError(toApiError(failure).message);
    }
  };

  return (
    <Dialog open={!!refund} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('refunds.approveTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t('refunds.approveHint')}
          </Typography>
          <TextField select label={t('refunds.payrollMonth')} value={value} onChange={(event) => setValue(event.target.value)}>
            {monthChoices().map((choice) => (
              <MenuItem key={`${choice.year}-${choice.month}`} value={`${choice.year}-${choice.month}`}>
                {String(choice.month).padStart(2, '0')}.{choice.year}
              </MenuItem>
            ))}
          </TextField>
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={review.isPending} onClick={() => void submit()}>
          {t('refunds.action.approve')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * Requests to be paid back for something bought for the firm. Each carries the receipt
 * and the reason; an approved one is paid with the payroll of the month it names.
 * Laid out like every other list in the panel.
 */
export function RefundsPage() {
  const { t, locale } = useI18n();
  const { user } = useAuth();
  const list = useListQueryState('createdAt', 'desc');
  const { targetId, isHighlighted } = useHighlightTarget();
  const review = useReviewRefund();

  const [waitingOnly, setWaitingOnly] = useState(true);
  const [creating, setCreating] = useState(false);
  const [approving, setApproving] = useState<Refund | null>(null);
  const [declining, setDeclining] = useState<Refund | null>(null);
  const [receipt, setReceipt] = useState<Refund | null>(null);
  useOpenOnParam('new', () => setCreating(true));

  const manages = canManageArticleOrders(user);

  const query = useMemo(
    () => ({
      ...list.query,
      // The API has no text search on this collection.
      search: undefined,
      status: waitingOnly ? ('Requested' as const) : undefined,
    }),
    [list.query, waitingOnly],
  );

  const { data, isLoading, isError, error, refetch } = useRefundsQuery(query);

  const columns: GridColDef<Refund>[] = useMemo(
    () => [
      { field: 'employeeName', headerName: t('refunds.employee'), flex: 1, minWidth: 160, sortable: false },
      { field: 'description', headerName: t('refunds.reason'), flex: 2, minWidth: 220, sortable: false },
      {
        field: 'amount',
        headerName: t('refunds.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (_value, row) => `${formatMoney(row.amount, locale)} ${row.currency}`,
      },
      { field: 'expenseDate', headerName: t('refunds.expenseDate'), width: 130, valueGetter: (value) => formatDate(value) },
      {
        field: 'status',
        headerName: t('refunds.statusColumn'),
        width: 150,
        renderCell: (params) => (
          <StatusChip status={params.row.status} kind="refundStatus" />
        ),
      },
      {
        field: 'payroll',
        headerName: t('refunds.payrollColumn'),
        width: 120,
        sortable: false,
        valueGetter: (_value, row) =>
          row.status === 'Approved' && row.payrollMonth ? `${String(row.payrollMonth).padStart(2, '0')}.${row.payrollYear}` : '—',
      },
      {
        field: 'actions',
        headerName: '',
        width: 330,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => {
          const refund = params.row;
          const own = !!user && refund.requestedByUserId === user.id;
          const waiting = refund.status === 'Requested';

          return (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
              <Button size="small" onClick={() => setReceipt(refund)}>
                {t('refunds.receipt')}
              </Button>
              {waiting && manages && !own && (
                <>
                  <Button size="small" variant="contained" onClick={() => setApproving(refund)}>
                    {t('refunds.action.approve')}
                  </Button>
                  <Button size="small" color="error" onClick={() => setDeclining(refund)}>
                    {t('refunds.action.decline')}
                  </Button>
                </>
              )}
              {waiting && own && (
                <Button
                  size="small"
                  disabled={review.isPending}
                  onClick={() => review.mutate({ id: refund.id, input: { status: 'Cancelled' } })}
                >
                  {t('refunds.action.withdraw')}
                </Button>
              )}
            </Stack>
          );
        },
      },
    ],
    [t, locale, user, manages, review],
  );

  return (
    <Box>
      <PageHeader
        title={t('refunds.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        description={t('refunds.description')}
        action={{ label: t('refunds.new'), icon: <AddOutlined />, onClick: () => setCreating(true) }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2, alignItems: { sm: 'center' } }}>
        <FormControlLabel
          control={
            <Switch
              checked={waitingOnly}
              onChange={(event) => {
                setWaitingOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('refunds.waitingOnly')}
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

      <NewRefundDialog open={creating} onClose={() => setCreating(false)} />
      <ApproveDialog refund={approving} onClose={() => setApproving(null)} />
      <ReasonDialog
        open={!!declining}
        title={t('refunds.declineTitle')}
        hint={t('refunds.declineHint')}
        label={t('refunds.declineReason')}
        submitLabel={t('refunds.action.decline')}
        onClose={() => setDeclining(null)}
        onSubmit={async (note) => {
          if (!declining) return;
          await review.mutateAsync({ id: declining.id, input: { status: 'Rejected', note } });
          setDeclining(null);
        }}
      />
      <Dialog open={!!receipt} onClose={() => setReceipt(null)} fullWidth maxWidth="sm">
        <DialogTitle>{t('refunds.receipt')}</DialogTitle>
        <DialogContent>
          {receipt && (
            <AttachmentList
              ownerType="Refund"
              ownerId={receipt.id}
              categories={['Photo', 'Other']}
              canUpload={manages || (!!user && receipt.requestedByUserId === user.id)}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReceipt(null)}>{t('common.close')}</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
