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
  TableSortLabel,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import type { Attachment } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useExpiringDocumentsQuery } from '../../features/attachments/useAttachments';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

const WINDOWS = [7, 30, 90, 180] as const;

type SortField = 'fileName' | 'ownerName' | 'category' | 'expiresAt';
type SortDirection = 'asc' | 'desc';

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

  const [sortBy, setSortBy] = useState<SortField>('expiresAt');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  const toggleSort = (field: SortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const sortedData = useMemo(() => {
    if (!data) return data;

    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: Attachment, b: Attachment): number => {
      switch (sortBy) {
        case 'fileName':
          return a.fileName.localeCompare(b.fileName) * factor;
        case 'ownerName':
          return (a.ownerName ?? '').localeCompare(b.ownerName ?? '') * factor;
        case 'category':
          return a.category.localeCompare(b.category) * factor;
        case 'expiresAt':
          return (a.expiresAt ?? '').localeCompare(b.expiresAt ?? '') * factor;
        default:
          return 0;
      }
    };

    return [...data].sort(compare);
  }, [data, sortBy, sortDirection]);

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

      {sortedData && (
        <Paper>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell sortDirection={sortBy === 'fileName' ? sortDirection : false}>
                  <TableSortLabel
                    active={sortBy === 'fileName'}
                    direction={sortBy === 'fileName' ? sortDirection : 'asc'}
                    onClick={() => toggleSort('fileName')}
                  >
                    {t('attachments.file')}
                  </TableSortLabel>
                </TableCell>
                <TableCell sortDirection={sortBy === 'ownerName' ? sortDirection : false}>
                  <TableSortLabel
                    active={sortBy === 'ownerName'}
                    direction={sortBy === 'ownerName' ? sortDirection : 'asc'}
                    onClick={() => toggleSort('ownerName')}
                  >
                    {t('attachments.owner')}
                  </TableSortLabel>
                </TableCell>
                <TableCell sortDirection={sortBy === 'category' ? sortDirection : false}>
                  <TableSortLabel
                    active={sortBy === 'category'}
                    direction={sortBy === 'category' ? sortDirection : 'asc'}
                    onClick={() => toggleSort('category')}
                  >
                    {t('attachments.category')}
                  </TableSortLabel>
                </TableCell>
                <TableCell sortDirection={sortBy === 'expiresAt' ? sortDirection : false}>
                  <TableSortLabel
                    active={sortBy === 'expiresAt'}
                    direction={sortBy === 'expiresAt' ? sortDirection : 'asc'}
                    onClick={() => toggleSort('expiresAt')}
                  >
                    {t('attachments.expiresAt')}
                  </TableSortLabel>
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sortedData.map((document) => {
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

              {sortedData.length === 0 && !isLoading && (
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
