import { LockOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Chip,
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
import { BarChart } from '@mui/x-charts/BarChart';
import { LineChart } from '@mui/x-charts/LineChart';
import { useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { overviewApi, type OverviewStepKey } from '../../api/overview';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { useElementSize } from '../../features/dashboard/useElementSize';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { chartPalette } from '../../theme';
import { formatDate, formatMoney } from '../../utils/formatting';

type Preset = 'thisMonth' | 'lastMonth' | 'last3' | 'thisQuarter' | 'lastQuarter' | 'thisYear' | 'custom';

const presets: Preset[] = ['thisMonth', 'lastMonth', 'last3', 'thisQuarter', 'lastQuarter', 'thisYear', 'custom'];

function iso(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function range(preset: Exclude<Preset, 'custom'>, now = new Date()): { from: string; to: string } {
  const y = now.getFullYear();
  const m = now.getMonth();

  switch (preset) {
    case 'thisMonth':
      return { from: iso(new Date(y, m, 1)), to: iso(new Date(y, m + 1, 0)) };
    case 'lastMonth':
      return { from: iso(new Date(y, m - 1, 1)), to: iso(new Date(y, m, 0)) };
    case 'last3':
      return { from: iso(new Date(y, m - 2, 1)), to: iso(new Date(y, m + 1, 0)) };
    case 'thisQuarter': {
      const q = Math.floor(m / 3) * 3;
      return { from: iso(new Date(y, q, 1)), to: iso(new Date(y, q + 3, 0)) };
    }
    case 'lastQuarter': {
      const q = Math.floor(m / 3) * 3 - 3;
      return { from: iso(new Date(y, q, 1)), to: iso(new Date(y, q + 3, 0)) };
    }
    case 'thisYear':
      return { from: iso(new Date(y, 0, 1)), to: iso(new Date(y, 11, 31)) };
  }
}

const stepColors: Record<OverviewStepKey, string> = {
  Billing: 'success.main',
  OtherIncome: 'success.light',
  Workers: 'warning.main',
  Subcontractors: 'warning.light',
  Office: 'info.main',
  SiteCosts: 'error.light',
  FixedCosts: 'error.main',
  Profit: 'primary.main',
};

/**
 * Money in, money out and profit for a period, from hours to profit, plus the next days of cash and an
 * estimate for next month. Every figure comes from the API; nothing is calculated here, so the number on
 * the screen is the number in the settlement.
 */
export function BookOverviewPage() {
  const { t, locale } = useI18n();
  const { branchId } = useBranchFilter();
  const [preset, setPreset] = useState<Preset>('thisMonth');
  const [customFrom, setCustomFrom] = useState(range('thisYear').from);
  const [customTo, setCustomTo] = useState(range('thisYear').to);
  const [openingBalance, setOpeningBalance] = useState('0');

  const period = preset === 'custom' ? { from: customFrom, to: customTo } : range(preset);

  const overview = useQuery({
    queryKey: ['book', 'overview', period.from, period.to, branchId ?? null] as const,
    queryFn: () => overviewApi.get({ ...period, branchId }),
  });

  const balance = Number(openingBalance.replace(',', '.'));

  const cashflow = useQuery({
    queryKey: ['book', 'cashflow', Number.isFinite(balance) ? balance : 0, branchId ?? null] as const,
    queryFn: () =>
      overviewApi.cashflow({ openingBalance: Number.isFinite(balance) ? balance : 0, branchId }),
  });

  const forecast = useQuery({
    queryKey: ['book', 'forecast', branchId ?? null] as const,
    queryFn: () => overviewApi.forecast({ branchId }),
  });

  const data = overview.data;
  const visible = data?.amountsVisible ?? true;
  const money = (value: number) => (visible ? formatMoney(value, locale) : '—');

  const maxStep = Math.max(1, ...(data?.steps ?? []).map((step) => Math.abs(step.amount)));
  const chartSize = useElementSize<HTMLDivElement>();

  const monthLabels = useMemo(
    () =>
      (data?.months ?? []).map((m) =>
        new Date(m.year, m.month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
          month: 'short',
          year: '2-digit',
        }),
      ),
    [data, locale],
  );

  const delta = data ? data.profit - data.previousProfit : 0;

  return (
    <Stack spacing={2}>
      <PageHeader title={t('overview.title')} description={t('overview.description')} />

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <TextField
            select
            size="small"
            label={t('overview.period')}
            value={preset}
            onChange={(event) => setPreset(event.target.value as Preset)}
            sx={{ minWidth: 200 }}
          >
            {presets.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`overview.preset.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          {preset === 'custom' && (
            <>
              <TextField
                type="date"
                size="small"
                label={t('book.filter.from')}
                value={customFrom}
                onChange={(event) => setCustomFrom(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <TextField
                type="date"
                size="small"
                label={t('book.filter.to')}
                value={customTo}
                onChange={(event) => setCustomTo(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </>
          )}
          <Typography variant="body2" color="text.secondary" sx={{ alignSelf: 'center' }}>
            {formatDate(period.from)} – {formatDate(period.to)}
          </Typography>
        </Stack>
      </Paper>

      {overview.error && <Alert severity="error">{toApiError(overview.error).message}</Alert>}

      {data && data.openMonths > 0 && (
        <Alert severity="info">{t('overview.openMonths', { count: data.openMonths })}</Alert>
      )}

      {data && !visible && <Alert severity="info">{t('overview.statisticsOnly')}</Alert>}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Card label={t('overview.income')} value={money(data?.income ?? 0)} />
        <Card label={t('overview.outflow')} value={money(data?.outflow ?? 0)} />
        <Card
          label={t('overview.profit')}
          value={money(data?.profit ?? 0)}
          color={(data?.profit ?? 0) < 0 ? 'error.main' : 'success.main'}
          note={
            visible && data
              ? t('overview.vsPrevious', {
                  amount: `${delta >= 0 ? '+' : '−'} ${formatMoney(Math.abs(delta), locale)}`,
                })
              : undefined
          }
        />
        <Card
          label={t('overview.margin')}
          value={data?.marginPercent != null ? `${data.marginPercent.toLocaleString(locale)} %` : '—'}
          note={
            data?.profitPerHour != null
              ? t('overview.perHour', { amount: formatMoney(data.profitPerHour, locale) })
              : undefined
          }
        />
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('overview.steps')}
        </Typography>
        <Stack spacing={1.25}>
          {(data?.steps ?? [])
            .filter((step) => step.key === 'Profit' || step.amount !== 0 || !visible)
            .map((step) => (
              <Box key={step.key}>
                <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                  <Typography variant="body2" sx={{ fontWeight: step.key === 'Profit' ? 700 : 400 }}>
                    {t(`overview.step.${step.key}` as MessageKey)}
                  </Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
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

      {visible && (data?.months.length ?? 0) > 1 && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
            {t('overview.byMonth')}
          </Typography>
          <Box ref={chartSize.ref} sx={{ height: 260, width: '100%' }}>
            {chartSize.width > 0 && (
              <BarChart
                width={chartSize.width}
                height={260}
                colors={chartPalette}
                series={[
                  { data: (data?.months ?? []).map((m) => m.income), label: t('overview.income') },
                  { data: (data?.months ?? []).map((m) => m.outflow), label: t('overview.outflow') },
                  { data: (data?.months ?? []).map((m) => m.profit), label: t('overview.profit') },
                ]}
                xAxis={[{ scaleType: 'band', data: monthLabels }]}
                margin={{ top: 10, bottom: 30, left: 64, right: 10 }}
              />
            )}
          </Box>
          <Table size="small" sx={{ mt: 1 }}>
            <TableHead>
              <TableRow>
                <TableCell>{t('overview.col.month')}</TableCell>
                <TableCell align="right">{t('overview.income')}</TableCell>
                <TableCell align="right">{t('overview.outflow')}</TableCell>
                <TableCell align="right">{t('overview.profit')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(data?.months ?? []).map((m, index) => (
                <TableRow key={`${m.year}-${m.month}`}>
                  <TableCell>{monthLabels[index]}</TableCell>
                  <TableCell align="right">{formatMoney(m.income, locale)}</TableCell>
                  <TableCell align="right">{formatMoney(m.outflow, locale)}</TableCell>
                  <TableCell align="right">{formatMoney(m.profit, locale)}</TableCell>
                  <TableCell>
                    {m.locked ? (
                      <Chip size="small" variant="outlined" icon={<LockOutlined />} label={t('overview.closed')} />
                    ) : m.hasSettlement ? (
                      <Chip size="small" color="warning" variant="outlined" label={t('overview.openMonth')} />
                    ) : (
                      <Chip size="small" variant="outlined" label={t('overview.noSettlement')} />
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', mb: 1 }}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('overview.cashflow')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('overview.cashflowHelp')}
            </Typography>
          </Box>
          <TextField
            size="small"
            label={t('overview.openingBalance')}
            value={openingBalance}
            onChange={(event) => setOpeningBalance(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            sx={{ maxWidth: 200 }}
          />
        </Stack>

        {cashflow.data?.belowZero && (
          <Alert severity="warning" sx={{ mb: 1 }}>
            {t('overview.belowZero', {
              amount: formatMoney(cashflow.data.lowestBalance, locale),
              date: formatDate(cashflow.data.lowestDate),
            })}
          </Alert>
        )}

        {cashflow.data && (
          <>
            <Stack direction="row" spacing={3} useFlexGap sx={{ flexWrap: 'wrap', mb: 1 }}>
              <Typography variant="body2">
                {t('overview.cashIn')}: <b>{formatMoney(cashflow.data.totalInflow, locale)}</b>
              </Typography>
              <Typography variant="body2">
                {t('overview.cashOut')}: <b>{formatMoney(cashflow.data.totalOutflow, locale)}</b>
              </Typography>
              <Typography variant="body2">
                {t('overview.closingBalance')}: <b>{formatMoney(cashflow.data.closingBalance, locale)}</b>
              </Typography>
            </Stack>
            <Box sx={{ height: 220, width: '100%' }}>
              <CashflowChart series={cashflow.data.series} />
            </Box>
          </>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('overview.forecast')}
        </Typography>
        {forecast.data?.hasData ? (
          <Stack spacing={0.5} sx={{ mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              {t('overview.forecastHelp', {
                months: forecast.data.monthsUsed,
                hours: Math.round(forecast.data.hours),
              })}
            </Typography>
            <Typography variant="h6">
              {formatMoney(forecast.data.profitLow, locale)} – {formatMoney(forecast.data.profitHigh, locale)}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('overview.forecastCentral', { amount: formatMoney(forecast.data.profit, locale) })}
            </Typography>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {t('overview.forecastNone')}
          </Typography>
        )}
      </Paper>
    </Stack>
  );
}

function Card({
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

function CashflowChart({ series }: { series: { date: string; balance: number }[] }) {
  const size = useElementSize<HTMLDivElement>();

  return (
    <Box ref={size.ref} sx={{ height: '100%', width: '100%' }}>
      {size.width > 0 && (
        <LineChart
          width={size.width}
          height={220}
          colors={chartPalette}
          series={[{ data: series.map((day) => day.balance), showMark: false, area: true }]}
          xAxis={[{ scaleType: 'point', data: series.map((day) => day.date.slice(5)) }]}
          margin={{ top: 10, bottom: 30, left: 64, right: 10 }}
          hideLegend
        />
      )}
    </Box>
  );
}
