import { UploadFileOutlined } from '@mui/icons-material';
import {
  Box,
  Chip,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import type { Attachment, AttachmentCategory, AttachmentOwnerType } from '../../api/types';
import { attachmentCategories, attachmentOwnerTypes } from '../../api/types';
import { AttachmentPreviewDialog } from '../../components/AttachmentPreviewDialog';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { UploadDocumentDialog } from '../../components/UploadDocumentDialog';
import { useExpiringDocumentsQuery } from '../../features/attachments/useAttachments';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

const WINDOWS = [7, 30, 90, 180] as const;
/** Distinct from every numeric window — selects "no cutoff" on the query. */
const ALL_WINDOW = 'all';

type WindowValue = (typeof WINDOWS)[number] | typeof ALL_WINDOW;

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
  const [windowValue, setWindowValue] = useState<WindowValue>(30);
  const [includeUndated, setIncludeUndated] = useState(false);
  const [ownerTypeFilter, setOwnerTypeFilter] = useState<AttachmentOwnerType | ''>('');
  const [categoryFilter, setCategoryFilter] = useState<AttachmentCategory | ''>('');
  const [uploading, setUploading] = useState(false);
  const [previewing, setPreviewing] = useState<Attachment | null>(null);

  const { data, isError, error, refetch, isLoading } = useExpiringDocumentsQuery(
    windowValue === ALL_WINDOW ? null : windowValue,
    includeUndated,
  );

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

    const filtered = data.filter(
      (document) =>
        (!ownerTypeFilter || document.ownerType === ownerTypeFilter) &&
        (!categoryFilter || document.category === categoryFilter),
    );

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

    return filtered.sort(compare);
  }, [data, sortBy, sortDirection, ownerTypeFilter, categoryFilter]);

  return (
    <Box>
      <PageHeader
        title={t('attachments.expiringTitle')}
        description={t('attachments.expiringDescription')}
        action={{
          label: t('attachments.upload'),
          icon: <UploadFileOutlined />,
          onClick: () => setUploading(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: 'wrap', rowGap: 1.5, alignItems: 'center' }}>
        <Select
          size="small"
          value={windowValue}
          onChange={(event) =>
            setWindowValue(
              event.target.value === ALL_WINDOW
                ? ALL_WINDOW
                : (Number(event.target.value) as (typeof WINDOWS)[number]),
            )
          }
        >
          {WINDOWS.map((days) => (
            <MenuItem key={days} value={days}>
              {t('attachments.expiringWindow', { days })}
            </MenuItem>
          ))}
          <MenuItem value={ALL_WINDOW}>{t('attachments.expiringAll')}</MenuItem>
        </Select>

        <FormControlLabel
          control={
            <Switch
              checked={includeUndated}
              onChange={(event) => setIncludeUndated(event.target.checked)}
            />
          }
          label={t('attachments.includeUndated')}
        />

        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="doc-owner-filter-label">{t('attachments.owner')}</InputLabel>
          <Select
            labelId="doc-owner-filter-label"
            label={t('attachments.owner')}
            value={ownerTypeFilter}
            onChange={(event) => setOwnerTypeFilter(event.target.value as AttachmentOwnerType | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {attachmentOwnerTypes.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('attachmentOwnerType', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="doc-category-filter-label">{t('attachments.category')}</InputLabel>
          <Select
            labelId="doc-category-filter-label"
            label={t('attachments.category')}
            value={categoryFilter}
            onChange={(event) => setCategoryFilter(event.target.value as AttachmentCategory | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {attachmentCategories.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('attachmentCategory', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Stack>

      <UploadDocumentDialog open={uploading} onClose={() => setUploading(false)} />

      <AttachmentPreviewDialog
        attachment={previewing}
        onClose={() => setPreviewing(null)}
      />

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
                  <TableRow
                    key={document.id}
                    hover
                    onDoubleClick={() => setPreviewing(document)}
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell>{document.fileName}</TableCell>
                    <TableCell>{document.ownerName ?? '—'}</TableCell>
                    <TableCell>
                      {enumLabel('attachmentCategory', document.category)}
                    </TableCell>
                    <TableCell>
                      {document.expiresAt ? (
                        <Chip
                          size="small"
                          color={lapsed ? 'error' : 'warning'}
                          label={
                            lapsed
                              ? t('attachments.expiredOn', {
                                  date: formatDate(document.expiresAt),
                                })
                              : formatDate(document.expiresAt)
                          }
                        />
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          {t('attachments.noExpiry')}
                        </Typography>
                      )}
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
