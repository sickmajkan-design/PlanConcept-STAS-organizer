import { ChevronRightOutlined, WarningAmberOutlined } from '@mui/icons-material';
import { Alert, Box, Chip, Paper, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';

import { exportsApi } from '../../api/exports';
import type { ProjectCostRow } from '../../api/types';
import {
  CompositionBar,
  CompositionLegend,
  Stat,
  numeric,
  useCostColors,
} from '../../components/costs/costUi';
import { SortBar, type SortDirection } from '../../components/costs/SortBar';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { ExportButton } from '../../components/ExportButton';
import { CostsLoading } from '../../components/costs/CostsLoading';
import { useProjectCostReport } from '../../features/costs/useCosts';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatMoney } from '../../utils/formatting';
import { splitHours, type Period } from './monthWindow';
import { ProjectCostDialog } from './ProjectCostDialog';

type Field =
  | 'total'
  | 'projectName'
  | 'labourCost'
  | 'labourMinutes'
  | 'materialCost'
  | 'generalExpenseCost'
  | 'accommodationCost'
  | 'materialsOnSiteValue'
  | 'manualPayAmount';

const hoursText = (minutes: number) => {
  const { hours, minutes: rest } = splitHours(minutes);
  return `${hours}:${String(rest).padStart(2, '0')}`;
};

/** Every site as a card: what it cost, what that is made of, one click to the itemised list. */
export function ProjectCostBoard({ period }: { period: Period }) {
  const t = useT();
  const { locale } = useI18n();
  const colors = useCostColors();
  const { data, isLoading, isError, error, refetch } = useProjectCostReport(period);
  const [sortBy, setSortBy] = useState<Field>('total');
  const [direction, setDirection] = useState<SortDirection>('desc');
  const [openId, setOpenId] = useState<string | null>(null);
  const money = (value: number) => formatMoney(value, locale);

  const rows = useMemo(() => {
    if (!data) return [];
    const factor = direction === 'asc' ? 1 : -1;
    const compare = (a: ProjectCostRow, b: ProjectCostRow) =>
      sortBy === 'projectName' ? a.projectName.localeCompare(b.projectName) * factor : (a[sortBy] - b[sortBy]) * factor;
    return [...data.rows].sort(compare);
  }, [data, sortBy, direction]);

  const unpricedMinutes = useMemo(() => (data?.rows ?? []).reduce((sum, r) => sum + r.unpricedMinutes, 0), [data]);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;
  if (isLoading) return <CostsLoading />;
  if (!data || data.rows.length === 0) return <EmptyState message={t('costs.empty')} />;

  const segmentsFor = (row: Pick<ProjectCostRow, 'labourCost' | 'materialCost' | 'generalExpenseCost' | 'accommodationCost'>) => [
    { key: 'labour', label: t('costs.labour'), value: row.labourCost, color: colors.labour },
    { key: 'material', label: t('costs.material'), value: row.materialCost, color: colors.material },
    { key: 'other', label: t('costs.generalExpense'), value: row.generalExpenseCost, color: colors.other },
    { key: 'housing', label: t('costs.accommodation'), value: row.accommodationCost, color: colors.housing },
  ];

  const overall = segmentsFor({
    labourCost: data.totalLabourCost,
    materialCost: data.totalMaterialCost,
    generalExpenseCost: data.totalGeneralExpenseCost,
    accommodationCost: data.totalAccommodationCost,
  });

  return (
    <Stack spacing={2}>
      {!data.includesLabour && <Alert severity="info">{t('costs.labourHidden')}</Alert>}

      {/* A total that quietly omits a third of the crew looks exactly like one that does not. */}
      {unpricedMinutes > 0 && (
        <Alert severity="warning">
          {t('costs.unpricedWarning', { count: Math.round(unpricedMinutes / 60) })}
        </Alert>
      )}

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 2, alignItems: 'flex-start', mb: 2 }}>
          <Stat label={t('costs.grandTotal')} value={money(data.total)} />
          <Stat
            label={t('costs.projects')}
            value={String(data.rows.length)}
            accent="text.disabled"
          />
          <Box sx={{ flex: 1 }} />
          <ExportButton onExport={(language) => exportsApi.projectCosts({ ...period, language })} />
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
          { value: 'projectName', label: t('costs.project') },
          { value: 'labourCost', label: t('costs.labour') },
          { value: 'labourMinutes', label: t('costs.hours') },
          { value: 'materialCost', label: t('costs.material') },
          { value: 'generalExpenseCost', label: t('costs.generalExpense') },
          { value: 'accommodationCost', label: t('costs.accommodation') },
          { value: 'materialsOnSiteValue', label: t('costs.materialsOnSite') },
          { value: 'manualPayAmount', label: t('costs.manualPay') },
        ]}
      />

      <Stack spacing={1.5}>
        {rows.map((row) => (
          <Paper
            key={row.projectId}
            variant="outlined"
            role="button"
            tabIndex={0}
            onClick={() => setOpenId(row.projectId)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                setOpenId(row.projectId);
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
                  {row.projectName}
                </Typography>
                <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mt: 0.25 }}>
                  {data.includesLabour && (
                    <Typography variant="caption" color="text.secondary" sx={numeric}>
                      {hoursText(row.labourMinutes)} h
                    </Typography>
                  )}
                  {row.unpricedMinutes > 0 && (
                    <Chip
                      size="small"
                      color="warning"
                      variant="outlined"
                      icon={<WarningAmberOutlined />}
                      label={t('costs.unpricedChip', { hours: hoursText(row.unpricedMinutes) })}
                    />
                  )}
                </Stack>
              </Box>
              <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                <Typography variant="h6" sx={{ fontWeight: 800, letterSpacing: -0.2, ...numeric }}>
                  {money(row.total)}
                </Typography>
                <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled' }} />
              </Stack>
            </Stack>
            <Box sx={{ mt: 1.5 }}>
              <CompositionBar segments={segmentsFor(row)} />
            </Box>
            <Box sx={{ mt: 1.25 }}>
              <CompositionLegend segments={segmentsFor(row)} format={money} />
            </Box>
          </Paper>
        ))}
      </Stack>

      <ProjectCostDialog projectId={openId} period={period} onClose={() => setOpenId(null)} />
    </Stack>
  );
}
