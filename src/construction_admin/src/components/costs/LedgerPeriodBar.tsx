import { Button, ButtonGroup, Stack, TextField } from '@mui/material';

import { useT } from '../../i18n/useI18n';
import { monthOf, yearOf } from '../../pages/costs/monthWindow';

export interface LedgerPeriod {
  from: string;
  to: string;
}

export const ALL_TIME: LedgerPeriod = { from: '', to: '' };

/** Quick periods plus two date fields; empty dates mean "everything". */
export function LedgerPeriodBar({
  value,
  onChange,
}: {
  value: LedgerPeriod;
  onChange: (period: LedgerPeriod) => void;
}) {
  const t = useT();

  return (
    <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap', alignItems: { md: 'center' } }}>
      <ButtonGroup size="small">
        <Button onClick={() => onChange(ALL_TIME)} variant={!value.from && !value.to ? 'contained' : 'outlined'}>
          {t('costs.allTime')}
        </Button>
        <Button onClick={() => onChange(monthOf(new Date()))}>{t('costs.thisMonth')}</Button>
        <Button onClick={() => onChange(monthOf(new Date(), 1))}>{t('costs.lastMonth')}</Button>
        <Button onClick={() => onChange(yearOf(new Date()))}>{t('costs.thisYear')}</Button>
      </ButtonGroup>
      <TextField
        type="date"
        size="small"
        label={t('costs.from')}
        value={value.from}
        onChange={(event) => onChange({ ...value, from: event.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        type="date"
        size="small"
        label={t('costs.to')}
        value={value.to}
        onChange={(event) => onChange({ ...value, to: event.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </Stack>
  );
}
