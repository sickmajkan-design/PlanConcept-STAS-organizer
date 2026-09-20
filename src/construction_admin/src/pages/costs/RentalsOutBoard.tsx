import { HandymanOutlined, LocalShippingOutlined } from '@mui/icons-material';
import { Box, Chip, Paper, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { Stat, numeric } from '../../components/costs/costUi';
import { CostsLoading } from '../../components/costs/CostsLoading';
import { SortBar, type SortDirection } from '../../components/costs/SortBar';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import {
  useToolRentalsOutQuery,
  useToolRentalsOutSummaryQuery,
  useVehicleRentalsOutQuery,
  useVehicleRentalsOutSummaryQuery,
} from '../../features/costs/useCosts';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';
import type { Period } from './monthWindow';

type Field = 'startDate' | 'endDate' | 'name' | 'renter' | 'dailyRate' | 'kind';

interface Row {
  id: string;
  kind: 'vehicle' | 'tool';
  assetId: string;
  name: string;
  renter: string;
  dailyRate: number;
  startDate: string;
  endDate: string | null;
}

/** What the firm lends out to others: each loan a card, still-out ones flagged. */
export function RentalsOutBoard({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const query = useMemo(() => ({ pageNumber: 1, pageSize: 200, from: period.from, to: period.to }), [period]);
  const vehicles = useVehicleRentalsOutQuery(query);
  const tools = useToolRentalsOutQuery(query);
  const vehicleSummary = useVehicleRentalsOutSummaryQuery(query);
  const toolSummary = useToolRentalsOutSummaryQuery(query);
  const [sortBy, setSortBy] = useState<Field>('startDate');
  const [direction, setDirection] = useState<SortDirection>('desc');

  const rows = useMemo<Row[]>(() => {
    const all: Row[] = [
      ...(vehicles.data?.items ?? []).map((r) => ({
        id: r.id,
        kind: 'vehicle' as const,
        assetId: r.vehicleId,
        name: r.vehicleName,
        renter: r.renterDisplayName,
        dailyRate: r.dailyRate,
        startDate: r.startDate,
        endDate: r.endDate,
      })),
      ...(tools.data?.items ?? []).map((r) => ({
        id: r.id,
        kind: 'tool' as const,
        assetId: r.toolId,
        name: r.toolName,
        renter: r.renterDisplayName,
        dailyRate: r.dailyRate,
        startDate: r.startDate,
        endDate: r.endDate,
      })),
    ];
    const factor = direction === 'asc' ? 1 : -1;

    return all.sort((a, b) => {
      switch (sortBy) {
        case 'dailyRate':
          return (a.dailyRate - b.dailyRate) * factor;
        case 'endDate':
          // A loan still out has no end date and counts as the furthest away.
          return (a.endDate ?? '9999-99-99').localeCompare(b.endDate ?? '9999-99-99') * factor;
        default:
          return String(a[sortBy]).localeCompare(String(b[sortBy])) * factor;
      }
    });
  }, [vehicles.data, tools.data, sortBy, direction]);

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

  if (isLoading) return <CostsLoading />;
  if (rows.length === 0) return <EmptyState message={t('costs.rentalsOutEmpty')} />;

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 2 }}>
          <Stat label={t('costs.rentalsOutTotalRevenue')} value={formatMoney(totalRevenue, locale)} accent="success.main" />
          <Stat label={t('costs.rentalsOutOpen')} value={String(openCount)} accent="warning.main" />
          <Stat label={t('costs.rentalsOut')} value={String(rows.length)} accent="text.disabled" />
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
          { value: 'startDate', label: t('vehicleRentalsOut.startDate') },
          { value: 'endDate', label: t('vehicleRentalsOut.endDate') },
          { value: 'name', label: t('costs.rentalsOutAsset') },
          { value: 'renter', label: t('vehicleRentalsOut.renter') },
          { value: 'dailyRate', label: t('vehicleRentalsOut.dailyRate') },
          { value: 'kind', label: t('costs.rentalsOutKind') },
        ]}
      />

      <Stack spacing={1.5}>
        {rows.map((row) => (
          <Paper
            key={`${row.kind}-${row.id}`}
            variant="outlined"
            component={RouterLink}
            to={row.kind === 'vehicle' ? paths.vehicleDetail(row.assetId) : paths.toolDetail(row.assetId)}
            sx={{
              p: 2,
              display: 'block',
              textDecoration: 'none',
              color: 'inherit',
              '&:hover': { bgcolor: 'action.hover', borderColor: 'text.disabled' },
            }}
          >
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <Box sx={{ color: 'text.secondary', display: 'flex' }}>
                {row.kind === 'vehicle' ? <LocalShippingOutlined /> : <HandymanOutlined />}
              </Box>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }} noWrap>
                  {row.name}
                </Typography>
                <Typography variant="body2" color="text.secondary" noWrap>
                  {row.renter}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatDate(row.startDate)} – {row.endDate ? formatDate(row.endDate) : ''}
                </Typography>
              </Box>
              <Stack sx={{ alignItems: 'flex-end' }} spacing={0.5}>
                <Typography variant="h6" sx={{ fontWeight: 800, letterSpacing: -0.2, ...numeric }}>
                  {formatMoney(row.dailyRate, locale)}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {t('costs.perDay')}
                </Typography>
                {!row.endDate && <Chip label={t('vehicleRentalsOut.stillOut')} color="warning" size="small" />}
              </Stack>
            </Stack>
          </Paper>
        ))}
      </Stack>
    </Stack>
  );
}
