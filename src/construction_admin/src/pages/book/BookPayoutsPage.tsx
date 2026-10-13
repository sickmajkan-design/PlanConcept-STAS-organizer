import { ChevronLeftOutlined, ChevronRightOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
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
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { bookPaymentMethods, type BookPaymentMethod } from '../../api/book';
import { newIdempotencyKey } from '../../api/idempotency';
import { payoutsApi, type PayoutGroupSummary, type WagePayout } from '../../api/payouts';
import { payoutGroups, type PayoutGroup } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
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

/** The first of next month: where a month's final payment usually falls. */
function usualFinalDate(year: number, month: number): string {
  const date = new Date(year, month, 10);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-10`;
}

/**
 * What workers are paid out for a month: what each group earned in the settlement, the advances already paid,
 * and the final payment per group with its own date and way of paying. What is left is worked out, never typed.
 */
export function BookPayoutsPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();

  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  // The advance form
  const [advDate, setAdvDate] = useState(today());
  const [advGroup, setAdvGroup] = useState<PayoutGroup>('Workers');
  const [advAmount, setAdvAmount] = useState('');
  const [advMethod, setAdvMethod] = useState<BookPaymentMethod>('Bank');
  const [advNote, setAdvNote] = useState('');
  const [advMessage, setAdvMessage] = useState<string | null>(null);
  const [advKey, setAdvKey] = useState(() => newIdempotencyKey());

  // The final payment of each group: date and way, until it is planned
  const [finalDates, setFinalDates] = useState<Partial<Record<PayoutGroup, string>>>({});
  const [finalMethods, setFinalMethods] = useState<Partial<Record<PayoutGroup, BookPaymentMethod>>>({});

  const monthLabel = new Date(year, month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
    month: 'long',
    year: 'numeric',
  });

  const payouts = useQuery({
    queryKey: [...payoutsKey, year, month, branchId ?? null] as const,
    queryFn: () => payoutsApi.get({ year, month, branchId }),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['book'] });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const shift = (delta: number) => {
    const date = new Date(year, month - 1 + delta, 1);
    setYear(date.getFullYear());
    setMonth(date.getMonth() + 1);
  };

  const addAdvance = useMutation({
    mutationFn: () =>
      payoutsApi.create(
        {
          year,
          month,
          group: advGroup,
          kind: 'Advance',
          payDate: advDate,
          amount: Number(advAmount.replace(/\./g, '').replace(',', '.')),
          method: advMethod,
          note: advNote.trim() || null,
        },
        advKey,
      ),
    onSuccess: () => {
      setAdvAmount('');
      setAdvNote('');
      setAdvKey(newIdempotencyKey());
      setAdvMessage(t('payouts.advanceAdded'));
      refresh();
    },
  });

  const scheduleFinal = useMutation({
    mutationFn: (group: PayoutGroupSummary) =>
      payoutsApi.create(
        {
          year,
          month,
          group: group.group,
          kind: 'Final',
          payDate: finalDates[group.group] ?? usualFinalDate(year, month),
          amount: group.remaining,
          method: finalMethods[group.group] ?? 'Bank',
        },
        newIdempotencyKey(),
      ),
    onSuccess: refresh,
  });

  const markPaid = useMutation({
    mutationFn: (item: WagePayout) => payoutsApi.markPaid(item.id, { paidOn: today(), method: item.method }),
    onSuccess: refresh,
  });

  const changePlan = useMutation({
    mutationFn: (variables: { item: WagePayout; payDate: string; method: BookPaymentMethod | null }) =>
      payoutsApi.update(variables.item.id, {
        payDate: variables.payDate,
        amount: variables.item.amount,
        employeeId: variables.item.employeeId,
        method: variables.method,
        note: variables.item.note,
        branchId: variables.item.branchId,
      }),
    onSuccess: refresh,
  });

  const remove = useMutation({
    mutationFn: (item: WagePayout) => payoutsApi.remove(item.id),
    onSuccess: refresh,
  });

  const data = payouts.data;
  const advances = (data?.items ?? []).filter((i) => i.kind === 'Advance');
  const advanceTotal = (data?.groups ?? []).reduce((sum, g) => sum + g.advances, 0);
  const left = data?.totalRemaining ?? 0;
  const advParsed = Number(advAmount.replace(/\./g, '').replace(',', '.'));

  const plannedFinal = (group: PayoutGroup) =>
    (data?.items ?? []).find((i) => i.group === group && i.kind === 'Final' && i.paidOn === null);

  const paidFinal = (group: PayoutGroup) =>
    (data?.items ?? []).find((i) => i.group === group && i.kind === 'Final' && i.paidOn !== null);

  const nextOut = (data?.items ?? [])
    .filter((i) => i.kind === 'Final' && i.paidOn === null)
    .sort((a, b) => a.payDate.localeCompare(b.payDate))[0];

  const error = payouts.error ?? addAdvance.error ?? scheduleFinal.error ?? markPaid.error ?? changePlan.error;

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('payouts.title')}
        description={t('payouts.subtitle', { month: monthLabel })}
        secondaryActions={
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <IconButton size="small" onClick={() => shift(-1)} aria-label={t('book.overview.previous')}>
              <ChevronLeftOutlined />
            </IconButton>
            <Typography variant="body2" sx={{ minWidth: 130, textAlign: 'center' }}>
              {monthLabel}
            </Typography>
            <IconButton size="small" onClick={() => shift(1)} aria-label={t('book.overview.next')}>
              <ChevronRightOutlined />
            </IconButton>
          </Stack>
        }
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <HowItWorks id="payouts">
        <HowLine>{t('payouts.how1')}</HowLine>
        <HowLine>{t('payouts.how2')}</HowLine>
        <HowLine>{t('payouts.how3')}</HowLine>
        <HowLine>{t('payouts.how4')}</HowLine>
        <HowLine>{t('payouts.how5')}</HowLine>
      </HowItWorks>

      {data && !data.hasSettlement && <Alert severity="info">{t('payouts.noSettlement')}</Alert>}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Kpi
          label={t('payouts.k.earned')}
          value={formatMoney(data?.totalEarned ?? 0, locale)}
          note={t('payouts.k.earnedNote')}
        />
        <Kpi
          label={t('payouts.k.advances')}
          value={formatMoney(advanceTotal, locale)}
          note={t('payouts.k.advanceCount', { count: advances.length })}
        />
        <Kpi
          label={t('payouts.k.left')}
          value={formatMoney(left, locale)}
          note={
            (data?.groups ?? []).some((g) => g.carryOver > 0)
              ? t('payouts.k.leftCarry')
              : t('payouts.k.leftNote')
          }
        />
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('payouts.finalByGroup')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('payouts.col.group')}</TableCell>
                <TableCell align="right">{t('payouts.col.earned')}</TableCell>
                <TableCell align="right">{t('payouts.col.advances')}</TableCell>
                <TableCell align="right">{t('payouts.col.left')}</TableCell>
                <TableCell>{t('payouts.col.date')}</TableCell>
                <TableCell>{t('payouts.col.way')}</TableCell>
                <TableCell>{t('payouts.col.status')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(data?.groups ?? []).map((group) => {
                const planned = plannedFinal(group.group);
                const paid = paidFinal(group.group);
                const date = planned?.payDate ?? paid?.paidOn ?? finalDates[group.group] ?? usualFinalDate(year, month);
                const method = planned?.method ?? paid?.method ?? finalMethods[group.group] ?? 'Bank';
                const done = paid !== undefined && group.remaining === 0;

                return (
                  <TableRow key={group.group}>
                    <TableCell>{t(`payouts.group.${group.group}` as MessageKey)}</TableCell>
                    <TableCell align="right">{formatMoney(group.earned, locale)}</TableCell>
                    <TableCell align="right">{formatMoney(group.advances, locale)}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 600 }}>
                      {formatMoney(group.remaining, locale)}
                      {group.carryOver > 0 && (
                        <Typography variant="caption" color="warning.main" sx={{ display: 'block' }}>
                          {t('payouts.carryOver', { amount: formatMoney(group.carryOver, locale) })}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <TextField
                        type="date"
                        size="small"
                        value={date}
                        disabled={done || group.earned === 0}
                        onChange={(event) => {
                          if (planned) {
                            changePlan.mutate({ item: planned, payDate: event.target.value, method: planned.method });
                          } else {
                            setFinalDates({ ...finalDates, [group.group]: event.target.value });
                          }
                        }}
                        slotProps={{ inputLabel: { shrink: true } }}
                      />
                    </TableCell>
                    <TableCell>
                      <TextField
                        select
                        size="small"
                        value={method}
                        disabled={done || group.earned === 0}
                        onChange={(event) => {
                          const value = event.target.value as BookPaymentMethod;

                          if (planned) {
                            changePlan.mutate({ item: planned, payDate: planned.payDate, method: value });
                          } else {
                            setFinalMethods({ ...finalMethods, [group.group]: value });
                          }
                        }}
                        sx={{ minWidth: 150 }}
                      >
                        {bookPaymentMethods.map((option) => (
                          <MenuItem key={option} value={option}>
                            {t(`payouts.way.${option}` as MessageKey)}
                          </MenuItem>
                        ))}
                      </TextField>
                    </TableCell>
                    <TableCell>
                      {done ? (
                        <Chip size="small" color="success" variant="outlined" label={t('payouts.status.paid')} />
                      ) : planned ? (
                        <Chip size="small" color="warning" variant="outlined" label={t('payouts.status.planned')} />
                      ) : group.earned === 0 ? (
                        <Chip size="small" variant="outlined" label={t('payouts.status.none')} />
                      ) : (
                        <Chip size="small" variant="outlined" label={t('payouts.status.notScheduled')} />
                      )}
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      {planned ? (
                        <Button size="small" loading={markPaid.isPending} onClick={() => markPaid.mutate(planned)}>
                          {t('payouts.markPaid')}
                        </Button>
                      ) : (
                        !done &&
                        group.remaining > 0 && (
                          <Button
                            size="small"
                            loading={scheduleFinal.isPending}
                            onClick={() => scheduleFinal.mutate(group)}
                          >
                            {t('payouts.schedule')}
                          </Button>
                        )
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </Box>

        <Alert severity={nextOut ? 'info' : 'success'} sx={{ mt: 1.5 }}>
          {nextOut
            ? t('payouts.cashNote', {
                date: formatDate(nextOut.payDate),
                amount: formatMoney(
                  (data?.items ?? [])
                    .filter((i) => i.kind === 'Final' && i.paidOn === null && i.payDate === nextOut.payDate)
                    .reduce((sum, i) => sum + i.amount, 0),
                  locale,
                ),
              })
            : t('payouts.cashNone')}
        </Alert>
      </Paper>

      <Paper
        variant="outlined"
        sx={{ p: 2 }}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && advParsed > 0 && !addAdvance.isPending) {
            event.preventDefault();
            addAdvance.mutate();
          }
        }}
      >
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('payouts.newAdvance')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('payouts.newAdvanceHelp')}
        </Typography>
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} useFlexGap>
          <TextField
            type="date"
            size="small"
            label={t('payouts.col.date')}
            value={advDate}
            onChange={(event) => setAdvDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today() } }}
          />
          <TextField
            select
            size="small"
            label={t('payouts.to')}
            value={advGroup}
            onChange={(event) => setAdvGroup(event.target.value as PayoutGroup)}
            sx={{ minWidth: 150 }}
          >
            {payoutGroups.map((group) => (
              <MenuItem key={group} value={group}>
                {t(`payouts.group.${group}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            label={t('payouts.amountEur')}
            placeholder="1.500,00"
            value={advAmount}
            onChange={(event) => setAdvAmount(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            error={advAmount !== '' && !(advParsed > 0)}
            sx={{ maxWidth: 160 }}
          />
          <TextField
            select
            size="small"
            label={t('payouts.col.way')}
            value={advMethod}
            onChange={(event) => setAdvMethod(event.target.value as BookPaymentMethod)}
            sx={{ minWidth: 160 }}
          >
            {bookPaymentMethods.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`payouts.way.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            label={t('payouts.note')}
            placeholder={t('payouts.notePlaceholder')}
            value={advNote}
            onChange={(event) => setAdvNote(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }}
            sx={{ minWidth: 220, flex: 1 }}
          />
          <Button
            variant="contained"
            disabled={!(advParsed > 0)}
            loading={addAdvance.isPending}
            onClick={() => addAdvance.mutate()}
          >
            {t('payouts.addAdvance')}
          </Button>
        </Stack>
        {advMessage && !addAdvance.isPending && (
          <Alert severity="success" sx={{ mt: 1.5 }} onClose={() => setAdvMessage(null)}>
            {advMessage}
          </Alert>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('payouts.all')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('payouts.allHelp')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('payouts.col.date')}</TableCell>
                <TableCell>{t('payouts.to')}</TableCell>
                <TableCell>{t('payouts.col.kind')}</TableCell>
                <TableCell>{t('payouts.col.way')}</TableCell>
                <TableCell>{t('payouts.note')}</TableCell>
                <TableCell align="right">{t('payouts.col.amount')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(data?.items ?? []).map((item) => (
                <TableRow key={item.id} hover>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(item.paidOn ?? item.payDate)}</TableCell>
                  <TableCell>
                    {t(`payouts.group.${item.group}` as MessageKey)}
                    {item.employeeName && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        {item.employeeName}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {t(`payouts.kind.${item.kind}` as MessageKey)}
                    {item.paidOn === null && (
                      <Chip size="small" color="warning" variant="outlined" label={t('payouts.status.planned')} sx={{ ml: 1 }} />
                    )}
                  </TableCell>
                  <TableCell>{item.method ? t(`payouts.way.${item.method}` as MessageKey) : '—'}</TableCell>
                  <TableCell>{item.note ?? '—'}</TableCell>
                  <TableCell align="right">{formatMoney(item.amount, locale)}</TableCell>
                  <TableCell align="right">
                    <Button size="small" color="inherit" onClick={() => remove.mutate(item)}>
                      {t('book.entries.remove')}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {!payouts.isLoading && (data?.items.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Typography color="text.secondary" sx={{ py: 2 }}>
                      {t('payouts.empty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </Paper>
    </Stack>
  );
}

function Kpi({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 700 }}>
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
