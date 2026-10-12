import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Paper,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import {
  bookApi,
  bookPaymentMethods,
  type BookPaymentMethod,
  type DebtAge,
  type DebtRow,
} from '../../api/book';
import { newIdempotencyKey } from '../../api/idempotency';
import { invoicesApi } from '../../api/invoices';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

type Side = 'owedToUs' | 'weOwe';

const ageColors: Record<DebtAge, string> = {
  NotDue: 'success.main',
  Days1To30: 'warning.light',
  Days31To60: 'warning.main',
  Over60: 'error.main',
};

function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * What clients still owe the firm and what the firm still owes, aged by due date. A client's
 * invoice is settled with part-payments here; a cost the firm owes is marked paid.
 */
export function BookReceivablesPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();
  const [side, setSide] = useState<Side>('owedToUs');
  const [settling, setSettling] = useState<DebtRow | null>(null);

  const debts = useQuery({
    queryKey: ['book', 'debts', side, branchId ?? null] as const,
    queryFn: () =>
      side === 'owedToUs'
        ? bookApi.receivables({ branchId })
        : bookApi.payables({ branchId }),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['book'] });
    void queryClient.invalidateQueries({ queryKey: ['invoices'] });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const data = debts.data;
  const rows = data?.rows ?? [];
  const maxBucket = Math.max(1, ...(data?.buckets ?? []).map((bucket) => bucket.amount));

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('book.receivables.title')}
        description={t('book.receivables.description')}
      />

      <Tabs value={side} onChange={(_, next: Side) => setSide(next)}>
        <Tab value="owedToUs" label={t('book.receivables.tabOwedToUs')} />
        <Tab value="weOwe" label={t('book.receivables.tabWeOwe')} />
      </Tabs>

      {debts.error && <Alert severity="error">{toApiError(debts.error).message}</Alert>}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t('book.receivables.openTotal')}
          </Typography>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {formatMoney(data?.openTotal ?? 0, locale)}
          </Typography>
        </Paper>
        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t('book.receivables.overdueTotal')}
          </Typography>
          <Typography
            variant="h5"
            sx={{ fontWeight: 700 }}
            color={(data?.overdueTotal ?? 0) > 0 ? 'error.main' : undefined}
          >
            {formatMoney(data?.overdueTotal ?? 0, locale)}
          </Typography>
        </Paper>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack spacing={1.5}>
          {(data?.buckets ?? []).map((bucket) => (
            <Box key={bucket.age}>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Typography variant="body2">{t(`book.age.${bucket.age}` as MessageKey)}</Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {formatMoney(bucket.amount, locale)}
                </Typography>
              </Stack>
              <Box sx={{ height: 8, bgcolor: 'action.hover', borderRadius: 1, mt: 0.5 }}>
                <Box
                  sx={{
                    height: '100%',
                    width: `${(bucket.amount / maxBucket) * 100}%`,
                    bgcolor: ageColors[bucket.age],
                    borderRadius: 1,
                  }}
                />
              </Box>
            </Box>
          ))}
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('book.receivables.col.title')}</TableCell>
              <TableCell>{t('book.receivables.col.who')}</TableCell>
              <TableCell>{t('book.receivables.col.project')}</TableCell>
              <TableCell>{t('book.receivables.col.due')}</TableCell>
              <TableCell align="right">{t('book.receivables.col.amount')}</TableCell>
              <TableCell align="right">{t('book.receivables.col.paid')}</TableCell>
              <TableCell align="right">{t('book.receivables.col.open')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={`${row.source}-${row.id}`} hover>
                <TableCell>
                  {row.title || '—'}
                  {row.source === 'Invoice' && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {t('book.receivables.invoice')} · {formatDate(row.issuedOn)}
                    </Typography>
                  )}
                </TableCell>
                <TableCell>{row.counterparty ?? '—'}</TableCell>
                <TableCell>{row.projectName ?? '—'}</TableCell>
                <TableCell sx={{ whiteSpace: 'nowrap' }}>
                  {row.dueDate ? formatDate(row.dueDate) : '—'}
                  <Typography
                    variant="caption"
                    sx={{ display: 'block', color: row.daysOverdue > 0 ? 'error.main' : 'text.secondary' }}
                  >
                    {row.daysOverdue > 0
                      ? t('book.receivables.late', { days: row.daysOverdue })
                      : t('book.receivables.notDueYet')}
                  </Typography>
                </TableCell>
                <TableCell align="right">{formatMoney(row.amount, locale)}</TableCell>
                <TableCell align="right">
                  {row.paidAmount > 0 ? formatMoney(row.paidAmount, locale) : '—'}
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 600 }}>
                  {formatMoney(row.openAmount, locale)}
                </TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  <Button size="small" onClick={() => setSettling(row)}>
                    {row.source === 'Invoice'
                      ? t('book.receivables.recordPayment')
                      : t('book.receivables.markPaid')}
                  </Button>
                </TableCell>
              </TableRow>
            ))}
            {!debts.isLoading && rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={8}>
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {t('book.receivables.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      {settling && (
        <SettleDialog
          row={settling}
          onClose={() => setSettling(null)}
          onSaved={() => {
            setSettling(null);
            refresh();
          }}
        />
      )}
    </Stack>
  );
}

