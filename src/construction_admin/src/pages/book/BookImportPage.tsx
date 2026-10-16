import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  MenuItem,
  Paper,
  Stack,
  Step,
  StepLabel,
  Stepper,
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
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import {
  detectHeaderRow,
  detectMapping,
  extractRows,
  type SheetField,
  type SheetMapping,
} from '../../features/book/importSheet';
import { DropArea } from '../../components/files/DropArea';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

const MAX_BYTES = 20 * 1024 * 1024;
const MAX_ROWS = 5000;

/** What a column of the sheet means. "week" can be given to several columns. */
type Meaning = SheetField | 'week' | 'skip';

const meanings: Meaning[] = [
  'label',
  'week',
  'workerRate',
  'clientRate',
  'advance',
  'difference',
  'contributions',
  'fuel',
  'housing',
  'holiday',
  'bonus',
  'skip',
];

interface Workbook {
  fileName: string;
  sheets: string[];
  read: (sheet: string) => string[][];
}

/** A small made-up month, so the steps can be tried without a file. */
const sample: string[][] = [
  ['Ime i prezime', 'KW40', 'KW41', 'KW42', 'KW43', 'SATNICA', 'SATNICA WH/TRANE', 'AKONTACIJA', 'GORIVO'],
  ['Hala B', '', '', '', '', '', '', '', ''],
  ['Marko Primjer', '40', '40', '38', '40', '20', '33', '500', '120'],
  ['Ivan Primjer', '40', '36', '40', '40', '21', '33', '', ''],
  ['Ana Primjer', '40', '40', '40', '8', '19', '33', '', '60'],
  ['Skladište C', '', '', '', '', '', '', '', ''],
  ['Petar Primjer', '40', '40', '40', 'x', '20', '31', '300', ''],
  ['Kooperanti', '120', '120', '110', '120', '15', '28', '', ''],
];

