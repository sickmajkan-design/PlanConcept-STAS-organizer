import { ToggleButton, ToggleButtonGroup } from '@mui/material';

import { useT } from '../i18n/useI18n';
import { dateOnlyOffset } from '../utils/formatting';

export type DateQuickFilter = 'yesterday' | 'today' | 'tomorrow';

/**
 * "Which day" buttons, for the question a supervisor actually opens a list to
 * answer: who's off today, not a date range picker they have to fill in.
 *
 * `value` is the day itself (`YYYY-MM-DD`), not the quick-filter label, so a
 * page that also supports typing a date can tell whether the current value
 * happens to match one of the three buttons.
 */
export function DateQuickFilters({
  value,
  onChange,
}: {
  value: string | null;
  onChange: (date: string | null) => void;
}) {
  const t = useT();

  const days: Record<DateQuickFilter, string> = {
    yesterday: dateOnlyOffset(-1),
    today: dateOnlyOffset(0),
    tomorrow: dateOnlyOffset(1),
  };

  const selected = (Object.keys(days) as DateQuickFilter[]).find(
    (key) => days[key] === value,
  );

  return (
    <ToggleButtonGroup
      size="small"
      exclusive
      value={selected ?? null}
      onChange={(_event, next: DateQuickFilter | null) => onChange(next ? days[next] : null)}
    >
      <ToggleButton value="yesterday">{t('dateFilter.yesterday')}</ToggleButton>
      <ToggleButton value="today">{t('dateFilter.today')}</ToggleButton>
      <ToggleButton value="tomorrow">{t('dateFilter.tomorrow')}</ToggleButton>
    </ToggleButtonGroup>
  );
}
