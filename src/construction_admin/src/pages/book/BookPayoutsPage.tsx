import { DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { bookPaymentMethods, type BookPaymentMethod } from '../../api/book';
import { newIdempotencyKey } from '../../api/idempotency';
import {
  payoutKinds,
  payoutsApi,
  type PayoutGroupSummary,
  type PayoutKind,
  type WagePayout,
} from '../../api/payouts';
import { payoutGroups, type PayoutGroup } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

const payoutsKey = ['book', 'payouts'] as const;

function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

function thisMonth(): string {
  return today().slice(0, 7);
}

/**
 * What workers are paid out for a month. Each group (workers, subcontractors, office) earns what the
 * month's settlement says; advances can be entered any number of times, and the final payment is
 * planned and then marked paid. What is left is worked out, never typed.
 */
export function BookPayoutsPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();
  const [month, setMonth] = useState(thisMonth());
  const [adding, setAdding] = useState<{ group: PayoutGroup; kind: PayoutKind } | null>(null);
  const [marking, setMarking] = useState<WagePayout | null>(null);
  const [deleting, setDeleting] = useState<WagePayout | null>(null);

  const [year, monthNumber] = month.split('-').map(Number);

  const payouts = useQuery({
    queryKey: [...payoutsKey, month, branchId ?? null] as const,
    queryFn: () => payoutsApi.get({ year: year!, month: monthNumber!, branchId }),
    enabled: Boolean(year && monthNumber),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: payoutsKey });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const remove = useMutation({
    mutationFn: (payout: WagePayout) => payoutsApi.remove(payout.id),
    onSuccess: refresh,
  });

  const data = payouts.data;

  return (
    <Stack spacing={2}>
      <PageHeader title={t('payouts.title')} description={t('payouts.description')} />

      <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
        <TextField
          type="month"
          size="small"
          label={t('payouts.month')}
          value={month}
          onChange={(event) => setMonth(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        {data && !data.hasSettlement && (
          <Alert severity="info" sx={{ flex: 1 }}>
            {t('payouts.noSettlement')}
          </Alert>
        )}
      </Stack>

      {payouts.error && <Alert severity="error">{toApiError(payouts.error).message}</Alert>}

      <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2}>
        {(data?.groups ?? []).map((group) => (
          <GroupCard
            key={group.group}
            group={group}
            locale={locale}
            onAdvance={() => setAdding({ group: group.group, kind: 'Advance' })}
            onFinal={() => setAdding({ group: group.group, kind: 'Final' })}
          />
        ))}
      </Stack>

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('payouts.col.date')}</TableCell>
              <TableCell>{t('payouts.col.group')}</TableCell>
              <TableCell>{t('payouts.col.kind')}</TableCell>
              <TableCell>{t('payouts.col.who')}</TableCell>
              <TableCell>{t('payouts.col.method')}</TableCell>
              <TableCell>{t('payouts.col.status')}</TableCell>
              <TableCell align="right">{t('payouts.col.amount')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {(data?.items ?? []).map((item) => (
              <TableRow key={item.id} hover>
                <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(item.payDate)}</TableCell>
                <TableCell>{t(`payouts.group.${item.group}` as MessageKey)}</TableCell>
                <TableCell>{t(`payouts.kind.${item.kind}` as MessageKey)}</TableCell>
                <TableCell>{item.employeeName ?? '—'}</TableCell>
                <TableCell>
                  {item.method ? t(`book.method.${item.method}` as MessageKey) : '—'}
                </TableCell>
                <TableCell>
                  {item.paidOn ? (
                    <Chip size="small" variant="outlined" label={t('payouts.status.paid')} />
                  ) : (
                    <Chip size="small" color="warning" variant="outlined" label={t('payouts.status.planned')} />
                  )}
                </TableCell>
                <TableCell align="right">{formatMoney(item.amount, locale)}</TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  {!item.paidOn && (
                    <Button size="small" onClick={() => setMarking(item)}>
                      {t('payouts.markPaid')}
                    </Button>
                  )}
                  <Tooltip title={t('common.delete')}>
                    <IconButton size="small" onClick={() => setDeleting(item)} aria-label={t('common.delete')}>
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {!payouts.isLoading && (data?.items.length ?? 0) === 0 && (
              <TableRow>
                <TableCell colSpan={8}>
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {t('payouts.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      {adding && year && monthNumber && (
        <PayoutDialog
          year={year}
          month={monthNumber}
          initialGroup={adding.group}
          initialKind={adding.kind}
          onClose={() => setAdding(null)}
          onSaved={() => {
            setAdding(null);
            refresh();
          }}
        />
      )}

      {marking && (
        <MarkPaidDialog
          payout={marking}
          onClose={() => setMarking(null)}
          onSaved={() => {
            setMarking(null);
            refresh();
          }}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title={t('payouts.deleteTitle')}
        description={t('payouts.deleteBody')}
        confirmLabel={t('common.delete')}
        destructive
        onConfirm={async () => {
          if (deleting) await remove.mutateAsync(deleting);
          setDeleting(null);
        }}
        onCancel={() => setDeleting(null)}
      />
    </Stack>
  );
}

function GroupCard({
  group,
  locale,
  onAdvance,
  onFinal,
}: {
  group: PayoutGroupSummary;
  locale: string;
  onAdvance: () => void;
  onFinal: () => void;
}) {
  const { t } = useI18n();

  const line = (label: MessageKey, value: number, emphasis = false) => (
    <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
      <Typography variant="body2" color={emphasis ? undefined : 'text.secondary'}>
        {t(label)}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: emphasis ? 700 : 400 }}>
        {formatMoney(value, locale)}
      </Typography>
    </Stack>
  );

  return (
    <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
      <Stack spacing={1}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          {t(`payouts.group.${group.group}` as MessageKey)}
        </Typography>
        {line('payouts.earned', group.earned)}
        {line('payouts.advances', group.advances)}
        {line('payouts.finalPaid', group.finalPaid)}
        {line('payouts.remaining', group.remaining, true)}
        {group.finalPlanned > 0 && line('payouts.finalPlanned', group.finalPlanned)}
        {group.carryOver > 0 && (
          <Alert severity="warning" sx={{ py: 0 }}>
            {t('payouts.carryOver', { amount: formatMoney(group.carryOver, locale) })}
          </Alert>
        )}
        <Stack direction="row" spacing={1} sx={{ pt: 1 }}>
          <Button size="small" variant="outlined" onClick={onAdvance}>
            {t('payouts.addAdvance')}
          </Button>
          <Button size="small" variant="outlined" onClick={onFinal}>
            {t('payouts.addFinal')}
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}

function PayoutDialog({
  year,
  month,
  initialGroup,
  initialKind,
  onClose,
  onSaved,
}: {
  year: number;
  month: number;
  initialGroup: PayoutGroup;
  initialKind: PayoutKind;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const [idempotencyKey] = useState(() => newIdempotencyKey());
  const [group, setGroup] = useState<PayoutGroup>(initialGroup);
  const [kind, setKind] = useState<PayoutKind>(initialKind);
  const [amount, setAmount] = useState('');
  const [payDate, setPayDate] = useState(today());
  const [method, setMethod] = useState<BookPaymentMethod | ''>('');
  const [note, setNote] = useState('');

  const parsed = Number(amount.replace(',', '.'));
  const valid = Number.isFinite(parsed) && parsed > 0 && payDate !== '';

  const save = useMutation({
    mutationFn: () =>
      payoutsApi.create(
        {
          year,
          month,
          group,
          kind,
          payDate,
          amount: parsed,
          method: method || null,
          note: note.trim() || null,
        },
        idempotencyKey,
      ),
    onSuccess: onSaved,
  });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('payouts.add')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            select
            label={t('payouts.col.group')}
            value={group}
            onChange={(event) => setGroup(event.target.value as PayoutGroup)}
          >
            {payoutGroups.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`payouts.group.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label={t('payouts.col.kind')}
            value={kind}
            onChange={(event) => setKind(event.target.value as PayoutKind)}
            helperText={t(`payouts.kind.${kind}.help` as MessageKey)}
          >
            {payoutKinds.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`payouts.kind.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label={t('payouts.col.amount')}
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            error={amount !== '' && !(parsed > 0)}
            autoFocus
          />
          <TextField
            type="date"
            label={kind === 'Advance' ? t('payouts.paidOn') : t('payouts.plannedFor')}
            value={payDate}
            onChange={(event) => setPayDate(event.target.value)}
            slotProps={{
              inputLabel: { shrink: true },
              htmlInput: kind === 'Advance' ? { max: today() } : {},
            }}
          />
          <TextField
            select
            label={t('payouts.col.method')}
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
          <TextField
            label={t('book.receivables.paymentNote')}
            value={note}
            onChange={(event) => setNote(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }}
          />
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

function MarkPaidDialog({
  payout,
  onClose,
  onSaved,
}: {
  payout: WagePayout;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const [paidOn, setPaidOn] = useState(today());
  const [method, setMethod] = useState<BookPaymentMethod | ''>(payout.method ?? '');

  const save = useMutation({
    mutationFn: () => payoutsApi.markPaid(payout.id, { paidOn, method: method || null }),
    onSuccess: onSaved,
  });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('payouts.markPaid')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            type="date"
            label={t('payouts.paidOn')}
            value={paidOn}
            onChange={(event) => setPaidOn(event.target.value)}
            slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today() } }}
          />
          <TextField
            select
            label={t('payouts.col.method')}
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
          {save.error && <Alert severity="error">{toApiError(save.error).message}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={paidOn === ''} loading={save.isPending} onClick={() => save.mutate()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
