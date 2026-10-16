import { DeleteOutlined, EditOutlined, UploadFileOutlined } from '@mui/icons-material';
import {
  Box,
  Chip,
  IconButton,
  Tooltip,
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
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import type { Attachment, AttachmentCategory, AttachmentOwnerType } from '../../api/types';
import { attachmentCategories, attachmentOwnerTypes } from '../../api/types';
import { AttachmentPreviewDialog } from '../../components/AttachmentPreviewDialog';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EditDocumentDialog } from '../../components/EditDocumentDialog';
import { ExportZipButton } from '../../components/ExportZipButton';
import { FirstUseHint, HintLine } from '../../components/files/FirstUseHint';
import { PageDropOverlay } from '../../components/files/PageDropOverlay';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { UploadDocumentDialog } from '../../components/UploadDocumentDialog';
import { useDeleteAttachment, useExpiringDocumentsQuery } from '../../features/attachments/useAttachments';
import { usePageDrop } from '../../features/files/usePageDrop';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

const WINDOWS = [7, 30, 90, 180] as const;
/** Distinct from every numeric window — selects "no cutoff" on the query. */
const ALL_WINDOW = 'all';

type WindowValue = (typeof WINDOWS)[number] | typeof ALL_WINDOW;

/** A retention date still in the future blocks deletion, same as on the record's own page. */
const isRetained = (retainUntil: string | null) =>
  !!retainUntil && new Date(`${retainUntil}T00:00`).getTime() > Date.now();

