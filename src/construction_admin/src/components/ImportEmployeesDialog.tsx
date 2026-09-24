import { FileDownloadOutlined, UploadFileOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  Radio,
  RadioGroup,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useEffect, useRef, useState } from 'react';

import type { ImportDuplicateHandling, ImportRowOutcome, ImportRowResult } from '../api/onboarding';
import { useImportEmployees, useImportEmployeesPreview } from '../features/onboarding/useOnboarding';
import type { MessageKey } from '../i18n/en';
import { useT } from '../i18n/useI18n';
import {
  downloadEmployeeTemplate,
  parseEmployeeFile,
  type ParsedEmployeeSheet,
} from '../utils/employeeSheet';

const MAX_FILE_BYTES = 5 * 1024 * 1024;
const MAX_ROWS = 1000;

const OUTCOME_COLOR: Record<ImportRowOutcome, 'success' | 'info' | 'default' | 'error'> = {
  Create: 'success',
  Update: 'info',
  Skip: 'default',
  Error: 'error',
};

type Translate = ReturnType<typeof useT>;

/**
 * The API answers row by row in English; this puts each answer in the user's
 * language. Unrecognised text is shown as it came, never dropped.
 */
function rowMessage(t: Translate, message: string | null): string {
  if (!message) return '';

  const quoted = /'([^']*)'/.exec(message)?.[1] ?? '';
  const rules: [RegExp, MessageKey][] = [
    [/^First name is missing/, 'onboarding.import.msg.firstNameMissing'],
    [/^Last name is missing/, 'onboarding.import.msg.lastNameMissing'],
    [/too long/, 'onboarding.import.msg.tooLong'],
    [/is not a valid email/, 'onboarding.import.msg.invalidEmail'],
    [/^Employment date is not/, 'onboarding.import.msg.invalidEmploymentDate'],
    [/^Date of birth is not/, 'onboarding.import.msg.invalidBirthDate'],
    [/^Date of birth must be in the past/, 'onboarding.import.msg.birthNotPast'],
    [/^Date of birth must be before/, 'onboarding.import.msg.birthAfterEmployment'],
    [/^Type '/, 'onboarding.import.msg.unknownType'],
    [/more than once/, 'onboarding.import.msg.duplicateInFile'],
    [/^Already exists/, 'onboarding.import.msg.exists'],
    [/^Nothing new/, 'onboarding.import.msg.nothingNew'],
    [/is already in use/, 'onboarding.import.msg.numberInUse'],
  ];

  const hit = rules.find(([pattern]) => pattern.test(message));

  return hit ? t(hit[1], { value: quoted }) : message;
}

/**
 * Bring a whole workforce in from a spreadsheet.
 *
 * Two steps on purpose: the file is read here, the server reports exactly what
 * it would do (new, updated, skipped, wrong), and only then does anything get
 * saved. A bad row is reported and left out; it never blocks the rest.
 */
