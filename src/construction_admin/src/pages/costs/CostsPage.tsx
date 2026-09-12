import {
  Alert,
  Box,
  Button,
  ButtonGroup,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  TableSortLabel,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import { exportsApi } from '../../api/exports';
import type { ProjectCostRow, ToolCostRow, VehicleCostRow } from '../../api/types';
import { EmptyState } from '../../components/EmptyState';
import { ExportButton } from '../../components/ExportButton';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import {
  useProjectCostReport,
  useToolCostReport,
  useToolRentalsOutQuery,
  useToolRentalsOutSummaryQuery,
  useVehicleCostReport,
  useVehicleRentalsOutQuery,
  useVehicleRentalsOutSummaryQuery,
} from '../../features/costs/useCosts';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney, formatQuantity } from '../../utils/formatting';
import { monthOf, splitHours, yearOf, type Period } from './monthWindow';

type SortDirection = 'asc' | 'desc';
type ProjectCostSortField =
  | 'projectName'
  | 'labourMinutes'
  | 'labourCost'
  | 'materialCost'
  | 'materialsOnSiteValue'
  | 'manualPayAmount'
  | 'generalExpenseCost'
  | 'total';
type VehicleCostSortField =
  | 'vehicleName'
  | 'fuelCost'
  | 'litres'
  | 'litresPer100Km'
  | 'serviceCost'
  | 'otherCost'
  | 'rentalCost'
  | 'total'
  | 'revenue'
  | 'profit';
type ToolCostSortField =
  | 'toolName'
  | 'repairCost'
  | 'maintenanceCost'
  | 'otherCost'
  | 'rentalCost'
  | 'total'
  | 'revenue'
  | 'profit';
type RentalsOutSortField = 'kind' | 'name' | 'renter' | 'dailyRate' | 'startDate' | 'endDate';

/** One row of either a vehicle or a tool currently or previously loaned out, shown side by side. */
interface RentalsOutRow {
  id: string;
  kind: 'vehicle' | 'tool';
  name: string;
  renter: string;
  dailyRate: number;
  startDate: string;
  endDate: string | null;
  isOpen: boolean;
}

export function CostsPage() {
  const t = useT();
  const [tab, setTab] = useState<'projects' | 'vehicles' | 'tools' | 'rentalsOut'>('projects');
  const [period, setPeriod] = useState<Period>(() => monthOf(new Date()));

  return (
    <Box>
      <PageHeader title={t('costs.title')} description={t('costs.subtitle')} />

      <PeriodPicker period={period} onChange={setPeriod} />

      <Tabs
        value={tab}
        onChange={(_event, value) => setTab(value as typeof tab)}
        sx={{ mb: 2 }}
      >
        <Tab value="projects" label={t('costs.projects')} />
        <Tab value="vehicles" label={t('costs.vehicles')} />
        <Tab value="tools" label={t('costs.tools')} />
        <Tab value="rentalsOut" label={t('costs.rentalsOut')} />
      </Tabs>

      {tab === 'projects' && <ProjectCosts period={period} />}
      {tab === 'vehicles' && <VehicleCosts period={period} />}
      {tab === 'tools' && <ToolCosts period={period} />}
      {tab === 'rentalsOut' && <RentalsOut period={period} />}
    </Box>
  );
}

