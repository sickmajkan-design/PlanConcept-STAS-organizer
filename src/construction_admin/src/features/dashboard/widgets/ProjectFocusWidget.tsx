import { Box, Button, LinearProgress, Stack, Typography } from '@mui/material';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useProjectFinanceSummary } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { StatTile } from './StatTile';
import { ProjectSettingsDialog } from './WidgetSettingsDialog';
import { WidgetShell } from './WidgetShell';

function Bar({ label, value, hint }: { label: string; value: number | null; hint: string }) {
  if (value === null) return null;

  // Bar stops at full; the number beside it goes on past 100 so an overrun is not hidden.
  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.5, gap: 1 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {label}
        </Typography>
        <Typography variant="body2" sx={{ fontWeight: 700 }} color={value > 100 ? 'error.main' : 'text.primary'}>
          {value.toFixed(1)}%
        </Typography>
      </Stack>
      <LinearProgress
        variant="determinate"
        value={Math.min(100, value)}
        color={value > 100 ? 'error' : 'primary'}
        sx={{ height: 8, borderRadius: 4 }}
      />
      <Typography variant="caption" color="text.secondary">
        {hint}
      </Typography>
    </Box>
  );
}

/**
 * One project, chosen in the widget's settings: what came in, what it cost and
 * what is left in the period, and — measured from the project's start, not the
 * period — how much of its budget is spent and how much of its contract has
 * been paid.
 */
export function ProjectFocusWidget({ settings, onSettingsChange, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const projectId = settings?.projectId ?? '';
  const { data, isLoading, error } = useProjectFinanceSummary(projectId || undefined);
  const [configuring, setConfiguring] = useState(false);

  const title = [t('dashboard.widget.ProjectFocus'), data?.projectName, projectId ? formatPeriod(period.from, period.to) : '']
    .filter(Boolean)
    .join(' — ');

  return (
    <WidgetShell
      title={title}
      isLoading={!!projectId && isLoading}
      error={projectId ? error : undefined}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      onConfigure={onSettingsChange ? () => setConfiguring(true) : undefined}
    >
      {!projectId ? (
        <Stack spacing={1.5} sx={{ alignItems: 'flex-start' }}>
          <Typography color="text.secondary" variant="body2">
            {t('finance.focus.choose')}
          </Typography>
          {onSettingsChange && (
            <Button variant="outlined" size="small" onClick={() => setConfiguring(true)}>
              {t('finance.focus.chooseButton')}
            </Button>
          )}
        </Stack>
      ) : (
        data && (
          <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5 }}>
              <StatTile label={t('finance.revenue')} value={formatMoney(data.period.revenue, locale)} icon={null} accent="#00897b" />
              <StatTile label={t('finance.expense')} value={formatMoney(data.period.expense, locale)} icon={null} accent="#e65100" />
              <StatTile
                label={t('finance.profit')}
                value={formatMoney(data.period.profit, locale)}
                hint={data.period.marginPercent === null ? undefined : `${t('finance.margin')}: ${data.period.marginPercent.toFixed(1)}%`}
                icon={null}
                accent={data.period.profit < 0 ? '#c62828' : '#37474f'}
              />
            </Box>

            <Bar
              label={t('finance.focus.budgetUsed')}
              value={data.budgetUsedPercent}
              hint={`${formatMoney(data.toDate.expense, locale)} / ${formatMoney(data.budget, locale)}`}
            />
            <Bar
              label={t('finance.focus.contractCollected')}
              value={data.contractCollectedPercent}
              hint={`${formatMoney(data.toDate.revenue, locale)} / ${formatMoney(data.contractValue, locale)}`}
            />
            {data.budget === null && (
              <Typography variant="caption" color="text.secondary">
                {t('finance.focus.noBudget')}
              </Typography>
            )}

            <Button component={RouterLink} to={paths.projectDetail(data.projectId)} size="small" sx={{ alignSelf: 'flex-start' }}>
              {t('finance.focus.open')}
            </Button>
          </Stack>
        )
      )}

      <ProjectSettingsDialog
        open={configuring}
        title={t('dashboard.widget.ProjectFocus')}
        projectId={projectId}
        allowNone={false}
        onClose={() => setConfiguring(false)}
        onSave={(next) => {
          setConfiguring(false);
          onSettingsChange?.({ projectId: next });
        }}
      />
    </WidgetShell>
  );
}
