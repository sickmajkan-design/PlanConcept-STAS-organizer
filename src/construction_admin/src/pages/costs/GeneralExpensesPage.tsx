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
import type { GridColDef, GridSortModel } from '@mui/x-data-grid';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { GeneralExpenseListQuery } from '../../api/costs';
import {
  generalExpenseCategories,
  type GeneralExpense,
  type GeneralExpenseCategory,
} from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import {
  useDeleteGeneralExpense,
  useGeneralExpensesQuery,
  useGeneralExpensesSummaryQuery,
  useRecordGeneralExpense,
  useUpdateGeneralExpense,
} from '../../features/costs/useCosts';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime, formatMoney } from '../../utils/formatting';

interface GeneralExpenseViewState {
  sortModel: GridSortModel;
  category: GeneralExpenseCategory | '';
}

export function GeneralExpensesPage() {
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [category, setCategory] = useState<GeneralExpenseCategory | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<GeneralExpense | null>(null);

  const savedViews = useSavedViews<GeneralExpenseViewState>('general-expenses');

  const applyView = (state: GeneralExpenseViewState) => {
    list.setSortModel(state.sortModel);
    setCategory(state.category);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, { sortModel: list.sortModel, category });
  };

  const query: GeneralExpenseListQuery = useMemo(
    () => ({
      ...list.query,
      search: undefined,
      category: category || undefined,
    }),
    [category, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useGeneralExpensesQuery(query);
  const { data: summary } = useGeneralExpensesSummaryQuery(query);
  const remove = useDeleteWithConfirm<GeneralExpense>(useDeleteGeneralExpense());

  const columns: GridColDef<GeneralExpense>[] = useMemo(
    () => [
      {
        field: 'occurredOn',
        headerName: t('generalExpenses.occurredOn'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'category',
        headerName: t('generalExpenses.category'),
        width: 150,
        valueGetter: (_value, row) => enumLabel('generalExpenseCategory', row.category),
      },
      {
        field: 'amount',
        headerName: t('generalExpenses.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number, locale),
      },
      {
        field: 'projectName',
        headerName: t('generalExpenses.project'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'employeeName',
        headerName: t('generalExpenses.employee'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'supplier',
        headerName: t('generalExpenses.supplier'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'recordedByName',
        headerName: t('generalExpenses.recordedBy'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'createdAt',
        headerName: t('generalExpenses.createdAt'),
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
        title={t('generalExpenses.title')}
        description={t('generalExpenses.description')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('generalExpenses.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <TextField
          select
          size="small"
          label={t('generalExpenses.category')}
          value={category}
          onChange={(event) => {
            setCategory(event.target.value as GeneralExpenseCategory | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">{t('generalExpenses.allCategories')}</MenuItem>
          {generalExpenseCategories.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('generalExpenseCategory', value)}
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

      {summary && (
        <Paper variant="outlined" sx={{ px: 2, py: 1, mb: 2 }}>
          <Typography variant="body2" color="text.secondary">
            {t('generalExpenses.summaryTotal')}:{' '}
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

      <GeneralExpenseDialog open={recording} onClose={() => setRecording(false)} />
      <GeneralExpenseDialog
        open={!!editing}
        editingExpense={editing}
        onClose={() => setEditing(null)}
        canAdminister={canAdministerAccounts(user)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('generalExpenses.deleteTitle')}
        description={t('generalExpenses.deleteBody')}
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

export function GeneralExpenseDialog({
  open,
  editingExpense,
  onClose,
  canAdminister,
}: {
  open: boolean;
  editingExpense?: GeneralExpense | null;
  onClose: () => void;
  canAdminister?: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: projects } = useAllProjectsQuery();
  const { data: employees } = useAllEmployeesQuery();
  const record = useRecordGeneralExpense();
  const update = useUpdateGeneralExpense();
  const isEditing = !!editingExpense;

  const [category, setCategory] = useState<GeneralExpenseCategory>('Other');
  const [amount, setAmount] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [projectId, setProjectId] = useState('');
  const [employeeId, setEmployeeId] = useState('');
  const [supplier, setSupplier] = useState('');
  const [note, setNote] = useState('');

  const resetRecord = record.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetRecord();
    resetUpdate();

    if (editingExpense) {
      setCategory(editingExpense.category);
      setAmount(String(editingExpense.amount));
      setOccurredOn(editingExpense.occurredOn);
      setProjectId(editingExpense.projectId ?? '');
      setEmployeeId(editingExpense.employeeId ?? '');
      setSupplier(editingExpense.supplier ?? '');
      setNote(editingExpense.note ?? '');
    } else {
      setCategory('Other');
      setAmount('');
      setOccurredOn('');
      setProjectId('');
      setEmployeeId('');
      setSupplier('');
      setNote('');
    }
  }, [open, editingExpense, resetRecord, resetUpdate]);

  const parsedAmount = Number(amount);
  const amountIsValid =
    amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount >= 0;

  const canSubmit = amountIsValid && (!isEditing || occurredOn !== '');
  const mutation = isEditing ? update : record;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const input = {
      category,
      amount: parsedAmount,
      occurredOn: occurredOn || null,
      projectId: projectId || null,
      employeeId: employeeId || null,
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
        {isEditing ? t('generalExpenses.editTitle') : t('generalExpenses.add')}
      </DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('generalExpenses.category')}
              value={category}
              onChange={(event) => setCategory(event.target.value as GeneralExpenseCategory)}
            >
              {generalExpenseCategories.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('generalExpenseCategory', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('generalExpenses.amount')}
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
              label={t('generalExpenses.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              fullWidth
              label={t('generalExpenses.supplier')}
              value={supplier}
              onChange={(event) => setSupplier(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('generalExpenses.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
              helperText={t('generalExpenses.optionalHint')}
            >
              <MenuItem value="">
                <em>{t('common.none')}</em>
              </MenuItem>
              {(projects?.items ?? []).map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('generalExpenses.employee')}
              value={employeeId}
              onChange={(event) => setEmployeeId(event.target.value)}
              helperText={t('generalExpenses.optionalHint')}
            >
              <MenuItem value="">
                <em>{t('common.none')}</em>
              </MenuItem>
              {(employees?.items ?? []).map((employee) => (
                <MenuItem key={employee.id} value={employee.id}>
                  {employee.firstName} {employee.lastName}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('generalExpenses.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <>
              <Grid size={12}>
                <AttachmentList
                  ownerType="GeneralExpense"
                  ownerId={editingExpense.id}
                  categories={['Other']}
                  canUpload={canAdminister}
                  canDelete={canAdminister}
                />
              </Grid>
              {canAdminister && (
                <Grid size={12}>
                  <AuditHistoryCard entityName="GeneralExpense" entityId={editingExpense.id} />
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
