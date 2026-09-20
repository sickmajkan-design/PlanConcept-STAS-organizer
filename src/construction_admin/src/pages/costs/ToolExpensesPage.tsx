import { AddOutlined, BuildCircleOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { GridSortModel } from '@mui/x-data-grid';
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
import { CostLedgerBoard, useLedgerWindow, type LedgerRow } from '../../components/costs/CostLedgerBoard';
import { Stat } from '../../components/costs/costUi';
import { ALL_TIME, LedgerPeriodBar, type LedgerPeriod } from '../../components/costs/LedgerPeriodBar';
import { SortBar } from '../../components/costs/SortBar';
import { SavedViewsBar } from '../../components/SavedViewsBar';
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
import { useSavedViews } from '../../hooks/useSavedViews';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

interface ToolExpenseViewState {
  sortModel: GridSortModel;
  kind: ToolExpenseKind | '';
}

export function ToolExpensesPage() {
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<ToolExpenseKind | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<ToolExpense | null>(null);

  const savedViews = useSavedViews<ToolExpenseViewState>('tool-expenses');

  const applyView = (state: ToolExpenseViewState) => {
    list.setSortModel(state.sortModel);
    setKind(state.kind);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, { sortModel: list.sortModel, kind });
  };

  const [period, setPeriod] = useState<LedgerPeriod>(ALL_TIME);
  const window = useLedgerWindow();

  const query: ToolExpenseListQuery = useMemo(
    () => ({
      ...list.query,
      pageNumber: 1,
      pageSize: window.pageSize,
      search: undefined,
      kind: kind || undefined,
      from: period.from || undefined,
      to: period.to || undefined,
    }),
    [kind, list.query, period, window.pageSize],
  );

  const { data, isLoading, isError, error, refetch } = useToolExpensesQuery(query);
  const { data: summary } = useToolExpensesSummaryQuery(query);
  const remove = useDeleteWithConfirm<ToolExpense>(useDeleteToolExpense());

  const rows: LedgerRow<ToolExpense>[] = useMemo(
    () =>
      (data?.items ?? []).map((expense) => ({
        item: expense,
        id: expense.id,
        date: expense.occurredOn,
        icon: <BuildCircleOutlined fontSize="small" />,
        title: expense.toolName,
        subtitle: [
          formatDate(expense.occurredOn),
          enumLabel('toolExpenseKind', expense.kind),
          expense.supplier,
          expense.note,
        ]
          .filter(Boolean)
          .join(' · '),
        amount: expense.amount,
      })),
    [data, enumLabel],
  );

  return (
    <Box>
      <PageHeader
        title={t('toolExpenses.title')}
        description={t('toolExpenses.description')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('toolExpenses.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2 }}>
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

      <Box sx={{ mb: 2 }}>
        <SavedViewsBar
          views={savedViews.views}
          onApply={applyView}
          onSave={saveCurrentView}
          onDelete={savedViews.deleteView}
        />
      </Box>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 2, alignItems: 'center', mb: 2 }}>
          <Stat label={t('toolExpenses.summaryTotal')} value={formatMoney(summary?.totalAmount ?? 0, locale)} />
          <Stat label={t('costs.entries')} value={String(data?.totalCount ?? 0)} accent="text.disabled" />
        </Stack>
        <LedgerPeriodBar
          value={period}
          onChange={(next) => {
            setPeriod(next);
            list.resetToFirstPage();
          }}
        />
      </Paper>

      <Box sx={{ mb: 2 }}>
        <SortBar
          value={(list.sortModel[0]?.field ?? 'occurredOn') as string}
          direction={list.sortModel[0]?.sort === 'asc' ? 'asc' : 'desc'}
          onChange={(field, dir) => list.setSortModel([{ field, sort: dir }])}
          options={[
            { value: 'occurredOn', label: t('toolExpenses.occurredOn') },
            { value: 'toolName', label: t('toolExpenses.tool') },
            { value: 'kind', label: t('toolExpenses.kind') },
            { value: 'amount', label: t('toolExpenses.amount') },
            { value: 'createdAt', label: t('toolExpenses.createdAt') },
          ]}
        />
      </Box>

      <CostLedgerBoard
        rows={rows}
        totalCount={data?.totalCount ?? 0}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        onOpen={(expense) => setEditing(expense)}
        onDelete={(expense) => remove.request(expense)}
        window={window}
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

export function ToolExpenseDialog({
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
