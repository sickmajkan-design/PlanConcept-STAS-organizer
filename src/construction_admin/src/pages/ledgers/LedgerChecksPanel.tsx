import { CheckCircleOutlined, FileDownloadOutlined, WarningAmberOutlined } from '@mui/icons-material';
import { Alert, Box, Button, Paper, Stack, Typography } from '@mui/material';
import { useState } from 'react';

import { exportsApi } from '../../api/exports';
import type { LedgerCheck } from '../../api/types';
import { useLedgerChecksQuery } from '../../features/ledgers/useLedgers';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';

/** How many issues are listed before the rest are summarised. */
const VISIBLE = 6;

type Translate = ReturnType<typeof useT>;

function describe(t: Translate, check: LedgerCheck): string {
  return t(`ledgers.check.${check.kind}` as MessageKey, {
    row: check.rowLabel ?? '',
    section: check.sectionName,
    column: check.columnName ?? '',
    hours: check.amount ?? 0,
    sections: [check.sectionName, ...check.otherSections].join(', '),
  });
}

/**
 * What is worth a second look before the month is closed and handed on: hours
 * with no price to bill them at, figures typed over a calculation, and one
 * person with more hours than a month holds.
 *
 * The list is computed by the server from the month as it stands, so fixing
 * a problem removes it at once. Nothing is shown for a month with nothing to
 * check unless it is a payroll month, where "all clear" is worth saying.
 */
export function LedgerChecksPanel({ ledgerId, hasHours }: { ledgerId: string; hasHours: boolean }) {
  const t = useT();
  const { data: checks } = useLedgerChecksQuery(ledgerId);
  const [showAll, setShowAll] = useState(false);

  if (!checks) return null;

  if (checks.length === 0) {
    if (!hasHours) return null;

    return (
      <Alert icon={<CheckCircleOutlined fontSize="inherit" />} severity="success" sx={{ mb: 2 }}>
        {t('ledgers.checksNone')}
      </Alert>
    );
  }

  const shown = showAll ? checks : checks.slice(0, VISIBLE);

  return (
    <Paper variant="outlined" sx={{ p: 2, mb: 2, borderColor: 'warning.main' }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
        <WarningAmberOutlined color="warning" fontSize="small" />
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('ledgers.checksTitle')} ({checks.length})
        </Typography>
      </Stack>

      <Stack spacing={0.75}>
        {shown.map((check, index) => (
          <Typography key={`${check.kind}-${check.rowId ?? index}-${check.columnName ?? ''}`} variant="body2">
            {describe(t, check)}
          </Typography>
        ))}
      </Stack>

      {checks.length > VISIBLE && (
        <Button size="small" sx={{ mt: 1 }} onClick={() => setShowAll((v) => !v)}>
          {showAll ? t('ledgers.checksLess') : t('ledgers.checksMore', { count: checks.length - VISIBLE })}
        </Button>
      )}
    </Paper>
  );
}

/** Downloads the month as a spreadsheet, ready to hand to an accountant. */
export function LedgerExportButton({ ledgerId }: { ledgerId: string }) {
  const t = useT();
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  const run = async () => {
    setBusy(true);
    setFailed(false);

    try {
      await exportsApi.ledger(ledgerId);
    } catch {
      setFailed(true);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Box>
      <Button variant="contained" startIcon={<FileDownloadOutlined />} disabled={busy} onClick={() => void run()}>
        {t('ledgers.exportMonth')}
      </Button>
      {failed && (
        <Typography variant="caption" color="error" sx={{ display: 'block' }}>
          {t('ledgers.exportFailed')}
        </Typography>
      )}
    </Box>
  );
}
