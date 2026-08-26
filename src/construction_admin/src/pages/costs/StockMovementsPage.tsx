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
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import {
  useDeleteMaterialMovement,
  useMaterialMovementsQuery,
  useMaterialMovementsSummaryQuery,
  useRecordMaterialMovement,
  useUpdateMaterialMovement,
} from '../../features/costs/useCosts';
import { useAllMaterialsQuery } from '../../features/materials/useMaterials';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime, formatMoney, formatQuantity, lastYearRange } from '../../utils/formatting';

export function StockMovementsPage() {
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('occurredOn', 'desc');

  const [kind, setKind] = useState<MaterialMovementKind | ''>('');
  const [recording, setRecording] = useState(false);
  const [editing, setEditing] = useState<MaterialMovement | null>(null);

  const query: MaterialMovementListQuery = useMemo(
    () => ({
      ...list.query,
      // The API has no text search on this collection; leaving one in the key
      // would refetch on every keystroke for nothing.
      search: undefined,
      kind: kind || undefined,
    }),
    [kind, list.query],
  );

  const { data, isLoading, isError, error, refetch } = useMaterialMovementsQuery(query);
  const { data: summary } = useMaterialMovementsSummaryQuery(query);
  const remove = useDeleteWithConfirm<MaterialMovement>(useDeleteMaterialMovement());

  const columns: GridColDef<MaterialMovement>[] = useMemo(
    () => [
      {
        field: 'occurredOn',
        headerName: t('movements.occurredOn'),
        width: 120,
        valueGetter: (value) => formatDate(value),
      },
      {
        field: 'materialName',
        headerName: t('movements.material'),
        flex: 1,
        minWidth: 160,
      },
      {
        field: 'kind',
        headerName: t('movements.kind'),
        width: 150,
        valueGetter: (_value, row) => enumLabel('materialMovementKind', row.kind),
      },
      {
        field: 'quantity',
        headerName: t('movements.quantity'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (_value, row) =>
          `${formatQuantity(row.quantity, locale)} ${row.unit}`,
      },
      {
        field: 'unitPrice',
        headerName: t('movements.unitPrice'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (value) => formatMoney(value as number | null, locale),
      },
      {
        field: 'totalCost',
        headerName: t('movements.totalCost'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        sortable: false,
        valueGetter: (value) => formatMoney(value as number | null, locale),
      },
      {
        field: 'projectName',
        headerName: t('movements.project'),
        flex: 1,
        minWidth: 140,
        valueGetter: (value) => value || t('movements.noProject'),
      },
      {
        field: 'recordedByName',
        headerName: t('movements.recordedBy'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'createdAt',
        headerName: t('movements.createdAt'),
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
        title={t('movements.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('movements.add'),
          icon: <AddOutlined />,
          onClick: () => setRecording(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
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

      {summary && (
        <Paper variant="outlined" sx={{ px: 2, py: 1, mb: 2 }}>
          <Typography variant="body2" color="text.secondary">
            {t('movements.summaryValue')}:{' '}
            <strong>{formatMoney(summary.totalCost, locale)}</strong>
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

function MovementDialog({
  open,
  editingMovement,
  onClose,
  canAdminister,
}: {
  open: boolean;
  editingMovement?: MaterialMovement | null;
  onClose: () => void;
  canAdminister?: boolean;
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
    } else {
      setMaterialId('');
      setKind('In');
      setQuantity('');
      setUnitPrice('');
      setProjectId('');
      setOccurredOn('');
      setNote('');
    }
  }, [open, editingMovement, resetRecord, resetUpdate]);

  const isIssue = kind === 'Out';
  const isAdjustment = kind === 'Adjustment';
  const parsedQuantity = Number(quantity);
  const quantityIsValid =
    quantity.trim() !== '' &&
    !Number.isNaN(parsedQuantity) &&
    (isAdjustment ? parsedQuantity !== 0 : parsedQuantity > 0);

  const canSubmit =
    materialId !== '' && quantityIsValid && (!isIssue || projectId !== '')
    && (!isEditing || occurredOn !== '');
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
    };

    if (isEditing) {
      update.mutate(
        { id: editingMovement.id, input: { ...input, occurredOn } },
        { onSuccess: onClose },
      );
    } else {
      record.mutate(input, { onSuccess: onClose });
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

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('movements.material')}
              value={materialId}
              onChange={(event) => setMaterialId(event.target.value)}
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
          disabled={!canSubmit || mutation.isPending}
          onClick={submit}
        >
          {isEditing ? t('common.save') : t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
