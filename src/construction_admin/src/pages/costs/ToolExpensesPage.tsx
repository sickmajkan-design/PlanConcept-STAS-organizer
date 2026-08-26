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
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { ToolExpenseListQuery } from '../../api/costs';
import { toolExpenseKinds, type ToolExpense, type ToolExpenseKind } from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useDeleteToolExpense,
  useRecordToolExpense,
  useToolExpensesQuery,
  useToolExpensesSummaryQuery,
  useUpdateToolExpense,
} from '../../features/costs/useCosts';
import { useAllToolsQuery } from '../../features/tools/useTools';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime, formatMoney } from '../../utils/formatting';

export function ToolExpensesPage() {
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<ToolExpenseKind | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<ToolExpense | null>(null);

  const query: ToolExpenseListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      kind: kind || undefined,
    }),
    [kind, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useToolExpensesQuery(query);
  const { data: summary } = useToolExpensesSummaryQuery(query);
  const remove = useDeleteWithConfirm<ToolExpense>(useDeleteToolExpense());

  const columns: GridColDef<ToolExpense>[] = useMemo(
    () => [
      {
        field: 'occurredOn',
        headerName: t('toolExpenses.occurredOn'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'toolName',
        headerName: t('toolExpenses.tool'),
        flex: 1,
        minWidth: 200,
      },
      {
        field: 'kind',
        headerName: t('toolExpenses.kind'),
        width: 130,
        valueGetter: (_value, row) => enumLabel('toolExpenseKind', row.kind),
      },
      {
        field: 'amount',
        headerName: t('toolExpenses.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'supplier',
        headerName: t('toolExpenses.supplier'),
        flex: 1,
        minWidth: 160,
        sortable: false,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'recordedByName',
        headerName: t('toolExpenses.recordedBy'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'createdAt',
        headerName: t('toolExpenses.createdAt'),
        width: 160,
        valueGetter: (value) => formatDateTime(value as string),
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
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              remove.request(params.row);
            }}
          >
            <DeleteOutlined fontSize="small" />
          </IconButton>
        ),
      },
    ],
    [enumLabel, locale, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('toolExpenses.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('toolExpenses.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <TextField
          select
          size="small"
          label={t('toolExpenses.kind')}
          value={kind}
          onChange={(event) => {
            setKind(event.target.value as ToolExpenseKind | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">{t('toolExpenses.allKinds')}</MenuItem>
          {toolExpenseKinds.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('toolExpenseKind', value)}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {summary && (
        <Paper variant="outlined" sx={{ px: 2, py: 1, mb: 2 }}>
          <Typography variant="body2" color="text.secondary">
            {t('toolExpenses.summaryTotal')}:{' '}
            <strong>{formatMoney(summary.totalAmount, locale)}</strong>
          </Typography>
        </Paper>
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
        onRowDoubleClick={(row) => setEditing(row)}
      />

      <ToolExpenseDialog open={recording} onClose={() => setRecording(false)} />
      <ToolExpenseDialog
        open={!!editing}
        editingExpense={editing}
        onClose={() => setEditing(null)}
        canAdminister={canAdministerAccounts(user)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('toolExpenses.deleteTitle')}
        description={t('toolExpenses.deleteBody')}
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

function ToolExpenseDialog({
  open,
  editingExpense,
  onClose,
  canAdminister,
}: {
  open: boolean;
  editingExpense?: ToolExpense | null;
  onClose: () => void;
  canAdminister?: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: tools } = useAllToolsQuery();
  const record = useRecordToolExpense();
  const update = useUpdateToolExpense();
  const isEditing = !!editingExpense;

  const [toolId, setToolId] = useState('');
  const [kind, setKind] = useState<ToolExpenseKind>('Repair');
  const [amount, setAmount] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [supplier, setSupplier] = useState('');
  const [note, setNote] = useState('');

  const resetRecord = record.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetRecord();
    resetUpdate();

    if (editingExpense) {
      setToolId(editingExpense.toolId);
      setKind(editingExpense.kind);
      setAmount(String(editingExpense.amount));
      setOccurredOn(editingExpense.occurredOn);
      setSupplier(editingExpense.supplier ?? '');
      setNote(editingExpense.note ?? '');
    } else {
      setToolId('');
      setKind('Repair');
      setAmount('');
      setOccurredOn('');
      setSupplier('');
      setNote('');
    }
  }, [open, editingExpense, resetRecord, resetUpdate]);

  const parsedAmount = Number(amount);
  const amountIsValid =
    amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount >= 0;

  const canSubmit = toolId !== '' && amountIsValid && (!isEditing || occurredOn !== '');
  const mutation = isEditing ? update : record;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const input = {
      toolId,
      kind,
      amount: parsedAmount,
      occurredOn: occurredOn || null,
      supplier: supplier.trim() || null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingExpense.id, input: { ...input, occurredOn } },
        { onSuccess: onClose },
      );
    } else {
      record.mutate(input, { onSuccess: onClose });
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {isEditing ? t('toolExpenses.editTitle') : t('toolExpenses.add')}
      </DialogTitle>
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
              label={t('toolExpenses.tool')}
              value={toolId}
              onChange={(event) => setToolId(event.target.value)}
            >
              {tools?.items.map((tool) => (
                <MenuItem key={tool.id} value={tool.id}>
                  {tool.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('toolExpenses.kind')}
              value={kind}
              onChange={(event) => setKind(event.target.value as ToolExpenseKind)}
            >
              {toolExpenseKinds.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('toolExpenseKind', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('toolExpenses.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              error={amount.trim() !== '' && !amountIsValid}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              required={isEditing}
              label={t('toolExpenses.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              fullWidth
              label={t('toolExpenses.supplier')}
              value={supplier}
              onChange={(event) => setSupplier(event.target.value)}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('toolExpenses.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <>
              <Grid size={12}>
                <AttachmentList
                  ownerType="ToolExpense"
                  ownerId={editingExpense.id}
                  categories={['Other']}
                  canUpload={canAdminister}
                  canDelete={canAdminister}
                />
              </Grid>
              {canAdminister && (
                <Grid size={12}>
                  <AuditHistoryCard entityName="ToolExpense" entityId={editingExpense.id} />
                </Grid>
              )}
            </>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || mutation.isPending}
          onClick={submit}
        >
          {isEditing ? t('common.save') : t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
