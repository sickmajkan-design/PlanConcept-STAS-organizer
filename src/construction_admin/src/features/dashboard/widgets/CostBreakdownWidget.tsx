import { Stack, Typography } from '@mui/material';
import { useState } from 'react';

import { useI18n } from '../../../i18n/useI18n';
import type { MessageKey } from '../../../i18n/en';
import { chartPalette } from '../../../theme';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceBreakdown } from '../../finance/useFinanceSeries';
import { useAllProjectsQuery } from '../../projects/useProjects';
import type { DashboardWidgetProps } from '../widgetTypes';
import { ProjectSettingsDialog } from './WidgetSettingsDialog';
import { WidgetShell } from './WidgetShell';
import { Donut } from './widgetCharts';
import { BigNumber } from './widgetUi';

const KIND_LABEL: Record<string, MessageKey> = {
  Labour: 'dashboard.projectsRealization.labour',
  ManualPay: 'dashboard.projectsRealization.manualPay',
  Material: 'dashboard.projectsRealization.material',
  GeneralExpenses: 'dashboard.projectsRealization.generalExpenses',
  Accommodation: 'dashboard.projectsRealization.accommodation',
  Vehicles: 'dashboard.projectsRealization.vehicles',
  Tools: 'dashboard.projectsRealization.tools',
};

/**
 * What the spending was on: the whole company's, or — with a project chosen in
 * the widget's settings — that one project's. The pieces are the report's own
 * kinds and add up to the total shown above the chart.
 */
export function CostBreakdownWidget({ settings, onSettingsChange, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const projectId = settings?.projectId ?? '';
  const { data, isLoading, error } = useFinanceBreakdown(projectId || undefined);
  const projects = useAllProjectsQuery();
  const [configuring, setConfiguring] = useState(false);

  const projectName = projectId ? projects.data?.items.find((p) => p.id === projectId)?.name : undefined;
  const slices = (data?.items ?? [])
    .filter((item) => item.amount > 0)
    .map((item) => ({ id: item.kind, label: t(KIND_LABEL[item.kind] ?? 'finance.expense'), value: item.amount }));

  const subtitle = [projectName, formatPeriod(period.from, period.to)].filter(Boolean).join(' · ');

  return (
    <WidgetShell
      title={t('dashboard.widget.CostBreakdown')}
      subtitle={subtitle}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      onConfigure={onSettingsChange ? () => setConfiguring(true) : undefined}
    >
      {slices.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('finance.noData')}
        </Typography>
      ) : (
        <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <BigNumber value={formatMoney(data?.total ?? 0, locale)} />
          {slices.length === 1 ? (
            // One kind is not a composition: a full ring says nothing a number does not say better.
            <Typography variant="body2">
              {slices[0]!.label}: <strong>{formatMoney(slices[0]!.value, locale)}</strong>
            </Typography>
          ) : (
            <Donut
              parts={slices.map((slice, index) => ({
                label: slice.label,
                value: slice.value,
                color: chartPalette[index % chartPalette.length]!,
                display: formatMoney(slice.value, locale),
              }))}
            />
          )}
        </Stack>
      )}

      <ProjectSettingsDialog
        open={configuring}
        title={t('dashboard.widget.CostBreakdown')}
        projectId={projectId}
        allowNone
        onClose={() => setConfiguring(false)}
        onSave={(next) => {
          setConfiguring(false);
          onSettingsChange?.(next === '' ? {} : { projectId: next });
        }}
      />
    </WidgetShell>
  );
}
