import AddIcon from '@mui/icons-material/Add';
import { Chip, IconButton, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';

import { useT } from '../i18n/useI18n';

/** Mirrors the API: a reminder is 1 to 365 days before expiry, at most five per document. */
const MAX_DAYS = 365;
const MAX_REMINDERS = 5;
const SUGGESTED = [90, 60, 30, 14];

/**
 * The document's own reminder lead times, in days before it lapses. Empty means the
 * general rule (each admin's own setting) applies.
 */
export function ReminderDaysField({
  value,
  onChange,
  disabled,
}: {
  value: number[];
  onChange: (next: number[]) => void;
  disabled?: boolean;
}) {
  const t = useT();
  const [draft, setDraft] = useState('');

  const add = (days: number) => {
    if (!Number.isInteger(days) || days < 1 || days > MAX_DAYS) return;
    if (value.includes(days) || value.length >= MAX_REMINDERS) return;
    onChange([...value, days].sort((a, b) => b - a));
  };

  const addDraft = () => {
    add(Number(draft));
    setDraft('');
  };

  return (
    <Stack spacing={1}>
      <Typography variant="body2" color="text.secondary">
        {t('attachments.reminders')}
      </Typography>

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
        {value.map((days) => (
          <Chip
            key={days}
            size="small"
            color="primary"
            label={t('attachments.reminderDaysBefore', { days })}
            disabled={disabled}
            onDelete={() => onChange(value.filter((d) => d !== days))}
          />
        ))}
        {value.length === 0 && (
          <Typography variant="caption" color="text.secondary">
            {t('attachments.remindersGeneral')}
          </Typography>
        )}
      </Stack>

      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        {SUGGESTED.filter((days) => !value.includes(days)).map((days) => (
          <Chip
            key={days}
            size="small"
            variant="outlined"
            label={`+ ${days}`}
            disabled={disabled || value.length >= MAX_REMINDERS}
            onClick={() => add(days)}
          />
        ))}
        <TextField
          size="small"
          type="number"
          label={t('attachments.reminderCustom')}
          value={draft}
          disabled={disabled || value.length >= MAX_REMINDERS}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault();
              addDraft();
            }
          }}
          slotProps={{ htmlInput: { min: 1, max: MAX_DAYS }, inputLabel: { shrink: true } }}
          sx={{ width: 150 }}
        />
        <IconButton
          size="small"
          aria-label={t('attachments.reminderAdd')}
          disabled={disabled || !draft}
          onClick={addDraft}
        >
          <AddIcon fontSize="small" />
        </IconButton>
      </Stack>
    </Stack>
  );
}