function SettleDialog({
  row,
  onClose,
  onSaved,
}: {
  row: DebtRow;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t, locale } = useI18n();
  const isInvoice = row.source === 'Invoice';

  // A second click or a retried request must not record the payment twice.
  const [idempotencyKey] = useState(() => newIdempotencyKey());
  const [amount, setAmount] = useState(String(row.openAmount));
  const [paidOn, setPaidOn] = useState(today());
  const [method, setMethod] = useState<BookPaymentMethod | ''>('');
  const [note, setNote] = useState('');

  const parsed = Number(amount.replace(',', '.'));
  const valid = isInvoice
    ? Number.isFinite(parsed) && parsed > 0 && parsed <= row.openAmount && paidOn !== ''
    : paidOn !== '';

  const save = useMutation({
    mutationFn: async (): Promise<void> => {
      if (isInvoice) {
        await invoicesApi.addPayment(
          row.id,
          { amount: parsed, paidOn, method: method || null, note: note.trim() || null },
          idempotencyKey,
        );
        return;
      }

      await bookApi.entries.markPaid(row.id, { paidOn, method: method || null });
    },
    onSuccess: onSaved,
  });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>
        {isInvoice
          ? t('book.receivables.paymentTitle', { title: row.title })
          : t('book.receivables.markPaidTitle', { title: row.title })}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {isInvoice && (
            <>
              <Typography variant="body2" color="text.secondary">
                {t('book.receivables.paymentOpen', { amount: formatMoney(row.openAmount, locale) })}
              </Typography>
              <TextField
                label={t('book.receivables.paymentAmount')}
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                slotProps={{ htmlInput: { inputMode: 'decimal' } }}
                error={amount !== '' && !valid}
                autoFocus
              />
            </>
          )}
          <TextField
            type="date"
            label={isInvoice ? t('book.receivables.paymentDate') : t('book.receivables.paidOn')}
            value={paidOn}
            onChange={(event) => setPaidOn(event.target.value)}
            slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today() } }}
          />
          <TextField
            select
            label={t('book.receivables.paymentMethod')}
            value={method}
            onChange={(event) => setMethod(event.target.value as BookPaymentMethod | '')}
          >
            <MenuItem value="">
              <em>{t('book.entries.noMethod')}</em>
            </MenuItem>
            {bookPaymentMethods.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.method.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          {isInvoice && (
            <TextField
              label={t('book.receivables.paymentNote')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 500 } }}
            />
          )}
          {save.error && <Alert severity="error">{toApiError(save.error).message}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid} loading={save.isPending} onClick={() => save.mutate()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
