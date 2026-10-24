import {
  CheckCircleOutlined,
  CloseOutlined,
  DownloadOutlined,
  ReplayOutlined,
  ZoomInOutlined,
  ZoomOutOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  ButtonGroup,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import { weeklySiteReportsApi } from '../../api/weeklySiteReports';
import type {
  ApproveWeeklySiteReportResult,
  WeeklyReportMonth,
  WeeklyReportPage,
  WeeklySiteReport,
  WeeklySiteReportDetail,
} from '../../api/types';
import { PdfPages } from '../../components/PdfPages';
import { StatusChip } from '../../components/StatusChip';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { formatIsoWeek } from '../../utils/isoWeek';
import { useAllEmployeesQuery } from '../employees/useEmployees';
import { useApproveWeeklySiteReport, useUpdateWeeklySiteReportLines, useWeeklySiteReportQuery } from './useWeeklySiteReports';

/** More hours than a week holds are a typing mistake. */
const MAX_WEEK_HOURS = 168;

interface Row {
  key: string;
  employeeId: string;
  name: string;
  /** Hours in the month the week begins in, and in the later month (a week that runs over a month's end), as typed. */
  first: string;
  second: string;
  /** The sheet already says something else for this worker and the cell is to stay as it is. */
  keep: boolean;
}

function toNumber(text: string): number {
  const value = Number(text.replace(',', '.'));
  return Number.isFinite(value) ? value : 0;
}

function formatHours(value: number): string {
  return String(Math.round(value * 100) / 100).replace('.', ',');
}

function monthLabel(month: WeeklyReportMonth): string {
  return `${String(month.month).padStart(2, '0')}/${month.year}`;
}

function rowsFrom(detail: WeeklySiteReportDetail): Row[] {
  return detail.lines.map((line) => ({
    key: line.id,
    employeeId: line.employeeId,
    name: line.employeeName,
    first: formatHours(line.hours - line.hoursSecondMonth),
    second: formatHours(line.hoursSecondMonth),
    keep: false,
  }));
}

/** One picture of the report, drawn in place: an image, a PDF page by page, or a note that it can only be downloaded. */
function PageView({ reportId, page, zoom }: { reportId: string; page: WeeklyReportPage; zoom: number }) {
  const t = useT();
  const [state, setState] = useState<
    { kind: 'loading' } | { kind: 'image'; url: string } | { kind: 'pdf'; blob: Blob; url: string } | { kind: 'other' } | { kind: 'error' }
  >({ kind: 'loading' });
  const [pdfFailed, setPdfFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    let objectUrl: string | undefined;

    setState({ kind: 'loading' });
    setPdfFailed(false);

    void (async () => {
      try {
        const blob = await weeklySiteReportsApi.pageBlob(reportId, page.id);
        if (cancelled) return;

        objectUrl = URL.createObjectURL(blob);
        const name = page.fileName.toLowerCase();

        if (page.contentType.startsWith('image/') || /\.(jpe?g|png|webp)$/.test(name)) {
          setState({ kind: 'image', url: objectUrl });
        } else if (page.contentType === 'application/pdf' || name.endsWith('.pdf')) {
          setState({ kind: 'pdf', blob, url: objectUrl });
        } else {
          setState({ kind: 'other' });
        }
      } catch {
        if (!cancelled) setState({ kind: 'error' });
      }
    })();

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [reportId, page.id, page.fileName, page.contentType]);

  if (state.kind === 'loading') {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress size={28} />
      </Stack>
    );
  }

  if (state.kind === 'image') {
    return (
      <Box sx={{ overflow: 'auto', maxHeight: '68vh' }}>
        <Box
          component="img"
          src={state.url}
          alt={page.fileName}
          sx={{ display: 'block', width: `${zoom * 100}%`, maxWidth: zoom === 1 ? '100%' : 'none', mx: 'auto' }}
        />
      </Box>
    );
  }

  if (state.kind === 'pdf') {
    return (
      <Box sx={{ overflow: 'auto', maxHeight: '68vh' }}>
        {pdfFailed ? (
          <Box component="iframe" src={state.url} title={page.fileName} sx={{ width: '100%', height: '66vh', border: 0 }} />
        ) : (
          <PdfPages blob={state.blob} onFailed={() => setPdfFailed(true)} />
        )}
      </Box>
    );
  }

  return (
    <Typography color={state.kind === 'error' ? 'error' : 'text.secondary'} sx={{ py: 6, textAlign: 'center' }}>
      {state.kind === 'error' ? t('attachments.previewError') : t('attachments.noPreview')}
    </Typography>
  );
}

