import { AddOutlined, RequestQuoteOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Chip,
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
import type { FinanceEntryListQuery } from '../../api/costs';
import { exportsApi } from '../../api/exports';
import { financeEntryKinds, type FinanceEntry, type FinanceEntryKind } from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import { CostLedgerBoard, useLedgerWindow, type LedgerRow } from '../../components/costs/CostLedgerBoard';
import { Stat } from '../../components/costs/costUi';
import { ALL_TIME, LedgerPeriodBar, type LedgerPeriod } from '../../components/costs/LedgerPeriodBar';
import { SortBar } from '../../components/costs/SortBar';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import {
  useDeleteFinanceEntry,
  useFinanceEntriesQuery,
  useFinanceEntriesSummaryQuery,
  useRecordFinanceEntry,
  useUpdateFinanceEntry,
} from '../../features/costs/useCosts';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { readLedgerDeepLinkPeriod, useOpenEntryFromLink } from '../../hooks/useLedgerDeepLink';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney, lastYearRange } from '../../utils/formatting';

interface FinanceEntryViewState {
  sortModel: GridSortModel;
  kind: FinanceEntryKind | '';
}

export function FinanceEntriesPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { locale } = useI18n();
  const { user } = useAuth();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<FinanceEntryKind | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<FinanceEntry | null>(null);

  const savedViews = useSavedViews<FinanceEntryViewState>('finance-entries');

  const applyView = (state: FinanceEntryViewState) => {
    list.setSortModel(state.sortModel);
    setKind(state.kind);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, { sortModel: list.sortModel, kind });
  };

  const [period, setPeriod] = useState<LedgerPeriod>(() => readLedgerDeepLinkPeriod() ?? ALL_TIME);
  const window = useLedgerWindow();

  const query: FinanceEntryListQuery = useMemo(
    () => ({
      ...list.query,
      pageNumber: 1,
      pageSize: window.pageSize,
      from: period.from || undefined,
      to: period.to || undefined,
      search: undefined,
      kind: kind || undefined,
    }),
    [kind, list.query, period, window.pageSize],
  );

  const { data, isLoading, isError, error, refetch } = useFinanceEntriesQuery(query);
  useOpenEntryFromLink(data?.items, (item) => setEditing(item));
  const { data: summary } = useFinanceEntriesSummaryQuery(query);
  const remove = useDeleteWithConfirm<FinanceEntry>(useDeleteFinanceEntry());

  const rows: LedgerRow<FinanceEntry>[] = useMemo(
    () =>
      (data?.items ?? []).map((entry) => ({
        item: entry,
        id: entry.id,
        date: entry.occurredOn,
        icon: <RequestQuoteOutlined fontSize="small" />,
        title: entry.employeeName,
        chips: <Chip size="small" variant="outlined" label={enumLabel('financeEntryKind', entry.kind)} />,
        subtitle: [
          formatDate(entry.occurredOn),
          entry.projectName,
          entry.hoursWorked === null ? null : `${entry.hoursWorked} h`,
          entry.note,
        ]
          .filter(Boolean)
          .join(' · '),
        amount: entry.amount,
      })),
    [data, enumLabel, locale],
  );

  return (
    <Box>
      <PageHeader
        title={t('financeEntries.title')}
        description={t('financeEntries.description')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('financeEntries.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2, alignItems: 'center' }}>
        <TextField
          select
          size="small"
          label={t('financeEntries.kind')}
          value={kind}
          onChange={(event) => {
            setKind(event.target.value as FinanceEntryKind | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">{t('financeEntries.allKinds')}</MenuItem>
          {financeEntryKinds.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('financeEntryKind', value)}
            </MenuItem>
          ))}
        </TextField>

        {/* Exports the last year rather than whatever's on screen: a
            spreadsheet of one page's rows is not what anyone opens this for. */}
        <ExportButton
          onExport={(language) =>
            exportsApi.financeEntries({ ...lastYearRange(), language })
          }
        />
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
          <Stat label={t('financeEntries.summaryTotal')} value={formatMoney(summary?.totalAmount ?? 0, locale)} />
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
            { value: 'employeeName', label: t('financeEntries.employee') },
            { value: 'kind', label: t('financeEntries.kind') },
            { value: 'amount', label: t('financeEntries.amount') },
            { value: 'hoursWorked', label: t('financeEntries.hoursWorked') },
            { value: 'occurredOn', label: t('financeEntries.occurredOn') },
            { value: 'projectName', label: t('financeEntries.project') },
            { value: 'recordedByName', label: t('financeEntries.recordedBy') },
            { value: 'createdAt', label: t('financeEntries.createdAt') },
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
        onOpen={(item) => setEditing(item)}
        onDelete={(item) => remove.request(item)}
        window={window}
      />

      <RecordFinanceEntryDialog open={recording} onClose={() => setRecording(false)} />
      <RecordFinanceEntryDialog
        open={!!editing}
        editingEntry={editing}
        onClose={() => setEditing(null)}
        canAdminister={canAdministerAccounts(user)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('financeEntries.deleteTitle')}
        description={remove.pending ? t('financeEntries.deleteBody') : ''}
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

function RecordFinanceEntryDialog({
  open,
  editingEntry,
  onClose,
  canAdminister,
}: {
  open: boolean;
  editingEntry?: FinanceEntry | null;
  onClose: () => void;
  canAdminister?: boolean;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: employees } = useAllEmployeesQuery();
  const { data: projects } = useAllProjectsQuery();
  const record = useRecordFinanceEntry();
  const update = useUpdateFinanceEntry();
  const isEditing = !!editingEntry;

  const [employeeId, setEmployeeId] = useState('');
  const [kind, setKind] = useState<FinanceEntryKind>('WorkerPaymentHourly');
  const [amount, setAmount] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [projectId, setProjectId] = useState('');
  const [hoursWorked, setHoursWorked] = useState('');
  const [note, setNote] = useState('');

  const resetRecord = record.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetRecord();
    resetUpdate();

    if (editingEntry) {
      setEmployeeId(editingEntry.employeeId);
      setKind(editingEntry.kind);
      setAmount(String(editingEntry.amount));
      setOccurredOn(editingEntry.occurredOn);
      setProjectId(editingEntry.projectId ?? '');
      setHoursWorked(editingEntry.hoursWorked === null ? '' : String(editingEntry.hoursWorked));
      setNote(editingEntry.note ?? '');
    } else {
      setEmployeeId('');
      setKind('WorkerPaymentHourly');
      setAmount('');
      setOccurredOn('');
      setProjectId('');
      setHoursWorked('');
      setNote('');
    }
  }, [open, editingEntry, resetRecord, resetUpdate]);

  const isHourly = kind === 'WorkerPaymentHourly';

  const parsedAmount = Number(amount);
  const amountIsValid = amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount >= 0;

  const parsedHours = Number(hoursWorked);
  const hoursAreValid =
    !isHourly || (hoursWorked.trim() !== '' && !Number.isNaN(parsedHours) && parsedHours >= 0);

  const canSubmit =
    employeeId !== '' && amountIsValid && hoursAreValid && (!isEditing || occurredOn !== '');
  const mutation = isEditing ? update : record;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const input = {
      employeeId,
      kind,
      amount: parsedAmount,
      occurredOn: occurredOn || null,
      projectId: projectId || null,
      hoursWorked: isHourly ? parsedHours : null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingEntry.id, input: { ...input, occurredOn } },
        { onSuccess: onClose },
      );
    } else {
      record.mutate(input, { onSuccess: onClose });
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {isEditing ? t('financeEntries.editTitle') : t('financeEntries.add')}
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
              label={t('financeEntries.employee')}
              value={employeeId}
              onChange={(event) => setEmployeeId(event.target.value)}
            >
              {employees?.items.map((employee) => (
                <MenuItem key={employee.id} value={employee.id}>
                  {employee.firstName} {employee.lastName}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('financeEntries.kind')}
              value={kind}
              onChange={(event) => setKind(event.target.value as FinanceEntryKind)}
            >
              {financeEntryKinds.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('financeEntryKind', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('financeEntries.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              error={amount.trim() !== '' && !amountIsValid}
            />
          </Grid>

          {isHourly && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                type="number"
                fullWidth
                label={t('financeEntries.hoursWorked')}
                value={hoursWorked}
                onChange={(event) => setHoursWorked(event.target.value)}
                error={hoursWorked.trim() !== '' && !hoursAreValid}
                helperText={
                  hoursWorked.trim() === '' ? t('financeEntries.hourlyNeedsHours') : undefined
                }
              />
            </Grid>
          )}

          <Grid size={{ xs: 12, sm: isHourly ? 6 : 6 }}>
            <TextField
              type="date"
              fullWidth
              required={isEditing}
              label={t('financeEntries.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('financeEntries.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
            >
              <MenuItem value="">{t('financeEntries.noProject')}</MenuItem>
              {projects?.items.map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('financeEntries.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <>
              <Grid size={12}>
                <AttachmentList
                  ownerType="FinanceEntry"
                  ownerId={editingEntry.id}
                  categories={['Other']}
                  canUpload={canAdminister}
                  canDelete={canAdminister}
                />
              </Grid>
              {canAdminister && (
                <Grid size={12}>
                  <AuditHistoryCard entityName="FinanceEntry" entityId={editingEntry.id} />
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