function monthNow(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

/** Turns "this column means that" back into the layout the reader works from. */
function mappingOf(columns: number, meaningOf: Record<number, Meaning>): SheetMapping {
  const fields = {
    label: -1,
    workerRate: -1,
    clientRate: -1,
    advance: -1,
    difference: -1,
    contributions: -1,
    fuel: -1,
    housing: -1,
    holiday: -1,
    bonus: -1,
  } as Record<SheetField, number>;
  const weeks: number[] = [];

  for (let index = 0; index < columns; index++) {
    const meaning = meaningOf[index] ?? 'skip';

    if (meaning === 'week') {
      if (weeks.length < 6) weeks.push(index);
    } else if (meaning !== 'skip' && fields[meaning] < 0) {
      fields[meaning] = index;
    }
  }

  return { fields, weeks };
}

/**
 * Brings a month of the old spreadsheet in, in the four steps of the mockup: the file, which column means what,
 * the check of every row, and the confirmation. The file is read in the browser and not kept; the server checks
 * each row and the check shows exactly what will be written.
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
  const [meaningOf, setMeaningOf] = useState<Record<number, Meaning>>({});
  const [sectionProjects, setSectionProjects] = useState<Record<string, string | null>>({});
  const [fillEmptyOnly, setFillEmptyOnly] = useState(false);
  const [createUnknown, setCreateUnknown] = useState(false);
  const [plan, setPlan] = useState<BookImportPlan | null>(null);
  const [result, setResult] = useState<BookImportResult | null>(null);
  const [readError, setReadError] = useState<string | null>(null);

  const matrix = useMemo(() => (workbook && sheet ? workbook.read(sheet) : []), [workbook, sheet]);
  const columnCount = Math.max(0, ...matrix.slice(0, 40).map((line) => line.length));

  const mapping = useMemo(() => mappingOf(columnCount, meaningOf), [columnCount, meaningOf]);
  const rows = useMemo(() => extractRows(matrix, headerRow, mapping), [matrix, headerRow, mapping]);

  const [year, monthNumber] = month.split('-').map(Number);

  const input: BookImportInput | null =
    workbook && year && monthNumber
      ? {
          fileName: workbook.fileName,
          year,
          month: monthNumber,
          fillEmptyOnly,
          createUnknownEmployees: createUnknown,
          rows,
          sectionProjects,
        }
      : null;

  const history = useQuery({ queryKey: ['book', 'imports'], queryFn: () => bookImportApi.list() });

  const adopt = (loaded: Workbook, sheetName: string) => {
    const data = loaded.read(sheetName);
    const header = detectHeaderRow(data);
    const detected = detectMapping(data[header] ?? []);
    const next: Record<number, Meaning> = {};

    (Object.keys(detected.fields) as SheetField[]).forEach((field) => {
      const column = detected.fields[field];
      if (column >= 0) next[column] = field;
    });
    detected.weeks.forEach((column) => {
      next[column] = 'week';
    });

    setWorkbook(loaded);
    setSheet(sheetName);
    setHeaderRow(header);
    setMeaningOf(next);
    setPlan(null);
    setResult(null);
  };

  const loadSample = () => {
    setReadError(null);
    adopt({ fileName: t('bookImport.sampleName'), sheets: ['Primjer'], read: () => sample }, 'Primjer');
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
      const book = XLSX.read(await file.arrayBuffer(), { type: 'array' });

      adopt(
        {
          fileName: file.name,
          sheets: book.SheetNames,
          read: (name) => {
            const target = book.Sheets[name];
            if (!target) return [];
            return (XLSX.utils.sheet_to_json(target, { header: 1, raw: false, defval: '' }) as unknown[][]).map(
              (line) => line.map((cell) => String(cell ?? '')),
            );
          },
        },
        book.SheetNames[0] ?? '',
      );
    } catch {
      setReadError(t('bookImport.unreadable'));
    }
  };

  const reset = () => {
    setStep(0);
    setWorkbook(null);
    setSheet('');
    setPlan(null);
    setResult(null);
    setMeaningOf({});
    setSectionProjects({});
    setFillEmptyOnly(false);
    setCreateUnknown(false);
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

  const header = matrix[headerRow] ?? [];
  const firstDataRow = matrix[headerRow + 1] ?? [];
  const hasName = mapping.fields.label >= 0;
  const tooMany = rows.length > MAX_ROWS;

  const unknownNames = (plan?.rows ?? [])
    .filter((row) => row.status !== 'Error' && row.employeeId === null && row.label.includes(' '))
    .map((row) => row.label);

  const monthLabel = new Date(year ?? 2026, (monthNumber ?? 1) - 1, 1).toLocaleDateString(
    locale === 'sr' ? 'sr-Latn' : 'en-GB',
    { month: 'long', year: 'numeric' },
  );

  const recentMonths = Array.from({ length: 14 }, (_, back) => {
    const date = new Date(new Date().getFullYear(), new Date().getMonth() - back, 1);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
  });

  const rowValue = (index: number, key: keyof (typeof rows)[number]): string => {
    const value = rows[index]?.[key];
    return typeof value === 'string' ? value : '';
  };

  const error =
    readError
    ?? (preview.error ? toApiError(preview.error).message : null)
    ?? (commit.error ? toApiError(commit.error).message : null)
    ?? (undo.error ? toApiError(undo.error).message : null);

  const stepKeys = ['file', 'columns', 'check', 'result'] as const;

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('bookImport.title')}
        description={t('bookImport.subtitle')}
        secondaryActions={<Button onClick={reset}>{t('bookImport.startOver')}</Button>}
      />

      <HowItWorks id="import">
        <HowLine>{t('bookImport.how1')}</HowLine>
        <HowLine>{t('bookImport.how2')}</HowLine>
        <HowLine>{t('bookImport.how3')}</HowLine>
        <HowLine>{t('bookImport.how4')}</HowLine>
        <HowLine>{t('bookImport.how5')}</HowLine>
      </HowItWorks>

      <Stepper activeStep={step}>
        {stepKeys.map((key) => (
          <Step key={key}>
            <StepLabel>{t(`bookImport.step.${key}` as MessageKey)}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {error && <Alert severity="error">{error}</Alert>}

      {step === 0 && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('bookImport.pick')}
            </Typography>
            <DropArea onFile={(f) => void onFile(f)}>
            <Box
              sx={{ border: '2px dashed', borderColor: 'divider', borderRadius: 2, p: 4, textAlign: 'center' }}
            >
              <Typography variant="subtitle1">{t('bookImport.drop')}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                {t('bookImport.fileHelp')}
              </Typography>
              <input
                ref={fileInput}
                type="file"
                accept=".xlsx,.xls"
                hidden
                onChange={(event) => void onFile(event.target.files?.[0])}
              />
              <Stack direction="row" spacing={1} sx={{ justifyContent: 'center' }}>
                <Button variant="outlined" onClick={() => fileInput.current?.click()}>
                  {t('bookImport.chooseFile')}
                </Button>
                <Button onClick={loadSample}>{t('bookImport.loadSample')}</Button>
              </Stack>
            </Box>
            </DropArea>

            {workbook && (
              <Stack spacing={2}>
                <Alert severity="success">
                  {t('bookImport.loaded', { name: workbook.fileName, count: workbook.sheets.length })}
                </Alert>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                  <TextField
                    select
                    label={t('bookImport.sheet')}
                    value={sheet}
                    onChange={(event) => adopt(workbook, event.target.value)}
                    sx={{ minWidth: 220 }}
                  >
                    {workbook.sheets.map((name) => (
                      <MenuItem key={name} value={name}>
                        {name}
                      </MenuItem>
                    ))}
                  </TextField>
                  <TextField
                    select
                    label={t('bookImport.importInto')}
                    value={month}
                    onChange={(event) => setMonth(event.target.value)}
                    sx={{ minWidth: 220 }}
                  >
                    {recentMonths.map((value) => {
                      const [y, m] = value.split('-').map(Number);
                      return (
                        <MenuItem key={value} value={value}>
                          {new Date(y!, m! - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
                            month: 'long',
                            year: 'numeric',
                          })}
                        </MenuItem>
                      );
                    })}
                  </TextField>
                </Stack>
                <Box>
                  <Button variant="contained" onClick={() => setStep(1)}>
                    {t('bookImport.nextColumns')}
                  </Button>
                </Box>
              </Stack>
            )}
          </Stack>
        </Paper>
      )}

      {step === 1 && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('bookImport.columnsTitle')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('bookImport.columnsHelp')}
            </Typography>

            <TextField
              select
              size="small"
              label={t('bookImport.headerRow')}
              value={headerRow}
              onChange={(event) => {
                const next = Number(event.target.value);
                const detected = detectMapping(matrix[next] ?? []);
                const meaningsNext: Record<number, Meaning> = {};
                (Object.keys(detected.fields) as SheetField[]).forEach((field) => {
                  if (detected.fields[field] >= 0) meaningsNext[detected.fields[field]] = field;
                });
                detected.weeks.forEach((column) => {
                  meaningsNext[column] = 'week';
                });
                setHeaderRow(next);
                setMeaningOf(meaningsNext);
                setPlan(null);
              }}
              sx={{ maxWidth: 380 }}
            >
              {matrix.slice(0, 20).map((line, index) => (
                <MenuItem key={index} value={index}>
                  {index + 1}. {line.filter(Boolean).slice(0, 4).join(' | ') || '—'}
                </MenuItem>
              ))}
            </TextField>

            <Box sx={{ overflowX: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{t('bookImport.col.excel')}</TableCell>
                    <TableCell>{t('bookImport.col.sample')}</TableCell>
                    <TableCell>{t('bookImport.col.means')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {Array.from({ length: columnCount }, (_, index) => (
                    <TableRow key={index}>
                      <TableCell>{header[index] || `—`}</TableCell>
                      <TableCell>{firstDataRow[index] || '—'}</TableCell>
                      <TableCell>
                        <TextField
                          select
                          size="small"
                          value={meaningOf[index] ?? 'skip'}
                          onChange={(event) => {
                            setMeaningOf({ ...meaningOf, [index]: event.target.value as Meaning });
                            setPlan(null);
                          }}
                          sx={{ minWidth: 220 }}
                        >
                          {meanings.map((option) => (
                            <MenuItem key={option} value={option}>
                              {t(`bookImport.means.${option}` as MessageKey)}
                            </MenuItem>
                          ))}
                        </TextField>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>

            {!hasName && <Alert severity="warning">{t('bookImport.noWorkerColumn')}</Alert>}
            {hasName && mapping.weeks.length === 0 && <Alert severity="warning">{t('bookImport.needNameAndWeeks')}</Alert>}
            {tooMany && <Alert severity="error">{t('bookImport.tooManyRows', { max: MAX_ROWS })}</Alert>}

            <Typography variant="body2">
              {t('bookImport.weeksFound', { count: mapping.weeks.length })} ·{' '}
              {t('bookImport.rowsFound', { count: rows.length })}
            </Typography>

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
                disabled={!hasName || mapping.weeks.length === 0 || tooMany || rows.length === 0 || !input}
                loading={preview.isPending}
                onClick={() => input && preview.mutate(input)}
              >
                {t('bookImport.nextCheck')}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      )}

      {step === 2 && plan && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('bookImport.checkTitle')}
            </Typography>
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
                        const next = { ...sectionProjects, [name]: event.target.value || null };
                        setSectionProjects(next);
                        if (input) preview.mutate({ ...input, sectionProjects: next });
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
                    <TableCell>{t('bookImport.col.worker')}</TableCell>
                    <TableCell align="right">{t('bookImport.col.hours')}</TableCell>
                    <TableCell align="right">{t('bookImport.col.workerRate')}</TableCell>
                    <TableCell align="right">{t('bookImport.col.clientRate')}</TableCell>
                    <TableCell align="right">{t('bookImport.col.advance')}</TableCell>
                    <TableCell align="right">{t('bookImport.col.fuel')}</TableCell>
                    <TableCell>{t('bookImport.col.check')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {plan.rows.map((row) => (
                    <TableRow key={row.index}>
                      <TableCell>
                        {row.employeeName ?? row.label}
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {row.projectName ?? row.section}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">{row.hours}</TableCell>
                      <TableCell align="right">{rowValue(row.index, 'workerRate') || '—'}</TableCell>
                      <TableCell align="right">{rowValue(row.index, 'clientRate') || '—'}</TableCell>
                      <TableCell align="right">{rowValue(row.index, 'advance') || '—'}</TableCell>
                      <TableCell align="right">{rowValue(row.index, 'fuel') || '—'}</TableCell>
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

            <Stack>
              <FormControlLabel
                control={<Checkbox checked disabled />}
                label={t('bookImport.skipErrors')}
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={createUnknown}
                    disabled={unknownNames.length === 0}
                    onChange={(event) => setCreateUnknown(event.target.checked)}
                  />
                }
                label={t('bookImport.createUnknown', { count: unknownNames.length })}
              />
            </Stack>

            <Stack direction="row" spacing={1}>
              <Button onClick={() => setStep(1)}>{t('bookImport.back')}</Button>
              <Button
                variant="contained"
                disabled={
                  plan.okCount + plan.warningCount === 0 || (plan.existingHasEntries && !fillEmptyOnly)
                }
                onClick={() => setStep(3)}
              >
                {t('bookImport.nextConfirm')}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      )}

      {step === 3 && plan && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('bookImport.confirmTitle')}
            </Typography>

            {!result ? (
              <>
                <Alert severity="info">
                  {t('bookImport.confirmText', {
                    month: monthLabel,
                    count: plan.okCount + plan.warningCount,
                    hours: plan.sheetHours.toLocaleString(locale),
                    billing: formatMoney(plan.sheetBilling, locale),
                    wage: formatMoney(plan.sheetWage, locale),
                    skipped: plan.errorCount,
                  })}
                </Alert>
                <Stack direction="row" spacing={1}>
                  <Button onClick={() => setStep(2)}>{t('bookImport.back')}</Button>
                  <Button
                    variant="contained"
                    loading={commit.isPending}
                    onClick={() => input && commit.mutate(input)}
                  >
                    {t('bookImport.import')}
                  </Button>
                </Stack>
              </>
            ) : (
              <>
                <Alert severity={result.hoursDifference === 0 && result.billingDifference === 0 ? 'success' : 'warning'}>
                  {t('bookImport.done', { month: monthLabel })}
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
                  <Button variant="contained" component={RouterLink} to={paths.bookSettlement}>
                    {t('bookImport.openSettlement')}
                  </Button>
                  <Button component={RouterLink} to={`${paths.ledgers}/${result.ledgerId}`}>
                    {t('bookImport.openMonth')}
                  </Button>
                  {result.ledgerCreated && (
                    <Button color="error" loading={undo.isPending} onClick={() => undo.mutate(result.batchId)}>
                      {t('bookImport.undo')}
                    </Button>
                  )}
                </Stack>
              </>
            )}
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
