import { Box, LinearProgress, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceByProject } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const TOP = 5;

/** Where the money goes: the projects that cost the most in the board's period, each bar against the biggest. */
export function TopProjectsByExpenseWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const { data, isLoading, error } = useFinanceByProject(TOP);

  const rows = (data?.rows ?? []).filter((row) => row.expense > 0);
  const largest = Math.max(...rows.map((row) => row.expense), 0);

  return (
    <WidgetShell
      title={`${t('dashboard.widget.TopProjectsByExpense')} — ${formatPeriod(period.from, period.to)}`}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {rows.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('finance.noData')}
        </Typography>
      ) : (
        <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {rows.map((row) => (
            <Box
              key={row.projectId}
              component={RouterLink}
              to={paths.projectDetail(row.projectId)}
              sx={{ color: 'inherit', textDecoration: 'none' }}
            >
              <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600, minWidth: 0 }} noWrap>
                  {row.projectName}
                </Typography>
                <Typography variant="body2" sx={{ flexShrink: 0 }}>
                  {formatMoney(row.expense, locale)}
                </Typography>
              </Stack>
              <LinearProgress
                variant="determinate"
                value={largest > 0 ? (row.expense / largest) * 100 : 0}
                sx={{ height: 8, borderRadius: 4, mt: 0.5 }}
              />
            </Box>
          ))}
        </Stack>
      )}
    </WidgetShell>
  );
}
