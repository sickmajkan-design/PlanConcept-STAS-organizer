import {
  AddOutlined,
  CheckCircleOutlined,
  DownloadOutlined,
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
  Paper,
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
  type WeeklyReportStatus,
  type WeeklySiteReport,
  type WeeklySiteReportsSummary,
} from '../../api/types';
import { canAdministerAccounts, canManageAssignments, canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import {
  useApproveWeeklySiteReport,
  useRequestWeeklySiteReportCorrection,
  useWeeklySiteReportsQuery,
  useWeeklySiteReportsSummaryQuery,
} from '../../features/weeklySiteReports/useWeeklySiteReports';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatIsoWeek } from '../../utils/isoWeek';
import { ReportReviewDialog } from '../../features/weeklySiteReports/ReportReviewDialog';
import { SubmitReportDialog } from '../../features/weeklySiteReports/SubmitReportDialog';
import { ProjectLabel } from '../../components/ProjectLabel';

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
  // The office also enters reports that came by e-mail or on paper, in the foreman's name.
  const enteringForOffice = canManageAssignments(user);

  const [submitOpen, setSubmitOpen] = useState(false);
  const [enteredNotice, setEnteredNotice] = useState<string | null>(null);
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

  const columns: GridColDef<WeeklySiteReport>[] = useMemo(
    () => [
      {
        field: 'isoWeek',
        headerName: t('weeklyReports.week'),
        width: 100,
        valueGetter: (_v, row) => formatIsoWeek(row.isoYear, row.isoWeek),
      },
      {
        field: 'projectName',
        headerName: t('weeklyReports.project'),
        flex: 1,
        minWidth: 160,
        renderCell: ({ row }) => <ProjectLabel name={row.projectName} projectId={row.projectId} variant="stacked" noWrap />,
      },
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
        field: 'source',
        headerName: t('weeklyReports.col.source'),
        width: 120,
        sortable: false,
        renderCell: (params) => (
          <Tooltip title={params.row.source === 'Office' ? [params.row.enteredByEmail, params.row.sourceNote].filter(Boolean).join(' · ') : ''}>
            <span>{params.row.source === 'Office' ? t('weeklyReports.source.office') : t('weeklyReports.source.app')}</span>
          </Tooltip>
        ),
      },
      {
        field: 'pageCount',
        headerName: t('weeklyReports.col.content'),
        width: 170,
        sortable: false,
        valueGetter: (_v, row) =>
          row.lineCount > 0
            ? t('weeklyReports.col.contentValue', { pages: row.pageCount, workers: row.lineCount })
            : t('weeklyReports.col.contentPagesOnly', { pages: row.pageCount }),
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
        width: 360,
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
            {params.row.status !== 'Processed' && params.row.lineCount === 0 && (
              <Button
                size="small"
                color="success"
                startIcon={<CheckCircleOutlined fontSize="small" />}
                onClick={() => approve({ id: params.row.id })}
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
                label: enteringForOffice ? t('weeklyReports.enter') : t('weeklyReports.submit'),
                icon: <AddOutlined />,
                onClick: () => setSubmitOpen(true),
              }
            : undefined
        }
      />

      <SubmitReportDialog
        open={submitOpen}
        onClose={() => setSubmitOpen(false)}
        onCreated={(report) => {
          // A report that joined an existing one is said so; the account that can review opens it at once,
          // with the pictures beside the workers, to check and enter into the Ledger.
          const joined = Date.now() - new Date(report.createdAt).getTime() > 60_000;
          setEnteredNotice(joined ? t('weeklyReports.enteredJoined') : t('weeklyReports.entered'));
          if (canReview) setPreviewing(report);
        }}
      />

      {enteredNotice && (
        <Alert severity="success" onClose={() => setEnteredNotice(null)} sx={{ mb: 2 }}>
          {enteredNotice}
        </Alert>
      )}

      <CorrectionDialog
        report={correcting}
        onClose={() => setCorrecting(null)}
        onSent={() => {
          setCorrecting(null);
          setPreviewing(null);
        }}
      />

      <ReportReviewDialog
        reportId={previewing?.id ?? null}
        onClose={() => setPreviewing(null)}
        onRequestCorrection={(report) => setCorrecting(report)}
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
          <Typography sx={{ fontWeight: 600 }}><ProjectLabel name={site.projectName} projectId={site.projectId} /></Typography>
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
