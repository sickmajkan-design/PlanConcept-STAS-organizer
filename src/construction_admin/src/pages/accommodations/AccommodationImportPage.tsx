import { CheckCircleOutlined, FileDownloadOutlined, UploadFileOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useRef, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { AccommodationImportPreview, AccommodationImportResult } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import {
  useImportAccommodations,
  usePreviewAccommodationImport,
} from '../../features/accommodations/useAccommodations';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

const MAX_BYTES = 10 * 1024 * 1024;

/** The header row and two example lines (two people in one flat), in the column names the importer looks for. */
const TEMPLATE =
  '﻿Adresa;Naziv;Grad;Tip;Sprat;Sobe;Ležajevi;Kirija;Radnik;Useljenje;Iseljenje;Gradilište\r\n' +
  'Ulica Kralja Petra 12;Stan Petra;Banja Luka;Stan;3;3;4;650,00;R-001;01.09.2026;;Naziv gradilišta\r\n' +
  'Ulica Kralja Petra 12;;;;;;;;R-002;01.09.2026;;Naziv gradilišta\r\n';

function downloadTemplate() {
  const url = URL.createObjectURL(new Blob([TEMPLATE], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a');

  link.href = url;
  link.download = 'smjestaj-sablon.csv';
  link.click();
  URL.revokeObjectURL(url);
}

/**
 * Bring in a whole housing list: one row per person, checked before anything
 * is saved. Columns are found by name, so a list kept in Excel works as long
 * as the headings say what each column holds.
 */
export function AccommodationImportPage() {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const input = useRef<HTMLInputElement>(null);

  const [file, setFile] = useState<File | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const [preview, setPreview] = useState<AccommodationImportPreview | null>(null);
  const [result, setResult] = useState<AccommodationImportResult | null>(null);

  const previewMutation = usePreviewAccommodationImport();
  const importMutation = useImportAccommodations();

  const reset = () => {
    setFile(null);
    setPreview(null);
    setResult(null);
    setProblem(null);
  };

  const choose = async (chosen: File | undefined) => {
    setProblem(null);
    setPreview(null);
    setResult(null);

    if (!chosen) return;

    const extension = chosen.name.slice(chosen.name.lastIndexOf('.')).toLowerCase();

    if (extension !== '.csv' && extension !== '.xlsx') {
      setProblem(t('accommodationImport.badType'));
      return;
    }

    if (chosen.size > MAX_BYTES) {
      setProblem(t('accommodationImport.tooBig'));
      return;
    }

    setFile(chosen);

    try {
      setPreview(await previewMutation.mutateAsync(chosen));
    } catch (err) {
      setProblem(toApiError(err).message);
    }
  };

  const commit = async () => {
    if (!file) return;

    try {
      setResult(await importMutation.mutateAsync(file));
    } catch (err) {
      setProblem(toApiError(err).message);
    }
  };

  const importable = preview ? preview.totalRows - preview.problemCount : 0;

  return (
    <Box sx={{ maxWidth: 1100 }}>
      <PageHeader
        title={t('accommodationImport.title')}
        description={t('accommodationImport.description')}
      />

      {problem && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {problem}
        </Alert>
      )}

      {result ? (
        <Paper variant="outlined" sx={{ p: 3 }}>
          <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CheckCircleOutlined color="success" />
              <Typography variant="h6">{t('accommodationImport.done')}</Typography>
            </Stack>
            <Typography>
              {t('accommodationImport.doneSummary', {
                accommodations: result.createdAccommodations,
                stays: result.createdStays,
                skipped: result.skippedCount,
              })}
            </Typography>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Button variant="contained" component={RouterLink} to={paths.accommodations}>
                {t('accommodationImport.toList')}
              </Button>
              <Button onClick={reset}>{t('accommodationImport.another')}</Button>
            </Stack>
          </Stack>
        </Paper>
      ) : (
        <Stack spacing={2}>
          <Paper variant="outlined" sx={{ p: 3 }}>
            <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
              <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 680 }}>
                {t('accommodationImport.columnsHint')}
              </Typography>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                <input
                  ref={input}
                  type="file"
                  hidden
                  accept=".csv,.xlsx"
                  onChange={(event) => {
                    void choose(event.target.files?.[0]);
                    event.target.value = '';
                  }}
                />
                <Button
                  variant="contained"
                  startIcon={<UploadFileOutlined />}
                  loading={previewMutation.isPending}
                  onClick={() => input.current?.click()}
                >
                  {file ? t('accommodationImport.chooseOther') : t('accommodationImport.choose')}
                </Button>
                <Button variant="outlined" startIcon={<FileDownloadOutlined />} onClick={downloadTemplate}>
                  {t('accommodationImport.template')}
                </Button>
              </Stack>
              {file && (
                <Typography variant="body2" color="text.secondary">
                  {file.name}
                </Typography>
              )}
            </Stack>
          </Paper>

          {preview && (
            <Paper variant="outlined">
              <Stack
                direction="row"
                spacing={1}
                useFlexGap
                sx={{ p: 2, flexWrap: 'wrap', alignItems: 'center' }}
              >
                {preview.newAccommodationCount > 0 && (
                  <Chip
                    color="info"
                    label={t('accommodationImport.newAccommodations', { count: preview.newAccommodationCount })}
                  />
                )}
                {preview.newStayCount > 0 && (
                  <Chip
                    color="success"
                    label={t('accommodationImport.newStays', { count: preview.newStayCount })}
                  />
                )}
                {preview.problemCount > 0 && (
                  <Chip
                    color="error"
                    label={t('accommodationImport.problemCount', { count: preview.problemCount })}
                  />
                )}
                <Box sx={{ flex: 1 }} />
                <Button
                  variant="contained"
                  disabled={preview.newAccommodationCount + preview.newStayCount === 0 || importable === 0}
                  loading={importMutation.isPending}
                  onClick={() => void commit()}
                >
                  {t('accommodationImport.import')}
                </Button>
              </Stack>

              <Box sx={{ overflowX: 'auto' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>#</TableCell>
                      <TableCell>{t('accommodations.address')}</TableCell>
                      <TableCell align="right">{t('accommodations.currentMonthlyAmount')}</TableCell>
                      <TableCell>{t('employees.title')}</TableCell>
                      <TableCell>{t('accommodations.startDate')}</TableCell>
                      <TableCell>{t('accommodationImport.creates')}</TableCell>
                      <TableCell>{t('accommodationImport.result')}</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {preview.rows.map((row) => (
                      <TableRow key={row.rowNumber} hover>
                        <TableCell>{row.rowNumber}</TableCell>
                        <TableCell>{row.name || row.address || '—'}</TableCell>
                        <TableCell align="right">{formatMoney(row.monthlyRent, locale)}</TableCell>
                        <TableCell>{row.employeeName || row.employeeText || '—'}</TableCell>
                        <TableCell sx={{ whiteSpace: 'nowrap' }}>
                          {row.moveIn ? formatDate(row.moveIn) : '—'}
                        </TableCell>
                        <TableCell>
                          {[
                            row.createsAccommodation ? t('accommodationImport.createsAccommodation') : null,
                            row.createsStay ? t('accommodationImport.createsStay') : null,
                          ]
                            .filter(Boolean)
                            .join(' + ') || '—'}
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            variant="outlined"
                            color={row.status === 'Ready' ? 'success' : 'error'}
                            label={enumLabel('accommodationImportRowStatus', row.status)}
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Box>
              {preview.totalRows > preview.rows.length && (
                <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
                  {t('accommodationImport.partial', { shown: preview.rows.length, total: preview.totalRows })}
                </Typography>
              )}
            </Paper>
          )}
        </Stack>
      )}
    </Box>
  );
}