export function ImportEmployeesDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const fileInput = useRef<HTMLInputElement>(null);
  const preview = useImportEmployeesPreview();
  const commit = useImportEmployees();

  const [parsed, setParsed] = useState<ParsedEmployeeSheet | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [onDuplicate, setOnDuplicate] = useState<ImportDuplicateHandling>('Skip');

  const resetPreview = preview.reset;
  const resetCommit = commit.reset;

  useEffect(() => {
    if (open) {
      setParsed(null);
      setFileError(null);
      setOnDuplicate('Skip');
      resetPreview();
      resetCommit();
    }
  }, [open, resetPreview, resetCommit]);

  const check = (sheet: ParsedEmployeeSheet, handling: ImportDuplicateHandling) => {
    preview.mutate({ rows: sheet.rows, onDuplicate: handling });
  };

  const pick = async (file: File | undefined) => {
    setFileError(null);
    setParsed(null);
    resetPreview();

    if (!file) return;

    if (file.size > MAX_FILE_BYTES) {
      setFileError(t('onboarding.import.tooLarge', { limit: MAX_FILE_BYTES / (1024 * 1024) }));
      return;
    }

    let sheet: ParsedEmployeeSheet;

    try {
      sheet = await parseEmployeeFile(file);
    } catch {
      setFileError(t('onboarding.import.noNameColumn'));
      return;
    }

    if (!sheet.ok) {
      setFileError(t('onboarding.import.noNameColumn'));
      return;
    }

    if (sheet.rows.length === 0) {
      setFileError(t('onboarding.import.empty'));
      return;
    }

    if (sheet.rows.length > MAX_ROWS) {
      setFileError(t('onboarding.import.tooManyRows', { limit: MAX_ROWS }));
      return;
    }

    setParsed(sheet);
    check(sheet, onDuplicate);
  };

  const changeDuplicates = (next: ImportDuplicateHandling) => {
    setOnDuplicate(next);

    if (parsed) check(parsed, next);
  };

  const result = preview.data;
  const done = commit.data;
  const importable = result ? result.created + result.updated : 0;

  const confirm = () => {
    if (!parsed) return;

    commit.mutate({ rows: parsed.rows, onDuplicate });
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{t('onboarding.import.title')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 0.5 }}>
          <Typography variant="body2" color="text.secondary">
            {t('onboarding.import.intro')}
          </Typography>

          {!done && (
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Button
                variant="contained"
                startIcon={<UploadFileOutlined />}
                onClick={() => fileInput.current?.click()}
              >
                {parsed ? t('onboarding.import.chooseAnother') : t('onboarding.import.choose')}
              </Button>
              <Button startIcon={<FileDownloadOutlined />} onClick={() => void downloadEmployeeTemplate()}>
                {t('onboarding.import.template')}
              </Button>
              <input
                ref={fileInput}
                id="import-employees-file"
                type="file"
                hidden
                accept=".xlsx,.xls,.csv"
                onChange={(event) => {
                  void pick(event.target.files?.[0]);
                  // Lets the same file be chosen again after fixing it.
                  event.target.value = '';
                }}
              />
            </Stack>
          )}

          {fileError && <Alert severity="error">{fileError}</Alert>}

          {parsed && !done && (
            <Typography variant="body2" color="text.secondary">
              {t('onboarding.import.readSummary', {
                count: parsed.rows.length,
                columns: parsed.recognised.map((field) => t(`onboarding.import.field.${field}` as MessageKey)).join(', '),
              })}
              {parsed.ignored.length > 0 && ` ${t('onboarding.import.ignoredColumns', { columns: parsed.ignored.join(', ') })}`}
            </Typography>
          )}

          {parsed && !done && (
            <FormControl>
              <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
                {t('onboarding.import.duplicates')}
              </Typography>
              <RadioGroup
                value={onDuplicate}
                onChange={(event) => changeDuplicates(event.target.value as ImportDuplicateHandling)}
              >
                <FormControlLabel value="Skip" control={<Radio size="small" />} label={t('onboarding.import.duplicatesSkip')} />
                <FormControlLabel value="Update" control={<Radio size="small" />} label={t('onboarding.import.duplicatesUpdate')} />
              </RadioGroup>
            </FormControl>
          )}

          {preview.isPending && <Typography variant="body2">{t('onboarding.import.checking')}</Typography>}
          {preview.error && <Alert severity="error">{preview.error.message}</Alert>}
          {commit.error && <Alert severity="error">{commit.error.message}</Alert>}

          {done && (
            <Alert severity="success">
              {t('onboarding.import.done', { created: done.created, updated: done.updated })}
            </Alert>
          )}

          {result && !done && (
            <>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
                <Typography variant="subtitle2">
                  {t('onboarding.import.summary', {
                    created: result.created,
                    updated: result.updated,
                    skipped: result.skipped,
                    errors: result.errors,
                  })}
                </Typography>
              </Stack>
              {result.errors > 0 && (
                <Typography variant="caption" color="text.secondary">
                  {t('onboarding.import.errorsLeftOut')}
                </Typography>
              )}

              <TableContainer sx={{ maxHeight: 340, border: 1, borderColor: 'divider', borderRadius: 1 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>{t('onboarding.import.colLine')}</TableCell>
                      <TableCell>{t('onboarding.import.colName')}</TableCell>
                      <TableCell>{t('onboarding.import.colNumber')}</TableCell>
                      <TableCell>{t('onboarding.import.colResult')}</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {result.rows.map((row: ImportRowResult) => (
                      <TableRow key={row.line}>
                        <TableCell>{row.line}</TableCell>
                        <TableCell>{row.fullName || '—'}</TableCell>
                        <TableCell>{row.employeeNumber ?? '—'}</TableCell>
                        <TableCell>
                          <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                            <Chip
                              size="small"
                              color={OUTCOME_COLOR[row.outcome]}
                              variant={row.outcome === 'Skip' ? 'outlined' : 'filled'}
                              label={t(`onboarding.import.outcome.${row.outcome}` as MessageKey)}
                            />
                            <Typography variant="body2" color="text.secondary">
                              {rowMessage(t, row.message)}
                            </Typography>
                          </Box>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{done ? t('common.close') : t('common.cancel')}</Button>
        {!done && (
          <Button
            variant="contained"
            onClick={confirm}
            disabled={!result || importable === 0 || preview.isPending || commit.isPending}
          >
            {t('onboarding.import.confirm', { count: importable })}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
