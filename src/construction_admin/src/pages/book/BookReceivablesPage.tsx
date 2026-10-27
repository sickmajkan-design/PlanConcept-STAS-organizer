import { ContentCopyOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { bookApi, bookPaymentMethods, type BookPaymentMethod, type DebtRow } from '../../api/book';
import { newIdempotencyKey } from '../../api/idempotency';
import { invoicesApi } from '../../api/invoices';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';
import { companyNow } from '../../utils/companyTime';
import { ProjectLabel } from '../../components/ProjectLabel';

function today(): string {
  const now = companyNow();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * Who owes the firm and whom the firm owes, on one screen, as the mockup lays it out: the three figures, how
 * late the money is, the invoices clients have not paid (with a reminder to prepare) and what the firm owes —
 * the workers' pay and the suppliers' bills.
 */
export function BookReceivablesPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();
  const [settling, setSettling] = useState<DebtRow | null>(null);
  const [reminder, setReminder] = useState<string | null>(null);

  const owedToUs = useQuery({
    queryKey: ['book', 'debts', 'owedToUs', branchId ?? null] as const,
    queryFn: () => bookApi.receivables({ branchId }),
  });

  const weOwe = useQuery({
    queryKey: ['book', 'debts', 'weOwe', branchId ?? null] as const,
    queryFn: () => bookApi.payables({ branchId }),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['book'] });
    void queryClient.invalidateQueries({ queryKey: ['invoices'] });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const receivable = owedToUs.data;
  const payable = weOwe.data;

  const owed = receivable?.openTotal ?? 0;
  const owe = payable?.openTotal ?? 0;
  const onTime = receivable?.buckets.find((b) => b.age === 'NotDue')?.amount ?? 0;
  const late = (receivable?.openTotal ?? 0) - onTime;
  const total = Math.max(1, owed);

  const mostLate = (receivable?.rows ?? []).find((r) => r.daysOverdue > 0);

  const prepareReminder = (row: DebtRow) => {
    setReminder(
      t('book.receivables.reminderText', {
        client: row.counterparty ?? '',
        number: row.title,
        amount: formatMoney(row.openAmount, locale),
        due: row.dueDate ? formatDate(row.dueDate) : '',
        days: row.daysOverdue,
      }),
    );
  };

  const statusOf = (row: DebtRow): { label: string; color: 'default' | 'warning' | 'error' } => {
    if (row.daysOverdue > 0) {
      return { label: t('book.receivables.status.late', { days: row.daysOverdue }), color: 'error' };
    }

    return row.dueDate
      ? { label: t('book.receivables.status.due', { date: formatDate(row.dueDate) }), color: 'default' }
      : { label: t('book.status.open'), color: 'default' };
  };

  const error = owedToUs.error ?? weOwe.error;

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('book.receivables.title')}
        description={t('book.receivables.asOf', { date: formatDate(today()) })}
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <HowItWorks id="receivables">
        <HowLine>{t('book.receivables.how1')}</HowLine>
        <HowLine>{t('book.receivables.how2')}</HowLine>
        <HowLine>{t('book.receivables.how3')}</HowLine>
        <HowLine>{t('book.receivables.how4')}</HowLine>
      </HowItWorks>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Kpi
          label={t('book.receivables.k.owedToUs')}
          value={formatMoney(owed, locale)}
          note={t('book.receivables.k.openCount', { count: receivable?.rows.length ?? 0 })}
        />
        <Kpi
          label={t('book.receivables.k.weOwe')}
          value={formatMoney(owe, locale)}
          note={t('book.receivables.k.weOweNote')}
        />
        <Kpi
          label={t('book.receivables.k.difference')}
          value={formatMoney(owed - owe, locale)}
          color={owed - owe < 0 ? 'error.main' : 'success.main'}
          note={t('book.receivables.k.differenceNote')}
        />
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.receivables.age')}
        </Typography>
        <Box sx={{ display: 'flex', height: 16, borderRadius: 1, overflow: 'hidden', bgcolor: 'action.hover' }}>
          <Box sx={{ width: `${(onTime / total) * 100}%`, bgcolor: 'success.main' }} />
          <Box sx={{ width: `${(late / total) * 100}%`, bgcolor: 'error.main' }} />
        </Box>
        <Stack direction="row" spacing={3} sx={{ mt: 1 }}>
          <Typography variant="body2">
            {t('book.receivables.notDue')}: <b>{formatMoney(onTime, locale)}</b>
          </Typography>
          <Typography variant="body2" color={late > 0 ? 'error.main' : undefined}>
            {t('book.receivables.late')}: <b>{formatMoney(late, locale)}</b>
          </Typography>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('book.receivables.k.owedToUs')}
          </Typography>
          {mostLate && (
            <Button size="small" variant="outlined" onClick={() => prepareReminder(mostLate)}>
              {t('book.receivables.prepareReminder', { client: mostLate.counterparty ?? mostLate.title })}
            </Button>
          )}
        </Stack>

        {reminder && (
          <Alert
            severity="info"
            sx={{ mb: 1 }}
            action={
              <Button
                size="small"
                startIcon={<ContentCopyOutlined />}
                onClick={() => void navigator.clipboard?.writeText(reminder)}
              >
                {t('book.receivables.copy')}
              </Button>
            }
          >
            <Typography variant="body2" sx={{ whiteSpace: 'pre-line' }}>
              {reminder}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {t('book.receivables.reminderPrepared')}
            </Typography>
          </Alert>
        )}

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.receivables.col.invoice')}</TableCell>
                <TableCell>{t('book.receivables.col.client')}</TableCell>
                <TableCell>{t('book.receivables.col.project')}</TableCell>
                <TableCell>{t('book.receivables.col.issued')}</TableCell>
                <TableCell>{t('book.receivables.col.due')}</TableCell>
                <TableCell>{t('book.col.status')}</TableCell>
                <TableCell align="right">{t('book.col.amount')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(receivable?.rows ?? []).map((row) => {
                const status = statusOf(row);

                return (
                  <TableRow key={`${row.source}-${row.id}`} hover>
                    <TableCell>{row.title || '—'}</TableCell>
                    <TableCell>{row.counterparty ?? '—'}</TableCell>
                    <TableCell>{row.projectName ? <ProjectLabel name={row.projectName} projectId={row.projectId} /> : '—'}</TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(row.issuedOn)}</TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.dueDate ? formatDate(row.dueDate) : '—'}</TableCell>
                    <TableCell>
                      <Chip size="small" variant="outlined" color={status.color} label={status.label} />
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      {formatMoney(row.openAmount, locale)}
                      {row.paidAmount > 0 && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {t('book.receivables.ofAmount', {
                            paid: formatMoney(row.paidAmount, locale),
                            total: formatMoney(row.amount, locale),
                          })}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      <Button size="small" onClick={() => setSettling(row)}>
                        {t('book.receivables.markPaid')}
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
              {!owedToUs.isLoading && (receivable?.rows.length ?? 0) === 0 && (
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
        </Box>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.receivables.k.weOwe')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.receivables.col.whom')}</TableCell>
                <TableCell>{t('book.receivables.col.what')}</TableCell>
                <TableCell>{t('book.receivables.col.due')}</TableCell>
                <TableCell>{t('book.col.status')}</TableCell>
                <TableCell align="right">{t('book.col.amount')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(payable?.rows ?? []).map((row, index) => {
                const status = statusOf(row);
                const payroll = row.source === 'Payroll';

                return (
                  <TableRow key={`${row.source}-${row.id}-${index}`} hover>
                    <TableCell>
                      {payroll
                        ? t(`payouts.group.${row.counterparty}` as MessageKey)
                        : (row.counterparty ?? '—')}
                    </TableCell>
                    <TableCell>
                      {payroll ? t('book.receivables.payrollOf', { month: row.title }) : row.title || '—'}
                    </TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.dueDate ? formatDate(row.dueDate) : '—'}</TableCell>
                    <TableCell>
                      <Chip size="small" variant="outlined" color={status.color} label={status.label} />
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      {formatMoney(row.openAmount, locale)}
                      {payroll && row.paidAmount > 0 && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {t('book.receivables.payrollParts', {
                            earned: formatMoney(row.amount, locale),
                            paid: formatMoney(row.paidAmount, locale),
                          })}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      {payroll ? (
                        <Button size="small" component={RouterLink} to={paths.bookPayouts}>
                          {t('book.receivables.toPayouts')}
                        </Button>
                      ) : (
                        <Button size="small" onClick={() => setSettling(row)}>
                          {t('book.receivables.markPaidOut')}
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
              {!weOwe.isLoading && (payable?.rows.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography color="text.secondary" sx={{ py: 2 }}>
                      {t('book.receivables.empty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
          {t('book.receivables.payrollFormula')}
        </Typography>
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

function Kpi({
  label,
  value,
  note,
  color,
}: {
  label: string;
  value: string;
  note?: string;
  color?: string;
}) {
  return (
    <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 700 }} color={color}>
        {value}
      </Typography>
      {note && (
        <Typography variant="caption" color="text.secondary">
          {note}
        </Typography>
      )}
    </Paper>
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
