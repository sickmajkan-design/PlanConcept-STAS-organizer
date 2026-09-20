import { AddOutlined, SwapVertOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
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
import { useNavigate } from 'react-router-dom';

import type { MaterialMovementListQuery } from '../../api/costs';
import { toApiError } from '../../api/apiError';
import {
  materialMovementKinds,
  type MaterialMovement,
  type MaterialMovementKind,
} from '../../api/types';
import { exportsApi } from '../../api/exports';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { paths } from '../../routes/paths';
import { InvoiceFilePicker } from '../../components/InvoiceFilePicker';
import { useUploadAttachment } from '../../features/attachments/useAttachments';
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import { CostLedgerBoard, useLedgerWindow, type LedgerRow } from '../../components/costs/CostLedgerBoard';
import { Stat } from '../../components/costs/costUi';
import { ALL_TIME, LedgerPeriodBar, type LedgerPeriod } from '../../components/costs/LedgerPeriodBar';
import { SortBar } from '../../components/costs/SortBar';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import {
  useDeleteMaterialMovement,
  useMaterialMovementsQuery,
  useMaterialMovementsSummaryQuery,
  useMovementSuppliersQuery,
  useRecordMaterialMovement,
  useUpdateMaterialMovement,
} from '../../features/costs/useCosts';
import { useAllMaterialsQuery } from '../../features/materials/useMaterials';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { readLedgerDeepLinkPeriod, useOpenEntryFromLink } from '../../hooks/useLedgerDeepLink';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatMoney, formatQuantity, lastYearRange } from '../../utils/formatting';

interface StockMovementViewState {
  sortModel: GridSortModel;
  kind: MaterialMovementKind | '';
}

