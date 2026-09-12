import { AddOutlined, DeleteOutlined, TableChartOutlined } from '@mui/icons-material';
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
  MenuItem,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { LedgerListQuery } from '../../api/ledgers';
import type { LedgerSummary } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import {
  useCreateLedger,
  useDeleteLedger,
  useLedgersQuery,
} from '../../features/ledgers/useLedgers';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';

const MONTH_NAMES_KEYS = [
  'january', 'february', 'march', 'april', 'may', 'june',
  'july', 'august', 'september', 'october', 'november', 'december',
] as const;

export function LedgersListPage() {
  const navigate = useNavigate();
  const t = useT();
  const list = useListQueryState('year', 'desc', 'ledgers');

  const query: LedgerListQuery = list.query;

  const { data, isLoading, isError, error, refetch } = useLedgersQuery(query);
  const remove = useDeleteWithConfirm<LedgerSummary>(useDeleteLedger());
  const [creating, setCreating] = useState(false);

  const columns: GridColDef<LedgerSummary>[] = useMemo(
    () => [
      { field: 'name', headerName: t('ledgers.name'), flex: 1, minWidth: 200 },
      {
        field: 'month',
        headerName: t('ledgers.period'),
        width: 160,
        valueGetter: (_value, row) => `${t(`ledgers.${MONTH_NAMES_KEYS[row.month - 1]}`)} ${row.year}`,
      },
      {
        field: 'note',
        headerName: t('ledgers.note'),
        flex: 1,
        minWidth: 200,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'createdByName',
        headerName: t('ledgers.createdBy'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'actions',
        headerName: '',
        width: 100,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <RowActions>
            <Tooltip title={t('common.view')}>
              <IconButton size="small" onClick={() => navigate(paths.ledgerDetail(params.row.id))}>
                <TableChartOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.delete')}>
              <IconButton size="small" onClick={() => remove.request(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </RowActions>
        ),
      },
    ],
    [navigate, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('ledgers.title')}
        description={t('ledgers.description')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('ledgers.add'),
          icon: <AddOutlined />,
          onClick: () => setCreating(true),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('ledgers.searchPlaceholder')}
        />
      </Stack>

      {list.savedViews && (
        <Box sx={{ mb: 2 }}>
          <SavedViewsBar
            views={list.savedViews.views}
            onApply={list.savedViews.applyView}
            onSave={list.savedViews.saveCurrentView}
            onDelete={list.savedViews.deleteView}
          />
        </Box>
      )}

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
        onRowClick={(row) => navigate(paths.ledgerDetail(row.id))}
      />

      <CreateLedgerDialog
        open={creating}
        onClose={() => setCreating(false)}
        existingLedgers={data?.items ?? []}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('ledgers.deleteTitle')}
        description={remove.pending ? t('ledgers.deleteBody', { name: remove.pending.name }) : ''}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {remove.error.message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}

function CreateLedgerDialog({
  open,
  onClose,
  existingLedgers,
}: {
  open: boolean;
  onClose: () => void;
  existingLedgers: LedgerSummary[];
}) {
  const t = useT();
  const navigate = useNavigate();
  const create = useCreateLedger();

  const today = new Date();
  const [name, setName] = useState('');
  const [year, setYear] = useState(String(today.getFullYear()));
  const [month, setMonth] = useState(String(today.getMonth() + 1));
  const [note, setNote] = useState('');
  const [copyFromId, setCopyFromId] = useState('');

  const resetCreate = create.reset;

  useEffect(() => {
    if (!open) return;
    resetCreate();
    setName('');
    setYear(String(today.getFullYear()));
    setMonth(String(today.getMonth() + 1));
    setNote('');
    setCopyFromId('');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, resetCreate]);

  const parsedYear = Number(year);
  const parsedMonth = Number(month);
  const canSubmit =
    name.trim() !== ''
    && Number.isInteger(parsedYear) && parsedYear >= 2000 && parsedYear <= 2100
    && Number.isInteger(parsedMonth) && parsedMonth >= 1 && parsedMonth <= 12;

  const error = create.isError ? toApiError(create.error) : null;

  const submit = () => {
    create.mutate(
      {
        name: name.trim(),
        year: parsedYear,
        month: parsedMonth,
        note: note.trim() || null,
        copyFromLedgerId: copyFromId || null,
      },
      {
        onSuccess: (ledger) => {
          onClose();
          navigate(paths.ledgerDetail(ledger.id));
        },
      },
    );
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('ledgers.newTitle')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              fullWidth
              label={t('ledgers.name')}
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 6, sm: 4 }}>
            <TextField
              type="number"
              fullWidth
              label={t('ledgers.year')}
              value={year}
              onChange={(event) => setYear(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 6, sm: 4 }}>
            <TextField
              select
              fullWidth
              label={t('ledgers.month')}
              value={month}
              onChange={(event) => setMonth(event.target.value)}
            >
              {MONTH_NAMES_KEYS.map((key, index) => (
                <MenuItem key={key} value={index + 1}>
                  {t(`ledgers.${key}`)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          {existingLedgers.length > 0 && (
            <Grid size={{ xs: 12, sm: 4 }}>
              <TextField
                select
                fullWidth
                label={t('ledgers.copyFrom')}
                value={copyFromId}
                onChange={(event) => setCopyFromId(event.target.value)}
                helperText={t('ledgers.copyFromHint')}
              >
                <MenuItem value="">
                  <em>{t('ledgers.copyFromNone')}</em>
                </MenuItem>
                {existingLedgers.map((ledger) => (
                  <MenuItem key={ledger.id} value={ledger.id}>
                    {ledger.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          )}

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('ledgers.note')}
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
          disabled={!canSubmit}
          loading={create.isPending}
          onClick={submit}
        >
          {t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
