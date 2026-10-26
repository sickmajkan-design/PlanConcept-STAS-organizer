import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
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
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import {
  vehicleFineBearers,
  vehicleFineStatuses,
  type VehicleFine,
  type VehicleFineBearer,
  type VehicleFineStatus,
} from '../../api/vehicleFines';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useI18n, useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';
import { dateOnlyOffset, formatDate, formatMoney } from '../../utils/formatting';
import { useAllEmployeesQuery } from '../employees/useEmployees';
import { useAddVehicleFine, useDeleteVehicleFine, useUpdateVehicleFine, useVehicleFinesQuery } from './useVehicleFines';

/** The fines a vehicle has collected: what for, how much, by when, who drove and who bears it. */
export function VehicleFinesCard({ vehicleId, holderId }: { vehicleId: string; holderId: string | null }) {
  const t = useT();
  const { locale } = useI18n();
  const { data } = useVehicleFinesQuery(vehicleId);
  const [editing, setEditing] = useState<VehicleFine | 'new' | null>(null);
  const [removing, setRemoving] = useState<VehicleFine | null>(null);
  const remove = useDeleteVehicleFine(vehicleId);
  const rows = data ?? [];

  const open = rows.filter((f) => f.status === 'Open');
  const openTotal = open.reduce((sum, f) => sum + f.amount, 0);

  return (
    <Card>
      <CardContent>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('vehicleFines.title')}
            </Typography>
            {open.length > 0 && (
              <Typography variant="caption" color="text.secondary">
                {t('vehicleFines.openSummary', { count: open.length, amount: formatMoney(openTotal, locale) })}
              </Typography>
            )}
          </Box>
          <Button size="small" startIcon={<AddOutlined />} onClick={() => setEditing('new')}>
            {t('vehicleFines.add')}
          </Button>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('vehicleFines.empty')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('vehicleFines.occurredOn')}</TableCell>
                  <TableCell>{t('vehicleFines.description')}</TableCell>
                  <TableCell align="right">{t('vehicleFines.amount')}</TableCell>
                  <TableCell>{t('vehicleFines.dueDate')}</TableCell>
                  <TableCell>{t('vehicleFines.driver')}</TableCell>
                  <TableCell>{t('vehicleFines.borneBy')}</TableCell>
                  <TableCell>{t('vehicleFines.status')}</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((fine) => (
                  <TableRow key={fine.id} hover>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(fine.occurredOn)}</TableCell>
                    <TableCell>{fine.description}</TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      {formatMoney(fine.amount, locale)}
                    </TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{fine.dueDate ? formatDate(fine.dueDate) : '—'}</TableCell>
                    <TableCell>{fine.driverName ?? t('vehicleFines.unknownDriver')}</TableCell>
                    <TableCell>
                      <Chip size="small" variant="outlined" color={fine.borneBy === 'Driver' ? 'warning' : 'default'} label={t(`vehicleFines.bearer.${fine.borneBy}` as MessageKey)} />
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        color={fine.overdue ? 'error' : fine.status === 'Open' ? 'warning' : fine.status === 'Paid' ? 'success' : 'default'}
                        variant={fine.status === 'Contested' ? 'outlined' : 'filled'}
                        label={fine.overdue ? t('vehicleFines.overdue') : t(`vehicleFines.state.${fine.status}` as MessageKey)}
                      />
                      {fine.status === 'Paid' && fine.paidOn && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {formatDate(fine.paidOn)}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      <Tooltip title={t('common.edit')}>
                        <IconButton size="small" onClick={() => setEditing(fine)}>
                          <EditOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={t('common.delete')}>
                        <IconButton size="small" onClick={() => setRemoving(fine)}>
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

      {editing && (
        <FineDialog vehicleId={vehicleId} holderId={holderId} fine={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />
      )}

      <ConfirmDialog
        open={!!removing}
        title={t('vehicleFines.deleteTitle')}
        description={removing ? t('vehicleFines.deleteBody', { name: removing.description }) : ''}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isPending}
        onConfirm={async () => {
          if (removing) await remove.mutateAsync(removing.id);
          setRemoving(null);
        }}
        onCancel={() => setRemoving(null)}
      />
    </Card>
  );
}