function PeriodPicker({
  period,
  onChange,
}: {
  period: Period;
  onChange: (period: Period) => void;
}) {
  const t = useT();

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={2}
      sx={{ mb: 2, alignItems: { md: 'center' } }}
    >
      {/* The three periods anyone actually asks for, before the date fields:
          "what did last month cost" is the question, and making somebody type
          two dates to ask it is how a report goes unread. */}
      <ButtonGroup size="small">
        <Button onClick={() => onChange(monthOf(new Date()))}>
          {t('costs.thisMonth')}
        </Button>
        <Button onClick={() => onChange(monthOf(new Date(), 1))}>
          {t('costs.lastMonth')}
        </Button>
        <Button onClick={() => onChange(yearOf(new Date()))}>
          {t('costs.thisYear')}
        </Button>
      </ButtonGroup>

      <TextField
        type="date"
        size="small"
        label={t('costs.from')}
        value={period.from}
        onChange={(event) => onChange({ ...period, from: event.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        type="date"
        size="small"
        label={t('costs.to')}
        value={period.to}
        onChange={(event) => onChange({ ...period, to: event.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </Stack>
  );
}

function ProjectCosts({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const { data, isLoading, isError, error, refetch } = useProjectCostReport(period);

  const unpricedMinutes = useMemo(
    () => (data?.rows ?? []).reduce((sum, row) => sum + row.unpricedMinutes, 0),
    [data],
  );

  const [sortBy, setSortBy] = useState<ProjectCostSortField>('total');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const toggleSort = (field: ProjectCostSortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const sortedRows = useMemo(() => {
    if (!data) return [];

    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: ProjectCostRow, b: ProjectCostRow): number => {
      switch (sortBy) {
        case 'projectName':
          return a.projectName.localeCompare(b.projectName) * factor;
        case 'labourMinutes':
          return (a.labourMinutes - b.labourMinutes) * factor;
        case 'labourCost':
          return (a.labourCost - b.labourCost) * factor;
        case 'materialCost':
          return (a.materialCost - b.materialCost) * factor;
        case 'materialsOnSiteValue':
          return (a.materialsOnSiteValue - b.materialsOnSiteValue) * factor;
        case 'manualPayAmount':
          return (a.manualPayAmount - b.manualPayAmount) * factor;
        case 'generalExpenseCost':
          return (a.generalExpenseCost - b.generalExpenseCost) * factor;
        case 'total':
          return (a.total - b.total) * factor;
        default:
          return 0;
      }
    };

    return [...data.rows].sort(compare);
  }, [data, sortBy, sortDirection]);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;
  if (isLoading) return <Loading />;
  if (!data || data.rows.length === 0) return <EmptyState message={t('costs.empty')} />;

  return (
    <Stack spacing={2}>
      <Box>
        <ExportButton
          onExport={(language) =>
            exportsApi.projectCosts({ ...period, language })
          }
        />
      </Box>

      {/* Said plainly rather than left as a suspiciously small number: a
          foreman comparing this against what they know was spent should
          understand at once why the two differ. */}
      {!data.includesLabour && (
        <Alert severity="info">{t('costs.labourHidden')}</Alert>
      )}

      {/* Unpriced hours are a warning, not a footnote. A total that quietly
          omits a third of the crew looks exactly like one that does not. */}
      {unpricedMinutes > 0 && (
        <Alert severity="warning">
          {t('costs.unpricedWarning', {
            count: Math.round(unpricedMinutes / 60),
          })}
        </Alert>
      )}

      <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sortDirection={sortBy === 'projectName' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'projectName'}
                  direction={sortBy === 'projectName' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('projectName')}
                >
                  {t('costs.project')}
                </TableSortLabel>
              </TableCell>
              {data.includesLabour && (
                <>
                  <TableCell align="right" sortDirection={sortBy === 'labourMinutes' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'labourMinutes'}
                      direction={sortBy === 'labourMinutes' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('labourMinutes')}
                    >
                      {t('costs.hours')}
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right" sortDirection={sortBy === 'labourCost' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'labourCost'}
                      direction={sortBy === 'labourCost' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('labourCost')}
                    >
                      {t('costs.labour')}
                    </TableSortLabel>
                  </TableCell>
                </>
              )}
              <TableCell align="right" sortDirection={sortBy === 'materialCost' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'materialCost'}
                  direction={sortBy === 'materialCost' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('materialCost')}
                >
                  {t('costs.material')}
                </TableSortLabel>
              </TableCell>
              <TableCell
                align="right"
                sortDirection={sortBy === 'generalExpenseCost' ? sortDirection : false}
              >
                <TableSortLabel
                  active={sortBy === 'generalExpenseCost'}
                  direction={sortBy === 'generalExpenseCost' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('generalExpenseCost')}
                >
                  {t('costs.generalExpense')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'total' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'total'}
                  direction={sortBy === 'total' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('total')}
                >
                  {t('costs.total')}
                </TableSortLabel>
              </TableCell>
              <TableCell
                align="right"
                sortDirection={sortBy === 'materialsOnSiteValue' ? sortDirection : false}
              >
                <Tooltip title={t('costs.materialsOnSiteHint')}>
                  <TableSortLabel
                    active={sortBy === 'materialsOnSiteValue'}
                    direction={sortBy === 'materialsOnSiteValue' ? sortDirection : 'asc'}
                    onClick={() => toggleSort('materialsOnSiteValue')}
                  >
                    {t('costs.materialsOnSite')}
                  </TableSortLabel>
                </Tooltip>
              </TableCell>
              {data.includesLabour && (
                <TableCell
                  align="right"
                  sortDirection={sortBy === 'manualPayAmount' ? sortDirection : false}
                >
                  <Tooltip title={t('costs.manualPayHint')}>
                    <TableSortLabel
                      active={sortBy === 'manualPayAmount'}
                      direction={sortBy === 'manualPayAmount' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('manualPayAmount')}
                    >
                      {t('costs.manualPay')}
                    </TableSortLabel>
                  </Tooltip>
                </TableCell>
              )}
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedRows.map((row) => (
              <TableRow key={row.projectId} hover>
                <TableCell>{row.projectName}</TableCell>
                {data.includesLabour && (
                  <>
                    <TableCell align="right">
                      <HoursCell minutes={row.labourMinutes} />
                    </TableCell>
                    <TableCell align="right">
                      {formatMoney(row.labourCost, locale)}
                    </TableCell>
                  </>
                )}
                <TableCell align="right">
                  {formatMoney(row.materialCost, locale)}
                </TableCell>
                <TableCell align="right">
                  {formatMoney(row.generalExpenseCost, locale)}
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 600 }}>
                  {formatMoney(row.total, locale)}
                </TableCell>
                <TableCell align="right" sx={{ color: 'text.secondary' }}>
                  {formatMoney(row.materialsOnSiteValue, locale)}
                </TableCell>
                {data.includesLabour && (
                  <TableCell align="right" sx={{ color: 'text.secondary' }}>
                    {formatMoney(row.manualPayAmount, locale)}
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
          <TableFooter>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>{t('costs.grandTotal')}</TableCell>
              {data.includesLabour && (
                <>
                  <TableCell />
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {formatMoney(data.totalLabourCost, locale)}
                  </TableCell>
                </>
              )}
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {formatMoney(data.totalMaterialCost, locale)}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {formatMoney(data.totalGeneralExpenseCost, locale)}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {formatMoney(data.total, locale)}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700, color: 'text.secondary' }}>
                {formatMoney(data.totalMaterialsOnSiteValue, locale)}
              </TableCell>
              {data.includesLabour && (
                <TableCell align="right" sx={{ fontWeight: 700, color: 'text.secondary' }}>
                  {formatMoney(data.totalManualPayAmount, locale)}
                </TableCell>
              )}
            </TableRow>
          </TableFooter>
        </Table>
      </TableContainer>
    </Stack>
  );
}

function VehicleCosts({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const { data, isLoading, isError, error, refetch } = useVehicleCostReport(period);

  const [sortBy, setSortBy] = useState<VehicleCostSortField>('total');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const toggleSort = (field: VehicleCostSortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const sortedRows = useMemo(() => {
    if (!data) return [];

    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: VehicleCostRow, b: VehicleCostRow): number => {
      switch (sortBy) {
        case 'vehicleName':
          return a.vehicleName.localeCompare(b.vehicleName) * factor;
        case 'fuelCost':
          return (a.fuelCost - b.fuelCost) * factor;
        case 'litres':
          return (a.litres - b.litres) * factor;
        case 'litresPer100Km':
          return ((a.litresPer100Km ?? -1) - (b.litresPer100Km ?? -1)) * factor;
        case 'serviceCost':
          return (a.serviceCost - b.serviceCost) * factor;
        case 'otherCost':
          return (a.otherCost - b.otherCost) * factor;
        case 'rentalCost':
          return (a.rentalCost - b.rentalCost) * factor;
        case 'total':
          return (a.total - b.total) * factor;
        case 'revenue':
          return (a.revenue - b.revenue) * factor;
        case 'profit':
          return (a.profit - b.profit) * factor;
        default:
          return 0;
      }
    };

    return [...data.rows].sort(compare);
  }, [data, sortBy, sortDirection]);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;
  if (isLoading) return <Loading />;
  if (!data || data.rows.length === 0) return <EmptyState message={t('costs.empty')} />;

  return (
    <Stack spacing={2}>
      <Box>
        <ExportButton
          onExport={(language) =>
            exportsApi.vehicleCosts({ ...period, language })
          }
        />
      </Box>

      <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto' }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell sortDirection={sortBy === 'vehicleName' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'vehicleName'}
                direction={sortBy === 'vehicleName' ? sortDirection : 'asc'}
                onClick={() => toggleSort('vehicleName')}
              >
                {t('costs.vehicle')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'fuelCost' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'fuelCost'}
                direction={sortBy === 'fuelCost' ? sortDirection : 'asc'}
                onClick={() => toggleSort('fuelCost')}
              >
                {t('costs.fuel')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'litres' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'litres'}
                direction={sortBy === 'litres' ? sortDirection : 'asc'}
                onClick={() => toggleSort('litres')}
              >
                {t('costs.litres')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'litresPer100Km' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'litresPer100Km'}
                direction={sortBy === 'litresPer100Km' ? sortDirection : 'asc'}
                onClick={() => toggleSort('litresPer100Km')}
              >
                {t('costs.consumption')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'serviceCost' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'serviceCost'}
                direction={sortBy === 'serviceCost' ? sortDirection : 'asc'}
                onClick={() => toggleSort('serviceCost')}
              >
                {t('costs.service')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'otherCost' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'otherCost'}
                direction={sortBy === 'otherCost' ? sortDirection : 'asc'}
                onClick={() => toggleSort('otherCost')}
              >
                {t('costs.other')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'rentalCost' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'rentalCost'}
                direction={sortBy === 'rentalCost' ? sortDirection : 'asc'}
                onClick={() => toggleSort('rentalCost')}
              >
                {t('costs.rental')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'total' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'total'}
                direction={sortBy === 'total' ? sortDirection : 'asc'}
                onClick={() => toggleSort('total')}
              >
                {t('costs.total')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'revenue' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'revenue'}
                direction={sortBy === 'revenue' ? sortDirection : 'asc'}
                onClick={() => toggleSort('revenue')}
              >
                {t('costs.revenue')}
              </TableSortLabel>
            </TableCell>
            <TableCell align="right" sortDirection={sortBy === 'profit' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'profit'}
                direction={sortBy === 'profit' ? sortDirection : 'asc'}
                onClick={() => toggleSort('profit')}
              >
                {t('costs.profit')}
              </TableSortLabel>
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {sortedRows.map((row) => (
            <TableRow key={row.vehicleId} hover>
              <TableCell>{row.vehicleName}</TableCell>
              <TableCell align="right">{formatMoney(row.fuelCost, locale)}</TableCell>
              <TableCell align="right">{formatQuantity(row.litres, locale)}</TableCell>
              <TableCell align="right">
                {row.litresPer100Km === null ? (
                  <Typography variant="caption" color="text.disabled">
                    {t('costs.noConsumption')}
                  </Typography>
                ) : (
                  formatQuantity(row.litresPer100Km, locale)
                )}
              </TableCell>
              <TableCell align="right">
                {formatMoney(row.serviceCost, locale)}
              </TableCell>
              <TableCell align="right">{formatMoney(row.otherCost, locale)}</TableCell>
              <TableCell align="right">{formatMoney(row.rentalCost, locale)}</TableCell>
              <TableCell align="right" sx={{ fontWeight: 600 }}>
                {formatMoney(row.total, locale)}
              </TableCell>
              <TableCell align="right">{formatMoney(row.revenue, locale)}</TableCell>
              <TableCell
                align="right"
                sx={{ fontWeight: 600, color: row.profit >= 0 ? 'success.main' : 'error.main' }}
              >
                {formatMoney(row.profit, locale)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
        <TableFooter>
          <TableRow>
            <TableCell sx={{ fontWeight: 700 }}>{t('costs.grandTotal')}</TableCell>
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {formatMoney(data.totalFuelCost, locale)}
            </TableCell>
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {formatQuantity(data.totalLitres, locale)}
            </TableCell>
            <TableCell />
            <TableCell />
            <TableCell />
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {formatMoney(data.totalRentalCost, locale)}
            </TableCell>
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {formatMoney(data.total, locale)}
            </TableCell>
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {formatMoney(data.totalRevenue, locale)}
            </TableCell>
            <TableCell
              align="right"
              sx={{ fontWeight: 700, color: data.totalProfit >= 0 ? 'success.main' : 'error.main' }}
            >
              {formatMoney(data.totalProfit, locale)}
            </TableCell>
          </TableRow>
        </TableFooter>
      </Table>
      </TableContainer>
    </Stack>
  );
}

function ToolCosts({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const { data, isLoading, isError, error, refetch } = useToolCostReport(period);

  const [sortBy, setSortBy] = useState<ToolCostSortField>('total');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const toggleSort = (field: ToolCostSortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const sortedRows = useMemo(() => {
    if (!data) return [];

    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: ToolCostRow, b: ToolCostRow): number => {
      switch (sortBy) {
        case 'toolName':
          return a.toolName.localeCompare(b.toolName) * factor;
        case 'repairCost':
          return (a.repairCost - b.repairCost) * factor;
        case 'maintenanceCost':
          return (a.maintenanceCost - b.maintenanceCost) * factor;
        case 'otherCost':
          return (a.otherCost - b.otherCost) * factor;
        case 'rentalCost':
          return (a.rentalCost - b.rentalCost) * factor;
        case 'total':
          return (a.total - b.total) * factor;
        case 'revenue':
          return (a.revenue - b.revenue) * factor;
        case 'profit':
          return (a.profit - b.profit) * factor;
        default:
          return 0;
      }
    };

    return [...data.rows].sort(compare);
  }, [data, sortBy, sortDirection]);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;
  if (isLoading) return <Loading />;
  if (!data || data.rows.length === 0) return <EmptyState message={t('costs.empty')} />;

  return (
    <Stack spacing={2}>
      <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sortDirection={sortBy === 'toolName' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'toolName'}
                  direction={sortBy === 'toolName' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('toolName')}
                >
                  {t('costs.tool')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'repairCost' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'repairCost'}
                  direction={sortBy === 'repairCost' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('repairCost')}
                >
                  {t('costs.repair')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'maintenanceCost' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'maintenanceCost'}
                  direction={sortBy === 'maintenanceCost' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('maintenanceCost')}
                >
                  {t('costs.maintenance')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'otherCost' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'otherCost'}
                  direction={sortBy === 'otherCost' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('otherCost')}
                >
                  {t('costs.other')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'rentalCost' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'rentalCost'}
                  direction={sortBy === 'rentalCost' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('rentalCost')}
                >
                  {t('costs.rental')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'total' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'total'}
                  direction={sortBy === 'total' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('total')}
                >
                  {t('costs.total')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'revenue' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'revenue'}
                  direction={sortBy === 'revenue' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('revenue')}
                >
                  {t('costs.revenue')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={sortBy === 'profit' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'profit'}
                  direction={sortBy === 'profit' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('profit')}
                >
                  {t('costs.profit')}
                </TableSortLabel>
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedRows.map((row) => (
              <TableRow key={row.toolId} hover>
                <TableCell>{row.toolName}</TableCell>
                <TableCell align="right">{formatMoney(row.repairCost, locale)}</TableCell>
                <TableCell align="right">{formatMoney(row.maintenanceCost, locale)}</TableCell>
                <TableCell align="right">{formatMoney(row.otherCost, locale)}</TableCell>
                <TableCell align="right">{formatMoney(row.rentalCost, locale)}</TableCell>
                <TableCell align="right" sx={{ fontWeight: 600 }}>
                  {formatMoney(row.total, locale)}
                </TableCell>
                <TableCell align="right">{formatMoney(row.revenue, locale)}</TableCell>
                <TableCell
                  align="right"
                  sx={{ fontWeight: 600, color: row.profit >= 0 ? 'success.main' : 'error.main' }}
                >
                  {formatMoney(row.profit, locale)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
          <TableFooter>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }} colSpan={4}>
                {t('costs.grandTotal')}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {formatMoney(data.totalRentalCost, locale)}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {formatMoney(data.total, locale)}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {formatMoney(data.totalRevenue, locale)}
              </TableCell>
              <TableCell
                align="right"
                sx={{ fontWeight: 700, color: data.totalProfit >= 0 ? 'success.main' : 'error.main' }}
              >
                {formatMoney(data.totalProfit, locale)}
              </TableCell>
            </TableRow>
          </TableFooter>
        </Table>
      </TableContainer>
    </Stack>
  );
}

/**
 * The fleet-wide view of the revenue direction: every vehicle and tool
 * currently or previously loaned out to another company, in one sortable
 * list, so "who has what and what are we charging" is answered without
 * opening each vehicle or tool one at a time. This total is shown here only —
 * it is not folded into the site cost report, because a loan-out is not tied
 * to a project the way labour or materials are.
 */
function RentalsOut({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();

  const listQuery = useMemo(
    () => ({
      pageNumber: 1,
      pageSize: 200,
      from: period.from,
      to: period.to,
    }),
    [period],
  );

  const vehicles = useVehicleRentalsOutQuery(listQuery);
  const tools = useToolRentalsOutQuery(listQuery);
  const vehicleSummary = useVehicleRentalsOutSummaryQuery(listQuery);
  const toolSummary = useToolRentalsOutSummaryQuery(listQuery);

  const [sortBy, setSortBy] = useState<RentalsOutSortField>('startDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const toggleSort = (field: RentalsOutSortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const rows = useMemo<RentalsOutRow[]>(() => {
    const vehicleRows: RentalsOutRow[] = (vehicles.data?.items ?? []).map((r) => ({
      id: r.id,
      kind: 'vehicle',
      name: r.vehicleName,
      renter: r.renterDisplayName,
      dailyRate: r.dailyRate,
      startDate: r.startDate,
      endDate: r.endDate,
      isOpen: r.isOpen,
    }));
    const toolRows: RentalsOutRow[] = (tools.data?.items ?? []).map((r) => ({
      id: r.id,
      kind: 'tool',
      name: r.toolName,
      renter: r.renterDisplayName,
      dailyRate: r.dailyRate,
      startDate: r.startDate,
      endDate: r.endDate,
      isOpen: r.isOpen,
    }));

    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: RentalsOutRow, b: RentalsOutRow): number => {
      switch (sortBy) {
        case 'kind':
          return a.kind.localeCompare(b.kind) * factor;
        case 'name':
          return a.name.localeCompare(b.name) * factor;
        case 'renter':
          return a.renter.localeCompare(b.renter) * factor;
        case 'dailyRate':
          return (a.dailyRate - b.dailyRate) * factor;
        case 'startDate':
          return a.startDate.localeCompare(b.startDate) * factor;
        case 'endDate':
          // Still-out rows (no end date) sort as if furthest in the future,
          // so "ongoing" reads as the most recent activity either way.
          return (
            ((a.endDate ?? '9999-99-99').localeCompare(b.endDate ?? '9999-99-99')) * factor
          );
        default:
          return 0;
      }
    };

    return [...vehicleRows, ...toolRows].sort(compare);
  }, [vehicles.data, tools.data, sortBy, sortDirection]);

  const isLoading = vehicles.isLoading || tools.isLoading;
  const isError = vehicles.isError || tools.isError;
  const error = vehicles.error ?? tools.error;
  const totalRevenue = (vehicleSummary.data?.totalValue ?? 0) + (toolSummary.data?.totalValue ?? 0);
  const openCount = (vehicleSummary.data?.openCount ?? 0) + (toolSummary.data?.openCount ?? 0);

  if (isError) {
    return (
      <ErrorState
        error={error}
        onRetry={() => {
          void vehicles.refetch();
          void tools.refetch();
        }}
      />
    );
  }
  if (isLoading) return <Loading />;
  if (rows.length === 0) return <EmptyState message={t('costs.rentalsOutEmpty')} />;

  const sortCell = (field: RentalsOutSortField, label: string, align: 'left' | 'right' = 'left') => (
    <TableCell align={align} sortDirection={sortBy === field ? sortDirection : false}>
      <TableSortLabel
        active={sortBy === field}
        direction={sortBy === field ? sortDirection : 'asc'}
        onClick={() => toggleSort(field)}
      >
        {label}
      </TableSortLabel>
    </TableCell>
  );

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={3}>
        <Typography variant="body2" color="text.secondary">
          {t('costs.rentalsOutOpenCount', { count: openCount })}
        </Typography>
        <Typography variant="body2" sx={{ fontWeight: 700 }}>
          {t('costs.rentalsOutTotalRevenue')}: {formatMoney(totalRevenue, locale)}
        </Typography>
      </Stack>

      <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              {sortCell('kind', t('costs.rentalsOutKind'))}
              {sortCell('name', t('costs.rentalsOutAsset'))}
              {sortCell('renter', t('vehicleRentalsOut.renter'))}
              {sortCell('dailyRate', t('vehicleRentalsOut.dailyRate'), 'right')}
              {sortCell('startDate', t('vehicleRentalsOut.startDate'))}
              {sortCell('endDate', t('vehicleRentalsOut.endDate'))}
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={`${row.kind}-${row.id}`} hover>
                <TableCell>
                  {row.kind === 'vehicle' ? t('costs.vehicle') : t('costs.tool')}
                </TableCell>
                <TableCell>{row.name}</TableCell>
                <TableCell>{row.renter}</TableCell>
                <TableCell align="right">{formatMoney(row.dailyRate, locale)}</TableCell>
                <TableCell>{formatDate(row.startDate)}</TableCell>
                <TableCell>
                  {row.endDate ? (
                    formatDate(row.endDate)
                  ) : (
                    <Chip label={t('vehicleRentalsOut.stillOut')} color="warning" size="small" />
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

function HoursCell({ minutes }: { minutes: number }) {
  const { hours, minutes: rest } = splitHours(minutes);

  return <>{`${hours}:${String(rest).padStart(2, '0')}`}</>;
}

function Loading() {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
      <CircularProgress />
    </Box>
  );
}
