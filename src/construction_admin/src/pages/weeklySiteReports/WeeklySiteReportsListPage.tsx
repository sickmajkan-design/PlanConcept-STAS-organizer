import {
  AddOutlined,
  AttachFileOutlined,
  CheckCircleOutlined,
  DownloadOutlined,
} from '@mui/icons-material';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import { weeklySiteReportsApi } from '../../api/weeklySiteReports';
import { weeklyReportTypes, type WeeklyReportType, type WeeklySiteReport } from '../../api/types';
import { canAdministerAccounts, canManageAssignments, canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useCreateWeeklySiteReport,
  useMarkWeeklySiteReportProcessed,
  useReportableProjectsQuery,
  useWeeklySiteReportsQuery,
} from '../../features/weeklySiteReports/useWeeklySiteReports';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatIsoWeek, getLastCompletedIsoWeek } from '../../utils/isoWeek';

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

export function WeeklySiteReportsListPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const canSubmit = canViewDirectory(user);
  const canReview = canAdministerAccounts(user);

  const [submitOpen, setSubmitOpen] = useState(false);
  const list = useListQueryState('createdAt', 'desc');
  const markProcessed = useMarkWeeklySiteReportProcessed();

  const { data, isLoading, isError, error, refetch } = useWeeklySiteReportsQuery({
    ...list.query,
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
        width: 100,
        valueGetter: (v) => (v == null ? '—' : v),
      },
      {
        field: 'status',
        headerName: t('common.status'),
        width: 130,
        renderCell: (params) => <StatusChip status={params.row.status} kind="weeklyReportStatus" />,
      },
      {
        field: 'actions',
        headerName: '',
        width: 130,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title={t('common.download')}>
              <IconButton
                size="small"
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
                <DownloadOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            {params.row.status === 'Submitted' && (
              <Tooltip title={t('weeklyReports.markProcessed')}>
                <IconButton
                  size="small"
                  onClick={() => markProcessed.mutate(params.row.id)}
                >
                  <CheckCircleOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            {params.row.status === 'Processed' && (
              <CheckCircleOutlined fontSize="small" color="success" />
            )}
          </Stack>
        ),
      },
    ],
    [t, enumLabel, markProcessed],
  );

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

      {canReview ? (
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
      ) : (
        <Typography color="text.secondary">{t('weeklyReports.submitOnlyHint')}</Typography>
      )}
    </Box>
  );
}
