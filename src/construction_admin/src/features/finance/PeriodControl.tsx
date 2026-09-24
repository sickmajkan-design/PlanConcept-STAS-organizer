import { RestartAltOutlined } from '@mui/icons-material';
import { IconButton, MenuItem, Stack, TextField, Tooltip } from '@mui/material';
import { useState } from 'react';

import { useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';
import { useFinancePeriod } from './PeriodContext';
import { periodPresets, validateCustomRange, type PeriodPreset } from './periods';

/**
 * Picks the period every finance widget shows. A custom range is applied only
 * once both dates are there and usable, so typing half a date never fires a
 * request the server would refuse.
 */
export function PeriodControl() {
  const t = useT();
  const { period, setPreset, setCustom, reset, isDefault } = useFinancePeriod();
  // Held here while typing; the context only ever sees a usable range.
  const [draft, setDraft] = useState({ from: period.from, to: period.to });
  const isCustom = period.preset === 'custom';
  const problem = isCustom ? validateCustomRange(draft.from, draft.to) : null;

  function changeDraft(next: { from: string; to: string }) {
    setDraft(next);
    setCustom(next.from, next.to);
  }

  return (
    <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
      <TextField
        select
        size="small"
        value={period.preset}
        label={t('finance.period')}
        onChange={(event) => {
          const preset = event.target.value as PeriodPreset;
          if (preset === 'custom') {
            setDraft({ from: period.from, to: period.to });
            setCustom(period.from, period.to);
          } else {
            setPreset(preset);
          }
        }}
        sx={{ minWidth: 170 }}
      >
        {periodPresets.map((preset) => (
          <MenuItem key={preset} value={preset}>
            {t(`finance.period.${preset}` as MessageKey)}
          </MenuItem>
        ))}
      </TextField>

      {isCustom && (
        <>
          <TextField
            size="small"
            type="date"
            label={t('finance.period.from')}
            value={draft.from}
            error={problem !== null}
            onChange={(event) => changeDraft({ ...draft, from: event.target.value })}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            size="small"
            type="date"
            label={t('finance.period.to')}
            value={draft.to}
            error={problem !== null}
            helperText={problem ? t(`finance.period.problem.${problem}` as MessageKey) : undefined}
            onChange={(event) => changeDraft({ ...draft, to: event.target.value })}
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </>
      )}

      {!isDefault && (
        <Tooltip title={t('finance.period.reset')}>
          <IconButton size="small" onClick={reset} aria-label={t('finance.period.reset')}>
            <RestartAltOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      )}
    </Stack>
  );
}
