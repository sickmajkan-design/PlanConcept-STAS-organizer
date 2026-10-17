import { Box, MenuItem, TextField } from '@mui/material';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceByProject } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetSettingsDialog } from './WidgetSettingsDialog';
import { WidgetShell } from './WidgetShell';
import { BarRow, EmptyNote } from './widgetUi';

const TOP_CHOICES = [5, 10] as const;
const DEFAULT_TOP = 5;

/** Where the money goes: the projects that cost the most in the board's period, each bar against the biggest. */
export function TopProjectsByExpenseWidget({ settings, onSettingsChange, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  // Anything but a choice on offer falls back, so a stale or hand-edited setting cannot ask for a size the API refuses.
  const chosen = Number(settings?.top);
  const top = (TOP_CHOICES as readonly number[]).includes(chosen) ? chosen : DEFAULT_TOP;
  const { data, isLoading, error } = useFinanceByProject(top);
  const [configuring, setConfiguring] = useState(false);
  const [draft, setDraft] = useState(top);

  const rows = (data?.rows ?? []).filter((row) => row.expense > 0);
  const largest = Math.max(...rows.map((row) => row.expense), 0);

  return (
    <WidgetShell
      title={t('dashboard.widget.TopProjectsByExpense')}
      subtitle={formatPeriod(period.from, period.to)}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      onConfigure={
        onSettingsChange
          ? () => {
              setDraft(top);
              setConfiguring(true);
            }
          : undefined
      }
    >
      {rows.length === 0 ? (
        <EmptyNote>{t('finance.noData')}</EmptyNote>
      ) : (
        <Box
          sx={{
            display: 'grid',
            gap: 2,
            alignContent: 'start',
            flex: 1,
            minHeight: 0,
            overflow: 'auto',
            gridTemplateColumns: '1fr',
            '@container widget (min-width: 640px)': { gridTemplateColumns: '1fr 1fr' },
          }}
        >
          {rows.map((row) => (
            <Box key={row.projectId} component={RouterLink} to={paths.projectDetail(row.projectId)} sx={{ color: 'inherit', textDecoration: 'none' }}>
              <BarRow label={row.projectName} value={formatMoney(row.expense, locale)} percent={largest > 0 ? (row.expense / largest) * 100 : 0} />
            </Box>
          ))}
        </Box>
      )}

      <WidgetSettingsDialog
        open={configuring}
        title={t('dashboard.widget.TopProjectsByExpense')}
        onClose={() => setConfiguring(false)}
        onSave={() => {
          setConfiguring(false);
          onSettingsChange?.(draft === DEFAULT_TOP ? {} : { top: String(draft) });
        }}
      >
        <TextField select fullWidth label={t('finance.rows')} value={draft} onChange={(event) => setDraft(Number(event.target.value))}>
          {TOP_CHOICES.map((choice) => (
            <MenuItem key={choice} value={choice}>
              {choice}
            </MenuItem>
          ))}
        </TextField>
      </WidgetSettingsDialog>
    </WidgetShell>
  );
}
