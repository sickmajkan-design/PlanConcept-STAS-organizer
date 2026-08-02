import { zodResolver } from '@hookform/resolvers/zod';
import { ApartmentOutlined, DeleteOutlined, EditOutlined, Inventory2Outlined } from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { useAdjustMaterial, useDeleteMaterial, useMaterialQuery } from '../../features/materials/useMaterials';
import { adjustMaterialSchema, type AdjustMaterialFormValues } from '../../features/materials/validation';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDateTime } from '../../utils/formatting';

export function MaterialDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();

  const { data: material, isLoading, isError, error, refetch } = useMaterialQuery(id);
  const adjust = useAdjustMaterial(id ?? '');
  const deleteMaterial = useDeleteMaterial();

  const [confirmDelete, setConfirmDelete] = useState(false);
  const [adjustOpen, setAdjustOpen] = useState(false);

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

  return (
    <Box sx={{ maxWidth: 900 }}>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={3}
            sx={{ alignItems: 'flex-start' }}
          >
            <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}>
              <Inventory2Outlined />
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {material.name}
              </Typography>
              <Typography variant="h4" color="primary" sx={{ fontWeight: 700, mt: 1 }}>
                {material.quantity} {material.unit}
              </Typography>
            </Box>
            <Stack direction="row" spacing={1}>
              <Button variant="contained" onClick={openAdjust}>
                Adjust stock
              </Button>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.materialEdit(material.id))}
              >
                Edit
              </Button>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => setConfirmDelete(true)}
              >
                Delete
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
                Stock
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('materials.warehouse')} value={material.warehouse} />
                <InfoRow label={t('materials.lastUpdated')} value={formatDateTime(material.lastUpdated)} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Project
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
                  Warehouse stock, not tied to a project.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={adjustOpen} onClose={() => setAdjustOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('materials.adjust')}</DialogTitle>
        <form onSubmit={handleSubmit(onAdjust)} noValidate>
          <DialogContent>
            <Stack spacing={2.5} sx={{ mt: 0.5 }}>
              <Typography variant="body2" color="text.secondary">
                Enter a positive number for received stock or a negative number for consumed
                stock. Current quantity: {material.quantity} {material.unit}.
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
              Cancel
            </Button>
            <Button type="submit" variant="contained" loading={adjust.isPending}>
              Apply
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <ConfirmDialog
        open={confirmDelete}
        title={t('materials.deleteTitle')}
        description={`${material.name} will be removed from active records.`}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteMaterial.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
}

function InfoRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value || '—'}</Typography>
    </Box>
  );
}
