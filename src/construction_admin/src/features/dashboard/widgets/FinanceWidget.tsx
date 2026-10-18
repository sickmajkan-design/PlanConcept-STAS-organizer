import { AccountBalanceWalletOutlined, TrendingDownOutlined, TrendingUpOutlined } from '@mui/icons-material';
import { Alert, Box, Skeleton, Stack, Typography } from '@mui/material';
import { useQueries, useQuery } from '@tanstack/react-query';
import { useRef, useState, type KeyboardEvent, type ReactNode } from 'react';

import { costsApi } from '../../../api/costs';
import { financeApi } from '../../../api/finance';
import { canViewFinance } from '../../../auth/authHelpers';
import { useAuth } from '../../../auth/useAuth';
import type { MessageKey } from '../../../i18n/en';
import { useI18n } from '../../../i18n/useI18n';
import { chartPalette } from '../../../theme';
import { formatMoney } from '../../../utils/formatting';
import { useBranchFilter } from '../../branches/BranchContext';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod, monthWindow, percentChange } from '../../finance/periods';
import { useFinanceBreakdown, useFinanceSeries } from '../../finance/useFinanceSeries';
import { useAllProjectsQuery } from '../../projects/useProjects';
import { useMonthToDateCost } from '../dashboardData';
import type { DashboardWidgetProps } from '../widgetTypes';
import { StatTile } from './StatTile';
import { ProjectSettingsDialog } from './WidgetSettingsDialog';
import { WidgetShell } from './WidgetShell';
import { Donut, TrendChart } from './widgetCharts';
import { BigNumber, EmptyNote, MiniStat, Pill, Segments, StatGrid } from './widgetUi';

type Tab = 'overview' | 'trend' | 'structure';
type TrendKind = 'months' | 'period';

const TABS: Tab[] = ['overview', 'trend', 'structure'];

const TAB_LABEL: Record<Tab, MessageKey> = {
  overview: 'dashboard.finance.tab.overview',
  trend: 'dashboard.finance.tab.trend',
  structure: 'dashboard.finance.tab.structure',
};

const KIND_LABEL: Record<string, MessageKey> = {
  Labour: 'dashboard.projectsRealization.labour',
  ManualPay: 'dashboard.projectsRealization.manualPay',
  Material: 'dashboard.projectsRealization.material',
  GeneralExpenses: 'dashboard.projectsRealization.generalExpenses',
  Accommodation: 'dashboard.projectsRealization.accommodation',
  Vehicles: 'dashboard.projectsRealization.vehicles',
  Tools: 'dashboard.projectsRealization.tools',
};

const MONTHS_SHOWN = 6;

function isTab(value: string | undefined): value is Tab {
  return TABS.includes(value as Tab);
}

/**
 * The company's money in one card: how income, spending and profit stand against the period before (Overview),
 * how spending moved over time (Trend) and what it went on (Structure). It replaces five cards that said the same
 * figure five ways. An account that may see percentages but not amounts gets the percentages, with no tabs,
 * since amounts are all the other two would show.
 *
 * Which tab was last open is kept in the widget's own settings, so it opens where it was left.
 */
export function FinanceWidget({ settings, onSettingsChange, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t } = useI18n();
  const { user } = useAuth();
  const { period } = useFinancePeriod();
  const projects = useAllProjectsQuery();
  const [configuring, setConfiguring] = useState(false);

  const full = canViewFinance(user);
  const tab: Tab = full && isTab(settings?.tab) ? settings.tab : 'overview';
  const trend: TrendKind = settings?.trend === 'period' ? 'period' : 'months';
  const projectId = settings?.projectId ?? '';

  /** Settings are one set for the widget: every change keeps what the others chose. */
  const change = (next: Record<string, string>) => {
    const merged = { ...settings, ...next };
    for (const key of Object.keys(merged)) {
      if (merged[key] === '') delete merged[key];
    }
    onSettingsChange?.(merged);
  };

  const projectName = projectId ? projects.data?.items.find((p) => p.id === projectId)?.name : undefined;
  const periodText = formatPeriod(period.from, period.to);
  const subtitle =
    tab === 'trend' && trend === 'months'
      ? t('dashboard.costTrend.totalCost')
      : tab === 'structure'
        ? [projectName, periodText].filter(Boolean).join(' · ')
        : periodText;

  return (
    <WidgetShell
      title={t('dashboard.widget.Finance')}
      subtitle={subtitle}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      onConfigure={tab === 'structure' && onSettingsChange ? () => setConfiguring(true) : undefined}
    >
      {full && <Tabs tab={tab} onChange={(next) => change({ tab: next })} />}

      {!full ? (
        <StatisticsPanel />
      ) : tab === 'overview' ? (
        <OverviewPanel />
      ) : tab === 'trend' ? (
        <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
          <TrendKinds value={trend} onChange={(next) => change({ trend: next })} />
          {trend === 'months' ? <MonthsTrendPanel /> : <PeriodTrendPanel />}
        </Stack>
      ) : (
        <StructurePanel projectId={projectId} />
      )}

      <ProjectSettingsDialog
        open={configuring}
        title={t('dashboard.widget.Finance')}
        projectId={projectId}
        allowNone
        onClose={() => setConfiguring(false)}
        onSave={(next) => {
          setConfiguring(false);
          change({ projectId: next });
        }}
      />
    </WidgetShell>
  );
}