type SortField = 'fileName' | 'ownerName' | 'category' | 'expiresAt' | 'retainUntil';
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
  const { targetId, isHighlighted, scrollIntoViewOnce } = useHighlightTarget();
  // A notification deep-link may point at a document outside the default
  // 30-day/dated-only view — widen the filters so the row it leads to is
  // actually there to land on, rather than opening to an empty-looking table.
  const [windowValue, setWindowValue] = useState<WindowValue>(targetId ? ALL_WINDOW : 30);
  const [includeUndated, setIncludeUndated] = useState(Boolean(targetId));
  const [ownerTypeFilter, setOwnerTypeFilter] = useState<AttachmentOwnerType | ''>('');
  const [categoryFilter, setCategoryFilter] = useState<AttachmentCategory | ''>('');
  const [search, setSearch] = useState('');
  const [tagFilter, setTagFilter] = useState('');
  const [uploading, setUploading] = useState(false);
  const [dropped, setDropped] = useState<File[]>([]);
  const [previewing, setPreviewing] = useState<Attachment | null>(null);
  const [editing, setEditing] = useState<Attachment | null>(null);
  const remove = useDeleteWithConfirm<Attachment>(useDeleteAttachment());

  // A file dropped anywhere on the page opens the upload with it already in.
  const pageDrop = usePageDrop({
    disabled: uploading,
    onFiles: (files) => {
      setDropped(files);
      setUploading(true);
    },
  });

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

  /** The labels in use on the documents currently loaded, for the tag filter. */
  const availableTags = useMemo(
    () =>
      [...new Set((data ?? []).flatMap((document) => document.tags ?? []))].sort((a, b) =>
        a.localeCompare(b),
      ),
    [data],
  );

  const sortedData = useMemo(() => {
    if (!data) return data;

    // Every word typed has to be found somewhere in the title, file name, labels, owner or note.
    const words = search.toLocaleLowerCase().split(/\s+/).filter(Boolean);

    const matchesSearch = (document: Attachment) => {
      if (words.length === 0) return true;
      const haystack = [
        document.title,
        document.fileName,
        document.ownerName,
        document.description,
        ...(document.tags ?? []),
      ]
        .join(' ')
        .toLocaleLowerCase();
      return words.every((word) => haystack.includes(word));
    };

    const filtered = data.filter(
      (document) =>
        (!ownerTypeFilter || document.ownerType === ownerTypeFilter) &&
        (!categoryFilter || document.category === categoryFilter) &&
        (!tagFilter || (document.tags ?? []).includes(tagFilter)) &&
        matchesSearch(document),
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
        case 'retainUntil':
          return (a.retainUntil ?? '').localeCompare(b.retainUntil ?? '') * factor;
        default:
          return 0;
      }
    };

    return filtered.sort(compare);
  }, [data, sortBy, sortDirection, ownerTypeFilter, categoryFilter, tagFilter, search]);

  useEffect(() => {
    if (!targetId || !sortedData) return;
    const match = sortedData.find((document) => document.id === targetId);
    if (match) setPreviewing(match);
    // Runs once the deep-linked document is available, not on every refetch.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [targetId, !!sortedData]);

  return (
    <Box>
      <PageHeader
        title={t('attachments.expiringTitle')}
        description={t('attachments.expiringDescription')}
        action={{
          label: t('attachments.upload'),
          icon: <UploadFileOutlined />,
          onClick: () => {
            setDropped([]);
            setUploading(true);
          },
        }}
      />

      <Box sx={{ mb: 2 }}>
        <FirstUseHint id="hub-search" title={t('files.hint.search.title')}>
          <HintLine>{t('files.hint.search.text')}</HintLine>
          <HintLine>{t('files.hint.search.tag')}</HintLine>
          <HintLine>{t('files.hint.search.scope')}</HintLine>
        </FirstUseHint>
      </Box>

      <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: 'wrap', rowGap: 1.5, alignItems: 'center' }}>
        <TextField
          size="small"
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={t('attachments.searchPlaceholder')}
          slotProps={{ htmlInput: { 'aria-label': t('attachments.searchPlaceholder') } }}
          sx={{ minWidth: 260 }}
        />

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

        {availableTags.length > 0 && (
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="doc-tag-filter-label">{t('attachments.tag')}</InputLabel>
            <Select
              labelId="doc-tag-filter-label"
              label={t('attachments.tag')}
              value={tagFilter}
              onChange={(event) => setTagFilter(event.target.value)}
            >
              <MenuItem value="">
                <em>{t('common.all')}</em>
              </MenuItem>
              {availableTags.map((tag) => (
                <MenuItem key={tag} value={tag}>
                  {tag}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}
      </Stack>

      <Stack direction="row" sx={{ mb: 1, justifyContent: 'flex-end' }}>
        <ExportZipButton ids={(sortedData ?? []).map((document) => document.id)} />
      </Stack>

      <UploadDocumentDialog open={uploading} onClose={() => setUploading(false)} initialFiles={dropped} />
      <PageDropOverlay show={pageDrop.isOver} />

      <AttachmentPreviewDialog
        attachment={previewing}
        onClose={() => setPreviewing(null)}
      />

      <EditDocumentDialog attachment={editing} onClose={() => setEditing(null)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('attachments.deleteTitle')}
        description={
          remove.pending ? t('attachments.deleteBody', { name: remove.pending.fileName }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Typography variant="body2" color="error" sx={{ mb: 1 }}>
          {remove.error.message}
        </Typography>
      )}

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {sortedData && (
        <Paper sx={{ overflowX: 'auto' }}>
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
                <TableCell sortDirection={sortBy === 'retainUntil' ? sortDirection : false}>
                  <TableSortLabel
                    active={sortBy === 'retainUntil'}
                    direction={sortBy === 'retainUntil' ? sortDirection : 'asc'}
                    onClick={() => toggleSort('retainUntil')}
                  >
                    {t('attachments.retainUntil')}
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right" />
              </TableRow>
            </TableHead>
            <TableBody>
              {sortedData.map((document) => {
                const lapsed =
                  !!document.expiresAt &&
                  new Date(`${document.expiresAt}T00:00`).getTime() < Date.now();

                const retentionEnded =
                  !!document.retainUntil &&
                  new Date(`${document.retainUntil}T00:00`).getTime() < Date.now();

                return (
                  <TableRow
                    key={document.id}
                    ref={(element: HTMLTableRowElement | null) => {
                      if (document.id === targetId) scrollIntoViewOnce(element);
                    }}
                    hover
                    onDoubleClick={() => setPreviewing(document)}
                    sx={{
                      cursor: 'pointer',
                      transition: 'background-color 1.5s ease',
                      bgcolor: isHighlighted(document.id) ? 'action.hover' : undefined,
                    }}
                  >
                    <TableCell>
                      {document.title || document.fileName}
                      {(document.tags ?? []).length > 0 && (
                        <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap', mt: 0.5 }}>
                          {(document.tags ?? []).map((tag) => (
                            <Chip key={tag} size="small" variant="outlined" label={tag} />
                          ))}
                        </Stack>
                      )}
                    </TableCell>
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
                    <TableCell>
                      {document.retainUntil ? (
                        <Chip
                          size="small"
                          variant="outlined"
                          color={retentionEnded ? 'success' : 'info'}
                          label={
                            retentionEnded
                              ? t('attachments.retentionEnded', {
                                  date: formatDate(document.retainUntil),
                                })
                              : t('attachments.retainedUntil', {
                                  date: formatDate(document.retainUntil),
                                })
                          }
                        />
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          —
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      <Tooltip title={t('common.edit')}>
                        <IconButton size="small" onClick={() => setEditing(document)}>
                          <EditOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip
                        title={
                          isRetained(document.retainUntil)
                            ? t('attachments.retainedCannotDelete', {
                                date: formatDate(document.retainUntil!),
                              })
                            : t('common.delete')
                        }
                      >
                        <span>
                          <IconButton
                            size="small"
                            disabled={isRetained(document.retainUntil)}
                            onClick={() => remove.request(document)}
                          >
                            <DeleteOutlined fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                );
              })}

              {sortedData.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography variant="body2" color="text.secondary">
                      {search || tagFilter || ownerTypeFilter || categoryFilter
                        ? t('attachments.searchEmpty')
                        : t('attachments.expiringEmpty')}
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
