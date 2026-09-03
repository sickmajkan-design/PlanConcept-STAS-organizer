import {
  ApartmentOutlined,
  DeleteOutlined,
  EditOutlined,
  HomeWorkOutlined,
} from '@mui/icons-material';
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
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { AccommodationRate } from '../../api/types';
import { AttachmentList } from '../../components/AttachmentList';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useAccommodationQuery, useDeleteAccommodation } from '../../features/accommodations/useAccommodations';
import {
  useAccommodationRatesQuery,
  useDeleteAccommodationRate,
  useSetAccommodationRate,
  useUpdateAccommodationRate,
} from '../../features/costs/useCosts';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

export function AccommodationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();

  const { data: accommodation, isLoading, isError, error, refetch } = useAccommodationQuery(id);
  const deleteAccommodation = useDeleteAccommodation();
  const [confirmDelete, setConfirmDelete] = useState(false);

  if (isLoading) return null;
  if (isError || !accommodation) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleDelete = async () => {
    await deleteAccommodation.mutateAsync(accommodation.id);
    navigate(paths.accommodations, { replace: true });
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
              <HomeWorkOutlined />
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {accommodation.address}
              </Typography>
              {accommodation.currentMonthlyAmount !== null && (
                <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                  {t('accommodations.currentMonthlyAmount')}: {' '}
                  {formatMoney(accommodation.currentMonthlyAmount, locale)}
                  {accommodation.currentProvider ? ` · ${accommodation.currentProvider}` : ''}
                </Typography>
              )}
              {accommodation.note && (
                <Typography color="text.secondary" sx={{ mt: 1 }}>
                  {accommodation.note}
                </Typography>
              )}
            </Box>
            <Stack direction="row" spacing={1}>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.accommodationEdit(accommodation.id))}
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
        <Grid size={12}>
          <AccommodationRateCard accommodationId={accommodation.id} />
        </Grid>

        <Grid size={12}>
          <Card>
            <CardContent>
              <AttachmentList
                ownerType="Accommodation"
                ownerId={accommodation.id}
                categories={['Photo', 'Other']}
                canDelete={canAdministerAccounts(user)}
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <ConfirmDialog
        open={confirmDelete}
        title={t('accommodations.deleteTitle')}
        description={t('accommodations.deleteBody', { name: accommodation.address })}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteAccommodation.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
}

/** The rent history for this accommodation, add a new rate to close off the one in force. */
function AccommodationRateCard({ accommodationId }: { accommodationId: string }) {
  const t = useT();
  const { locale } = useI18n();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<AccommodationRate | null>(null);

  const query = useMemo(
    () => ({
      accommodationId,
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'startDate',
      sortDescending: true,
    }),
    [accommodationId],
  );

  const { data } = useAccommodationRatesQuery(query);
  const remove = useDeleteWithConfirm<AccommodationRate>(useDeleteAccommodationRate());
  const rows = data?.items ?? [];

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('accommodations.rateHistory')}
          </Typography>
          <Button size="small" startIcon={<ApartmentOutlined />} onClick={() => setAdding(true)}>
            {t('accommodations.addRate')}
          </Button>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('accommodations.noRatesSentence')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('accommodations.provider')}</TableCell>
                  <TableCell align="right">{t('accommodations.monthlyAmount')}</TableCell>
                  <TableCell>{t('accommodations.startDate')}</TableCell>
                  <TableCell>{t('accommodations.endDate')}</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow
                    key={row.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onDoubleClick={() => setEditing(row)}
                  >
                    <TableCell>{row.provider || '—'}</TableCell>
                    <TableCell align="right">{formatMoney(row.monthlyAmount, locale)}</TableCell>
                    <TableCell>{formatDate(row.startDate)}</TableCell>
                    <TableCell>
                      {row.endDate ? formatDate(row.endDate) : t('rates.open')}
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title={t('common.delete')}>
                        <IconButton
                          size="small"
                          onClick={(event) => {
                            event.stopPropagation();
                            remove.request(row);
                          }}
                        >
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      <AccommodationRateDialog
        open={adding}
        accommodationId={accommodationId}
        onClose={() => setAdding(false)}
      />
      <AccommodationRateDialog
        open={!!editing}
        accommodationId={accommodationId}
        editingRate={editing}
        onClose={() => setEditing(null)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('accommodations.deleteRateTitle')}
        description={t('accommodations.deleteRateBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Card>
  );
}

function AccommodationRateDialog({
  open,
  accommodationId,
  editingRate,
  onClose,
}: {
  open: boolean;
  accommodationId: string;
  editingRate?: AccommodationRate | null;
  onClose: () => void;
}) {
  const t = useT();
  const set = useSetAccommodationRate();
  const update = useUpdateAccommodationRate();
  const isEditing = !!editingRate;

  const [monthlyAmount, setMonthlyAmount] = useState('');
  const [provider, setProvider] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [note, setNote] = useState('');

  const resetSet = set.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetSet();
    resetUpdate();

    if (editingRate) {
      setMonthlyAmount(String(editingRate.monthlyAmount));
      setProvider(editingRate.provider ?? '');
      setStartDate(editingRate.startDate);
      setEndDate(editingRate.endDate ?? '');
      setNote(editingRate.note ?? '');
    } else {
      setMonthlyAmount('');
      setProvider('');
      setStartDate('');
      setEndDate('');
      setNote('');
    }
  }, [open, editingRate, resetSet, resetUpdate]);

  const parsedAmount = Number(monthlyAmount);
  const amountIsValid = monthlyAmount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount > 0;
  const datesAreValid = !startDate || !endDate || endDate >= startDate;
  const canSubmit = amountIsValid && datesAreValid;

  const mutation = isEditing ? update : set;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const shared = {
      accommodationId,
      monthlyAmount: parsedAmount,
      provider: provider.trim() || null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingRate.id, input: { ...shared, startDate, endDate: endDate || null } },
        { onSuccess: onClose },
      );
    } else {
      set.mutate(
        { ...shared, startDate: startDate || null, endDate: endDate || null },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {isEditing ? t('accommodations.editRateTitle') : t('accommodations.addRate')}
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
              type="number"
              fullWidth
              label={t('accommodations.monthlyAmount')}
              value={monthlyAmount}
              onChange={(event) => setMonthlyAmount(event.target.value)}
              error={monthlyAmount.trim() !== '' && !amountIsValid}
              helperText={
                monthlyAmount.trim() !== '' && !amountIsValid
                  ? t('accommodations.mustBePositive')
                  : undefined
              }
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              label={t('accommodations.provider')}
              value={provider}
              onChange={(event) => setProvider(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('accommodations.startDate')}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('accommodations.endDate')}
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              error={!datesAreValid}
              helperText={!datesAreValid ? t('rates.endsBeforeStart') : undefined}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('rates.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <Grid size={12}>
              <AttachmentList
                ownerType="AccommodationRate"
                ownerId={editingRate.id}
                categories={['Contract', 'Other']}
              />
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit}
          loading={mutation.isPending}
          onClick={submit}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
