import { useBranchFilter } from '../../branches/BranchContext';
import { Box, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link as RouterLink } from 'react-router-dom';

import { financeApi } from '../../../api/finance';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { BarRow } from './widgetUi';

/**
 * Running projects that have spent the share of their budget — or of their
 * contract — the office asked to be warned at. Measured from each project's
 * start to today, so it does not follow the board's period: a budget is for
 * the whole job.
 */
export function ProjectsOverBudgetWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { branchId } = useBranchFilter();
  const { data, isLoading, error } = useQuery({
    queryKey: ['finance', 'budget-alerts', branchId ?? null] as const,
    queryFn: () => financeApi.budgetAlerts({ branchId }),
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
      empty={
        data && data.alerts.length === 0
          ? {
              text: data.measuredProjects === 0 ? t('finance.alerts.nothingSet') : t('finance.alerts.allWithin'),
              tone: data.measuredProjects === 0 ? 'none' : 'ok',
            }
          : undefined
      }
    >
      {data && data.alerts.length > 0 && (
        <Box
          sx={{
            display: 'grid',
            gap: 1.25,
            alignContent: 'start',
            flex: 1,
            minHeight: 0,
            overflow: 'auto',
            gridTemplateColumns: '1fr',
            '@container widget (min-width: 640px)': { gridTemplateColumns: '1fr 1fr' },
          }}
        >
          {(data?.alerts ?? []).map((alert) => {
            const over = alert.level === 'Over';

            return (
              <Box
                key={alert.projectId}
                component={RouterLink}
                to={paths.projectDetail(alert.projectId)}
                sx={(theme) => ({
                  color: 'inherit',
                  textDecoration: 'none',
                  display: 'grid',
                  gap: 0.5,
                  borderRadius: '16px',
                  px: 1.75,
                  py: 1.4,
                  bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.05)' : 'rgba(31,27,22,0.04)',
                  transition: 'transform 0.12s ease-out',
                  '&:active': { transform: 'scale(0.98)' },
                })}
              >
                {/* The bar stops at full; the number beside it goes on, so an overrun is not hidden. */}
                <BarRow
                  label={alert.projectName}
                  value={`${alert.usedPercent.toFixed(1)}%`}
                  percent={Math.min(100, alert.usedPercent)}
                  tone={over ? 'bad' : 'warn'}
                />
                <Typography variant="caption" color="text.secondary" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatMoney(alert.spent, locale)} / {formatMoney(alert.limit, locale)} ·{' '}
                  {t(alert.basis === 'Budget' ? 'finance.alerts.ofBudget' : 'finance.alerts.ofContract')}
                </Typography>
              </Box>
            );
          })}
        </Box>
      )}
    </WidgetShell>
  );
}
