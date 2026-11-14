import { Box, Card, CardContent, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { systemApi, type MetricPoint } from '../../api/system';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { TrendChart } from '../dashboard/widgets/widgetCharts';
import { formatSize } from './formatSize';

const percent = (value: number) => `${Math.round(value)}%`;

/** Fewer points than this is a line with nothing to say: wait for a few hours of data. */
const MIN_POINTS = 3;

/**
 * The last seven days of processor load, memory and disk, one point per hour. The host script writes a sample every five
 * minutes; the disk chart also says how fast it fills and when it would be full at that speed.
 */
export function ServerMetricsCard() {
  const t = useT();
  const { data } = useQuery({ queryKey: ['system', 'metrics'] as const, queryFn: systemApi.metrics, refetchInterval: 5 * 60_000 });

  if (!data) {
    return null;
  }

  const chart = (pick: (point: MetricPoint) => number, reference?: number, referenceLabel?: string) => (
    <TrendChart
      points={data.points.map((point) => ({ label: formatDateTime(point.atUtc), value: pick(point) }))}
      format={percent}
      reference={reference}
      referenceLabel={referenceLabel}
    />
  );

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="subtitle1" sx={{ mb: 0.5 }}>
          {t('serverMetrics.title')}
        </Typography>
        {!data.known || data.points.length < MIN_POINTS ? (
          <Typography variant="body2" color="text.secondary">
            {t('serverMetrics.none')}
          </Typography>
        ) : (
          <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(3, minmax(0, 1fr))' } }}>
            <Box>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                {t('serverMetrics.load')}
              </Typography>
              {chart((point) => point.loadPercent, 100, t('serverMetrics.loadLimit'))}
            </Box>
            <Box>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                {t('serverMetrics.memory')}
              </Typography>
              {chart((point) => point.memoryPercent, 85, t('serverMetrics.memoryLimit'))}
            </Box>
            <Box>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                {t('serverMetrics.disk')}
              </Typography>
              {chart((point) => point.diskPercent, 85, t('serverMetrics.diskLimit'))}
              {data.diskGrowthBytesPerDay !== null && data.diskDaysLeft !== null && (
                <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 0.5 }}>
                  {t('serverMetrics.diskForecast', { growth: formatSize(data.diskGrowthBytesPerDay), days: data.diskDaysLeft })}
                </Typography>
              )}
            </Box>
          </Box>
        )}
      </CardContent>
    </Card>
  );
}
