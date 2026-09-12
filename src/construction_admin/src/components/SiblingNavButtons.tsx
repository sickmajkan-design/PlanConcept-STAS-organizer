import { KeyboardArrowLeft, KeyboardArrowRight } from '@mui/icons-material';
import { IconButton, Stack, Tooltip } from '@mui/material';
import { useNavigate } from 'react-router-dom';

import { useT } from '../i18n/useI18n';

/**
 * ‹ › to step through the list a detail page was opened from, one record at
 * a time — for reviewing a set of records without going back to the list
 * between each one. Renders nothing when there is no list to step through
 * (see {@link useSiblingNavigation}).
 */
export function SiblingNavButtons({
  prevId,
  nextId,
  siblingIds,
  buildPath,
}: {
  prevId: string | null;
  nextId: string | null;
  siblingIds: string[] | null;
  buildPath: (id: string) => string;
}) {
  const navigate = useNavigate();
  const t = useT();

  if (!siblingIds) return null;

  const go = (id: string) => navigate(buildPath(id), { state: { siblingIds } });

  return (
    <Stack direction="row" spacing={0.5}>
      <Tooltip title={t('common.previous')}>
        <span>
          <IconButton size="small" disabled={!prevId} onClick={() => prevId && go(prevId)}>
            <KeyboardArrowLeft />
          </IconButton>
        </span>
      </Tooltip>
      <Tooltip title={t('common.next')}>
        <span>
          <IconButton size="small" disabled={!nextId} onClick={() => nextId && go(nextId)}>
            <KeyboardArrowRight />
          </IconButton>
        </span>
      </Tooltip>
    </Stack>
  );
}
