import { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Step,
  StepLabel,
  Stepper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';

import { FUEL_STATEMENT_ACCEPTED_EXTENSIONS, MAX_FUEL_STATEMENT_BYTES } from '../../api/fuelCards';
import type { FuelImportColumnMapping, FuelImportPreviewResult, FuelImportResult } from '../../api/types';
import { toApiError } from '../../api/apiError';
import { PageHeader } from '../../components/PageHeader';
import { usePreviewFuelImport, useImportFuelTransactions } from '../../features/fuelCards/useFuelCards';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatMoney } from '../../utils/formatting';

interface ColumnOption {
  index: number;
  label: string;
}

/**
 * Header detection is best-effort, since there is no real DKV sample to
 * calibrate against yet. A .csv's first line is read client-side to show real
 * column names; a .xlsx cannot be parsed without a heavier dependency than
 * this wizard warrants, so it falls back to generic "Column N" labels — the
 * mapping still works, it is just less self-explanatory to fill in.
 */
async function detectColumns(file: File): Promise<string[]> {
  if (file.name.toLowerCase().endsWith('.csv')) {
    const text = await file.text();
    const firstLine = text.split(/\r\n|\n/).find((line) => line.length > 0) ?? '';
    const cells = firstLine
      .split(/[,;]/)
      .map((cell) => cell.trim().replace(/^"|"$/g, ''));

    if (cells.length > 1) {
      return cells;
    }
  }

  return Array.from({ length: 20 }, (_, i) => `Column ${i + 1}`);
}

const REQUIRED_FIELDS = ['cardNumberColumn', 'occurredOnColumn', 'amountColumn', 'litresColumn'] as const;

type MappingState = {
  cardNumberColumn: number | '';
  occurredOnColumn: number | '';
  amountColumn: number | '';
  litresColumn: number | '';
  supplierColumn: number | '';
  noteColumn: number | '';
  odometerColumn: number | '';
  fuelProductTypeColumn: number | '';
};

const emptyMapping: MappingState = {
  cardNumberColumn: '',
  occurredOnColumn: '',
  amountColumn: '',
  litresColumn: '',
  supplierColumn: '',
  noteColumn: '',
  odometerColumn: '',
  fuelProductTypeColumn: '',
};

