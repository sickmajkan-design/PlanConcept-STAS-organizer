import { ChevronRightOutlined } from '@mui/icons-material';
import { Box, Chip, Paper, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';

import { exportsApi } from '../../api/exports';
import type {
  ToolCostRow,
  ToolExpense,
  VehicleCostRow,
  VehicleExpense,
} from '../../api/types';
import {
  CompositionBar,
  CompositionLegend,
  Stat,
  numeric,
  useCostColors,
  type Segment,
} from '../../components/costs/costUi';
import { CostsLoading } from '../../components/costs/CostsLoading';
import { SortBar, type SortDirection } from '../../components/costs/SortBar';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { ExportButton } from '../../components/ExportButton';
import { toApiError } from '../../api/apiError';
import {
  useToolCostReport,
  useToolExpensesQuery,
  useVehicleCostReport,
  useVehicleExpensesQuery,
} from '../../features/costs/useCosts';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney, formatQuantity } from '../../utils/formatting';
import { AssetCostDialog, type AssetSection } from './AssetCostDialog';
import type { Period } from './monthWindow';

// ---- shared card ---------------------------------------------------------

function AssetCard({
  name,
  total,
  segments,
  chips,
  onOpen,
  format,
}: {
  name: string;
  total: string;
  segments: Segment[];
  chips?: React.ReactNode;
  onOpen: () => void;
  format: (value: number) => string;
}) {
  return (
    <Paper
      variant="outlined"
      role="button"
      tabIndex={0}
      onClick={onOpen}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onOpen();
        }
      }}
      sx={{
        p: 2,
        cursor: 'pointer',
        transition: 'background-color 120ms, border-color 120ms',
        '&:hover, &:focus-visible': { bgcolor: 'action.hover', borderColor: 'text.disabled', outline: 'none' },
        '&:active': { bgcolor: 'action.selected' },
      }}
    >
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }} noWrap>
            {name}
          </Typography>
          {chips && (
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mt: 0.5 }}>
              {chips}
            </Stack>
          )}
        </Box>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <Typography variant="h6" sx={{ fontWeight: 800, letterSpacing: -0.2, ...numeric }}>
            {total}
          </Typography>
          <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled' }} />
        </Stack>
      </Stack>
      <Box sx={{ mt: 1.5 }}>
        <CompositionBar segments={segments} />
      </Box>
      <Box sx={{ mt: 1.25 }}>
        <CompositionLegend segments={segments} format={format} />
      </Box>
    </Paper>
  );
}

function ProfitChip({ revenue, profit, format }: { revenue: number; profit: number; format: (v: number) => string }) {
  const t = useT();

  if (revenue <= 0) return null;

  return (
    <>
      <Chip size="small" variant="outlined" label={`${t('costs.revenue')} ${format(revenue)}`} />
      <Chip
        size="small"
        variant="outlined"
        color={profit >= 0 ? 'success' : 'error'}
        label={`${t('costs.profit')} ${format(profit)}`}
      />
    </>
  );
}

// ---- vehicles ------------------------------------------------------------

type VehicleField = 'total' | 'vehicleName' | 'fuelCost' | 'litres' | 'litresPer100Km' | 'serviceCost' | 'otherCost' | 'rentalCost' | 'revenue' | 'profit';

