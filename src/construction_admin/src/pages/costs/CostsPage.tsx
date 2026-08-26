import {
  Alert,
  Box,
  Button,
  ButtonGroup,
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
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import { exportsApi } from '../../api/exports';
import type { ProjectCostRow, VehicleCostRow } from '../../api/types';
import { EmptyState } from '../../components/EmptyState';
import { ExportButton } from '../../components/ExportButton';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import {
  useProjectCostReport,
  useVehicleCostReport,
} from '../../features/costs/useCosts';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatMoney, formatQuantity } from '../../utils/formatting';
import { monthOf, splitHours, yearOf, type Period } from './monthWindow';

type SortDirection = 'asc' | 'desc';
type ProjectCostSortField = 'projectName' | 'labourMinutes' | 'labourCost' | 'materialCost' | 'total';
type VehicleCostSortField =
  | 'vehicleName'
  | 'fuelCost'
  | 'litres'
  | 'litresPer100Km'
  | 'serviceCost'
  | 'otherCost'
  | 'total';

export function CostsPage() {
  const t = useT();
  const [tab, setTab] = useState<'projects' | 'vehicles'>('projects');
  const [period, setPeriod] = useState<Period>(() => monthOf(new Date()));

  return (
    <Box>
      <PageHeader title={t('costs.title')} subtitle={t('costs.subtitle')} />

      <PeriodPicker period={period} onChange={setPeriod} />

      <Tabs
        value={tab}
        onChange={(_event, value) => setTab(value as typeof tab)}
        sx={{ mb: 2 }}
      >
        <Tab value="projects" label={t('costs.projects')} />
        <Tab value="vehicles" label={t('costs.vehicles')} />
      </Tabs>

      {tab === 'projects' ? (
        <ProjectCosts period={period} />
      ) : (
        <VehicleCosts period={period} />
      )}
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
              <TableCell align="right" sortDirection={sortBy === 'total' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'total'}
                  direction={sortBy === 'total' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('total')}
                >
                  {t('costs.total')}
                </TableSortLabel>
              </TableCell>
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
                <TableCell align="right" sx={{ fontWeight: 600 }}>
                  {formatMoney(row.total, locale)}
                </TableCell>
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
                {formatMoney(data.total, locale)}
              </TableCell>
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
            <TableCell align="right" sortDirection={sortBy === 'total' ? sortDirection : false}>
              <TableSortLabel
                active={sortBy === 'total'}
                direction={sortBy === 'total' ? sortDirection : 'asc'}
                onClick={() => toggleSort('total')}
              >
                {t('costs.total')}
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
              <TableCell align="right" sx={{ fontWeight: 600 }}>
                {formatMoney(row.total, locale)}
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
              {formatMoney(data.total, locale)}
            </TableCell>
          </TableRow>
        </TableFooter>
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