export function FuelImportPage() {
  const t = useT();
  const { locale } = useI18n();
  const [step, setStep] = useState(0);
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [columns, setColumns] = useState<ColumnOption[]>([]);
  const [hasHeaderRow, setHasHeaderRow] = useState(true);
  const [mapping, setMapping] = useState<MappingState>(emptyMapping);
  const [preview, setPreview] = useState<FuelImportPreviewResult | null>(null);
  const [result, setResult] = useState<FuelImportResult | null>(null);

  const previewMutation = usePreviewFuelImport();
  const importMutation = useImportFuelTransactions();

  const columnOptions = useMemo(
    () =>
      columns.map((c, i) => ({
        index: i,
        label: hasHeaderRow ? c.label || `Column ${i + 1}` : `Column ${i + 1}`,
      })),
    [columns, hasHeaderRow],
  );

  const handleFileChange = async (selected: File | null) => {
    setFileError(null);
    setFile(selected);
    setColumns([]);

    if (!selected) return;

    const extension = selected.name.slice(selected.name.lastIndexOf('.')).toLowerCase();
    if (!FUEL_STATEMENT_ACCEPTED_EXTENSIONS.split(',').includes(extension)) {
      setFileError(t('fuelImport.invalidFileType'));
      setFile(null);
      return;
    }

    if (selected.size > MAX_FUEL_STATEMENT_BYTES) {
      setFileError(t('fuelImport.fileTooLarge'));
      setFile(null);
      return;
    }

    const detected = await detectColumns(selected);
    setColumns(detected.map((label) => ({ index: 0, label })));
  };

  const mappingComplete = REQUIRED_FIELDS.every((field) => mapping[field] !== '');

  const toApiMapping = (): FuelImportColumnMapping => ({
    cardNumberColumn: Number(mapping.cardNumberColumn),
    occurredOnColumn: Number(mapping.occurredOnColumn),
    amountColumn: Number(mapping.amountColumn),
    litresColumn: Number(mapping.litresColumn),
    supplierColumn: mapping.supplierColumn === '' ? null : Number(mapping.supplierColumn),
    noteColumn: mapping.noteColumn === '' ? null : Number(mapping.noteColumn),
    odometerColumn: mapping.odometerColumn === '' ? null : Number(mapping.odometerColumn),
    fuelProductTypeColumn:
      mapping.fuelProductTypeColumn === '' ? null : Number(mapping.fuelProductTypeColumn),
  });

  const handlePreview = () => {
    if (!file) return;

    previewMutation.mutate(
      { file, mapping: toApiMapping(), hasHeaderRow },
      {
        onSuccess: (data) => {
          setPreview(data);
          setResult(null);
          setStep(2);
        },
      },
    );
  };

  const handleConfirm = () => {
    if (!file) return;

    importMutation.mutate(
      { file, mapping: toApiMapping(), hasHeaderRow },
      { onSuccess: (data) => setResult(data) },
    );
  };

  const handleStartOver = () => {
    setStep(0);
    setFile(null);
    setColumns([]);
    setMapping(emptyMapping);
    setPreview(null);
    setResult(null);
  };

  return (
    <Box>
      <PageHeader title={t('fuelImport.title')} description={t('fuelImport.description')} />

      <Stepper activeStep={step} sx={{ mb: 4 }}>
        <Step>
          <StepLabel>{t('fuelImport.stepUpload')}</StepLabel>
        </Step>
        <Step>
          <StepLabel>{t('fuelImport.stepMap')}</StepLabel>
        </Step>
        <Step>
          <StepLabel>{t('fuelImport.stepReview')}</StepLabel>
        </Step>
      </Stepper>

      {step === 0 && (
        <Paper sx={{ p: 3 }}>
          <Stack spacing={2} sx={{ maxWidth: 480 }}>
            {fileError && <Alert severity="error">{fileError}</Alert>}
            <Button variant="outlined" component="label">
              {t('fuelImport.chooseFile')}
              <input
                type="file"
                hidden
                accept={FUEL_STATEMENT_ACCEPTED_EXTENSIONS}
                onChange={(event) => void handleFileChange(event.target.files?.[0] ?? null)}
              />
            </Button>
            {file && (
              <Typography variant="body2">
                {t('fuelImport.chosenFile')}: <strong>{file.name}</strong>
              </Typography>
            )}
            <Box>
              <Button variant="contained" disabled={!file || columns.length === 0} onClick={() => setStep(1)}>
                {t('fuelImport.continue')}
              </Button>
            </Box>
          </Stack>
        </Paper>
      )}

      {step === 1 && (
        <Paper sx={{ p: 3 }}>
          <Stack spacing={2} sx={{ maxWidth: 480 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={hasHeaderRow}
                  onChange={(event) => setHasHeaderRow(event.target.checked)}
                />
              }
              label={t('fuelImport.hasHeaderRow')}
            />

            <ColumnSelect
              label={t('fuelImport.mapCardNumber')}
              value={mapping.cardNumberColumn}
              options={columnOptions}
              onChange={(v) => setMapping((m) => ({ ...m, cardNumberColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapOccurredOn')}
              value={mapping.occurredOnColumn}
              options={columnOptions}
              onChange={(v) => setMapping((m) => ({ ...m, occurredOnColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapAmount')}
              value={mapping.amountColumn}
              options={columnOptions}
              onChange={(v) => setMapping((m) => ({ ...m, amountColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapLitres')}
              value={mapping.litresColumn}
              options={columnOptions}
              onChange={(v) => setMapping((m) => ({ ...m, litresColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapSupplier')}
              value={mapping.supplierColumn}
              options={columnOptions}
              optional
              onChange={(v) => setMapping((m) => ({ ...m, supplierColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapNote')}
              value={mapping.noteColumn}
              options={columnOptions}
              optional
              onChange={(v) => setMapping((m) => ({ ...m, noteColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapOdometer')}
              value={mapping.odometerColumn}
              options={columnOptions}
              optional
              onChange={(v) => setMapping((m) => ({ ...m, odometerColumn: v }))}
            />
            <ColumnSelect
              label={t('fuelImport.mapFuelProductType')}
              value={mapping.fuelProductTypeColumn}
              options={columnOptions}
              optional
              onChange={(v) => setMapping((m) => ({ ...m, fuelProductTypeColumn: v }))}
            />

            {previewMutation.isError && (
              <Alert severity="error">{toApiError(previewMutation.error).message}</Alert>
            )}

            <Stack direction="row" spacing={2}>
              <Button onClick={() => setStep(0)}>{t('fuelImport.back')}</Button>
              <Button
                variant="contained"
                disabled={!mappingComplete}
                loading={previewMutation.isPending}
                onClick={handlePreview}
              >
                {t('fuelImport.preview')}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      )}

      {step === 2 && preview && (
        <Paper sx={{ p: 3 }}>
          <Stack spacing={2}>
            {!result && (
              <>
                <Stack direction="row" spacing={2}>
                  <Chip color="success" label={t('fuelImport.summaryReady', { count: preview.readyCount })} />
                  <Chip
                    color={preview.problemCount > 0 ? 'warning' : 'default'}
                    label={t('fuelImport.summaryProblems', { count: preview.problemCount })}
                  />
                  <Chip label={t('fuelImport.summaryTotal', { count: preview.totalRows })} />
                </Stack>

                {preview.totalRows > preview.rows.length && (
                  <Typography variant="caption" color="text.secondary">
                    {t('fuelImport.previewLimited', {
                      shown: preview.rows.length,
                      total: preview.totalRows,
                    })}
                  </Typography>
                )}

                <PreviewTable rows={preview.rows} locale={locale} />

                {importMutation.isError && (
                  <Alert severity="error">{toApiError(importMutation.error).message}</Alert>
                )}

                <Stack direction="row" spacing={2}>
                  <Button onClick={() => setStep(1)}>{t('fuelImport.back')}</Button>
                  <Button
                    variant="contained"
                    disabled={preview.readyCount === 0}
                    loading={importMutation.isPending}
                    onClick={handleConfirm}
                  >
                    {t('fuelImport.confirmImport')}
                  </Button>
                </Stack>
              </>
            )}

            {result && (
              <>
                <Alert severity="success">{t('fuelImport.resultTitle')}</Alert>
                <Typography>{t('fuelImport.resultCreated', { count: result.createdCount })}</Typography>
                <Typography>{t('fuelImport.resultSkipped', { count: result.skippedCount })}</Typography>

                {result.skipped.length > 0 && (
                  <Stack spacing={0.5}>
                    {result.skipped.map((row) => (
                      <Typography key={row.rowNumber} variant="body2" color="text.secondary">
                        {t('fuelImport.resultSkippedReason', {
                          rowNumber: row.rowNumber,
                          cardNumber: row.cardNumber ?? '—',
                          reason: row.reason,
                        })}
                      </Typography>
                    ))}
                  </Stack>
                )}

                <Box>
                  <Button variant="outlined" onClick={handleStartOver}>
                    {t('fuelImport.startOver')}
                  </Button>
                </Box>
              </>
            )}
          </Stack>
        </Paper>
      )}
    </Box>
  );
}

function ColumnSelect({
  label,
  value,
  options,
  optional,
  onChange,
}: {
  label: string;
  value: number | '';
  options: ColumnOption[];
  optional?: boolean;
  onChange: (value: number | '') => void;
}) {
  const t = useT();

  return (
    <FormControl size="small" fullWidth>
      <InputLabel>{label}</InputLabel>
      <Select
        label={label}
        value={value}
        onChange={(event) => {
          const raw = event.target.value as number | '';
          onChange(raw === '' ? '' : Number(raw));
        }}
      >
        {optional && (
          <MenuItem value="">
            <em>{t('fuelImport.mapNone')}</em>
          </MenuItem>
        )}
        {options.map((option) => (
          <MenuItem key={option.index} value={option.index}>
            {option.label}
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

function PreviewTable({
  rows,
  locale,
}: {
  rows: FuelImportPreviewResult['rows'];
  locale: string;
}) {
  const t = useT();

  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>{t('fuelImport.rowNumber')}</TableCell>
            <TableCell>{t('fuelImport.rowCard')}</TableCell>
            <TableCell>{t('fuelImport.rowVehicle')}</TableCell>
            <TableCell>{t('fuelImport.rowDate')}</TableCell>
            <TableCell align="right">{t('fuelImport.rowAmount')}</TableCell>
            <TableCell align="right">{t('fuelImport.rowLitres')}</TableCell>
            <TableCell align="right">{t('fuelImport.rowOdometer')}</TableCell>
            <TableCell>{t('fuelImport.rowFuelProductType')}</TableCell>
            <TableCell>{t('fuelImport.rowStatus')}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.rowNumber}>
              <TableCell>{row.rowNumber}</TableCell>
              <TableCell>{row.cardNumber ?? '—'}</TableCell>
              <TableCell>{row.vehicleName ?? '—'}</TableCell>
              <TableCell>{row.occurredOn ?? '—'}</TableCell>
              <TableCell align="right">
                {row.amount != null ? formatMoney(row.amount, locale) : '—'}
              </TableCell>
              <TableCell align="right">{row.litres ?? '—'}</TableCell>
              <TableCell align="right">{row.odometerKm ?? '—'}</TableCell>
              <TableCell>{row.fuelProductType ?? '—'}</TableCell>
              <TableCell>
                <Chip
                  size="small"
                  color={row.status === 'Ready' ? 'success' : 'warning'}
                  label={t(`fuelImport.status.${row.status}`)}
                />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
