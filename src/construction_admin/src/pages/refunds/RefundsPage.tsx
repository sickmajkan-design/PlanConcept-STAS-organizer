import { AddOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Refund, RefundStatus } from '../../api/types';
import { AttachmentList } from '../../components/AttachmentList';
import { PageHeader } from '../../components/PageHeader';
import { ReasonDialog } from '../../components/ReasonDialog';
import { useAuth } from '../../auth/useAuth';
import { canManageArticleOrders } from '../../auth/authHelpers';
import { useRefundsQuery, useReviewRefund } from '../../features/refunds/useRefunds';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';
import { NewRefundDialog } from './NewRefundDialog';

type TabValue = 'open' | 'approved' | 'all';

const statusColor: Record<RefundStatus, 'default' | 'success' | 'warning' | 'error'> = {
  Requested: 'warning',
  Approved: 'success',
  Rejected: 'error',
  Cancelled: 'default',
};

/** The month the payroll pays it with; months around today, since that is what gets picked. */
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

function RefundCard({ refund, highlighted }: { refund: Refund; highlighted: boolean }) {
  const { t, locale } = useI18n();
  const { user } = useAuth();
  const review = useReviewRefund();
  const [showReceipt, setShowReceipt] = useState(false);
  const [approving, setApproving] = useState(false);
  const [declining, setDeclining] = useState(false);

  const own = !!user && refund.requestedByUserId === user.id;
  const manages = canManageArticleOrders(user);
  const waiting = refund.status === 'Requested';

  return (
    <Card variant="outlined" sx={highlighted ? { borderColor: 'primary.main', borderWidth: 2 } : undefined}>
      <CardContent>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 1 }}>
          <Typography sx={{ fontWeight: 700, flex: 1, minWidth: 0 }}>{refund.employeeName}</Typography>
          <Typography sx={{ fontWeight: 700 }}>
            {formatMoney(refund.amount, locale)} {refund.currency}
          </Typography>
          <Chip size="small" color={statusColor[refund.status]} label={t(`refunds.status.${refund.status}`)} />
        </Stack>

        <Typography variant="body2">{refund.description}</Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
          {formatDate(refund.expenseDate)}
          {refund.projectName ? ` · ${refund.projectName}` : ''}
          {refund.status === 'Approved' && refund.payrollMonth
            ? ` · ${t('refunds.paidWith', { month: `${String(refund.payrollMonth).padStart(2, '0')}.${refund.payrollYear}` })}`
            : ''}
        </Typography>
        {refund.status === 'Rejected' && refund.reviewNote && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {refund.reviewNote}
          </Alert>
        )}

        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mt: 1.5 }}>
          <Button size="small" onClick={() => setShowReceipt((shown) => !shown)}>
            {t('refunds.receipt')}
          </Button>
          {waiting && manages && !own && (
            <>
              <Button size="small" variant="contained" onClick={() => setApproving(true)}>
                {t('refunds.action.approve')}
              </Button>
              <Button size="small" color="error" onClick={() => setDeclining(true)}>
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

        <Collapse in={showReceipt} unmountOnExit>
          <Box sx={{ mt: 1 }}>
            <AttachmentList ownerType="Refund" ownerId={refund.id} categories={['Photo', 'Other']} canUpload={own || manages} />
          </Box>
        </Collapse>

        <ApproveDialog refund={approving ? refund : null} onClose={() => setApproving(false)} />
        <ReasonDialog
          open={declining}
          title={t('refunds.declineTitle')}
          hint={t('refunds.declineHint')}
          label={t('refunds.declineReason')}
          submitLabel={t('refunds.action.decline')}
          onClose={() => setDeclining(false)}
          onSubmit={async (note) => {
            await review.mutateAsync({ id: refund.id, input: { status: 'Rejected', note } });
            setDeclining(false);
          }}
        />
      </CardContent>
    </Card>
  );
}

/**
 * Requests to be paid back for something bought for the firm. Each carries the receipt
 * and the reason; an approved one is paid with the payroll of the month it names.
 */
export function RefundsPage() {
  const { t } = useI18n();
  const { targetId } = useHighlightTarget();
  const [tab, setTab] = useState<TabValue>('open');
  const [creating, setCreating] = useState(false);

  const query = useRefundsQuery({
    pageNumber: 1,
    pageSize: 100,
    status: tab === 'open' ? 'Requested' : tab === 'approved' ? 'Approved' : undefined,
  });
  const items = query.data?.items ?? [];

  return (
    <>
      <PageHeader
        title={t('refunds.title')}
        description={t('refunds.description')}
        action={{ label: t('refunds.new'), icon: <AddOutlined />, onClick: () => setCreating(true) }}
      />

      <Tabs value={tab} onChange={(_, value: TabValue) => setTab(value)} sx={{ mb: 2 }}>
        <Tab value="open" label={t('refunds.tab.open')} />
        <Tab value="approved" label={t('refunds.tab.approved')} />
        <Tab value="all" label={t('refunds.tab.all')} />
      </Tabs>

      {query.isError ? (
        <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
      ) : items.length === 0 && !query.isLoading ? (
        <Typography color="text.secondary">{t('refunds.empty')}</Typography>
      ) : (
        <Stack spacing={2}>
          {items.map((refund) => (
            <RefundCard key={refund.id} refund={refund} highlighted={refund.id === targetId} />
          ))}
        </Stack>
      )}

      <NewRefundDialog open={creating} onClose={() => setCreating(false)} />
    </>
  );
}
