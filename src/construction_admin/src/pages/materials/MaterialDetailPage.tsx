import { zodResolver } from '@hookform/resolvers/zod';
import {
  ApartmentOutlined,
  DeleteOutlined,
  EditOutlined,
  Inventory2Outlined,
  MoveToInboxOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Link as MuiLink,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { useAuth } from '../../auth/useAuth';
import { canSeeSpending } from '../../auth/authHelpers';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { useMaterialMovementsQuery } from '../../features/costs/useCosts';
import { useAdjustMaterial, useDeleteMaterial, useMaterialQuery } from '../../features/materials/useMaterials';
import { adjustMaterialSchema, type AdjustMaterialFormValues } from '../../features/materials/validation';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatDateTime, formatMoney, formatQuantity } from '../../utils/formatting';
import { MovementDialog } from '../costs/StockMovementsPage';

const HISTORY_ROWS = 15;

export function MaterialDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const canSeeHistory = canSeeSpending(user);

  const { locale } = useI18n();
  const { data: material, isLoading, isError, error, refetch } = useMaterialQuery(id);
  const adjust = useAdjustMaterial(id ?? '');
  const deleteMaterial = useDeleteMaterial();
  const history = useMaterialMovementsQuery({
    materialId: id,
    pageNumber: 1,
    pageSize: HISTORY_ROWS,
    sortBy: 'occurredOn',
    sortDescending: true,
  }, canSeeHistory);

  const [confirmDelete, setConfirmDelete] = useState(false);
  const [adjustOpen, setAdjustOpen] = useState(false);
  const [receiveOpen, setReceiveOpen] = useState(false);

  const { control, handleSubmit, reset, formState: { errors } } = useForm<AdjustMaterialFormValues>({
    resolver: zodResolver(adjustMaterialSchema),
    defaultValues: { change: '', reason: '' },
  });

  if (isLoading) return null;
  if (isError || !material) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleDelete = async () => {
    await deleteMaterial.mutateAsync(material.id);
    navigate(paths.materials, { replace: true });
  };

  const openAdjust = () => {
    reset({ change: '', reason: '' });
    setAdjustOpen(true);
  };

  const onAdjust = async (values: AdjustMaterialFormValues) => {
    try {
      await adjust.mutateAsync({ change: Number(values.change), reason: values.reason || null });
      setAdjustOpen(false);
    } catch {
      // Error surfaced in the dialog below.
    }
  };

  const movements = history.data?.items ?? [];

  return (
    <Box sx={{ maxWidth: 960 }}>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={3}
            sx={{ alignItems: { md: 'center' } }}
          >
            <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}>
              <Inventory2Outlined />
            </Avatar>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="h5" sx={{ fontWeight: 700, overflowWrap: 'anywhere' }}>
                {material.name}
              </Typography>
              <Typography variant="h4" color="primary" sx={{ fontWeight: 700, mt: 1 }}>
                {formatQuantity(material.quantity, locale)} {material.unit}
              </Typography>
            </Box>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              {canSeeHistory && (
                <Button
                  variant="contained"
                  startIcon={<MoveToInboxOutlined />}
                  onClick={() => setReceiveOpen(true)}
                >
                  {t('materials.receiveGoods')}
                </Button>
              )}
              <Button variant="outlined" onClick={openAdjust}>
                {t('materials.adjust')}
              </Button>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.materialEdit(material.id))}
              >
                {t('common.edit')}
              </Button>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => setConfirmDelete(true)}
              >
                {t('common.delete')}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                {t('materials.stockSection')}
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('materials.warehouse')} value={material.warehouse} />
                <InfoRow
                  label={t('materials.unitPrice')}
                  value={
                    material.unitPrice === null
                      ? null
                      : `${formatMoney(material.unitPrice, locale)} / ${material.unit}`
                  }
                  flagMissing
                />
                {material.unitPrice !== null && (
                  <InfoRow
                    label={t('materials.estimatedValue')}
                    value={formatMoney(material.unitPrice * material.quantity, locale)}
                  />
                )}
                <InfoRow label={t('materials.lastUpdated')} value={formatDateTime(material.lastUpdated)} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                {t('materials.projectSection')}
              </Typography>

              {material.projectId ? (
                <Stack
                  direction="row"
                  spacing={1.5}
                  sx={{ mt: 1, alignItems: 'center', cursor: 'pointer' }}
                  onClick={() => navigate(paths.projectDetail(material.projectId!))}
                >
                  <Avatar sx={{ bgcolor: 'action.selected' }}>
                    <ApartmentOutlined />
                  </Avatar>
                  <Typography>{material.projectName}</Typography>
                </Stack>
              ) : (
                <Typography color="text.secondary" sx={{ mt: 1 }}>
                  {t('materials.noProjectNote')}
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {canSeeHistory && (
        <Paper variant="outlined" sx={{ mt: 3 }}>
          <Stack
            direction="row"
            useFlexGap
            sx={{ p: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}
          >
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('materials.history')}
            </Typography>
            <MuiLink component={RouterLink} to={paths.stockMovements} variant="body2">
              {t('materials.allMovements')}
            </MuiLink>
          </Stack>

          {movements.length === 0 ? (
            <Typography color="text.secondary" sx={{ px: 2, pb: 3 }}>
              {t('materials.historyEmpty')}
            </Typography>
          ) : (
            <Box sx={{ overflowX: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{t('movements.occurredOn')}</TableCell>
                    <TableCell>{t('movements.kind')}</TableCell>
                    <TableCell align="right">{t('movements.quantity')}</TableCell>
                    <TableCell align="right">{t('movements.unitPrice')}</TableCell>
                    <TableCell align="right">{t('materials.historyValue')}</TableCell>
                    <TableCell>{t('movements.supplier')}</TableCell>
                    <TableCell>{t('movements.invoiceNumber')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {movements.map((movement) => (
                    <TableRow key={movement.id} hover>
                      <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(movement.occurredOn)}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          variant="outlined"
                          color={
                            movement.kind === 'In'
                              ? 'success'
                              : movement.kind === 'Out'
                                ? 'warning'
                                : 'default'
                          }
                          label={enumLabel('materialMovementKind', movement.kind)}
                        />
                      </TableCell>
                      <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                        {movement.kind === 'Out' ? '−' : movement.quantity > 0 ? '+' : ''}
                        {formatQuantity(movement.quantity, locale)}{' '}
                        {movement.unit}
                      </TableCell>
                      <TableCell align="right">{formatMoney(movement.unitPrice, locale)}</TableCell>
                      <TableCell align="right">{formatMoney(movement.totalCost, locale)}</TableCell>
                      <TableCell>{movement.supplier || '—'}</TableCell>
                      <TableCell>{movement.invoiceNumber || '—'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          )}
        </Paper>
      )}

      <MovementDialog
        open={receiveOpen}
        onClose={() => setReceiveOpen(false)}
        defaultMaterialId={material.id}
        defaultKind="In"
      />

      <Dialog open={adjustOpen} onClose={() => setAdjustOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('materials.adjustTitle')}</DialogTitle>
        <form onSubmit={handleSubmit(onAdjust)} noValidate>
          <DialogContent>
            <Stack spacing={2.5} sx={{ mt: 0.5 }}>
              <Typography variant="body2" color="text.secondary">
                {t('materials.adjustHint', {
                  quantity: formatQuantity(material.quantity, locale),
                  unit: material.unit,
                })}
              </Typography>
              <Controller
                name="change"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    label={t('materials.change')}
                    type="number"
                    fullWidth
                    autoFocus
                    error={!!fieldState.error}
                    helperText={fieldState.error?.message}
                  />
                )}
              />
              <Controller
                name="reason"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    label={t('materials.reasonOptional')}
                    fullWidth
                    multiline
                    minRows={2}
                    error={!!fieldState.error}
                    helperText={fieldState.error?.message}
                  />
                )}
              />
              {errors.root?.message && <Alert severity="error">{errors.root.message}</Alert>}
              {adjust.isError && <Alert severity="error">{toApiError(adjust.error).message}</Alert>}
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setAdjustOpen(false)} disabled={adjust.isPending}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="contained" loading={adjust.isPending}>
              {t('materials.adjustApply')}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <ConfirmDialog
        open={confirmDelete}
        title={t('materials.deleteTitle')}
        description={t('materials.deleteBody', { name: material.name })}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteMaterial.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
}

function InfoRow({
  label,
  value,
  flagMissing,
}: {
  label: string;
  value: string | null | undefined;
  /** Shows a warning-colored prompt instead of a plain dash when unset. */
  flagMissing?: boolean;
}) {
  const t = useT();
  const missing = !value && flagMissing;

  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1" color={missing ? 'warning.main' : undefined}>
        {value || (missing ? t('common.incomplete') : '—')}
      </Typography>
    </Box>
  );
}