function FineDialog({
  vehicleId,
  holderId,
  fine,
  onClose,
}: {
  vehicleId: string;
  holderId: string | null;
  fine: VehicleFine | null;
  onClose: () => void;
}) {
  const t = useT();
  const add = useAddVehicleFine(vehicleId);
  const update = useUpdateVehicleFine(vehicleId);
  const { data: employees } = useAllEmployeesQuery();
  const isEdit = !!fine;

  const [occurredOn, setOccurredOn] = useState(fine?.occurredOn ?? dateOnlyOffset(0));
  const [description, setDescription] = useState(fine?.description ?? '');
  const [amount, setAmount] = useState(fine ? String(fine.amount) : '');
  const [dueDate, setDueDate] = useState(fine?.dueDate ?? '');
  // The person who has the vehicle now is the likeliest driver; the office can change it.
  const [driverId, setDriverId] = useState(fine ? (fine.driverEmployeeId ?? '') : (holderId ?? ''));
  const [borneBy, setBorneBy] = useState<VehicleFineBearer>(fine?.borneBy ?? 'Firm');
  const [status, setStatus] = useState<VehicleFineStatus>(fine?.status ?? 'Open');
  const [paidOn, setPaidOn] = useState(fine?.paidOn ?? '');
  const [note, setNote] = useState(fine?.note ?? '');

  const mutation = isEdit ? update : add;
  const error = mutation.isError ? toApiError(mutation.error) : null;
  const amountNumber = Number(amount.replace(',', '.'));
  const needsDriver = borneBy === 'Driver' && !driverId;
  const datesOk = !dueDate || dueDate >= occurredOn;
  const valid = description.trim() !== '' && amount !== '' && Number.isFinite(amountNumber) && amountNumber >= 0 && datesOk && !needsDriver;

  const submit = () => {
    const input = {
      occurredOn,
      description: description.trim(),
      amount: amountNumber,
      dueDate: dueDate || null,
      driverEmployeeId: driverId || null,
      borneBy,
      status,
      paidOn: status === 'Paid' ? paidOn || null : null,
      note: note.trim() || null,
    };

    if (fine) update.mutate({ id: fine.id, input }, { onSuccess: onClose });
    else add.mutate(input, { onSuccess: onClose });
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isEdit ? t('vehicleFines.editTitle') : t('vehicleFines.addTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error.message}</Alert>}

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField type="date" label={t('vehicleFines.occurredOn')} value={occurredOn} onChange={(e) => setOccurredOn(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} fullWidth />
            <TextField type="date" label={t('vehicleFines.dueDate')} value={dueDate} onChange={(e) => setDueDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} error={!datesOk} helperText={!datesOk ? t('vehicleFines.dueBeforeOffence') : undefined} fullWidth />
          </Stack>

          <TextField label={t('vehicleFines.description')} value={description} onChange={(e) => setDescription(e.target.value)} fullWidth autoFocus />
          <TextField label={t('vehicleFines.amount')} value={amount} onChange={(e) => setAmount(e.target.value)} slotProps={{ htmlInput: { inputMode: 'decimal' } }} fullWidth />

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField select label={t('vehicleFines.driver')} value={driverId} onChange={(e) => setDriverId(e.target.value)} fullWidth slotProps={{ select: { displayEmpty: true } }}>
              <MenuItem value="">{t('vehicleFines.unknownDriver')}</MenuItem>
              {(employees?.items ?? []).map((e) => (
                <MenuItem key={e.id} value={e.id}>
                  {e.fullName}
                </MenuItem>
              ))}
            </TextField>
            <TextField select label={t('vehicleFines.borneBy')} value={borneBy} onChange={(e) => setBorneBy(e.target.value as VehicleFineBearer)} fullWidth>
              {vehicleFineBearers.map((b) => (
                <MenuItem key={b} value={b}>
                  {t(`vehicleFines.bearer.${b}` as MessageKey)}
                </MenuItem>
              ))}
            </TextField>
          </Stack>
          <Typography variant="caption" color={needsDriver ? 'error' : 'text.secondary'}>
            {needsDriver ? t('vehicleFines.needsDriver') : borneBy === 'Driver' ? t('vehicleFines.driverHint') : t('vehicleFines.firmHint')}
          </Typography>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField select label={t('vehicleFines.status')} value={status} onChange={(e) => setStatus(e.target.value as VehicleFineStatus)} fullWidth>
              {vehicleFineStatuses.map((s) => (
                <MenuItem key={s} value={s}>
                  {t(`vehicleFines.state.${s}` as MessageKey)}
                </MenuItem>
              ))}
            </TextField>
            {status === 'Paid' && (
              <TextField type="date" label={t('vehicleFines.paidOn')} value={paidOn} onChange={(e) => setPaidOn(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} fullWidth />
            )}
          </Stack>

          <TextField label={t('vehicleFines.note')} value={note} onChange={(e) => setNote(e.target.value)} multiline minRows={2} fullWidth />
          <Typography variant="caption" color="text.secondary">
            {t('vehicleFines.notifyHint')}
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid} loading={mutation.isPending} onClick={submit}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
