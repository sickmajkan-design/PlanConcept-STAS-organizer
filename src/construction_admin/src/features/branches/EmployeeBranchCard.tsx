import { DeleteOutlined, SwapHorizOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Card,
  CardContent,
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
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { EmployeeBranchPeriod } from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { BranchDot } from './BranchDot';
import { useBranchesQuery, useRemoveEmployeeBranchPeriod, useSetEmployeeBranch } from './useBranches';

const todayIso = () => new Date().toISOString().slice(0, 10);

/**
 * The business unit that employs a person, with the dated history behind it. Hours and pay are
 * attributed by the unit that covers the day, so moving someone here never rewrites the past:
 * the stretch they were in simply ends the day before.
 */
export function EmployeeBranchCard({
  employeeId,
  employmentDate,
  history,
}: {
  employeeId: string;
  /** `YYYY-MM-DD` — where a first assignment starts, so what was worked before is not left without a unit. */
  employmentDate: string;
  /** Most recent first, as the API returns it. */
  history: EmployeeBranchPeriod[];
}) {
  const t = useT();
  const { user } = useAuth();
  const canManage = canAdministerAccounts(user);
  const { data: branches } = useBranchesQuery();
  const move = useSetEmployeeBranch(employeeId);
  const removePeriod = useRemoveEmployeeBranchPeriod(employeeId);

  const current = history.find((period) => period.endDate === null);

  const [open, setOpen] = useState(false);
  const [branchId, setBranchId] = useState('');
  const [from, setFrom] = useState(todayIso());
  const [error, setError] = useState<string | null>(null);
  const [toRemove, setToRemove] = useState<EmployeeBranchPeriod | null>(null);

  const openDialog = () => {
    setBranchId(current?.branchId ?? '');
    // A first assignment covers everything they worked; a later move starts today.
    setFrom(history.length === 0 ? employmentDate.slice(0, 10) : todayIso());
    setError(null);
    setOpen(true);
  };

  const submit = async () => {
    try {
      await move.mutateAsync({ branchId: branchId || null, from });
      setOpen(false);
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('employees.branch.title')}
          </Typography>
          {canManage && (
            <Button size="small" variant="outlined" startIcon={<SwapHorizOutlined />} onClick={openDialog}>
              {current ? t('employees.branch.move') : t('employees.branch.set')}
            </Button>
          )}
        </Stack>

        {current ? (
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', mb: 1.5 }}>
            <BranchDot color={current.branchColor} />
            <Typography variant="body1" sx={{ fontWeight: 600 }}>
              {current.branchName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('employees.branch.since', { date: formatDate(current.startDate) })}
            </Typography>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            {t('employees.branch.none')}
          </Typography>
        )}

        {history.length > 0 && (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('branches.single')}</TableCell>
                <TableCell>{t('employees.branch.from')}</TableCell>
                <TableCell>{t('employees.branch.to')}</TableCell>
                {canManage && <TableCell />}
              </TableRow>
            </TableHead>
            <TableBody>
              {history.map((period) => (
                <TableRow key={period.id}>
                  <TableCell>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                      <BranchDot color={period.branchColor} />
                      <span>{period.branchName}</span>
                    </Stack>
                  </TableCell>
                  <TableCell>{formatDate(period.startDate)}</TableCell>
                  <TableCell>{period.endDate ? formatDate(period.endDate) : t('employees.branch.ongoing')}</TableCell>
                  {canManage && (
                    <TableCell align="right">
                      <Tooltip title={t('employees.branch.removePeriod')}>
                        <IconButton size="small" color="error" onClick={() => setToRemove(period)}>
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t('employees.branch.moveTitle')}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              select
              fullWidth
              label={t('branches.single')}
              value={branchId}
              onChange={(event) => setBranchId(event.target.value)}
              helperText={t('employees.branch.moveHint')}
            >
              <MenuItem value="">
                <em>{t('employees.branch.noUnit')}</em>
              </MenuItem>
              {(branches ?? [])
                .filter((b) => b.isActive || b.id === branchId)
                .map((branch) => (
                  <MenuItem key={branch.id} value={branch.id} sx={{ gap: 1 }}>
                    <BranchDot color={branch.color} />
                    {branch.name}
                  </MenuItem>
                ))}
            </TextField>
            <TextField
              type="date"
              fullWidth
              label={t('employees.branch.effective')}
              value={from}
              onChange={(event) => setFrom(event.target.value)}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: todayIso() } }}
              helperText={t('employees.branch.effectiveHint')}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{t('common.cancel')}</Button>
          <Button variant="contained" onClick={() => void submit()} disabled={move.isPending || !from}>
            {t('common.save')}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={!!toRemove}
        title={t('employees.branch.removeTitle')}
        description={t('employees.branch.removeDescription', {
          name: toRemove?.branchName ?? '',
          from: toRemove ? formatDate(toRemove.startDate) : '',
        })}
        confirmLabel={t('common.delete')}
        destructive
        onConfirm={async () => {
          if (toRemove) await removePeriod.mutateAsync(toRemove.id);
          setToRemove(null);
        }}
        onCancel={() => setToRemove(null)}
      />
    </Card>
  );
}