export function VehicleCostBoard({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const colors = useCostColors();
  const { data, isLoading, isError, error, refetch } = useVehicleCostReport(period);
  const [sortBy, setSortBy] = useState<VehicleField>('total');
  const [direction, setDirection] = useState<SortDirection>('desc');
  const [open, setOpen] = useState<VehicleCostRow | null>(null);
  const money = (value: number) => formatMoney(value, locale);

  const rows = useMemo(() => {
    if (!data) return [];
    const factor = direction === 'asc' ? 1 : -1;
    return [...data.rows].sort((a, b) =>
      sortBy === 'vehicleName'
        ? a.vehicleName.localeCompare(b.vehicleName) * factor
        : ((a[sortBy] ?? -1) - (b[sortBy] ?? -1)) * factor,
    );
  }, [data, sortBy, direction]);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;
  if (isLoading) return <CostsLoading />;
  if (!data || data.rows.length === 0) return <EmptyState message={t('costs.empty')} />;

  const segmentsFor = (r: VehicleCostRow): Segment[] => [
    { key: 'fuel', label: t('costs.fuel'), value: r.fuelCost, color: colors.fuel },
    { key: 'service', label: t('costs.service'), value: r.serviceCost, color: colors.service },
    { key: 'other', label: t('costs.other'), value: r.otherCost, color: colors.other },
    { key: 'rental', label: t('costs.rental'), value: r.rentalCost, color: colors.rental },
  ];

  const overall = segmentsFor({
    fuelCost: data.totalFuelCost,
    serviceCost: data.rows.reduce((s, r) => s + r.serviceCost, 0),
    otherCost: data.rows.reduce((s, r) => s + r.otherCost, 0),
    rentalCost: data.totalRentalCost,
  } as VehicleCostRow);

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 2, alignItems: 'flex-start', mb: 2 }}>
          <Stat label={t('costs.grandTotal')} value={money(data.total)} />
          <Stat label={t('costs.litres')} value={formatQuantity(data.totalLitres, locale)} accent="text.disabled" />
          {data.totalRevenue > 0 && <Stat label={t('costs.revenue')} value={money(data.totalRevenue)} accent="success.main" />}
          {data.totalRevenue > 0 && (
            <Stat
              label={t('costs.profit')}
              value={money(data.totalProfit)}
              accent={data.totalProfit >= 0 ? 'success.main' : 'error.main'}
            />
          )}
          <Box sx={{ flex: 1 }} />
          <ExportButton onExport={(language) => exportsApi.vehicleCosts({ ...period, language })} />
        </Stack>
        <Stack spacing={1.25}>
          <CompositionBar segments={overall} height={12} />
          <CompositionLegend segments={overall} format={money} showShare />
        </Stack>
      </Paper>

      <SortBar
        value={sortBy}
        direction={direction}
        onChange={(field, dir) => {
          setSortBy(field);
          setDirection(dir);
        }}
        options={[
          { value: 'total', label: t('costs.total') },
          { value: 'vehicleName', label: t('costs.vehicle') },
          { value: 'fuelCost', label: t('costs.fuel') },
          { value: 'litres', label: t('costs.litres') },
          { value: 'litresPer100Km', label: t('costs.consumption') },
          { value: 'serviceCost', label: t('costs.service') },
          { value: 'otherCost', label: t('costs.other') },
          { value: 'rentalCost', label: t('costs.rental') },
          { value: 'revenue', label: t('costs.revenue') },
          { value: 'profit', label: t('costs.profit') },
        ]}
      />

      <Stack spacing={1.5}>
        {rows.map((row) => (
          <AssetCard
            key={row.vehicleId}
            name={row.vehicleName}
            total={money(row.total)}
            segments={segmentsFor(row)}
            format={money}
            onOpen={() => setOpen(row)}
            chips={
              <>
                {row.litres > 0 && (
                  <Chip size="small" variant="outlined" label={`${formatQuantity(row.litres, locale)} L`} />
                )}
                {row.litresPer100Km !== null && (
                  <Chip
                    size="small"
                    variant="outlined"
                    label={`${formatQuantity(row.litresPer100Km, locale)} ${t('costs.consumption')}`}
                  />
                )}
                <ProfitChip revenue={row.revenue} profit={row.profit} format={money} />
              </>
            }
          />
        ))}
      </Stack>

      {open && <VehicleCostDialog row={open} period={period} onClose={() => setOpen(null)} />}
    </Stack>
  );
}

const VEHICLE_GROUPS: { key: 'fuel' | 'service' | 'other'; kinds: string[] }[] = [
  { key: 'fuel', kinds: ['Fuel'] },
  { key: 'service', kinds: ['Service', 'Repair'] },
  { key: 'other', kinds: ['Insurance', 'Registration', 'Other'] },
];

