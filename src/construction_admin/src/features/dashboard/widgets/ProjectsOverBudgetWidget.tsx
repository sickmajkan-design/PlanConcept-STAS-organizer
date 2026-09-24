import { Box, LinearProgress, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link as RouterLink } from 'react-router-dom';

import { financeApi } from '../../../api/finance';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

/**
 * Running projects that have spent the share of their budget — or of their
 * contract — the office asked to be warned at. Measured from each project's
 * start to today, so it does not follow the board's period: a budget is for
 * the whole job.
 */
export function ProjectsOverBudgetWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { data, isLoading, error } = useQuery({
    queryKey: ['finance', 'budget-alerts'] as const,
    queryFn: () => financeApi.budgetAlerts(),
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  });

  return (
    <WidgetShell
      title={t('dashboard.widget.ProjectsOverBudget')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {data && data.alerts.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {data.measuredProjects === 0 ? t('finance.alerts.nothingSet') : t('finance.alerts.allWithin')}
        </Typography>
      ) : (
        <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {(data?.alerts ?? []).map((alert) => {
            const over = alert.level === 'Over';

            return (
              <Box
                key={alert.projectId}
                component={RouterLink}
                to={paths.projectDetail(alert.projectId)}
                sx={{ color: 'inherit', textDecoration: 'none' }}
              >
                <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600, minWidth: 0 }} noWrap>
                    {alert.projectName}
                  </Typography>
                  <Typography variant="body2" sx={{ flexShrink: 0, fontWeight: 700 }} color={over ? 'error.main' : 'warning.main'}>
                    {alert.usedPercent.toFixed(1)}%
                  </Typography>
                </Stack>
                {/* The bar stops at full; the number beside it goes on, so an overrun is not hidden. */}
                <LinearProgress
                  variant="determinate"
                  value={Math.min(100, alert.usedPercent)}
                  color={over ? 'error' : 'warning'}
                  sx={{ height: 8, borderRadius: 4, mt: 0.5 }}
                />
                <Typography variant="caption" color="text.secondary">
                  {formatMoney(alert.spent, locale)} / {formatMoney(alert.limit, locale)} ·{' '}
                  {t(alert.basis === 'Budget' ? 'finance.alerts.ofBudget' : 'finance.alerts.ofContract')}
                </Typography>
              </Box>
            );
          })}
        </Stack>
      )}
    </WidgetShell>
  );
}
