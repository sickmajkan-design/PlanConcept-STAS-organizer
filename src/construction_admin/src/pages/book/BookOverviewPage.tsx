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
import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { overviewApi, type OverviewStepKey } from '../../api/overview';
import { PageHeader } from '../../components/PageHeader';
import { useAuth } from '../../auth/useAuth';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

const WINDOW = 12;

interface MonthRef {
  year: number;
  month: number;
}

function monthsEndingNow(count: number): MonthRef[] {
  const now = new Date();
  const list: MonthRef[] = [];

  for (let back = count - 1; back >= 0; back--) {
    const date = new Date(now.getFullYear(), now.getMonth() - back, 1);
    list.push({ year: date.getFullYear(), month: date.getMonth() + 1 });
  }

  return list;
}

function iso(year: number, month: number, day: number): string {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

function lastDay(year: number, month: number): number {
  return new Date(year, month, 0).getDate();
}

type PresetKey = 'thisMonth' | 'lastMonth' | 'last3' | 'lastQuarter' | 'yearToDate';

const stepColors: Record<OverviewStepKey, string> = {
  Billing: 'success.main',
  OtherIncome: 'success.light',
  Workers: 'error.main',
  Subcontractors: 'error.light',
  Office: 'error.light',
  SiteCosts: 'error.light',
  FixedCosts: 'error.light',
  Profit: 'text.primary',
};

/**
 * The front page of the book: what comes in, what goes out and what is left, for a period you choose; what
 * needs a look; the next days of cash; and an estimate for next month. Every figure comes from the server —
 * nothing is worked out here beyond laying it out — so the number on the screen is the number in the settlement.
 */
export function BookOverviewPage() {
  const { t, locale } = useI18n();
  const { user } = useAuth();
  const { branchId } = useBranchFilter();
  const navigate = useNavigate();

  const months = useMemo(() => monthsEndingNow(WINDOW), []);
  const last = months.length - 1;

  const [from, setFrom] = useState(last);
  const [to, setTo] = useState(last);
  const [balanceText, setBalanceText] = useState('0');
  const [hoursText, setHoursText] = useState('');
  const [debouncedHours, setDebouncedHours] = useState<number | undefined>(undefined);

  const lo = Math.min(from, to);
  const hi = Math.max(from, to);
  const length = hi - lo + 1;

  const period = {
    from: iso(months[lo]!.year, months[lo]!.month, 1),
    to: iso(months[hi]!.year, months[hi]!.month, lastDay(months[hi]!.year, months[hi]!.month)),
  };

  const windowRange = {
    from: iso(months[0]!.year, months[0]!.month, 1),
    to: iso(months[last]!.year, months[last]!.month, lastDay(months[last]!.year, months[last]!.month)),
  };

  const overview = useQuery({
    queryKey: ['book', 'overview', period.from, period.to, branchId ?? null] as const,
    queryFn: () => overviewApi.get({ ...period, branchId }),
  });

  const trend = useQuery({
    queryKey: ['book', 'overview', 'window', windowRange.from, windowRange.to, branchId ?? null] as const,
    queryFn: () => overviewApi.get({ ...windowRange, branchId }),
    staleTime: 60_000,
  });

  const balance = Number(balanceText.replace(',', '.'));

  const cashflow = useQuery({
    queryKey: ['book', 'cashflow', Number.isFinite(balance) ? balance : 0, branchId ?? null] as const,
    queryFn: () => overviewApi.cashflow({ openingBalance: Number.isFinite(balance) ? balance : 0, branchId }),
  });

  useEffect(() => {
    const handle = window.setTimeout(() => {
      const value = Number(hoursText.replace(/\./g, '').replace(',', '.'));
      setDebouncedHours(hoursText.trim() !== '' && Number.isFinite(value) && value >= 0 ? value : undefined);
    }, 350);
    return () => window.clearTimeout(handle);
  }, [hoursText]);

  const forecast = useQuery({
    queryKey: ['book', 'forecast', debouncedHours ?? null, branchId ?? null] as const,
    queryFn: () => overviewApi.forecast({ hours: debouncedHours, branchId }),
  });

  const attention = useQuery({
    queryKey: ['book', 'attention', branchId ?? null] as const,
    queryFn: () => overviewApi.attention({ branchId }),
  });

  const data = overview.data;
  const visible = data?.amountsVisible ?? true;
  const money = (value: number) => (visible ? formatMoney(value, locale) : '—');
  const eur = (value: number) => (visible ? `${formatMoney(value, locale)}` : '—');

  const monthLabel = (m: MonthRef, style: 'short' | 'long' = 'long') =>
    new Date(m.year, m.month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
      month: style,
      year: style === 'long' ? 'numeric' : undefined,
    });

  const periodLabel =
    length === 1
      ? monthLabel(months[lo]!)
      : t('book.overview.periodRange', { from: monthLabel(months[lo]!), to: monthLabel(months[hi]!) });

  const shift = (direction: -1 | 1) => {
    const nextFrom = lo + direction * length;
    const nextTo = hi + direction * length;
    if (nextFrom < 0 || nextTo > last) return;
    setFrom(nextFrom);
    setTo(nextTo);
  };

  const quarterStart = (() => {
    const now = new Date();
    return Math.floor(now.getMonth() / 3) * 3;
  })();

  const presets: { key: PresetKey; from: number; to: number }[] = [
    { key: 'thisMonth', from: last, to: last },
    { key: 'lastMonth', from: last - 1, to: last - 1 },
    { key: 'last3', from: last - 2, to: last },
    {
      key: 'lastQuarter',
      // The calendar quarter before this one, counted back from the current month.
      from: last - (new Date().getMonth() - quarterStart) - 3,
      to: last - (new Date().getMonth() - quarterStart) - 1,
    },
    { key: 'yearToDate', from: last - new Date().getMonth(), to: last },
  ];

  const delta = data && visible && data.previousProfit !== 0
    ? Math.round(((data.profit - data.previousProfit) / Math.abs(data.previousProfit)) * 100)
    : null;

  const windowMonths = trend.data?.months ?? [];
  const maxBar = Math.max(
    1,
    ...windowMonths.map((m) => Math.max(m.income, m.outflow)),
    forecast.data?.hasData ? Math.max(forecast.data.income, forecast.data.outflow) : 0,
  );
  const bar = (value: number) => Math.max(2, Math.round((Math.abs(value) / maxBar) * 160));

  const compact = (value: number) =>
    `${value < 0 ? '−' : ''}${(Math.abs(value) / 1000).toLocaleString(locale, { minimumFractionDigits: 1, maximumFractionDigits: 1 })}k`;

  const maxCost = Math.max(1, ...(data?.costsByKind ?? []).map((c) => c.amount));
  const maxStep = Math.max(1, ...(data?.steps ?? []).map((s) => Math.abs(s.amount)));

  const firstName = user?.firstName ?? '';
  const cash = cashflow.data;

  const costLabel = (key: string): string => {
    const known = `book.cost.${key}` as MessageKey;
    const translated = t(known);
    return translated === known ? key : translated;
  };

  const errorMessage = overview.error ?? trend.error ?? cashflow.error;

  return (
    <Stack spacing={2}>
      <Box>
        <Typography variant="body2" color="text.secondary">
          {firstName ? t('book.overview.greeting', { name: firstName }) : t('book.overview.greetingPlain')}
        </Typography>
        <PageHeader
          title={t('book.overview.title')}
          description={t('book.overview.subtitle', { period: periodLabel })}
          action={{ label: t('book.newEntry'), onClick: () => navigate(paths.bookEntries) }}
        />
      </Box>

      {errorMessage && <Alert severity="error">{toApiError(errorMessage).message}</Alert>}

      {data && !visible && <Alert severity="info">{t('overview.statisticsOnly')}</Alert>}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.overview.attention')}
        </Typography>
        <Stack spacing={1}>
          {(attention.data ?? []).map((item, index) => (
            <Alert key={`${item.kind}-${index}`} severity={item.kind === 'LateInvoice' ? 'warning' : 'info'}>
              {item.kind === 'LateInvoice' && item.reference
                ? t('book.overview.attn.late', {
                    number: item.reference,
                    days: item.days,
                    amount: formatMoney(item.amount, locale),
                  })
                : item.kind === 'LateInvoice'
                  ? t('book.overview.attn.lateMore', {
                      count: item.count,
                      amount: formatMoney(item.amount, locale),
                    })
                  : item.kind === 'OpenMonth'
                    ? t('book.overview.attn.openMonths', { count: item.count })
                    : t('book.overview.attn.checks', { count: item.count })}
            </Alert>
          ))}
          {cash?.belowZero && (
            <Alert severity="error">
              {t('book.overview.attn.cash', {
                date: formatDate(cash.lowestDate),
                amount: formatMoney(cash.lowestBalance, locale),
              })}
            </Alert>
          )}
          {!attention.isLoading && (attention.data?.length ?? 0) === 0 && !cash?.belowZero && (
            <Typography variant="body2" color="text.secondary">
              {t('book.overview.attn.none')}
            </Typography>
          )}
        </Stack>
      </Paper>

      <Box>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.overview.quick')}
        </Typography>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
          {(
            [
              ['entry', paths.bookEntries, 'book.overview.quick.entry', 'book.overview.quick.entryHelp'],
              ['payment', paths.bookReceivables, 'book.overview.quick.payment', 'book.overview.quick.paymentHelp'],
              ['advance', paths.bookPayouts, 'book.overview.quick.advance', 'book.overview.quick.advanceHelp'],
              ['settlement', paths.bookSettlement, 'book.overview.quick.settlement', 'book.overview.quick.settlementHelp'],
            ] as const
          ).map(([key, to, title, help]) => (
            <Paper
              key={key}
              variant="outlined"
              component={RouterLink}
              to={to}
              sx={{ p: 2, flex: 1, textDecoration: 'none', color: 'inherit', '&:hover': { borderColor: 'primary.main' } }}
            >
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {t(title)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t(help)}
              </Typography>
            </Paper>
          ))}
        </Stack>
      </Box>

      <HowItWorks id="overview">
        <HowLine>{t('book.overview.how1')}</HowLine>
        <HowLine>{t('book.overview.how2')}</HowLine>
        <HowLine>{t('book.overview.how3')}</HowLine>
        <HowLine>{t('book.overview.how4')}</HowLine>
      </HowItWorks>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap', alignItems: { lg: 'center' } }}>
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <IconButton size="small" disabled={lo - length < 0} onClick={() => shift(-1)} aria-label={t('book.overview.previous')}>
              <ChevronLeftOutlined />
            </IconButton>
            {presets.map((preset) => (
              <Button
                key={preset.key}
                size="small"
                variant={lo === Math.max(0, preset.from) && hi === preset.to ? 'contained' : 'outlined'}
                disabled={preset.from < 0}
                onClick={() => {
                  setFrom(preset.from);
                  setTo(preset.to);
                }}
              >
                {t(`book.overview.preset.${preset.key}` as MessageKey)}
              </Button>
            ))}
            <IconButton size="small" disabled={hi + length > last} onClick={() => shift(1)} aria-label={t('book.overview.next')}>
              <ChevronRightOutlined />
            </IconButton>
          </Stack>
          <Stack direction="row" spacing={1}>
            <TextField
              select
              size="small"
              label={t('book.filter.from')}
              value={lo}
              onChange={(event) => setFrom(Number(event.target.value))}
              sx={{ minWidth: 170 }}
            >
              {months.map((m, index) => (
                <MenuItem key={index} value={index}>
                  {monthLabel(m)}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              size="small"
              label={t('book.filter.to')}
              value={hi}
              onChange={(event) => setTo(Number(event.target.value))}
              sx={{ minWidth: 170 }}
            >
              {months.map((m, index) => (
                <MenuItem key={index} value={index}>
                  {monthLabel(m)}
                </MenuItem>
              ))}
            </TextField>
          </Stack>
        </Stack>
      </Paper>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Kpi
          label={t('book.overview.k.income')}
          value={`${money(data?.income ?? 0)}`}
          note={
            data && data.hours > 0 && visible
              ? t('book.overview.k.incomeNote', {
                  hours: Math.round(data.hours).toLocaleString(locale),
                  price: formatMoney(data.income / data.hours, locale),
                })
              : undefined
          }
        />
        <Kpi
          label={t('book.overview.k.outflow')}
          value={`${money(data?.outflow ?? 0)}`}
          note={t('book.overview.k.outflowNote')}
        />
        <Kpi
          label={t('book.overview.k.profit')}
          value={`${money(data?.profit ?? 0)}`}
          color={(data?.profit ?? 0) < 0 ? 'error.main' : 'success.main'}
          note={
            delta === null
              ? t('book.overview.k.noPrevious')
              : t('book.overview.k.vsPrevious', { percent: `${delta >= 0 ? '+' : '−'}${Math.abs(delta)}` })
          }
        />
        <Kpi
          label={t('book.overview.k.margin')}
          value={data?.marginPercent != null ? `${data.marginPercent.toLocaleString(locale)} %` : '—'}
          note={t('book.overview.k.marginNote')}
        />
        <Kpi
          label={t('book.overview.k.perHour')}
          value={data?.profitPerHour != null ? money(data.profitPerHour) : '—'}
          note={t('book.overview.k.perHourNote')}
        />
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.overview.waterfall')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('book.overview.waterfallHelp')}
        </Typography>
        <Stack spacing={1.25}>
          {(data?.steps ?? [])
            .filter((step) => step.key === 'Profit' || step.key === 'Billing' || step.amount !== 0 || !visible)
            .map((step) => (
              <Box key={step.key}>
                <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                  <Typography variant="body2" sx={{ fontWeight: step.key === 'Profit' || step.key === 'Billing' ? 700 : 400 }}>
                    {t(`overview.step.${step.key}` as MessageKey)}
                  </Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {step.key !== 'Billing' && step.key !== 'OtherIncome' && step.key !== 'Profit' && visible ? '−' : ''}
                    {money(step.amount)}
                    {step.sharePercent != null && (
                      <Typography component="span" variant="caption" color="text.secondary">
                        {'  '}
                        {step.sharePercent.toLocaleString(locale)} %
                      </Typography>
                    )}
                  </Typography>
                </Stack>
                <Box sx={{ height: 8, bgcolor: 'action.hover', borderRadius: 1, mt: 0.5 }}>
                  <Box
                    sx={{
                      height: '100%',
                      width: `${visible ? Math.min(100, (Math.abs(step.amount) / maxStep) * 100) : Math.min(100, Math.abs(step.sharePercent ?? 0))}%`,
                      bgcolor: stepColors[step.key],
                      borderRadius: 1,
                    }}
                  />
                </Box>
              </Box>
            ))}
        </Stack>
      </Paper>

      {visible && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('book.overview.byMonth')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            {t('book.overview.byMonthHelp')}
          </Typography>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-end', overflowX: 'auto', pb: 1 }}>
            {windowMonths.map((m, index) => {
              const selected = index >= lo && index <= hi;

              return (
                <Box
                  key={`${m.year}-${m.month}`}
                  component="button"
                  type="button"
                  onClick={() => {
                    setFrom(index);
                    setTo(index);
                  }}
                  aria-label={`${monthLabel(months[index]!)}: ${formatMoney(m.profit, locale)}`}
                  sx={{
                    all: 'unset',
                    cursor: 'pointer',
                    textAlign: 'center',
                    minWidth: 56,
                    p: 0.5,
                    borderRadius: 1,
                    bgcolor: selected ? 'action.selected' : 'transparent',
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                >
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: 'flex-end', justifyContent: 'center', height: 170 }}>
                    <Box sx={{ width: 16, height: bar(m.income), bgcolor: 'success.main', borderRadius: '2px 2px 0 0' }} />
                    <Box sx={{ width: 16, height: bar(m.outflow), bgcolor: 'error.main', borderRadius: '2px 2px 0 0' }} />
                  </Stack>
                  <Typography variant="caption" sx={{ display: 'block', fontWeight: selected ? 700 : 400 }}>
                    {monthLabel(months[index]!, 'short')}
                  </Typography>
                  <Typography
                    variant="caption"
                    color={m.profit < 0 ? 'error.main' : 'success.main'}
                    sx={{ display: 'block' }}
                  >
                    {compact(m.profit)}
                  </Typography>
                </Box>
              );
            })}
            {forecast.data?.hasData && (
              <Box sx={{ textAlign: 'center', minWidth: 56, p: 0.5, opacity: 0.5 }}>
                <Stack direction="row" spacing={0.5} sx={{ alignItems: 'flex-end', justifyContent: 'center', height: 170 }}>
                  <Box sx={{ width: 16, height: bar(forecast.data.income), bgcolor: 'success.main', borderRadius: '2px 2px 0 0' }} />
                  <Box sx={{ width: 16, height: bar(forecast.data.outflow), bgcolor: 'error.main', borderRadius: '2px 2px 0 0' }} />
                </Stack>
                <Typography variant="caption" sx={{ display: 'block' }}>
                  {t('book.overview.nextMonthShort')}
                </Typography>
                <Typography variant="caption" sx={{ display: 'block' }}>
                  {compact(forecast.data.profit)}
                </Typography>
              </Box>
            )}
          </Stack>
        </Paper>
      )}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', mb: 1 }}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('book.overview.cashflow')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('book.overview.cashflowHelp', { date: cash ? formatDate(cash.from) : '' })}
            </Typography>
          </Box>
          <TextField
            size="small"
            label={t('book.overview.balanceToday')}
            value={balanceText}
            onChange={(event) => setBalanceText(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            sx={{ maxWidth: 220 }}
          />
        </Stack>

        {cash && (
          <Alert severity={cash.belowZero ? 'warning' : 'success'} sx={{ mb: 1 }}>
            {cash.belowZero
              ? t('book.overview.cash.bad', {
                  date: formatDate(cash.lowestDate),
                  amount: formatMoney(cash.lowestBalance, locale),
                  reserve: formatMoney(-cash.lowestBalance, locale),
                })
              : t('book.overview.cash.good', {
                  amount: formatMoney(cash.lowestBalance, locale),
                  date: formatDate(cash.lowestDate),
                })}
          </Alert>
        )}

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.col.date')}</TableCell>
                <TableCell>{t('book.overview.cash.what')}</TableCell>
                <TableCell>{t('book.col.kind')}</TableCell>
                <TableCell align="right">{t('book.col.amount')}</TableCell>
                <TableCell align="right">{t('book.overview.cash.after')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(cash?.events ?? []).map((event, index) => (
                <TableRow key={index}>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(event.date)}</TableCell>
                  <TableCell>
                    {t(`book.overview.event.${event.kind}` as MessageKey, { what: event.what ?? '', who: event.who ?? '' })}
                  </TableCell>
                  <TableCell>
                    {event.amount >= 0 ? t('book.overview.cash.in') : t('book.overview.cash.out')}
                    {event.estimated ? `, ${t('book.overview.cash.estimate')}` : ''}
                  </TableCell>
                  <TableCell align="right" sx={{ color: event.amount < 0 ? 'error.main' : 'success.main', whiteSpace: 'nowrap' }}>
                    {event.amount >= 0 ? '+' : '−'} {formatMoney(Math.abs(event.amount), locale)}
                  </TableCell>
                  <TableCell align="right" sx={{ color: event.balanceAfter < 0 ? 'error.main' : undefined, whiteSpace: 'nowrap' }}>
                    {formatMoney(event.balanceAfter, locale)}
                  </TableCell>
                </TableRow>
              ))}
              {cash && cash.events.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5}>
                    <Typography color="text.secondary" sx={{ py: 1 }}>
                      {t('book.overview.cash.empty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.overview.forecast')}
        </Typography>
        {forecast.data?.hasData ? (
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              {t('book.overview.forecastHelp')}
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: { sm: 'center' } }}>
              <TextField
                size="small"
                label={t('book.overview.forecastHours')}
                value={hoursText === '' ? String(Math.round(forecast.data.averageHours)) : hoursText}
                onChange={(event) => setHoursText(event.target.value)}
                slotProps={{ htmlInput: { inputMode: 'numeric' } }}
                sx={{ maxWidth: 200 }}
              />
              <Typography variant="caption" color="text.secondary">
                {t('book.overview.forecastAverage', { hours: Math.round(forecast.data.averageHours).toLocaleString(locale) })}
              </Typography>
            </Stack>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <Kpi
                label={t('book.overview.fc.income')}
                value={eur(forecast.data.income)}
                note={t('book.overview.fc.incomeNote', { price: formatMoney(forecast.data.incomePerHour, locale) })}
              />
              <Kpi
                label={t('book.overview.fc.outflow')}
                value={eur(forecast.data.outflow)}
                note={t('book.overview.fc.outflowNote', { perHour: formatMoney(forecast.data.costPerHour, locale) })}
              />
              <Kpi
                label={t('book.overview.fc.profit')}
                value={eur(forecast.data.profit)}
                color={forecast.data.profit < 0 ? 'error.main' : 'success.main'}
                note={t('book.overview.fc.range', {
                  low: formatMoney(forecast.data.profitLow, locale),
                  high: formatMoney(forecast.data.profitHigh, locale),
                })}
              />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {t('book.overview.fc.parts', {
                workers: formatMoney(forecast.data.workers, locale),
                subs: formatMoney(forecast.data.subcontractors, locale),
                site: formatMoney(forecast.data.siteCosts, locale),
                office: formatMoney(forecast.data.office, locale),
                fixed: formatMoney(forecast.data.fixed, locale),
              })}
            </Typography>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {t('overview.forecastNone')}
          </Typography>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.overview.whereMoney')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('book.overview.whereMoneyHelp')}
        </Typography>
        <Stack spacing={1.25}>
          {(data?.costsByKind ?? []).map((cost) => (
            <Box key={cost.key}>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Typography variant="body2">{costLabel(cost.key)}</Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {formatMoney(cost.amount, locale)}
                  {cost.sharePercent != null && (
                    <Typography component="span" variant="caption" color="text.secondary">
                      {' · '}
                      {Math.round(cost.sharePercent)} %
                    </Typography>
                  )}
                </Typography>
              </Stack>
              <Box sx={{ height: 8, bgcolor: 'action.hover', borderRadius: 1, mt: 0.5 }}>
                <Box sx={{ height: '100%', width: `${(cost.amount / maxCost) * 100}%`, bgcolor: 'primary.main', borderRadius: 1 }} />
              </Box>
            </Box>
          ))}
          {visible && (data?.costsByKind.length ?? 0) === 0 && (
            <Typography variant="body2" color="text.secondary">
              {t('book.overview.noCosts')}
            </Typography>
          )}
        </Stack>

        {visible && data && (
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mt: 2 }}>
            {length > 1 && data.bestMonth && (
              <Chip
                variant="outlined"
                label={t('book.overview.best', {
                  month: monthLabel({ year: data.bestMonth.year, month: data.bestMonth.month }),
                  amount: formatMoney(data.bestMonth.profit, locale),
                })}
              />
            )}
            {length > 1 && data.worstMonth && (
              <Chip
                variant="outlined"
                label={t('book.overview.worst', {
                  month: monthLabel({ year: data.worstMonth.year, month: data.worstMonth.month }),
                  amount: formatMoney(data.worstMonth.profit, locale),
                })}
              />
            )}
            <Chip
              variant="outlined"
              label={t('book.overview.average', { amount: formatMoney(data.averageMonthlyProfit, locale) })}
            />
            {data.openMonths > 0 && (
              <Chip color="warning" variant="outlined" label={t('overview.openMonths', { count: data.openMonths })} />
            )}
          </Stack>
        )}
      </Paper>
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