function VehicleCostDialog({
  row,
  period,
  onClose,
}: {
  row: VehicleCostRow;
  period: Period;
  onClose: () => void;
}) {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const colors = useCostColors();
  const money = (value: number) => formatMoney(value, locale);

  const query = useMemo(
    () => ({
      pageNumber: 1,
      pageSize: 200,
      vehicleId: row.vehicleId,
      from: period.from,
      to: period.to,
      sortBy: 'occurredOn',
      sortDescending: true,
    }),
    [row.vehicleId, period],
  );

  const expenses = useVehicleExpensesQuery(query);
  const items: VehicleExpense[] = expenses.data?.items ?? [];

  const line = (e: VehicleExpense) => ({
    id: e.id,
    primary: `${enumLabel('vehicleExpenseKind', e.kind)}${e.fuelProductType ? ` · ${e.fuelProductType}` : ''}`,
    secondary: [
      formatDate(e.occurredOn),
      e.supplier,
      e.odometerKm !== null ? `${formatQuantity(e.odometerKm, locale)} km` : null,
      e.note,
    ]
      .filter(Boolean)
      .join(' · '),
    amount: money(e.amount),
    meta:
      e.litres !== null
        ? `${formatQuantity(e.litres, locale)} L${e.pricePerLitre !== null ? ` × ${money(e.pricePerLitre)}` : ''}`
        : undefined,
  });

  const approved = items.filter((e) => e.status === 'Approved');
  const notCounted = items.filter((e) => e.status !== 'Approved');
  const groupTotal = (kinds: string[]) => approved.filter((e) => kinds.includes(e.kind)).reduce((s, e) => s + e.amount, 0);

  const segments: Segment[] = [
    { key: 'fuel', label: t('costs.fuel'), value: row.fuelCost, color: colors.fuel },
    { key: 'service', label: t('costs.service'), value: row.serviceCost, color: colors.service },
    { key: 'other', label: t('costs.other'), value: row.otherCost, color: colors.other },
    { key: 'rental', label: t('costs.rental'), value: row.rentalCost, color: colors.rental },
  ];

  const sections: AssetSection[] = [
    ...VEHICLE_GROUPS.map((g) => ({
      key: g.key,
      title: t(`costs.${g.key}`),
      color: colors[g.key === 'fuel' ? 'fuel' : g.key === 'service' ? 'service' : 'other'],
      total: money(groupTotal(g.kinds)),
      share: row.total > 0 ? (groupTotal(g.kinds) / row.total) * 100 : 0,
      lines: approved.filter((e) => g.kinds.includes(e.kind)).map(line),
    })),
    ...(row.rentalCost > 0
      ? [
          {
            key: 'rental',
            title: t('costs.rental'),
            color: colors.rental,
            total: money(row.rentalCost),
            share: row.total > 0 ? (row.rentalCost / row.total) * 100 : 0,
            note: t('costs.rentalNote'),
            lines: [],
          },
        ]
      : []),
    ...(notCounted.length > 0
      ? [
          {
            key: 'pending',
            title: t('costs.notCounted'),
            note: t('costs.notCountedNote'),
            lines: notCounted.map((e) => ({
              ...line(e),
              secondary: `${enumLabel('vehicleExpenseStatus', e.status)} · ${line(e).secondary}`,
            })),
          },
        ]
      : []),
  ];

  return (
    <AssetCostDialog
      open
      onClose={onClose}
      period={period}
      title={row.vehicleName}
      linkTo={paths.vehicleDetail(row.vehicleId)}
      linkLabel={t('costs.openVehicle')}
      total={money(row.total)}
      segments={segments}
      formatMoneyValue={money}
      loading={expenses.isLoading}
      error={expenses.isError ? toApiError(expenses.error).message : null}
      tiles={[
        ...(row.litres > 0 ? [{ label: t('costs.litres'), value: `${formatQuantity(row.litres, locale)} L` }] : []),
        {
          label: t('costs.consumption'),
          value: row.litresPer100Km !== null ? formatQuantity(row.litresPer100Km, locale) : '—',
          hint: row.litresPer100Km === null ? t('costs.noConsumption') : undefined,
        },
        ...(row.distanceKm !== null
          ? [{ label: t('costs.distance'), value: `${formatQuantity(row.distanceKm, locale)} km` }]
          : []),
        ...(row.revenue > 0
          ? [
              { label: t('costs.revenue'), value: money(row.revenue) },
              { label: t('costs.profit'), value: money(row.profit), tone: row.profit < 0 ? ('warn' as const) : undefined },
            ]
          : []),
      ]}
      sections={sections}
    />
  );
}

