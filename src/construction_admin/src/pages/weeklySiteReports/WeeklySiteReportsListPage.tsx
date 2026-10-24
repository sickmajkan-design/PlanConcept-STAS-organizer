import {
  AddOutlined,
  AttachFileOutlined,
  CheckCircleOutlined,
  DownloadOutlined,
  ReplayOutlined,
  VisibilityOutlined,
} from '@mui/icons-material';
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
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';

import { weeklySiteReportsApi } from '../../api/weeklySiteReports';
import {
  weeklyReportTypes,
  type WeeklyReportStatus,
  type WeeklyReportType,
  type WeeklySiteReport,
  type WeeklySiteReportsSummary,
} from '../../api/types';
import { canAdministerAccounts, canManageAssignments, canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentPreviewDialog } from '../../components/AttachmentPreviewDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useCreateWeeklySiteReport,
  useApproveWeeklySiteReport,
  useReportableProjectsQuery,
  useRequestWeeklySiteReportCorrection,
  useWeeklySiteReportsQuery,
  useWeeklySiteReportsSummaryQuery,
} from '../../features/weeklySiteReports/useWeeklySiteReports';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';
import { formatIsoWeek, getLastCompletedIsoWeek } from '../../utils/isoWeek';
import { DropArea } from '../../components/files/DropArea';

function SubmitReportDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const canPickEmployee = canManageAssignments(user);
  const { data: projects } = useReportableProjectsQuery();
  const { data: employeesPage } = useAllEmployeesQuery();
  const create = useCreateWeeklySiteReport();

  const defaultWeek = getLastCompletedIsoWeek();
  const [projectId, setProjectId] = useState('');
  const [isoYear, setIsoYear] = useState(defaultWeek.isoYear);
  const [isoWeek, setIsoWeek] = useState(defaultWeek.isoWeek);
  const [type, setType] = useState<WeeklyReportType>('SignedHours');
  const [submittedByEmployeeId, setSubmittedByEmployeeId] = useState('');
  const [quantity, setQuantity] = useState('');
  const [note, setNote] = useState('');
  const [file, setFile] = useState<File | null>(null);

  const canSubmit = !!projectId && !!file && (type !== 'SignedHours' || !!quantity);

  const submit = () => {
    if (!file || !projectId) return;

    create.mutate(
      {
        projectId,
        isoYear,
        isoWeek,
        type,
        submittedByEmployeeId: canPickEmployee && submittedByEmployeeId ? submittedByEmployeeId : null,
        quantity: type === 'SignedHours' && quantity ? Number(quantity) : null,
        note: note.trim() || null,
        file,
      },
      {
        onSuccess: () => {
          setProjectId('');
          setSubmittedByEmployeeId('');
          setQuantity('');
          setNote('');
          setFile(null);
          onClose();
        },
      },
    );
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('weeklyReports.submit')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <FormControl fullWidth>
            <InputLabel id="wr-project-label">{t('weeklyReports.project')}</InputLabel>
            <Select
              labelId="wr-project-label"
              label={t('weeklyReports.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
            >
              {(projects ?? []).map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <Stack direction="row" spacing={1}>
            <TextField
              type="number"
              label={t('weeklyReports.isoYear')}
              value={isoYear}
              onChange={(event) => setIsoYear(Number(event.target.value) || isoYear)}
              sx={{ flex: 1 }}
            />
            <TextField
              type="number"
              label={t('weeklyReports.isoWeek')}
              value={isoWeek}
              onChange={(event) =>
                setIsoWeek(Math.min(53, Math.max(1, Number(event.target.value) || 1)))
              }
              slotProps={{ htmlInput: { min: 1, max: 53 } }}
              sx={{ flex: 1 }}
            />
          </Stack>

          <FormControl fullWidth>
            <InputLabel id="wr-type-label">{t('weeklyReports.type')}</InputLabel>
            <Select
              labelId="wr-type-label"
              label={t('weeklyReports.type')}
              value={type}
              onChange={(event) => setType(event.target.value as WeeklyReportType)}
            >
              {weeklyReportTypes.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('weeklyReportType', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {canPickEmployee && (
            <FormControl fullWidth>
              {/* `shrink` forced on: `displayEmpty` always shows real text in
                  the field (the "auto-detect" placeholder is not really
                  empty), but MUI's label only floats up on its own when
                  `value` is non-empty — without this it sits in the middle of
                  the field, printed right on top of that placeholder text. */}
              <InputLabel id="wr-employee-label" shrink>
                {t('weeklyReports.submittedByEmployee')}
              </InputLabel>
              <Select
                labelId="wr-employee-label"
                label={t('weeklyReports.submittedByEmployee')}
                value={submittedByEmployeeId}
                onChange={(event) => setSubmittedByEmployeeId(event.target.value)}
                displayEmpty
              >
                <MenuItem value="">
                  <em>{t('weeklyReports.submittedByEmployeeAuto')}</em>
                </MenuItem>
                {(employeesPage?.items ?? []).map((employee) => (
                  <MenuItem key={employee.id} value={employee.id}>
                    {employee.firstName} {employee.lastName}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          {type === 'SignedHours' && (
            <TextField
              type="number"
              label={t('weeklyReports.hours')}
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
              fullWidth
            />
          )}

          <TextField
            label={t('weeklyReports.note')}
            value={note}
            onChange={(event) => setNote(event.target.value)}
            multiline
            minRows={2}
            fullWidth
          />

<DropArea onFile={(f) => setFile(f)}>
          <Button
            variant="outlined"
            component="label"
            startIcon={<AttachFileOutlined />}
            color={file ? 'success' : 'primary'}
          >
            {file ? file.name : t('weeklyReports.attachFile')}
            <input
              hidden
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,.webp,.heic"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            />
          </Button>
</DropArea>

          {create.error && (
            <Typography variant="body2" color="error">
              {create.error.message}
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          onClick={submit}
          disabled={!canSubmit || create.isPending}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** What the office reads off the report in the preview, beside the sheet itself. */
function ReportDetails({ report }: { report: WeeklySiteReport }) {
  const t = useT();
  const enumLabel = useEnumLabel();

  return (
    <Stack spacing={1.5} sx={{ mb: 2 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {report.projectName} · {formatIsoWeek(report.isoYear, report.isoWeek)}
        </Typography>
        <StatusChip status={report.status} kind="weeklyReportStatus" />
      </Stack>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
          gap: 1.5,
        }}
      >
        <Fact label={t('weeklyReports.type')} value={enumLabel('weeklyReportType', report.type)} />
        <Fact label={t('weeklyReports.hours')} value={report.quantity == null ? '—' : String(report.quantity)} />
        <Fact label={t('weeklyReports.submittedBy')} value={report.submittedByEmployeeName} />
        <Fact label={t('weeklyReports.sentAt')} value={formatDateTime(report.createdAt)} />
      </Box>
      {report.note && <Fact label={t('weeklyReports.note')} value={report.note} />}
      {report.status === 'CorrectionRequested' && report.reviewNote && (
        <Alert severity="warning">{t('weeklyReports.reviewNote', { note: report.reviewNote })}</Alert>
      )}
      {report.status === 'Processed' && report.processedAt && (
        <Alert severity="success">
          {t('weeklyReports.approvedBy', {
            date: formatDateTime(report.processedAt),
            who: report.processedByEmail ?? '—',
          })}
        </Alert>
      )}
    </Stack>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
        {value}
      </Typography>
    </Box>
  );
}

function CorrectionDialog({
  report,
  onClose,
  onSent,
}: {
  report: WeeklySiteReport | null;
  onClose: () => void;
  onSent: () => void;
}) {
  const t = useT();
  const [note, setNote] = useState('');
  const { mutate, isPending, error } = useRequestWeeklySiteReportCorrection();

  const close = () => {
    setNote('');
    onClose();
  };

  return (
    <Dialog open={!!report} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('weeklyReports.correctionTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {report && `${report.projectName} · ${formatIsoWeek(report.isoYear, report.isoWeek)}`}
          </Typography>
          <TextField
            autoFocus
            multiline
            minRows={3}
            label={t('weeklyReports.correctionLabel')}
            helperText={t('weeklyReports.correctionHelp')}
            value={note}
            onChange={(event) => setNote(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 1000 } }}
          />
          {error && (
            <Typography variant="body2" color="error">
              {error.message}
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          color="warning"
          disabled={!note.trim() || isPending}
          onClick={() => {
            if (!report) return;
            mutate(
              { id: report.id, note: note.trim() },
              {
                onSuccess: () => {
                  setNote('');
                  onSent();
                },
              },
            );
          }}
        >
          {t('weeklyReports.correctionSend')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

type View = 'all' | WeeklyReportStatus | 'overdue';

export function WeeklySiteReportsListPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const canSubmit = canViewDirectory(user);
  const canReview = canAdministerAccounts(user);

  const [submitOpen, setSubmitOpen] = useState(false);
  // A notice opens the list already at what waits for a look.
  const [searchParams] = useSearchParams();
  const [view, setView] = useState<View>(() => {
    const wanted = searchParams.get('view');
    return wanted === 'Submitted' || wanted === 'CorrectionRequested' || wanted === 'overdue' ? wanted : 'all';
  });
  const [previewing, setPreviewing] = useState<WeeklySiteReport | null>(null);
  const [correcting, setCorrecting] = useState<WeeklySiteReport | null>(null);
  const list = useListQueryState('createdAt', 'desc');
  // `mutate` only: the mutation object is a new one on every render, which would rebuild the
  // memoised columns each time and make the grid update itself while this page renders.
  const { mutate: approve } = useApproveWeeklySiteReport();
  const { data: summary } = useWeeklySiteReportsSummaryQuery(canReview);

  const { setPaginationModel } = list;
  const choose = (next: View) => {
    setView(next);
    setPaginationModel((model) => ({ ...model, page: 0 }));
  };

  const { data, isLoading, isError, error, refetch } = useWeeklySiteReportsQuery({
    ...list.query,
    status: view === 'all' || view === 'overdue' ? '' : view,
    search: undefined,
  });

  // Fresh data for the open preview: approving changes the row the dialog was opened from.
  const openReport = previewing ? (data?.items.find((r) => r.id === previewing.id) ?? previewing) : null;

  const columns: GridColDef<WeeklySiteReport>[] = useMemo(
    () => [
      {
        field: 'isoWeek',
        headerName: t('weeklyReports.week'),
        width: 100,
        valueGetter: (_v, row) => formatIsoWeek(row.isoYear, row.isoWeek),
      },
      { field: 'projectName', headerName: t('weeklyReports.project'), flex: 1, minWidth: 160 },
      {
        field: 'submittedByEmployeeName',
        headerName: t('weeklyReports.submittedBy'),
        flex: 1,
        minWidth: 150,
      },
      {
        field: 'type',
        headerName: t('weeklyReports.type'),
        width: 130,
        valueGetter: (_v, row) => enumLabel('weeklyReportType', row.type),
      },
      {
        field: 'quantity',
        headerName: t('weeklyReports.hours'),
        width: 90,
        valueGetter: (v) => (v == null ? '—' : v),
      },
      {
        field: 'status',
        headerName: t('common.status'),
        width: 180,
        renderCell: (params) => (
          <Tooltip title={params.row.reviewNote ?? ''}>
            <span>
              <StatusChip status={params.row.status} kind="weeklyReportStatus" />
            </span>
          </Tooltip>
        ),
      },
      {
        field: 'actions',
        headerName: t('weeklyReports.col.actions'),
        width: 380,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', height: '100%' }}>
            <Button
              size="small"
              variant="outlined"
              startIcon={<VisibilityOutlined fontSize="small" />}
              onClick={() => setPreviewing(params.row)}
            >
              {t('weeklyReports.preview')}
            </Button>
            <Button
              size="small"
              startIcon={<DownloadOutlined fontSize="small" />}
              onClick={async () => {
                const blob = await weeklySiteReportsApi.blob(params.row.id);
                const url = URL.createObjectURL(blob);
                const anchor = document.createElement('a');
                anchor.href = url;
                anchor.download = params.row.fileName;
                anchor.click();
                URL.revokeObjectURL(url);
              }}
            >
              {t('weeklyReports.download')}
            </Button>
            {params.row.status !== 'Processed' && (
              <Button
                size="small"
                color="success"
                startIcon={<CheckCircleOutlined fontSize="small" />}
                onClick={() => approve(params.row.id)}
              >
                {t('weeklyReports.approve')}
              </Button>
            )}
          </Stack>
        ),
      },
    ],
    [t, enumLabel, approve],
  );

  const chips: { view: View; label: string; count?: number; color?: 'warning' | 'error' }[] = [
    { view: 'all', label: t('weeklyReports.chip.all') },
    { view: 'Submitted', label: t('weeklyReports.chip.awaiting'), count: summary?.awaitingReview, color: 'warning' },
    {
      view: 'CorrectionRequested',
      label: t('weeklyReports.chip.correction'),
      count: summary?.correctionRequested,
      color: 'warning',
    },
    { view: 'overdue', label: t('weeklyReports.chip.overdue'), count: summary?.overdue.length, color: 'error' },
    { view: 'Processed', label: t('weeklyReports.chip.approved') },
  ];

  return (
    <Box>
      <PageHeader
        title={t('weeklyReports.title')}
        description={t('weeklyReports.description')}
        action={
          canSubmit
            ? {
                label: t('weeklyReports.submit'),
                icon: <AddOutlined />,
                onClick: () => setSubmitOpen(true),
              }
            : undefined
        }
      />

      <SubmitReportDialog open={submitOpen} onClose={() => setSubmitOpen(false)} />

      <CorrectionDialog
        report={correcting}
        onClose={() => setCorrecting(null)}
        onSent={() => {
          setCorrecting(null);
          setPreviewing(null);
        }}
      />

      <AttachmentPreviewDialog
        attachment={openReport ? { id: openReport.id, fileName: openReport.fileName, contentType: '' } : null}
        onClose={() => setPreviewing(null)}
        loadBlob={weeklySiteReportsApi.blob}
        details={openReport ? <ReportDetails report={openReport} /> : undefined}
        actions={
          openReport && openReport.status !== 'Processed' ? (
            <>
              <Button
                color="warning"
                startIcon={<ReplayOutlined />}
                onClick={() => setCorrecting(openReport)}
              >
                {t('weeklyReports.requestCorrection')}
              </Button>
              <Button
                variant="contained"
                color="success"
                startIcon={<CheckCircleOutlined />}
                onClick={() => approve(openReport.id, { onSuccess: () => setPreviewing(null) })}
              >
                {t('weeklyReports.approve')}
              </Button>
            </>
          ) : openReport ? (
            <Button color="warning" startIcon={<ReplayOutlined />} onClick={() => setCorrecting(openReport)}>
              {t('weeklyReports.requestCorrection')}
            </Button>
          ) : undefined
        }
      />

      {canReview ? (
        <Stack spacing={2}>
          <HowItWorks id="weeklyReports">
            <HowLine>{t('weeklyReports.how1')}</HowLine>
            <HowLine>{t('weeklyReports.how2')}</HowLine>
            <HowLine>{t('weeklyReports.how3')}</HowLine>
            <HowLine>{t('weeklyReports.how4')}</HowLine>
          </HowItWorks>

          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
            {chips.map((chip) => {
              const selected = view === chip.view;
              const highlighted = !!chip.count && !selected;

              return (
                <Chip
                  key={chip.view}
                  clickable
                  onClick={() => choose(chip.view)}
                  color={selected ? 'primary' : highlighted ? chip.color : 'default'}
                  variant={selected || highlighted ? 'filled' : 'outlined'}
                  label={chip.count == null ? chip.label : `${chip.label} (${chip.count})`}
                />
              );
            })}
          </Stack>

          {view === 'overdue' ? (
            <OverduePanel summary={summary} />
          ) : (
            <ResourceDataGrid
              data={data}
              columns={columns}
              isLoading={isLoading}
              isError={isError}
              error={error}
              onRetry={() => void refetch()}
              paginationModel={list.paginationModel}
              onPaginationModelChange={list.setPaginationModel}
              sortModel={list.sortModel}
              onSortModelChange={list.setSortModel}
            />
          )}
        </Stack>
      ) : (
        <Typography color="text.secondary">{t('weeklyReports.submitOnlyHint')}</Typography>
      )}
    </Box>
  );
}

/** The sites that were posted people last week and have sent no report by the deadline. */
function OverduePanel({ summary }: { summary: WeeklySiteReportsSummary | undefined }) {
  const t = useT();

  if (!summary) return null;

  if (summary.overdue.length === 0) {
    return <Alert severity="success">{t('weeklyReports.overdueNone', { week: summary.isoWeek })}</Alert>;
  }

  return (
    <Stack spacing={1}>
      <Alert severity="error">
        {t('weeklyReports.overdueTitle', { week: summary.isoWeek, count: summary.overdue.length })}
      </Alert>
      {summary.overdue.map((site) => (
        <Paper key={site.projectId} variant="outlined" sx={{ px: 2, py: 1.25 }}>
          <Typography sx={{ fontWeight: 600 }}>{site.projectName}</Typography>
          <Typography variant="body2" color="text.secondary">
            {site.foremanNames.length > 0
              ? t('weeklyReports.overdueForeman', { names: site.foremanNames.join(', ') })
              : t('weeklyReports.overdueNoForeman')}
          </Typography>
        </Paper>
      ))}
    </Stack>
  );
}