export function StockMovementsPage() {
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<MaterialMovementKind | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<MaterialMovement | null>(null);

  const savedViews = useSavedViews<StockMovementViewState>('stock-movements');

  const applyView = (state: StockMovementViewState) => {
    list.setSortModel(state.sortModel);
    setKind(state.kind);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, { sortModel: list.sortModel, kind });
  };

  const [period, setPeriod] = useState<LedgerPeriod>(() => readLedgerDeepLinkPeriod() ?? ALL_TIME);
  const window = useLedgerWindow();

  const query: MaterialMovementListQuery = useMemo(
    () => ({
      ...list.query,
      pageNumber: 1,
      pageSize: window.pageSize,
      from: period.from || undefined,
      to: period.to || undefined,
      // The API has no text search on this collection; leaving one in the key
      // would refetch on every keystroke for nothing.
      search: undefined,
      kind: kind || undefined,
    }),
    [kind, list.query, period, window.pageSize],
  );

  const { data, isLoading, isError, error, refetch } = useMaterialMovementsQuery(query);
  useOpenEntryFromLink(data?.items, (item) => setEditing(item));
  const { data: summary } = useMaterialMovementsSummaryQuery(query);
  const remove = useDeleteWithConfirm<MaterialMovement>(useDeleteMaterialMovement());

  const rows: LedgerRow<MaterialMovement>[] = useMemo(
    () =>
      (data?.items ?? []).map((movement) => ({
        item: movement,
        id: movement.id,
        date: movement.occurredOn,
        icon: <SwapVertOutlined fontSize="small" />,
        title: movement.materialName,
        chips: <Chip size="small" variant="outlined" label={enumLabel('materialMovementKind', movement.kind)} />,
        subtitle: [
          formatDate(movement.occurredOn),
          movement.projectName,
          movement.supplier,
          movement.invoiceNumber,
          movement.note,
        ]
          .filter(Boolean)
          .join(' · '),
        amount: movement.totalCost,
        meta:
          movement.unitPrice !== null
            ? `${formatQuantity(movement.quantity, locale)} ${movement.unit} × ${formatMoney(movement.unitPrice, locale)}`
            : `${formatQuantity(movement.quantity, locale)} ${movement.unit}`,
      })),
    [data, enumLabel, locale],
  );

  return (
    <Box>
      <PageHeader
        title={t('movements.title')}
        description={t('movements.description')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('movements.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2 }}>
        <Button variant="outlined" onClick={() => navigate(paths.materialDeliveryImport)}>
          {t('deliveryImport.title')}
        </Button>
        <TextField
          select
          size="small"
          label={t('movements.kind')}
          value={kind}
          onChange={(event) => {
            setKind(event.target.value as MaterialMovementKind | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">{t('movements.allKinds')}</MenuItem>
          {materialMovementKinds.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('materialMovementKind', value)}
            </MenuItem>
          ))}
        </TextField>

        {/* Exports the last year rather than the page on screen: a spreadsheet
            of twenty rows is not what anyone opens this for. */}
        <ExportButton
          onExport={(language) =>
            exportsApi.materialMovements({ ...lastYearRange(), language })
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
          <Stat label={t('movements.summaryValue')} value={formatMoney(summary?.totalCost ?? 0, locale)} />
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
            { value: 'occurredOn', label: t('movements.occurredOn') },
            { value: 'materialName', label: t('movements.material') },
            { value: 'kind', label: t('movements.kind') },
            { value: 'quantity', label: t('movements.quantity') },
            { value: 'unitPrice', label: t('movements.unitPrice') },
            { value: 'supplier', label: t('movements.supplier') },
            { value: 'invoiceNumber', label: t('movements.invoiceNumber') },
            { value: 'totalCost', label: t('movements.totalCost') },
            { value: 'projectName', label: t('movements.project') },
            { value: 'recordedByName', label: t('movements.recordedBy') },
            { value: 'createdAt', label: t('movements.createdAt') },
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

      <MovementDialog open={recording} onClose={() => setRecording(false)} />
      <MovementDialog
        open={!!editing}
        editingMovement={editing}
        onClose={() => setEditing(null)}
        canAdminister={canAdministerAccounts(user)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('movements.deleteTitle')}
        description={t('movements.deleteBody')}
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

export function MovementDialog({
  open,
  editingMovement,
  onClose,
  canAdminister,
  defaultMaterialId,
  defaultKind = 'In',
}: {
  open: boolean;
  editingMovement?: MaterialMovement | null;
  onClose: () => void;
  canAdminister?: boolean;
  /** Opened from a material's own page: that material is already chosen. */
  defaultMaterialId?: string;
  defaultKind?: MaterialMovementKind;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: materials } = useAllMaterialsQuery();
  const { data: projects } = useAllProjectsQuery();
  const record = useRecordMaterialMovement();
  const update = useUpdateMaterialMovement();
  const isEditing = !!editingMovement;

  const [materialId, setMaterialId] = useState('');
  const [kind, setKind] = useState<MaterialMovementKind>('In');
  const [quantity, setQuantity] = useState('');
  const [unitPrice, setUnitPrice] = useState('');
  const [projectId, setProjectId] = useState('');
  const [occurredOn, setOccurredOn] = useState('');
  const [note, setNote] = useState('');
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [supplier, setSupplier] = useState('');
  const [invoiceFile, setInvoiceFile] = useState<File | null>(null);
  // Set when the delivery was saved but its invoice file was not: the form must
  // not be submitted again, or the delivery would be recorded twice.
  const [uploadProblem, setUploadProblem] = useState<string | null>(null);
  const uploadAttachment = useUploadAttachment();
  const { data: knownSuppliers } = useMovementSuppliersQuery(open);

  const resetRecord = record.reset;
  const resetUpdate = update.reset;

  // Reopening with the last entry still in the fields is how a delivery gets
  // recorded twice.
  useEffect(() => {
    if (!open) return;

    resetRecord();
    resetUpdate();

    if (editingMovement) {
      setMaterialId(editingMovement.materialId);
      setKind(editingMovement.kind);
      setQuantity(String(editingMovement.quantity));
      setUnitPrice(editingMovement.unitPrice === null ? '' : String(editingMovement.unitPrice));
      setProjectId(editingMovement.projectId ?? '');
      setOccurredOn(editingMovement.occurredOn);
      setNote(editingMovement.note ?? '');
      setInvoiceNumber(editingMovement.invoiceNumber ?? '');
      setSupplier(editingMovement.supplier ?? '');
    } else {
      setMaterialId(defaultMaterialId ?? '');
      setKind(defaultKind);
      setQuantity('');
      setUnitPrice('');
      setProjectId('');
      setOccurredOn('');
      setNote('');
      setInvoiceNumber('');
      setSupplier('');
    }

    setInvoiceFile(null);
    setUploadProblem(null);
  }, [open, editingMovement, defaultMaterialId, defaultKind, resetRecord, resetUpdate]);

  const isDelivery = kind === 'In';
  const isIssue = kind === 'Out';
  const isAdjustment = kind === 'Adjustment';
  const parsedQuantity = Number(quantity);
  const quantityIsValid =
    quantity.trim() !== '' &&
    !Number.isNaN(parsedQuantity) &&
    (isAdjustment ? parsedQuantity !== 0 : parsedQuantity > 0);

  const canSubmit =
    materialId !== '' && quantityIsValid && (!isIssue || projectId !== '')
    && (!isEditing || occurredOn !== '')
    && (!isDelivery || invoiceNumber.trim() !== '');
  const mutation = isEditing ? update : record;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const input = {
      materialId,
      kind,
      quantity: parsedQuantity,
      unitPrice: unitPrice.trim() === '' ? null : Number(unitPrice),
      projectId: projectId || null,
      occurredOn: occurredOn || null,
      note: note.trim() || null,
      invoiceNumber: invoiceNumber.trim() || null,
      supplier: isDelivery ? supplier.trim() || null : null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingMovement.id, input: { ...input, occurredOn } },
        { onSuccess: onClose },
      );
    } else {
      record.mutate(input, {
        onSuccess: async (saved) => {
          if (invoiceFile) {
            try {
              await uploadAttachment.mutateAsync({
                ownerType: 'MaterialMovement',
                ownerId: saved.id,
                category: 'Other',
                file: invoiceFile,
              });
            } catch (err) {
              setUploadProblem(toApiError(err).message);
              return;
            }
          }

          onClose();
        },
      });
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isEditing ? t('movements.editTitle') : t('movements.add')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        {uploadProblem && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t('invoiceFile.uploadFailed', { reason: uploadProblem })}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('movements.material')}
              value={materialId}
              onChange={(event) => {
                const nextId = event.target.value;
                setMaterialId(nextId);

                // A convenience, not a rule: only nudges a still-empty price
                // on a brand new delivery, so it never overwrites a number
                // the admin already typed or a value already on record.
                if (!isEditing && kind === 'In' && unitPrice.trim() === '') {
                  const selected = materials?.items.find((m) => m.id === nextId);
                  if (selected?.unitPrice != null) {
                    setUnitPrice(String(selected.unitPrice));
                  }
                }
              }}
            >
              {materials?.items.map((material) => (
                <MenuItem key={material.id} value={material.id}>
                  {material.name} ({material.unit})
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('movements.kind')}
              value={kind}
              onChange={(event) =>
                setKind(event.target.value as MaterialMovementKind)
              }
            >
              {materialMovementKinds.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('materialMovementKind', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={t('movements.quantity')}
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
              error={quantity.trim() !== '' && !quantityIsValid}
            />
          </Grid>

          {/* A price only means something on a delivery. On an issue the
              system works out the average, and on a correction there is
              nothing to price. */}
          {kind === 'In' && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                type="number"
                fullWidth
                label={t('movements.unitPrice')}
                value={unitPrice}
                onChange={(event) => setUnitPrice(event.target.value)}
              />
            </Grid>
          )}

          {/* The invoice is the paper trail back to what was actually paid;
              only a delivery has one of its own. */}
          {isDelivery && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                fullWidth
                required
                label={t('movements.invoiceNumber')}
                value={invoiceNumber}
                onChange={(event) => setInvoiceNumber(event.target.value)}
                error={invoiceNumber.trim() === ''}
                helperText={invoiceNumber.trim() === '' ? t('movements.needsInvoiceNumber') : undefined}
              />
            </Grid>
          )}

          {isDelivery && (
            <Grid size={12}>
              <Autocomplete
                freeSolo
                options={knownSuppliers ?? []}
                inputValue={supplier}
                onInputChange={(_event, value) => setSupplier(value)}
                renderInput={(params) => <TextField {...params} label={t('movements.supplier')} />}
              />
            </Grid>
          )}

          {isDelivery && !isEditing && (
            <Grid size={12}>
              <InvoiceFilePicker file={invoiceFile} onChange={setInvoiceFile} />
            </Grid>
          )}

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              required={isEditing}
              label={t('movements.occurredOn')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              select
              fullWidth
              required={isIssue}
              label={t('movements.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
              error={isIssue && projectId === ''}
              helperText={isIssue && projectId === '' ? t('movements.needsProject') : undefined}
            >
              <MenuItem value="">{t('movements.noProject')}</MenuItem>
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
              multiline
              minRows={2}
              label={t('movements.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isIssue && (
            <Grid size={12}>
              <Alert severity="info">{t('movements.issueHint')}</Alert>
            </Grid>
          )}
          {isAdjustment && (
            <Grid size={12}>
              <Alert severity="info">{t('movements.adjustmentHint')}</Alert>
            </Grid>
          )}

          {isEditing && (
            <>
              <Grid size={12}>
                <AttachmentList
                  ownerType="MaterialMovement"
                  ownerId={editingMovement.id}
                  categories={['Other']}
                  canUpload={canAdminister}
                  canDelete={canAdminister}
                />
              </Grid>
              {canAdminister && (
                <Grid size={12}>
                  <AuditHistoryCard entityName="MaterialMovement" entityId={editingMovement.id} />
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
          disabled={!canSubmit || mutation.isPending || uploadAttachment.isPending || !!uploadProblem}
          onClick={submit}
        >
          {isEditing ? t('common.save') : t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