/** A segmented control: the one that is open is the raised one. Arrow keys move between them, as in any tab list. */
function Tabs({ tab, onChange }: { tab: Tab; onChange: (tab: Tab) => void }) {
  const t = useI18n().t;
  const refs = useRef<Record<string, HTMLButtonElement | null>>({});

  const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    if (step === 0) return;

    event.preventDefault();
    const next = TABS[(TABS.indexOf(tab) + step + TABS.length) % TABS.length]!;
    onChange(next);
    refs.current[next]?.focus();
  };

  return (
    <Box
      role="tablist"
      aria-label={t('dashboard.widget.Finance')}
      onKeyDown={onKeyDown}
      sx={(theme) => ({
        display: 'inline-flex',
        alignSelf: 'flex-start',
        gap: 0.5,
        p: 0.5,
        mb: 1.5,
        borderRadius: '12px',
        flexShrink: 0,
        bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.06)' : 'rgba(31,27,22,0.06)',
      })}
    >
      {TABS.map((id) => {
        const selected = id === tab;

        return (
          <Box
            key={id}
            component="button"
            type="button"
            role="tab"
            id={`finance-tab-${id}`}
            aria-selected={selected}
            tabIndex={selected ? 0 : -1}
            ref={(node: HTMLButtonElement | null) => {
              refs.current[id] = node;
            }}
            onClick={() => onChange(id)}
            sx={(theme) => ({
              font: 'inherit',
              fontSize: '0.8125rem',
              fontWeight: selected ? 600 : 500,
              px: 1.75,
              py: 0.75,
              border: 0,
              borderRadius: '9px',
              cursor: 'pointer',
              color: selected ? theme.palette.text.primary : theme.palette.text.secondary,
              bgcolor: selected ? theme.palette.background.paper : 'transparent',
              boxShadow: selected ? '0 1px 2px rgba(31,27,22,0.14)' : 'none',
              // Colour and shadow only, and quickly: it is pressed many times in a visit.
              transition: 'background-color 0.15s ease-out, box-shadow 0.15s ease-out, color 0.15s ease-out',
              '&:focus-visible': { outline: `2px solid ${theme.palette.primary.main}`, outlineOffset: 2 },
              '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
            })}
          >
            {t(TAB_LABEL[id])}
          </Box>
        );
      })}
    </Box>
  );
}

/** Which of the two trends is drawn: the last six months, or the board's own period. */
function TrendKinds({ value, onChange }: { value: TrendKind; onChange: (value: TrendKind) => void }) {
  const t = useI18n().t;

  return (
    <Stack direction="row" role="group" aria-label={t('dashboard.finance.tab.trend')} sx={{ gap: 1, flexShrink: 0 }}>
      {(['months', 'period'] as const).map((kind) => {
        const selected = kind === value;

        return (
          <Box
            key={kind}
            component="button"
            type="button"
            aria-pressed={selected}
            onClick={() => onChange(kind)}
            sx={(theme) => ({
              font: 'inherit',
              fontSize: '0.8rem',
              fontWeight: selected ? 600 : 500,
              px: 1.5,
              py: 0.5,
              border: 0,
              borderRadius: 999,
              cursor: 'pointer',
              color: selected
                ? theme.palette.mode === 'dark'
                  ? theme.palette.primary.light
                  : theme.palette.primary.dark
                : theme.palette.text.secondary,
              bgcolor: selected
                ? theme.palette.mode === 'dark'
                  ? 'rgba(255, 138, 61, 0.16)'
                  : 'rgba(230, 81, 0, 0.1)'
                : theme.palette.mode === 'dark'
                  ? 'rgba(255,255,255,0.06)'
                  : 'rgba(31,27,22,0.06)',
              transition: 'background-color 0.15s ease-out, color 0.15s ease-out',
              '&:focus-visible': { outline: `2px solid ${theme.palette.primary.main}`, outlineOffset: 2 },
              '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
            })}
          >
            {t(kind === 'months' ? 'dashboard.finance.trend.months' : 'dashboard.finance.trend.period')}
          </Box>
        );
      })}
    </Stack>
  );
}

