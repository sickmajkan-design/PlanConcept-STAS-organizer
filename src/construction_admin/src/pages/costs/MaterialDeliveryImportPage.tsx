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
import type { MaterialImportPreview, MaterialImportResult, MaterialImportRowStatus } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import {
  useImportMaterialDeliveries,
  usePreviewMaterialDeliveryImport,
} from '../../features/materials/useMaterials';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney, formatQuantity } from '../../utils/formatting';

const MAX_BYTES = 10 * 1024 * 1024;

/** The header row and one example line, in the column names the importer looks for. */
const TEMPLATE =
  '﻿Materijal;Količina;Jedinica;Nabavna cijena;Broj fakture;Dobavljač;Datum;Napomena\r\n' +
  'Cement CEM II;40;vreća;9,50;INV-2026-0142;Kastel d.o.o.;01.03.2026;\r\n';

function downloadTemplate() {
  const url = URL.createObjectURL(new Blob([TEMPLATE], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a');

  link.href = url;
  link.download = 'prijem-robe-sablon.csv';
  link.click();
  URL.revokeObjectURL(url);
}

const OK_STATUSES: MaterialImportRowStatus[] = ['Ready', 'NewMaterial'];

/**
 * Bring in a whole delivery note: one row per material, checked before
 * anything is saved. The columns are found by name, so a list exported from
 * elsewhere works as long as the headings say what each column holds.
 */
export function MaterialDeliveryImportPage() {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const input = useRef<HTMLInputElement>(null);

  const [file, setFile] = useState<File | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const [preview, setPreview] = useState<MaterialImportPreview | null>(null);
  const [result, setResult] = useState<MaterialImportResult | null>(null);

  const previewMutation = usePreviewMaterialDeliveryImport();
  const importMutation = useImportMaterialDeliveries();

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
      setProblem(t('deliveryImport.badType'));
      return;
    }

    if (chosen.size > MAX_BYTES) {
      setProblem(t('deliveryImport.tooBig'));
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

  const importable = (preview?.readyCount ?? 0) + (preview?.newMaterialCount ?? 0);

  return (
    <Box sx={{ maxWidth: 1000 }}>
      <PageHeader title={t('deliveryImport.title')} description={t('deliveryImport.description')} />

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
              <Typography variant="h6">{t('deliveryImport.done')}</Typography>
            </Stack>
            <Typography>
              {t('deliveryImport.doneSummary', {
                deliveries: result.createdDeliveries,
                materials: result.createdMaterials,
                skipped: result.skippedCount,
              })}
            </Typography>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Button variant="contained" component={RouterLink} to={paths.stockMovements}>
                {t('deliveryImport.toMovements')}
              </Button>
              <Button onClick={reset}>{t('deliveryImport.another')}</Button>
            </Stack>
          </Stack>
        </Paper>
      ) : (
        <Stack spacing={2}>
          <Paper variant="outlined" sx={{ p: 3 }}>
            <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
              <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 640 }}>
                {t('deliveryImport.columnsHint')}
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
                  {file ? t('deliveryImport.chooseOther') : t('deliveryImport.choose')}
                </Button>
                <Button variant="outlined" startIcon={<FileDownloadOutlined />} onClick={downloadTemplate}>
                  {t('deliveryImport.template')}
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
                {preview.readyCount > 0 && (
                  <Chip color="success" label={t('deliveryImport.readyCount', { count: preview.readyCount })} />
                )}
                {preview.newMaterialCount > 0 && (
                  <Chip
                    color="info"
                    label={t('deliveryImport.newCount', { count: preview.newMaterialCount })}
                  />
                )}
                {preview.problemCount > 0 && (
                  <Chip
                    color="error"
                    label={t('deliveryImport.problemCount', { count: preview.problemCount })}
                  />
                )}
                <Box sx={{ flex: 1 }} />
                <Button
                  variant="contained"
                  disabled={importable === 0}
                  loading={importMutation.isPending}
                  onClick={() => void commit()}
                >
                  {t('deliveryImport.import', { count: importable })}
                </Button>
              </Stack>

              <Box sx={{ overflowX: 'auto' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>#</TableCell>
                      <TableCell>{t('materials.name')}</TableCell>
                      <TableCell align="right">{t('movements.quantity')}</TableCell>
                      <TableCell align="right">{t('movements.unitPrice')}</TableCell>
                      <TableCell>{t('movements.invoiceNumber')}</TableCell>
                      <TableCell>{t('movements.supplier')}</TableCell>
                      <TableCell>{t('movements.occurredOn')}</TableCell>
                      <TableCell>{t('deliveryImport.result')}</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {preview.rows.map((row) => (
                      <TableRow key={row.rowNumber} hover>
                        <TableCell>{row.rowNumber}</TableCell>
                        <TableCell>{row.materialName || '—'}</TableCell>
                        <TableCell align="right">
                          {row.quantity == null ? '—' : `${formatQuantity(row.quantity, locale)} ${row.unit ?? ''}`}
                        </TableCell>
                        <TableCell align="right">{formatMoney(row.unitPrice, locale)}</TableCell>
                        <TableCell>{row.invoiceNumber || '—'}</TableCell>
                        <TableCell>{row.supplier || '—'}</TableCell>
                        <TableCell sx={{ whiteSpace: 'nowrap' }}>
                          {row.occurredOn ? formatDate(row.occurredOn) : '—'}
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            variant="outlined"
                            color={
                              row.status === 'NewMaterial'
                                ? 'info'
                                : OK_STATUSES.includes(row.status)
                                  ? 'success'
                                  : 'error'
                            }
                            label={enumLabel('materialImportRowStatus', row.status)}
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Box>
              {preview.totalRows > preview.rows.length && (
                <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
                  {t('deliveryImport.partial', { shown: preview.rows.length, total: preview.totalRows })}
                </Typography>
              )}
            </Paper>
          )}
        </Stack>
      )}
    </Box>
  );
}