// ---- tools ---------------------------------------------------------------

type ToolField = 'total' | 'toolName' | 'repairCost' | 'maintenanceCost' | 'otherCost' | 'rentalCost' | 'revenue' | 'profit';

export function ToolCostBoard({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const colors = useCostColors();
  const { data, isLoading, isError, error, refetch } = useToolCostReport(period);
  const [sortBy, setSortBy] = useState<ToolField>('total');
  const [direction, setDirection] = useState<SortDirection>('desc');
  const [open, setOpen] = useState<ToolCostRow | null>(null);
  const money = (value: number) => formatMoney(value, locale);

  const rows = useMemo(() => {
    if (!data) return [];
    const factor = direction === 'asc' ? 1 : -1;
    return [...data.rows].sort((a, b) =>
      sortBy === 'toolName' ? a.toolName.localeCompare(b.toolName) * factor : (a[sortBy] - b[sortBy]) * factor,
    );
  }, [data, sortBy, direction]);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;
  if (isLoading) return <CostsLoading />;
  if (!data || data.rows.length === 0) return <EmptyState message={t('costs.empty')} />;

  const segmentsFor = (r: ToolCostRow): Segment[] => [
    { key: 'repair', label: t('costs.repair'), value: r.repairCost, color: colors.fuel },
    { key: 'maintenance', label: t('costs.maintenance'), value: r.maintenanceCost, color: colors.service },
    { key: 'other', label: t('costs.other'), value: r.otherCost, color: colors.other },
    { key: 'rental', label: t('costs.rental'), value: r.rentalCost, color: colors.rental },
  ];

  const overall = segmentsFor({
    repairCost: data.rows.reduce((s, r) => s + r.repairCost, 0),
    maintenanceCost: data.rows.reduce((s, r) => s + r.maintenanceCost, 0),
    otherCost: data.rows.reduce((s, r) => s + r.otherCost, 0),
    rentalCost: data.totalRentalCost,
  } as ToolCostRow);

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 2, alignItems: 'flex-start', mb: 2 }}>
          <Stat label={t('costs.grandTotal')} value={money(data.total)} />
          {data.totalRevenue > 0 && <Stat label={t('costs.revenue')} value={money(data.totalRevenue)} accent="success.main" />}
          {data.totalRevenue > 0 && (
            <Stat
              label={t('costs.profit')}
              value={money(data.totalProfit)}
              accent={data.totalProfit >= 0 ? 'success.main' : 'error.main'}
            />
          )}
          <Box sx={{ flex: 1 }} />
          <ExportButton onExport={(language) => exportsApi.toolCosts({ ...period, language })} />
        </Stack>
        <Stack spacing={1.25}>
          <CompositionBar segments={overall} height={12} />
          <CompositionLegend segments={overall} format={money} showShare />
        </Stack>
      </Paper>

      <SortBar
        value={sortBy}
        direction={direction}
        onChange={(field, dir) => {
          setSortBy(field);
          setDirection(dir);
        }}
        options={[
          { value: 'total', label: t('costs.total') },
          { value: 'toolName', label: t('costs.tool') },
          { value: 'repairCost', label: t('costs.repair') },
          { value: 'maintenanceCost', label: t('costs.maintenance') },
          { value: 'otherCost', label: t('costs.other') },
          { value: 'rentalCost', label: t('costs.rental') },
          { value: 'revenue', label: t('costs.revenue') },
          { value: 'profit', label: t('costs.profit') },
        ]}
      />

      <Stack spacing={1.5}>
        {rows.map((row) => (
          <AssetCard
            key={row.toolId}
            name={row.toolName}
            total={money(row.total)}
            segments={segmentsFor(row)}
            format={money}
            onOpen={() => setOpen(row)}
            chips={<ProfitChip revenue={row.revenue} profit={row.profit} format={money} />}
          />
        ))}
      </Stack>

      {open && <ToolCostDialog row={open} period={period} onClose={() => setOpen(null)} />}
    </Stack>
  );
}

