import { Alert, Stack } from '@mui/material';
import { Link } from 'react-router-dom';

import type { DuplicateCandidate } from '../hooks/useDuplicateWarning';
import { useT } from '../i18n/useI18n';

/** A quiet "this might already exist" nudge — never blocks saving. */
export function DuplicateWarningAlert({ candidates }: { candidates: DuplicateCandidate[] }) {
  const t = useT();

  if (candidates.length === 0) return null;

  return (
    <Alert severity="warning">
      {t('duplicateWarning.message')}
      <Stack component="span" sx={{ display: 'block', mt: 0.5 }}>
        {candidates.map((candidate) => (
          <Link
            key={candidate.id}
            to={candidate.path}
            target="_blank"
            rel="noopener noreferrer"
            style={{ display: 'block' }}
          >
            {candidate.label}
          </Link>
        ))}
      </Stack>
    </Alert>
  );
}
