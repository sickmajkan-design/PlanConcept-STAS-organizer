import { AddOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  LinearProgress,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { ProjectRevenueListQuery } from '../../api/projects';
import type { ProjectRevenue } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import {
  useAllProjectsQuery,
  useAnnualRealizationPlanQuery,
  useDeleteProjectRevenue,
  useProjectRevenuesQuery,
  useRecordProjectRevenue,
} from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

/** The years STAS's own picker offers: a handful back, one ahead. */
function yearOptions(): number[] {
  const current = new Date().getFullYear();
  return Array.from({ length: 8 }, (_, i) => current + 1 - i);
}

export function AnnualRealizationPlanPage() {
  const t = useT();
  const { locale } = useI18n();
  const [year, setYear] = useState(() => new Date().getFullYear());
  const [recording, setRecording] = useState(false);

  const { data, isLoading, isError, error, refetch } = useAnnualRealizationPlanQuery(year);

  return (
    <Box>
      <PageHeader
        title={t('realization.title')}
        subtitle={t('realization.subtitle')}
        action={{
          label: t('realization.recordPayment'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <TextField
        select
        size="small"
        label={t('realization.year')}
        value={year}
        onChange={(event) => setYear(Number(event.target.value))}
        sx={{ minWidth: 140, mb: 2 }}
      >
        {yearOptions().map((value) => (
          <MenuItem key={value} value={value}>
            {value}
          </MenuItem>
        ))}
      </TextField>

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {!isError && isLoading && <LinearProgress sx={{ mb: 2 }} />}

      {!isError && !isLoading && data && data.rows.length === 0 && (
        <EmptyState message={t('realization.empty')} />
      )}

      {!isError && data && data.rows.length > 0 && (
        <Stack spacing={2}>
          <SummaryCards
            contracted={data.totalContractValue}
            realizedThisYear={data.totalRealizedThisYear}
            remaining={data.totalRemaining}
            percent={data.percentRealized}
            locale={locale}
          />

          <TableContainer component={Paper} variant="outlined" sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('realization.project')}</TableCell>
                  <TableCell>{t('realization.status')}</TableCell>
                  <TableCell align="right">{t('realization.contracted')}</TableCell>
                  <TableCell align="right">{t('realization.realizedThisYear')}</TableCell>
                  <TableCell align="right">{t('realization.realizedToDate')}</TableCell>
                  <TableCell align="right">{t('realization.remaining')}</TableCell>
                  <TableCell align="right">{t('realization.percentOfContract')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.rows.map((row) => (
                  <TableRow key={row.projectId} hover>
                    <TableCell>{row.projectName}</TableCell>
                    <TableCell>
                      <StatusChip status={row.status} kind="projectStatus" />
                    </TableCell>
                    <TableCell align="right">
                      {row.contractValue > 0 ? (
                        formatMoney(row.contractValue, locale)
                      ) : (
                        <Typography variant="caption" color="text.disabled">
                          {t('realization.noContract')}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right">
                      {formatMoney(row.realizedThisYear, locale)}
                    </TableCell>
                    <TableCell align="right">
                      {formatMoney(row.realizedToDate, locale)}
                    </TableCell>
                    <TableCell align="right">{formatMoney(row.remaining, locale)}</TableCell>
                    <TableCell align="right">
                      {row.percentOfContract === null
                        ? '—'
                        : `${(row.percentOfContract * 100).toFixed(1)}%`}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
              <TableFooter>
                <TableRow>
                  <TableCell sx={{ fontWeight: 700 }} colSpan={2}>
                    {t('common.total')}
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {formatMoney(data.totalContractValue, locale)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {formatMoney(data.totalRealizedThisYear, locale)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {formatMoney(data.totalRealizedToDate, locale)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {formatMoney(data.totalRemaining, locale)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {data.percentRealized === null
                      ? '—'
                      : `${(data.percentRealized * 100).toFixed(1)}%`}
                  </TableCell>
                </TableRow>
              </TableFooter>
            </Table>
          </TableContainer>

          <PaymentsList year={year} />
        </Stack>
      )}

      <RecordRevenueDialog open={recording} onClose={() => setRecording(false)} />
    </Box>
  );
}

function SummaryCards({
  contracted,
  realizedThisYear,
  remaining,
  percent,
  locale,
}: {
  contracted: number;
  realizedThisYear: number;
  remaining: number;
  percent: number | null;
  locale: string;
}) {
  const t = useT();

  const cards = [
    { label: t('realization.contracted'), value: formatMoney(contracted, locale) },
    { label: t('realization.realizedThisYear'), value: formatMoney(realizedThisYear, locale) },
    { label: t('realization.remaining'), value: formatMoney(remaining, locale) },
    {
      label: t('realization.percentOfContract'),
      value: percent === null ? '—' : `${(percent * 100).toFixed(1)}%`,
    },
  ];

  return (
    <Grid container spacing={2}>
      {cards.map((card) => (
        <Grid key={card.label} size={{ xs: 6, sm: 3 }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="caption" color="text.secondary">
              {card.label}
            </Typography>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              {card.value}
            </Typography>
          </Paper>
        </Grid>
      ))}
    </Grid>
  );
}

/** The individual payments the year's totals are built from, so a wrong one is easy to find and remove. */
function PaymentsList({ year }: { year: number }) {
  const t = useT();
  const { locale } = useI18n();
  const list = useListQueryState('occurredOn', 'desc');

  const query: ProjectRevenueListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      from: `${year}-01-01`,
      to: `${year}-12-31`,
    }),
    [list.query, year],
  );

  const { data, isLoading, isError, error, refetch } = useProjectRevenuesQuery(query);
  const remove = useDeleteWithConfirm<ProjectRevenue>(useDeleteProjectRevenue());

  const columns: GridColDef<ProjectRevenue>[] = useMemo(
    () => [
      {
        field: 'projectName',
        headerName: t('realization.project'),
        flex: 1,
        minWidth: 180,
      },
      {
        field: 'amount',
        headerName: t('realization.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'occurredOn',
        headerName: t('realization.occurredOn'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'note',
        headerName: t('realization.note'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'actions',
        headerName: '',
        width: 60,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Tooltip title={t('common.delete')}>
            <IconButton size="small" onClick={() => remove.request(params.row)}>
              <DeleteOutlined fontSize="small" />
            </IconButton>
          </Tooltip>
        ),
      },
    ],
    [locale, remove, t],
  );

  return (
    <Box>
      <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
        {t('realization.payments')}
      </Typography>

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

      <ConfirmDialog
        open={!!remove.pending}
        title={t('realization.deleteTitle')}
        description={t('realization.deleteBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Box>
  );
}

function RecordRevenueDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const { data: projects } = useAllProjectsQuery();
  const record = useRecordProjectRevenue();

  const [projectId, setProjectId] = useState('');
  const [amount, setAmount] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [note, setNote] = useState('');

  const reset = record.reset;

  useEffect(() => {
    if (open) {
      setProjectId('');
      setAmount('');
      setOccurredOn('');
      setNote('');
      reset();
    }
  }, [open, reset]);

  const parsedAmount = Number(amount);
  const amountIsValid = amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount > 0;
  const canSubmit = projectId !== '' && amountIsValid;
  const error = record.isError ? toApiError(record.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('realization.recordPayment')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('realization.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
            >
              {projects?.items.map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('realization.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              error={amount.trim() !== '' && !amountIsValid}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('realization.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('realization.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || record.isPending}
          onClick={() =>
            record.mutate(
              {
                projectId,
                amount: parsedAmount,
                occurredOn: occurredOn || null,
                note: note.trim() || null,
              },
              { onSuccess: onClose },
            )
          }
        >
          {t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
