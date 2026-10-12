import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  MenuItem,
  Paper,
  Step,
  StepLabel,
  Stepper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useRef, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  bookImportApi,
  type BookImportInput,
  type BookImportPlan,
  type BookImportResult,
} from '../../api/bookImport';
import { PageHeader } from '../../components/PageHeader';
import {
  detectHeaderRow,
  detectMapping,
  extractRows,
  sheetFields,
  type SheetField,
  type SheetMapping,
} from '../../features/book/importSheet';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

const MAX_BYTES = 20 * 1024 * 1024;
const MAX_ROWS = 5000;

interface Workbook {
  fileName: string;
  sheets: string[];
  read: (sheet: string) => string[][];
}

function monthNow(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

/**
 * Brings a month of the spreadsheet the firm kept before into the settlement, in four steps: the file, which
 * column is what, the check, and the result. The file is read in the browser and not kept; the server checks
 * it row by row and the check shows exactly what will be written.
 */
export function BookImportPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const projects = useAllProjectsQuery();
  const fileInput = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState(0);
  const [workbook, setWorkbook] = useState<Workbook | null>(null);
  const [sheet, setSheet] = useState('');
  const [month, setMonth] = useState(monthNow());
  const [headerRow, setHeaderRow] = useState(0);
  const [mapping, setMapping] = useState<SheetMapping | null>(null);
  const [sectionProjects, setSectionProjects] = useState<Record<string, string | null>>({});
  const [fillEmptyOnly, setFillEmptyOnly] = useState(false);
  const [plan, setPlan] = useState<BookImportPlan | null>(null);
  const [result, setResult] = useState<BookImportResult | null>(null);
  const [readError, setReadError] = useState<string | null>(null);

  const matrix = useMemo(() => (workbook && sheet ? workbook.read(sheet) : []), [workbook, sheet]);
  const rows = useMemo(
    () => (mapping ? extractRows(matrix, headerRow, mapping) : []),
    [matrix, headerRow, mapping],
  );

  const [year, monthNumber] = month.split('-').map(Number);

  const input: BookImportInput | null =
    workbook && mapping && year && monthNumber
      ? {
          fileName: workbook.fileName,
          year,
          month: monthNumber,
          fillEmptyOnly,
          rows,
          sectionProjects,
        }
      : null;

  const history = useQuery({ queryKey: ['book', 'imports'], queryFn: () => bookImportApi.list() });

  const choose = (name: string, workbookRead: Workbook) => {
    const first = workbookRead.sheets[0] ?? '';
    const data = first ? workbookRead.read(first) : [];
    const header = detectHeaderRow(data);

    setWorkbook({ ...workbookRead, fileName: name });
    setSheet(first);
    setHeaderRow(header);
    setMapping(detectMapping(data[header] ?? []));
    setPlan(null);
    setResult(null);
  };

  const onFile = async (file: File | undefined) => {
    setReadError(null);
    if (!file) return;

    if (file.size > MAX_BYTES) {
      setReadError(t('bookImport.tooBig'));
      return;
    }

    try {
      const XLSX = await import('xlsx');
      const workbookRead = XLSX.read(await file.arrayBuffer(), { type: 'array' });

      choose(file.name, {
        fileName: file.name,
        sheets: workbookRead.SheetNames,
        read: (name) => {
          const target = workbookRead.Sheets[name];
          if (!target) return [];
          return (XLSX.utils.sheet_to_json(target, { header: 1, raw: false, defval: '' }) as unknown[][]).map(
            (line) => line.map((cell) => String(cell ?? '')),
          );
        },
      });
    } catch {
      setReadError(t('bookImport.unreadable'));
    }
  };

  const changeSheet = (name: string) => {
    if (!workbook) return;
    const data = workbook.read(name);
    const header = detectHeaderRow(data);

    setSheet(name);
    setHeaderRow(header);
    setMapping(detectMapping(data[header] ?? []));
    setPlan(null);
  };

  const preview = useMutation({
    mutationFn: (value: BookImportInput) => bookImportApi.preview(value),
    onSuccess: (data) => {
      setPlan(data);
      setStep(2);
    },
  });

  const commit = useMutation({
    mutationFn: (value: BookImportInput) => bookImportApi.commit(value),
    onSuccess: (data) => {
      setResult(data);
      setStep(3);
      void queryClient.invalidateQueries({ queryKey: ['book'] });
      void queryClient.invalidateQueries({ queryKey: ['ledgers'] });
    },
  });

  const undo = useMutation({
    mutationFn: (id: string) => bookImportApi.undo(id),
    onSuccess: () => {
      setResult(null);
      setStep(0);
      void queryClient.invalidateQueries({ queryKey: ['book'] });
      void queryClient.invalidateQueries({ queryKey: ['ledgers'] });
    },
  });

  const columnOptions = (matrix[headerRow] ?? []).map((name, index) => ({
    index,
    label: `${index + 1}. ${name || '—'}`,
  }));

  const setField = (field: SheetField, column: number) => {
    if (!mapping) return;
    setMapping({ ...mapping, fields: { ...mapping.fields, [field]: column } });
    setPlan(null);
  };

  const tooMany = rows.length > MAX_ROWS;
  const mappingOk = mapping !== null && mapping.fields.label >= 0 && mapping.weeks.length > 0;
  const error = readError ?? (preview.error ? toApiError(preview.error).message : null)
    ?? (commit.error ? toApiError(commit.error).message : null)
    ?? (undo.error ? toApiError(undo.error).message : null);

  return (
    <Stack spacing={2}>
      <PageHeader title={t('bookImport.title')} description={t('bookImport.description')} />

      <Stepper activeStep={step}>
        {(['file', 'columns', 'check', 'result'] as const).map((key) => (
          <Step key={key}>
            <StepLabel>{t(`bookImport.step.${key}` as MessageKey)}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {error && <Alert severity="error">{error}</Alert>}

      {step === 0 && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {t('bookImport.fileHelp')}
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
              <input
                ref={fileInput}
                type="file"
                accept=".xlsx,.xls"
                hidden
                onChange={(event) => void onFile(event.target.files?.[0])}
              />
              <Button variant="outlined" onClick={() => fileInput.current?.click()}>
                {t('bookImport.chooseFile')}
              </Button>
              {workbook && <Typography variant="body2">{workbook.fileName}</Typography>}
            </Stack>

            {workbook && (
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  select
                  label={t('bookImport.sheet')}
                  value={sheet}
                  onChange={(event) => changeSheet(event.target.value)}
                  sx={{ minWidth: 220 }}
                >
                  {workbook.sheets.map((name) => (
                    <MenuItem key={name} value={name}>
                      {name}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  type="month"
                  label={t('bookImport.month')}
                  value={month}
                  onChange={(event) => setMonth(event.target.value)}
                  slotProps={{ inputLabel: { shrink: true } }}
                />
              </Stack>
            )}

            <Box>
              <Button variant="contained" disabled={!workbook} onClick={() => setStep(1)}>
                {t('bookImport.next')}
              </Button>
            </Box>
          </Stack>
        </Paper>
      )}

      {step === 1 && mapping && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {t('bookImport.columnsHelp')}
            </Typography>

            <TextField
              select
              label={t('bookImport.headerRow')}
              value={headerRow}
              onChange={(event) => {
                const next = Number(event.target.value);
                setHeaderRow(next);
                setMapping(detectMapping(matrix[next] ?? []));
                setPlan(null);
              }}
              sx={{ maxWidth: 360 }}
            >
              {matrix.slice(0, 20).map((line, index) => (
                <MenuItem key={index} value={index}>
                  {index + 1}. {line.filter(Boolean).slice(0, 4).join(' | ') || '—'}
                </MenuItem>
              ))}
            </TextField>

            <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
              {sheetFields.map((field) => (
                <TextField
                  key={field}
                  select
                  size="small"
                  label={t(`bookImport.field.${field}` as MessageKey)}
                  value={mapping.fields[field]}
                  onChange={(event) => setField(field, Number(event.target.value))}
                  sx={{ minWidth: 220 }}
                >
                  <MenuItem value={-1}>
                    <em>{t('bookImport.noColumn')}</em>
                  </MenuItem>
                  {columnOptions.map((option) => (
                    <MenuItem key={option.index} value={option.index}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              ))}
            </Stack>

            <Typography variant="body2">
              {t('bookImport.weeksFound', { count: mapping.weeks.length })} ·{' '}
              {t('bookImport.rowsFound', { count: rows.length })}
            </Typography>

            {!mappingOk && <Alert severity="warning">{t('bookImport.needNameAndWeeks')}</Alert>}
            {tooMany && <Alert severity="error">{t('bookImport.tooManyRows', { max: MAX_ROWS })}</Alert>}

            <FormControlLabel
              control={
                <Checkbox checked={fillEmptyOnly} onChange={(event) => setFillEmptyOnly(event.target.checked)} />
              }
              label={t('bookImport.fillEmpty')}
            />

            <Stack direction="row" spacing={1}>
              <Button onClick={() => setStep(0)}>{t('bookImport.back')}</Button>
              <Button
                variant="contained"
                disabled={!mappingOk || tooMany || rows.length === 0 || !input}
                loading={preview.isPending}
                onClick={() => input && preview.mutate(input)}
              >
                {t('bookImport.check')}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      )}

      {step === 2 && plan && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Chip color="success" variant="outlined" label={t('bookImport.ok', { count: plan.okCount })} />
              <Chip color="warning" variant="outlined" label={t('bookImport.warnings', { count: plan.warningCount })} />
              <Chip color="error" variant="outlined" label={t('bookImport.errors', { count: plan.errorCount })} />
            </Stack>

            {plan.existingLedgerId && plan.existingHasEntries && !fillEmptyOnly && (
              <Alert severity="error">{t('bookImport.monthHasEntries')}</Alert>
            )}

            {plan.unmatchedSections.length > 0 && (
              <Paper variant="outlined" sx={{ p: 2 }}>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('bookImport.unmatched')}
                </Typography>
                <Stack spacing={1}>
                  {plan.unmatchedSections.map((name) => (
                    <TextField
                      key={name}
                      select
                      size="small"
                      label={name}
                      value={sectionProjects[name] ?? ''}
                      onChange={(event) => {
                        setSectionProjects({ ...sectionProjects, [name]: event.target.value || null });
                        setPlan(null);
                        setStep(1);
                      }}
                    >
                      <MenuItem value="">
                        <em>{t('bookImport.noProject')}</em>
                      </MenuItem>
                      {(projects.data?.items ?? []).map((project) => (
                        <MenuItem key={project.id} value={project.id}>
                          {project.name}
                        </MenuItem>
                      ))}
                    </TextField>
                  ))}
                </Stack>
              </Paper>
            )}

            <Box sx={{ overflowX: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{t('bookImport.col.client')}</TableCell>
                    <TableCell>{t('bookImport.col.person')}</TableCell>
                    <TableCell align="right">{t('bookImport.col.hours')}</TableCell>
                    <TableCell>{t('bookImport.col.status')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {plan.rows.map((row) => (
                    <TableRow key={row.index}>
                      <TableCell>{row.projectName ?? row.section}</TableCell>
                      <TableCell>{row.employeeName ?? row.label}</TableCell>
                      <TableCell align="right">{row.hours}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          variant="outlined"
                          color={row.status === 'Ok' ? 'success' : row.status === 'Warning' ? 'warning' : 'error'}
                          label={t(`bookImport.status.${row.status}` as MessageKey)}
                        />
                        {row.messages.map((message) => (
                          <Typography key={message} variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                            {message}
                          </Typography>
                        ))}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>

            <Typography variant="body2">
              {t('bookImport.sheetTotals', {
                hours: plan.sheetHours,
                billing: formatMoney(plan.sheetBilling, locale),
              })}
            </Typography>

            <Stack direction="row" spacing={1}>
              <Button onClick={() => setStep(1)}>{t('bookImport.back')}</Button>
              <Button
                variant="contained"
                disabled={
                  !input
                  || plan.okCount + plan.warningCount === 0
                  || (plan.existingHasEntries && !fillEmptyOnly)
                }
                loading={commit.isPending}
                onClick={() => input && commit.mutate(input)}
              >
                {t('bookImport.import', { count: plan.okCount + plan.warningCount })}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      )}

      {step === 3 && result && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Alert severity={result.hoursDifference === 0 && result.billingDifference === 0 ? 'success' : 'warning'}>
              {t('bookImport.done', { imported: result.imported, skipped: result.skipped })}
            </Alert>

            <Table size="small" sx={{ maxWidth: 520 }}>
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell align="right">{t('bookImport.sheetColumn')}</TableCell>
                  <TableCell align="right">{t('bookImport.ledgerColumn')}</TableCell>
                  <TableCell align="right">{t('bookImport.difference')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell>{t('bookImport.col.hours')}</TableCell>
                  <TableCell align="right">{result.sheetHours}</TableCell>
                  <TableCell align="right">{result.ledgerHours}</TableCell>
                  <TableCell align="right">{result.hoursDifference}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>{t('bookImport.billing')}</TableCell>
                  <TableCell align="right">{formatMoney(result.sheetBilling, locale)}</TableCell>
                  <TableCell align="right">{formatMoney(result.ledgerBilling, locale)}</TableCell>
                  <TableCell align="right">{formatMoney(result.billingDifference, locale)}</TableCell>
                </TableRow>
              </TableBody>
            </Table>

            <Stack direction="row" spacing={1}>
              <Button variant="contained" component={RouterLink} to={`${paths.ledgers}/${result.ledgerId}`}>
                {t('bookImport.openMonth')}
              </Button>
              {result.ledgerCreated && (
                <Button color="error" loading={undo.isPending} onClick={() => undo.mutate(result.batchId)}>
                  {t('bookImport.undo')}
                </Button>
              )}
            </Stack>
          </Stack>
        </Paper>
      )}

      {(history.data?.length ?? 0) > 0 && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
            {t('bookImport.history')}
          </Typography>
          <Table size="small">
            <TableBody>
              {(history.data ?? []).map((batch) => (
                <TableRow key={batch.id}>
                  <TableCell>{formatDate(batch.createdAt)}</TableCell>
                  <TableCell>{batch.fileName}</TableCell>
                  <TableCell>
                    {String(batch.month).padStart(2, '0')}/{batch.year}
                  </TableCell>
                  <TableCell align="right">
                    {t('bookImport.historyRows', { imported: batch.importedCount, skipped: batch.skippedCount })}
                  </TableCell>
                  <TableCell>
                    {batch.status === 'Undone' ? (
                      <Chip size="small" variant="outlined" label={t('bookImport.undone')} />
                    ) : batch.ledgerId ? (
                      <Button size="small" component={RouterLink} to={`${paths.ledgers}/${batch.ledgerId}`}>
                        {t('bookImport.openMonth')}
                      </Button>
                    ) : null}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
    </Stack>
  );
}