/**
 * The office's look at a weekly report: the pictures of the signed sheet on one side, the workers and hours the
 * foreman wrote on the other, to be checked against each other and corrected. Approving enters what is on the
 * right into the month's sheet; where the sheet already says something else the office chooses which stays.
 */
export function ReportReviewDialog({
  reportId,
  onClose,
  onRequestCorrection,
}: {
  reportId: string | null;
  onClose: () => void;
  onRequestCorrection: (report: WeeklySiteReport) => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: detail, isLoading, isError } = useWeeklySiteReportQuery(reportId);
  const { data: employeesPage } = useAllEmployeesQuery();
  const approve = useApproveWeeklySiteReport();
  const saveLines = useUpdateWeeklySiteReportLines();

  const [pageIndex, setPageIndex] = useState(0);
  const [zoom, setZoom] = useState(1);
  const [rows, setRows] = useState<Row[]>([]);
  const [result, setResult] = useState<ApproveWeeklySiteReportResult | null>(null);
  const [saved, setSaved] = useState(false);

  // The rows start from what the foreman wrote, once per report that is opened.
  const loadedId = detail?.id;
  useEffect(() => {
    if (!detail) return;
    setRows(rowsFrom(detail));
    setPageIndex(0);
    setZoom(1);
    setResult(null);
    setSaved(false);
    // Only when another report is opened; saving a correction must not throw away what is typed.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loadedId]);

  const split = !!detail?.secondMonth;
  const existingById = useMemo(
    () => new Map((detail?.lines ?? []).map((line) => [line.employeeId, line] as const)),
    [detail],
  );

  const analysed = rows.map((row) => {
    const first = toNumber(row.first);
    const second = toNumber(row.second);
    const line = existingById.get(row.employeeId);
    const duplicate = rows.filter((other) => other.employeeId === row.employeeId).length > 1;
    const invalid = first < 0 || second < 0 || first + second > MAX_WEEK_HOURS;

    const part = (hours: number, existing: number | null | undefined) =>
      existing == null ? ('empty' as const) : Math.abs(existing - hours) < 0.01 ? ('same' as const) : ('differs' as const);

    return {
      row,
      first,
      second,
      duplicate,
      invalid,
      firstState: part(first, line?.existingFirstMonth),
      secondState: split && second > 0 ? part(second, line?.existingSecondMonth) : null,
      existingFirst: line?.existingFirstMonth ?? null,
      existingSecond: line?.existingSecondMonth ?? null,
    };
  });

  const total = analysed.reduce((sum, a) => sum + a.first + a.second, 0);
  const reportedTotal = detail?.quantity ?? null;
  const totalDiffers = reportedTotal != null && rows.length > 0 && Math.abs(total - reportedTotal) > 0.01;

  const willWrite = analysed.reduce((n, a) => n + (a.firstState !== 'same' && !(a.firstState === 'differs' && a.row.keep) ? 1 : 0)
    + (a.secondState && a.secondState !== 'same' && !(a.secondState === 'differs' && a.row.keep) ? 1 : 0), 0);
  const alreadySame = analysed.reduce((n, a) => n + (a.firstState === 'same' ? 1 : 0) + (a.secondState === 'same' ? 1 : 0), 0);
  const keptCount = analysed.reduce((n, a) => n + ((a.firstState === 'differs' || a.secondState === 'differs') && a.row.keep ? 1 : 0), 0);

  const firstMonthBlocked = !!detail && rows.length > 0 && (!detail.firstMonth.ledgerFound || detail.firstMonth.locked);
  const secondMonthBlocked =
    !!detail?.secondMonth && analysed.some((a) => a.second > 0) && (!detail.secondMonth.ledgerFound || detail.secondMonth.locked);
  const hasProblem = analysed.some((a) => a.duplicate || a.invalid);
  const approved = detail?.status === 'Processed';
  const canApprove = !!detail && !approved && !hasProblem && !firstMonthBlocked && !secondMonthBlocked && !approve.isPending;

  const addable = (employeesPage?.items ?? []).filter((e) => !rows.some((r) => r.employeeId === e.id));

  const setRow = (key: string, patch: Partial<Row>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...patch } : row)));

  const submit = () => {
    if (!detail) return;

    approve.mutate(
      {
        id: detail.id,
        lines: rows.map((row) => ({
          employeeId: row.employeeId,
          hours: toNumber(row.first) + toNumber(row.second),
          hoursSecondMonth: toNumber(row.second),
        })),
        keepExisting: rows.filter((row) => row.keep).map((row) => row.employeeId),
      },
      { onSuccess: (outcome) => setResult(outcome) },
    );
  };

  /** Keeps what was typed, for later: the report stays waiting for review. */
  const save = () => {
    if (!detail) return;

    setSaved(false);
    saveLines.mutate(
      {
        id: detail.id,
        lines: rows.map((row) => ({
          employeeId: row.employeeId,
          hours: toNumber(row.first) + toNumber(row.second),
          hoursSecondMonth: toNumber(row.second),
        })),
      },
      { onSuccess: () => setSaved(true) },
    );
  };

  const download = async (page: WeeklyReportPage) => {
    if (!detail) return;
    const blob = await weeklySiteReportsApi.pageBlob(detail.id, page.id);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = page.fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const pages = detail?.pages ?? [];
  const page = pages[Math.min(pageIndex, Math.max(pages.length - 1, 0))];

  const monthWarning = (month: WeeklyReportMonth | null | undefined) => {
    if (!month) return null;
    if (month.locked) return t('weeklyReports.review.locked', { month: monthLabel(month) });
    if (!month.ledgerFound) return t('weeklyReports.review.noLedger', { month: monthLabel(month) });
    return null;
  };

  return (
    <Dialog open={!!reportId} onClose={onClose} fullWidth maxWidth="xl" scroll="paper">
      <DialogTitle sx={{ pb: 1 }}>
        {detail ? (
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <span>
              {detail.projectName} · {formatIsoWeek(detail.isoYear, detail.isoWeek)}
            </span>
            <StatusChip status={detail.status} kind="weeklyReportStatus" />
          </Stack>
        ) : (
          t('weeklyReports.title')
        )}
        {detail && (
          <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 400 }}>
            {t('weeklyReports.review.sentBy', {
              who: detail.submittedByEmployeeName,
              when: formatDateTime(detail.createdAt),
              pages: detail.pageCount,
            })}
            {' · '}
            {enumLabel('weeklyReportType', detail.type)}
            {detail.source === 'Office' &&
              ` · ${t('weeklyReports.review.enteredByOffice', { who: detail.enteredByEmail ?? '—', note: detail.sourceNote ? `: ${detail.sourceNote}` : '' })}`}
            {reportedTotal != null && ` · ${t('weeklyReports.review.reportedTotal', { hours: formatHours(reportedTotal) })}`}
          </Typography>
        )}
      </DialogTitle>

      <DialogContent dividers>
        {isLoading && (
          <Stack sx={{ alignItems: 'center', py: 8 }}>
            <CircularProgress />
          </Stack>
        )}
        {isError && <Alert severity="error">{t('attachments.previewError')}</Alert>}

        {detail && (
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1.1fr) minmax(0, 1fr)' },
              alignItems: 'start',
            }}
          >
            {/* The pictures */}
            <Stack spacing={1} sx={{ minWidth: 0 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
                <ButtonGroup size="small" aria-label={t('weeklyReports.review.pages')}>
                  {pages.map((p, index) => (
                    <Button
                      key={p.id}
                      variant={index === pageIndex ? 'contained' : 'outlined'}
                      onClick={() => setPageIndex(index)}
                    >
                      {t('weeklyReports.review.page', { n: index + 1 })}
                    </Button>
                  ))}
                </ButtonGroup>
                <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                  <Typography variant="caption" color="text.secondary">
                    {t('weeklyReports.review.pageOf', { n: Math.min(pageIndex, pages.length - 1) + 1, count: pages.length })}
                  </Typography>
                  <Tooltip title={t('weeklyReports.review.zoomOut')}>
                    <span>
                      <IconButton size="small" disabled={zoom <= 1} onClick={() => setZoom((z) => Math.max(1, z - 0.5))}>
                        <ZoomOutOutlined fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                  <Tooltip title={t('weeklyReports.review.zoomIn')}>
                    <span>
                      <IconButton size="small" disabled={zoom >= 3} onClick={() => setZoom((z) => Math.min(3, z + 0.5))}>
                        <ZoomInOutlined fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                  {page && (
                    <Button size="small" startIcon={<DownloadOutlined fontSize="small" />} onClick={() => void download(page)}>
                      {t('weeklyReports.download')}
                    </Button>
                  )}
                </Stack>
              </Stack>
              <Box sx={{ bgcolor: 'action.hover', borderRadius: 1, p: 1, minHeight: 240 }}>
                {page ? <PageView reportId={detail.id} page={page} zoom={zoom} /> : null}
              </Box>
              {detail.note && (
                <Typography variant="body2">
                  <strong>{t('weeklyReports.note')}:</strong> {detail.note}
                </Typography>
              )}
            </Stack>

            {/* The data to check against them */}
            <Stack spacing={1.5} sx={{ minWidth: 0 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                {t('weeklyReports.review.dataTitle')}
              </Typography>

              {detail.status === 'CorrectionRequested' && detail.reviewNote && (
                <Alert severity="warning">{t('weeklyReports.reviewNote', { note: detail.reviewNote })}</Alert>
              )}
              {approved && detail.processedAt && (
                <Alert severity="success">
                  {t('weeklyReports.approvedBy', { date: formatDateTime(detail.processedAt), who: detail.processedByEmail ?? '—' })}
                </Alert>
              )}
              {[monthWarning(detail.firstMonth), analysed.some((a) => a.second > 0) ? monthWarning(detail.secondMonth) : null]
                .filter((message): message is string => !!message && rows.length > 0 && !approved)
                .map((message) => (
                  <Alert key={message} severity="error">
                    {message}
                  </Alert>
                ))}

              {rows.length === 0 && !approved && (
                <Alert severity="info">{t('weeklyReports.review.noLines')}</Alert>
              )}

              {/* header */}
              <Box
                sx={{
                  display: 'grid',
                  gap: 1,
                  gridTemplateColumns: split ? 'minmax(0,1.4fr) 84px 84px minmax(0,1.3fr) 40px' : 'minmax(0,1.5fr) 92px minmax(0,1.4fr) 40px',
                  px: 0.5,
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  {t('weeklyReports.review.worker')}
                </Typography>
                <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'right' }}>
                  {split && detail.firstMonth ? t('weeklyReports.review.hoursIn', { month: monthLabel(detail.firstMonth) }) : t('weeklyReports.hours')}
                </Typography>
                {split && detail.secondMonth && (
                  <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'right' }}>
                    {t('weeklyReports.review.hoursIn', { month: monthLabel(detail.secondMonth) })}
                  </Typography>
                )}
                <Typography variant="caption" color="text.secondary">
                  {t('weeklyReports.review.inSheet')}
                </Typography>
                <span />
              </Box>

              <Stack spacing={0.5} sx={{ maxHeight: '40vh', overflow: 'auto' }}>
                {analysed.map((a) => (
                  <Box
                    key={a.row.key}
                    sx={{
                      display: 'grid',
                      gap: 1,
                      alignItems: 'center',
                      gridTemplateColumns: split ? 'minmax(0,1.4fr) 84px 84px minmax(0,1.3fr) 40px' : 'minmax(0,1.5fr) 92px minmax(0,1.4fr) 40px',
                      borderTop: 1,
                      borderColor: 'divider',
                      py: 0.75,
                      px: 0.5,
                    }}
                  >
                    <Box sx={{ minWidth: 0 }}>
                      <Typography sx={{ fontWeight: 600 }} noWrap>
                        {a.row.name}
                      </Typography>
                      {a.duplicate && (
                        <Typography variant="caption" color="error">
                          {t('weeklyReports.review.duplicate')}
                        </Typography>
                      )}
                      {a.invalid && (
                        <Typography variant="caption" color="error">
                          {t('weeklyReports.review.invalid')}
                        </Typography>
                      )}
                    </Box>
                    <TextField
                      size="small"
                      value={a.row.first}
                      disabled={approved}
                      onChange={(event) => setRow(a.row.key, { first: event.target.value })}
                      slotProps={{ htmlInput: { inputMode: 'decimal', 'aria-label': t('weeklyReports.hours'), style: { textAlign: 'right' } } }}
                      error={a.invalid}
                    />
                    {split && (
                      <TextField
                        size="small"
                        value={a.row.second}
                        disabled={approved}
                        onChange={(event) => setRow(a.row.key, { second: event.target.value })}
                        slotProps={{ htmlInput: { inputMode: 'decimal', 'aria-label': t('weeklyReports.hours'), style: { textAlign: 'right' } } }}
                        error={a.invalid}
                      />
                    )}
                    <Box>
                      {[
                        { state: a.firstState, existing: a.existingFirst, hours: a.first },
                        ...(a.secondState ? [{ state: a.secondState, existing: a.existingSecond, hours: a.second }] : []),
                      ].map((part, index) => (
                        <Typography
                          key={index}
                          variant="caption"
                          sx={{ display: 'block', lineHeight: 1.3 }}
                          color={part.state === 'differs' ? 'warning.main' : part.state === 'same' ? 'text.secondary' : 'success.main'}
                        >
                          {part.state === 'empty' && t('weeklyReports.review.willFill', { hours: formatHours(part.hours) })}
                          {part.state === 'same' && t('weeklyReports.review.same', { hours: formatHours(part.hours) })}
                          {part.state === 'differs' && t('weeklyReports.review.differs', { hours: formatHours(part.existing ?? 0) })}
                        </Typography>
                      ))}
                      {(a.firstState === 'differs' || a.secondState === 'differs') && !approved && (
                        <ButtonGroup size="small" sx={{ mt: 0.5 }}>
                          <Button
                            variant={a.row.keep ? 'outlined' : 'contained'}
                            color="warning"
                            onClick={() => setRow(a.row.key, { keep: false })}
                          >
                            {t('weeklyReports.review.replace')}
                          </Button>
                          <Button
                            variant={a.row.keep ? 'contained' : 'outlined'}
                            color="warning"
                            onClick={() => setRow(a.row.key, { keep: true })}
                          >
                            {t('weeklyReports.review.keepOld')}
                          </Button>
                        </ButtonGroup>
                      )}
                    </Box>
                    <Tooltip title={t('weeklyReports.review.removeRow')}>
                      <span>
                        <IconButton
                          size="small"
                          disabled={approved}
                          aria-label={t('weeklyReports.review.removeRow')}
                          onClick={() => setRows((current) => current.filter((row) => row.key !== a.row.key))}
                        >
                          <CloseOutlined fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </Box>
                ))}
              </Stack>

              {!approved && (
                <Autocomplete
                  size="small"
                  options={addable}
                  value={null}
                  blurOnSelect
                  clearOnBlur
                  getOptionLabel={(employee) => `${employee.firstName} ${employee.lastName}`}
                  onChange={(_event, employee) => {
                    if (!employee) return;
                    setRows((current) => [
                      ...current,
                      {
                        key: `new-${employee.id}`,
                        employeeId: employee.id,
                        name: `${employee.firstName} ${employee.lastName}`,
                        first: '',
                        second: '',
                        keep: false,
                      },
                    ]);
                  }}
                  renderInput={(params) => <TextField {...params} label={t('weeklyReports.review.addWorker')} />}
                />
              )}

              <Box sx={{ borderTop: 1, borderColor: 'divider', pt: 1 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
                  <Typography>
                    {t('weeklyReports.review.total')}: <strong>{formatHours(total)} h</strong>
                  </Typography>
                  {reportedTotal != null && rows.length > 0 && (
                    <Typography variant="body2" color={totalDiffers ? 'warning.main' : 'success.main'}>
                      {totalDiffers
                        ? t('weeklyReports.review.totalDiffers', { reported: formatHours(reportedTotal), diff: formatHours(total - reportedTotal) })
                        : t('weeklyReports.review.totalMatches', { reported: formatHours(reportedTotal) })}
                    </Typography>
                  )}
                </Stack>
                {rows.length > 0 && !approved && (
                  <Box sx={{ mt: 1, p: 1.25, border: 1, borderStyle: 'dashed', borderColor: 'divider', borderRadius: 1 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {t('weeklyReports.review.willEnter', {
                        month: detail.secondMonth
                          ? `${monthLabel(detail.firstMonth)}, ${monthLabel(detail.secondMonth)}`
                          : monthLabel(detail.firstMonth),
                        project: detail.projectName,
                        week: detail.isoWeek,
                      })}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('weeklyReports.review.counts', { write: willWrite, same: alreadySame, keep: keptCount })}
                    </Typography>
                  </Box>
                )}
              </Box>

              {saved && !result && <Alert severity="info">{t('weeklyReports.review.saved')}</Alert>}
              {saveLines.error && <Alert severity="error">{saveLines.error.message}</Alert>}
              {result && (
                <Alert severity="success">
                  {t('weeklyReports.review.done', { written: result.cellsWritten, same: result.cellsUnchanged, kept: result.cellsKept })}
                </Alert>
              )}
              {approve.error && <Alert severity="error">{approve.error.message}</Alert>}
            </Stack>
          </Box>
        )}
      </DialogContent>

      <DialogActions sx={{ flexWrap: 'wrap', gap: 0.5 }}>
        <Button onClick={onClose}>{result ? t('common.close') : t('common.cancel')}</Button>
        {detail && !result && (
          <Button color="warning" startIcon={<ReplayOutlined />} onClick={() => onRequestCorrection(detail)}>
            {t('weeklyReports.requestCorrection')}
          </Button>
        )}
        {detail && !approved && !result && rows.length > 0 && (
          <Button variant="outlined" disabled={hasProblem || saveLines.isPending} onClick={save}>
            {t('weeklyReports.review.saveForLater')}
          </Button>
        )}
        {detail && !approved && !result && (
          <Button variant="contained" color="success" startIcon={<CheckCircleOutlined />} disabled={!canApprove} onClick={submit}>
            {rows.length > 0 ? t('weeklyReports.review.approveEnter') : t('weeklyReports.approve')}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
