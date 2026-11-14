import { Box, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import { systemApi, type MetricPoint } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { TrendChart } from '../dashboard/widgets/widgetCharts';
import { formatSize } from './formatSize';
import { BigNumber, Panel } from './serverUi';

type Series = 'load' | 'memory' | 'disk';

const PICK: Record<Series, (point: MetricPoint) => number> = {
  load: (point) => point.loadPercent,
  memory: (point) => point.memoryPercent,
  disk: (point) => point.diskPercent,
};

/** The line the chart marks as the warning level: every core busy for load, 85% for memory and disk. */
const LIMIT: Record<Series, number> = { load: 100, memory: 85, disk: 85 };

/** Fewer points than this is a line with nothing to say: wait for a few hours of data. */
const MIN_POINTS = 3;

/**
 * The last seven days of processor load, memory and disk, one point per hour, one chart at a time: a switch above it
 * chooses which. The host script writes a sample every five minutes; the disk chart also says how fast it fills.
 */
export function ServerMetricsCard() {
  const t = useT();
  const [series, setSeries] = useState<Series>('memory');
  const { data } = useQuery({ queryKey: ['system', 'metrics'] as const, queryFn: systemApi.metrics, refetchInterval: 5 * 60_000 });

  if (!data) {
    return null;
  }

  const ready = data.known && data.points.length >= MIN_POINTS;
  const latest = ready ? PICK[series](data.points[data.points.length - 1]!) : 0;

  return (
    <Panel
      title={t('serverMetrics.title')}
      action={
        <ToggleButtonGroup
          exclusive
          size="small"
          value={series}
          onChange={(_, next: Series | null) => next && setSeries(next)}
          aria-label={t('serverMetrics.choose')}
          sx={(theme) => ({
            bgcolor: alpha(theme.palette.text.primary, 0.06),
            borderRadius: '12px',
            p: '3px',
            gap: '2px',
            '& .MuiToggleButton-root': {
              border: 0,
              borderRadius: '9px !important',
              px: 2,
              fontWeight: 600,
              color: 'text.secondary',
              textTransform: 'none',
              transition: 'background-color 200ms ease, color 200ms ease',
              '&.Mui-selected': {
                bgcolor: 'background.paper',
                color: 'text.primary',
                boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 3px rgba(0, 0, 0, 0.12)',
              },
              '&.Mui-selected:hover': { bgcolor: 'background.paper' },
              '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
            },
          })}
        >
          <ToggleButton value="load">{t('serverMetrics.loadShort')}</ToggleButton>
          <ToggleButton value="memory">{t('serverMetrics.memory')}</ToggleButton>
          <ToggleButton value="disk">{t('serverMetrics.disk')}</ToggleButton>
        </ToggleButtonGroup>
      }
    >
      {!ready ? (
        <Typography variant="body2" color="text.secondary">
          {t('serverMetrics.none')}
        </Typography>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          <Box>
            <BigNumber>{Math.round(latest)}%</BigNumber>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
              {t(`serverMetrics.caption.${series}`)}
            </Typography>
          </Box>
          <TrendChart
            points={data.points.map((point) => ({ label: formatDateTime(point.atUtc), value: PICK[series](point) }))}
            format={(value) => `${Math.round(value)}%`}
            reference={LIMIT[series]}
            referenceLabel={t(series === 'load' ? 'serverMetrics.loadLimit' : 'serverMetrics.memoryLimit')}
            minHeight={220}
          />
          {series === 'disk' && data.diskGrowthBytesPerDay !== null && data.diskDaysLeft !== null && (
            <Typography variant="body2" color="text.secondary">
              {t('serverMetrics.diskForecast', { growth: formatSize(data.diskGrowthBytesPerDay), days: data.diskDaysLeft })}
            </Typography>
          )}
        </Box>
      )}
    </Panel>
  );
}