/** What a panel shows while it waits, or when its request failed: the same in every tab. */
function PanelState({ isLoading, error, children }: { isLoading: boolean; error: unknown; children: ReactNode }) {
  const t = useI18n().t;

  if (isLoading) {
    return (
      <Stack
        spacing={1.5}
        aria-busy="true"
        sx={{ flex: 1, '& .MuiSkeleton-root': { '@media (prefers-reduced-motion: reduce)': { animation: 'none' } } }}
      >
        <Skeleton variant="rounded" height={22} width="45%" />
        <Skeleton variant="rounded" height={64} />
        <Skeleton variant="rounded" height={64} />
      </Stack>
    );
  }

  if (error) {
    return <Alert severity="error">{t('common.somethingWentWrong')}</Alert>;
  }

  return <>{children}</>;
}

type Hint = { text: string; color: 'success.main' | 'error.main' | 'text.secondary' };

/**
 * Income, spending and profit for the board's period, each against the period before it. `higherIsBetter` is what
 * turns a rise into green or red: more income is good news, more spending is not.
 */
function OverviewPanel() {
  const { t, locale } = useI18n();
  const { data, isLoading, error } = useFinanceSeries();

  const hint = (current: number, previous: number, higherIsBetter: boolean): Hint => {
    const delta = percentChange(current, previous);
    if (delta === null) return { text: `— ${t('finance.vsPrevious')}`, color: 'text.secondary' };
    const rounded = Math.round(delta * 10) / 10;
    const arrow = rounded > 0 ? '▲' : rounded < 0 ? '▼' : '=';
    const good = rounded === 0 ? null : rounded > 0 === higherIsBetter;
    return {
      text: `${arrow} ${Math.abs(rounded).toFixed(1)}% ${t('finance.vsPrevious')}`,
      color: good === null ? 'text.secondary' : good ? 'success.main' : 'error.main',
    };
  };

  const totals = data?.totals;
  const previous = data?.previous;
  const isEmpty = !!totals && totals.revenue === 0 && totals.expense === 0;

  return (
    <PanelState isLoading={isLoading} error={error}>
      {isEmpty ? (
        <EmptyNote>{t('finance.noData')}</EmptyNote>
      ) : (
        totals &&
        previous && (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, flex: 1, alignContent: 'flex-start' }}>
            <StatTile
              icon={<TrendingUpOutlined fontSize="small" />}
              label={t('finance.revenue')}
              value={formatMoney(totals.revenue, locale)}
              {...toTileHint(hint(totals.revenue, previous.revenue, true))}
              accent="#00897b"
            />
            <StatTile
              icon={<TrendingDownOutlined fontSize="small" />}
              label={t('finance.expense')}
              value={formatMoney(totals.expense, locale)}
              {...toTileHint(hint(totals.expense, previous.expense, false))}
              accent="#e65100"
            />
            <StatTile
              icon={<AccountBalanceWalletOutlined fontSize="small" />}
              label={t('finance.profit')}
              value={formatMoney(totals.profit, locale)}
              {...toTileHint(hint(totals.profit, previous.profit, true))}
              accent={totals.profit < 0 ? '#c62828' : '#37474f'}
            />
            {totals.marginPercent !== null && (
              <Pill sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
                {t('finance.margin')}: {totals.marginPercent.toFixed(1)}%
              </Pill>
            )}
          </Box>
        )
      )}
    </PanelState>
  );
}

function toTileHint(hint: Hint) {
  return { hint: hint.text, hintColor: hint.color };
}

/**
 * How income, spending and profit moved against the period before, and what the spending was on, in percent with
 * no amount anywhere. This is what an account that may see statistics but not money gets.
 */
