import { AddOutlined } from '@mui/icons-material';
import { Button, Chip, Popover, Stack, TextField } from '@mui/material';
import { useState } from 'react';

import { useT } from '../i18n/useI18n';
import type { SavedView } from '../hooks/useSavedViews';

/** A row of clickable named filter snapshots, plus a small popover to save the current one. */
export function SavedViewsBar<TState>({
  views,
  onApply,
  onSave,
  onDelete,
}: {
  views: SavedView<TState>[];
  onApply: (state: TState) => void;
  onSave: (name: string) => void;
  onDelete: (id: string) => void;
}) {
  const t = useT();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [name, setName] = useState('');

  const submit = () => {
    const trimmed = name.trim();
    if (!trimmed) return;
    onSave(trimmed);
    setName('');
    setAnchorEl(null);
  };

  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', rowGap: 1 }}>
      {views.map((view) => (
        <Chip
          key={view.id}
          label={view.name}
          onClick={() => onApply(view.state)}
          onDelete={() => onDelete(view.id)}
          size="small"
          variant="outlined"
        />
      ))}
      <Button
        size="small"
        color="inherit"
        startIcon={<AddOutlined fontSize="small" />}
        onClick={(event) => setAnchorEl(event.currentTarget)}
      >
        {t('savedViews.save')}
      </Button>
      <Popover
        open={!!anchorEl}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Stack direction="row" spacing={1} sx={{ p: 1.5, alignItems: 'center' }}>
          <TextField
            size="small"
            autoFocus
            placeholder={t('savedViews.namePlaceholder')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') submit();
            }}
          />
          <Button size="small" variant="contained" onClick={submit}>
            {t('common.save')}
          </Button>
        </Stack>
      </Popover>
    </Stack>
  );
}
