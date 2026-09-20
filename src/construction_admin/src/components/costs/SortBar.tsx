import { ArrowDownwardOutlined, ArrowUpwardOutlined } from '@mui/icons-material';
import { IconButton, MenuItem, Stack, TextField, Tooltip } from '@mui/material';

import { useT } from '../../i18n/useI18n';

export type SortDirection = 'asc' | 'desc';

/**
 * Sorting for a list of cards: every figure a card shows is a sort choice, the
 * arrow flips the direction. Stands in for the clickable column headers of a table.
 */
export function SortBar<T extends string>({
  value,
  direction,
  options,
  onChange,
}: {
  value: T;
  direction: SortDirection;
  options: { value: T; label: string }[];
  onChange: (value: T, direction: SortDirection) => void;
}) {
  const t = useT();

  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
      <TextField
        select
        size="small"
        label={t('costs.sortBy')}
        value={value}
        onChange={(event) => onChange(event.target.value as T, direction)}
        sx={{ minWidth: 200 }}
      >
        {options.map((option) => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </TextField>
      <Tooltip title={direction === 'asc' ? t('costs.ascending') : t('costs.descending')}>
        <IconButton
          onClick={() => onChange(value, direction === 'asc' ? 'desc' : 'asc')}
          aria-label={t('costs.sortDirection')}
        >
          {direction === 'asc' ? <ArrowUpwardOutlined /> : <ArrowDownwardOutlined />}
        </IconButton>
      </Tooltip>
    </Stack>
  );
}
