import { InfoOutlined } from '@mui/icons-material';
import { IconButton, Popover, Stack, Tooltip, Typography } from '@mui/material';
import { useState } from 'react';

import { useT } from '../i18n/useI18n';
import { StatusChip } from './StatusChip';
import type { EnumKind } from '../i18n/enumLabels';

/**
 * What each status colour on this list means, on demand.
 *
 * `StatusChip` groups colour by meaning rather than by entity, so the same
 * green always means "good" — but nothing on the page says so the first time
 * someone sees it. This is that explanation, kept out of the way until asked
 * for rather than printed as a permanent row every list would otherwise carry.
 */
export function StatusLegend({
  kind,
  values,
}: {
  kind: EnumKind;
  values: readonly string[];
}) {
  const t = useT();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <>
      <Tooltip title={t('statusLegend.title')}>
        <IconButton size="small" onClick={(event) => setAnchor(event.currentTarget)}>
          <InfoOutlined fontSize="small" />
        </IconButton>
      </Tooltip>
      <Popover
        open={!!anchor}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Stack spacing={1} sx={{ p: 2, minWidth: 200 }}>
          <Typography variant="subtitle2">{t('statusLegend.title')}</Typography>
          {values.map((value) => (
            <Stack key={value} direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <StatusChip status={value} kind={kind} sx={{ minWidth: 96 }} />
            </Stack>
          ))}
        </Stack>
      </Popover>
    </>
  );
}