function StatisticsPanel() {
  const { t } = useI18n();
  const { period } = useFinancePeriod();
  const { branchId, basis } = useBranchFilter();
  const people = branchId && basis === 'Employer' ? 'Employer' : undefined;
  const { data, isLoading, error } = useQuery({
    queryKey: ['finance', 'statistics', period.from, period.to, branchId ?? null, people ?? null] as const,
    queryFn: () => financeApi.statistics({ from: period.from, to: period.to, branchId, basis: people }),
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  });

  /** Higher is better for income and profit, worse for spending. */
  const delta = (label: string, value: number | null | undefined, higherIsBetter: boolean) => {
    const rounded = value == null ? null : Math.round(value * 10) / 10;
    const good = rounded === null || rounded === 0 ? null : rounded > 0 === higherIsBetter;

    return (
      <MiniStat
        key={label}
        label={label}
        tone={good === null ? 'neutral' : good ? 'good' : 'bad'}
        value={rounded === null ? '—' : `${rounded > 0 ? '▲' : rounded < 0 ? '▼' : '='} ${Math.abs(rounded).toFixed(1)}%`}
      />
    );
  };

  return (
    <PanelState isLoading={isLoading} error={error}>
      {data && (
        <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <Typography variant="caption" color="text.secondary">
            {t('finance.statistics.noAmounts')}
          </Typography>

          <Stack spacing={1}>
            <Typography variant="body2" color="text.secondary">
              {t('finance.statistics.change')}
            </Typography>
            <StatGrid min={110}>
              {delta(t('finance.revenue'), data.revenueChangePercent, true)}
              {delta(t('finance.expense'), data.expenseChangePercent, false)}
              {delta(t('finance.profit'), data.profitChangePercent, true)}
            </StatGrid>
          </Stack>

          <Stack spacing={1}>
            <Typography variant="body2" color="text.secondary">
              {t('finance.statistics.structure')}
            </Typography>
            {data.shares.length === 0 ? (
              <EmptyNote>{t('finance.noData')}</EmptyNote>
            ) : (
              <Segments
                parts={data.shares.map((share, index) => ({
                  label: t(KIND_LABEL[share.kind] ?? 'finance.expense'),
                  value: share.sharePercent,
                  color: chartPalette[index % chartPalette.length]!,
                  display: `${share.sharePercent.toFixed(1)}%`,
                }))}
              />
            )}
          </Stack>
        </Stack>
      )}
    </PanelState>
  );
}

/**
 * There is no multi-period cost report endpoint — the company report only answers for one `from`/`to` window at a
 * time — so the trend is assembled from `MONTHS_SHOWN` parallel calls to that single-period endpoint, one per month.
 */
