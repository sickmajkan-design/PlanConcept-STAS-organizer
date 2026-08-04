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
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import { exportsApi } from '../../api/exports';
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
              <TableCell>{t('costs.project')}</TableCell>
              {data.includesLabour && (
                <>
                  <TableCell align="right">{t('costs.hours')}</TableCell>
                  <TableCell align="right">{t('costs.labour')}</TableCell>
                </>
              )}
              <TableCell align="right">{t('costs.material')}</TableCell>
              <TableCell align="right">{t('costs.total')}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data.rows.map((row) => (
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
            <TableCell>{t('costs.vehicle')}</TableCell>
            <TableCell align="right">{t('costs.fuel')}</TableCell>
            <TableCell align="right">{t('costs.litres')}</TableCell>
            <TableCell align="right">{t('costs.consumption')}</TableCell>
            <TableCell align="right">{t('costs.service')}</TableCell>
            <TableCell align="right">{t('costs.other')}</TableCell>
            <TableCell align="right">{t('costs.total')}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.rows.map((row) => (
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
