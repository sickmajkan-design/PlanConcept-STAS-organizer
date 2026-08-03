import {
  Box,
  Chip,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useExpiringDocumentsQuery } from '../../features/attachments/useAttachments';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

const WINDOWS = [7, 30, 90, 180] as const;

/**
 * Everything lapsing across the whole company, soonest first.
 *
 * The reason expiry dates are stored at all. A certificate that ran out three
 * months ago is a person who should not have been on site, and nobody finds
 * that by opening employee records one at a time.
 */
export function ExpiringDocumentsPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const [withinDays, setWithinDays] = useState<number>(30);

  const { data, isError, error, refetch, isLoading } =
    useExpiringDocumentsQuery(withinDays);

  return (
    <Box>
      <PageHeader title={t('attachments.expiringTitle')} />

      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <Select
          size="small"
          value={withinDays}
          onChange={(event) => setWithinDays(Number(event.target.value))}
        >
          {WINDOWS.map((days) => (
            <MenuItem key={days} value={days}>
              {t('attachments.expiringWindow', { days })}
            </MenuItem>
          ))}
        </Select>
      </Stack>

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {data && (
        <Paper>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('attachments.file')}</TableCell>
                <TableCell>{t('attachments.owner')}</TableCell>
                <TableCell>{t('attachments.category')}</TableCell>
                <TableCell>{t('attachments.expiresAt')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.map((document) => {
                const lapsed =
                  !!document.expiresAt &&
                  new Date(`${document.expiresAt}T00:00`).getTime() < Date.now();

                return (
                  <TableRow key={document.id} hover>
                    <TableCell>{document.fileName}</TableCell>
                    <TableCell>{document.ownerName ?? '—'}</TableCell>
                    <TableCell>
                      {enumLabel('attachmentCategory', document.category)}
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        color={lapsed ? 'error' : 'warning'}
                        label={
                          lapsed
                            ? t('attachments.expired')
                            : formatDate(document.expiresAt)
                        }
                      />
                    </TableCell>
                  </TableRow>
                );
              })}

              {data.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={4}>
                    <Typography variant="body2" color="text.secondary">
                      {t('attachments.expiringEmpty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Paper>
      )}
    </Box>
  );
}