function MonthsTrendPanel() {
  const { t, locale } = useI18n();
  const { branchId, basis } = useBranchFilter();
  const people = branchId && basis === 'Employer' ? 'Employer' : undefined;

  const months = Array.from({ length: MONTHS_SHOWN }, (_, i) => monthWindow(MONTHS_SHOWN - 1 - i));

  // The figure and the pill are this month so far against the same days of last month, the same as the key
  // figures; the bars are whole months.
  const cost = useMonthToDateCost(true);

  const results = useQueries({
    queries: months.map((month) => ({
      queryKey: ['dashboard', 'company-cost-trend', month.from, month.to, branchId ?? null, people ?? null] as const,
      queryFn: () => costsApi.companyReport({ from: month.from, to: month.to, branchId, basis: people }),
    })),
  });

  const isLoading = results.some((r) => r.isLoading) || cost.thisMonth.isLoading;
  const error = results.find((r) => r.error)?.error ?? cost.thisMonth.error;

  const totals = results.map((r) => r.data?.total ?? 0);
  const monthLabels = months.map((m) => m.label.toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', { month: 'short' }));

  // Nothing spent in any of the months shown: there is no trend to draw.
  const hasCosts = totals.some((total) => total > 0);

  const latest = cost.thisMonth.data?.total ?? 0;
  const delta = cost.changePercent;

  return (
    <PanelState isLoading={isLoading} error={error}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        {hasCosts && (
          <Stack direction="row" sx={{ alignItems: 'flex-end', gap: 1.5, flexWrap: 'wrap', flexShrink: 0 }}>
            <BigNumber value={formatMoney(latest, locale)} />
            {delta !== null && (
              // More spending is not good news, so a rise is amber-red and a fall is green.
              <Pill tone={delta > 0 ? 'bad' : delta < 0 ? 'good' : 'neutral'}>
                {delta > 0 ? '+' : ''}
                {delta.toFixed(1)}% {t('dashboard.costTrend.vsSameDays')}
              </Pill>
            )}
          </Stack>
        )}

        {!hasCosts ? (
          <EmptyNote>{t('dashboard.costTrend.empty')}</EmptyNote>
        ) : (
          <TrendChart
            points={totals.map((value, i) => ({ label: monthLabels[i] ?? '', value }))}
            format={(value) => formatMoney(value, locale)}
            minHeight={150}
          />
        )}
      </Stack>
    </PanelState>
  );
}

/** "24.09" for a day, "24.09–30.09" for a week, "09.2026" for a month — as short as the point allows. */
function pointLabel(from: string, to: string, granularity: string): string {
  const [year, month, day] = from.split('-');
  if (granularity === 'Month') return `${month}.${year}`;
  if (granularity === 'Day' || from === to) return `${day}.${month}`;
  const [, toMonth, toDay] = to.split('-');
  return `${day}.${month}–${toDay}.${toMonth}`;
}

/**
 * Spending across the board's period, day by day, week by week or month by month, against its own average: "are we
 * spending more than usual". The average is the period's total over its number of points.
 */
function PeriodTrendPanel() {
  const { t, locale } = useI18n();
  const { data, isLoading, error } = useFinanceSeries();

  const buckets = data?.buckets ?? [];
  const isEmpty = !!data && data.totals.expense === 0;
  const average = buckets.length > 0 ? (data?.totals.expense ?? 0) / buckets.length : 0;
  const highest = buckets.reduce<(typeof buckets)[number] | null>(
    (top, bucket) => (top === null || bucket.expense > top.expense ? bucket : top),
    null,
  );

  return (
    <PanelState isLoading={isLoading} error={error}>
      {isEmpty ? (
        <EmptyNote>{t('finance.noData')}</EmptyNote>
      ) : (
        data && (
          <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
            <BigNumber value={formatMoney(data.totals.expense, locale)} caption={t('finance.expense')} />
            <TrendChart
              points={buckets.map((b) => ({ label: pointLabel(b.from, b.to, data.granularity), value: b.expense }))}
              format={(value) => formatMoney(value, locale)}
              reference={average}
              referenceLabel={t('finance.average')}
              minHeight={170}
            />
            <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, flexShrink: 0 }}>
              <Pill>
                {t('finance.average')}: {formatMoney(average, locale)}
              </Pill>
              {highest && highest.expense > 0 && (
                <Pill tone="warn">
                  {t('finance.highest')}: {formatMoney(highest.expense, locale)} ({pointLabel(highest.from, highest.to, data.granularity)})
                </Pill>
              )}
            </Stack>
          </Stack>
        )
      )}
    </PanelState>
  );
}

/**
 * What the spending was on: the whole company's, or, with a project chosen in the widget's settings, that one
 * project's. The pieces are the report's own kinds and add up to the total shown above the chart.
 */
function StructurePanel({ projectId }: { projectId: string }) {
  const { t, locale } = useI18n();
  const { data, isLoading, error } = useFinanceBreakdown(projectId || undefined);

  const slices = (data?.items ?? [])
    .filter((item) => item.amount > 0)
    .map((item) => ({ id: item.kind, label: t(KIND_LABEL[item.kind] ?? 'finance.expense'), value: item.amount }));

  return (
    <PanelState isLoading={isLoading} error={error}>
      {slices.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('finance.noData')}
        </Typography>
      ) : (
        <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <BigNumber value={formatMoney(data?.total ?? 0, locale)} />
          {slices.length === 1 ? (
            // One kind is not a composition: a full ring says nothing a number does not say better.
            <Typography variant="body2">
              {slices[0]!.label}: <strong>{formatMoney(slices[0]!.value, locale)}</strong>
            </Typography>
          ) : (
            <Donut
              parts={slices.map((slice, index) => ({
                label: slice.label,
                value: slice.value,
                color: chartPalette[index % chartPalette.length]!,
                display: formatMoney(slice.value, locale),
              }))}
            />
          )}
        </Stack>
      )}
    </PanelState>
  );
}