const TOOL_GROUPS: { key: 'repair' | 'maintenance' | 'other'; kinds: string[] }[] = [
  { key: 'repair', kinds: ['Repair'] },
  { key: 'maintenance', kinds: ['Maintenance', 'Calibration'] },
  { key: 'other', kinds: ['Other'] },
];

function ToolCostDialog({
  row,
  period,
  onClose,
}: {
  row: ToolCostRow;
  period: Period;
  onClose: () => void;
}) {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const colors = useCostColors();
  const money = (value: number) => formatMoney(value, locale);

  const query = useMemo(
    () => ({
      pageNumber: 1,
      pageSize: 200,
      toolId: row.toolId,
      from: period.from,
      to: period.to,
      sortBy: 'occurredOn',
      sortDescending: true,
    }),
    [row.toolId, period],
  );

  const expenses = useToolExpensesQuery(query);
  const items: ToolExpense[] = expenses.data?.items ?? [];

  const line = (e: ToolExpense) => ({
    id: e.id,
    primary: enumLabel('toolExpenseKind', e.kind),
    secondary: [formatDate(e.occurredOn), e.supplier, e.note].filter(Boolean).join(' · '),
    amount: money(e.amount),
  });

  const groupTotal = (kinds: string[]) => items.filter((e) => kinds.includes(e.kind)).reduce((s, e) => s + e.amount, 0);

  const segments: Segment[] = [
    { key: 'repair', label: t('costs.repair'), value: row.repairCost, color: colors.fuel },
    { key: 'maintenance', label: t('costs.maintenance'), value: row.maintenanceCost, color: colors.service },
    { key: 'other', label: t('costs.other'), value: row.otherCost, color: colors.other },
    { key: 'rental', label: t('costs.rental'), value: row.rentalCost, color: colors.rental },
  ];

  const sections: AssetSection[] = [
    ...TOOL_GROUPS.map((g) => ({
      key: g.key,
      title: t(`costs.${g.key}`),
      color: colors[g.key === 'repair' ? 'fuel' : g.key === 'maintenance' ? 'service' : 'other'],
      total: money(groupTotal(g.kinds)),
      share: row.total > 0 ? (groupTotal(g.kinds) / row.total) * 100 : 0,
      lines: items.filter((e) => g.kinds.includes(e.kind)).map(line),
    })),
    ...(row.rentalCost > 0
      ? [
          {
            key: 'rental',
            title: t('costs.rental'),
            color: colors.rental,
            total: money(row.rentalCost),
            share: row.total > 0 ? (row.rentalCost / row.total) * 100 : 0,
            note: t('costs.rentalNote'),
            lines: [],
          },
        ]
      : []),
  ];

  return (
    <AssetCostDialog
      open
      onClose={onClose}
      period={period}
      title={row.toolName}
      linkTo={paths.toolDetail(row.toolId)}
      linkLabel={t('costs.openTool')}
      total={money(row.total)}
      segments={segments}
      formatMoneyValue={money}
      loading={expenses.isLoading}
      error={expenses.isError ? toApiError(expenses.error).message : null}
      tiles={
        row.revenue > 0
          ? [
              { label: t('costs.revenue'), value: money(row.revenue) },
              { label: t('costs.profit'), value: money(row.profit), tone: row.profit < 0 ? ('warn' as const) : undefined },
            ]
          : []
      }
      sections={sections}
    />
  );
}
