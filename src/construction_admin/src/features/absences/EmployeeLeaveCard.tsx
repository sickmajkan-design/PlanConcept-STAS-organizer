import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { ChevronLeftOutlined, ChevronRightOutlined } from '@mui/icons-material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { canReviewAbsences } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import {
  useAbsenceBalanceQuery,
  useCreateLeaveAdjustment,
  useLeaveAdjustmentsQuery,
} from './useAbsences';

/**
 * A person's annual leave for one year, in working days: the year's right, what carried
 * over (and when that runs out), corrections with who made them, what was taken and what
 * is left. Management may write a correction; nobody edits one afterwards.
 */
export function EmployeeLeaveCard({ employeeId }: { employeeId: string }) {
  const t = useT();
  const { user } = useAuth();
  const [year, setYear] = useState(new Date().getFullYear());
  const [adjusting, setAdjusting] = useState(false);

  const balance = useAbsenceBalanceQuery(employeeId, year);
  const history = useLeaveAdjustmentsQuery(employeeId, year);
  const b = balance.data;

  const row = (label: string, value: string | number, hint?: string) => (
    <Stack direction="row" sx={{ justifyContent: 'space-between' }} spacing={2}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Tooltip title={hint ?? ''} disableHoverListener={!hint}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {value}
        </Typography>
      </Tooltip>
    </Stack>
  );

  return (
    <Card>
      <CardContent>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('leave.title')}
          </Typography>
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <IconButton size="small" aria-label={t('leave.previousYear')} onClick={() => setYear(year - 1)}>
              <ChevronLeftOutlined fontSize="small" />
            </IconButton>
            <Typography variant="body2" sx={{ fontWeight: 700, minWidth: 40, textAlign: 'center' }}>
              {year}
            </Typography>
            <IconButton size="small" aria-label={t('leave.nextYear')} onClick={() => setYear(year + 1)}>
              <ChevronRightOutlined fontSize="small" />
            </IconButton>
          </Stack>
        </Stack>

        {balance.isError && <Alert severity="error">{toApiError(balance.error).message}</Alert>}

        {b && (
          <Stack spacing={0.75}>
            {row(t('leave.entitlement'), b.entitlementDays, t('leave.entitlementHint'))}
            {row(t('leave.carriedOver'), b.carriedOverDays)}
            {b.carriedOverDays > 0 &&
              row(
                t('leave.carriedOverExpires', { date: formatDate(b.carryOverExpiresOn) }),
                b.carriedOverExpiredDays > 0
                  ? t('leave.expired', { count: b.carriedOverExpiredDays })
                  : t('leave.usedOfCarried', { used: b.carriedOverUsedDays, total: b.carriedOverDays }),
              )}
            {row(t('leave.corrections'), b.adjustmentDays > 0 ? `+${b.adjustmentDays}` : b.adjustmentDays)}
            {row(t('leave.taken'), b.usedDays, t('leave.takenHint'))}
            <Divider />
            <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
              <Typography sx={{ fontWeight: 700 }}>{t('leave.remaining')}</Typography>
              <Typography
                sx={{ fontWeight: 700 }}
                color={b.remainingDays < 0 ? 'error.main' : 'text.primary'}
              >
                {b.remainingDays}
              </Typography>
            </Stack>
            {b.carryingIntoNextYearDays > 0 &&
              row(t('leave.intoNextYear', { year: year + 1 }), b.carryingIntoNextYearDays)}
          </Stack>
        )}

        {history.data && history.data.length > 0 && (
          <Box sx={{ mt: 2 }}>
            <Typography variant="caption" color="text.secondary">
              {t('leave.history')}
            </Typography>
            <Stack spacing={0.5} sx={{ mt: 0.5 }}>
              {history.data.map((item) => (
                <Typography key={item.id} variant="body2">
                  <b>{item.days > 0 ? `+${item.days}` : item.days}</b> · {item.reason}
                  <Typography component="span" variant="caption" color="text.secondary">
                    {' '}
                    · {formatDate(item.createdAt)}
                    {item.createdBy ? ` · ${item.createdBy}` : ''}
                  </Typography>
                </Typography>
              ))}
            </Stack>
          </Box>
        )}

        {canReviewAbsences(user) && (
          <Box sx={{ mt: 2 }}>
            <Button size="small" variant="outlined" onClick={() => setAdjusting(true)}>
              {t('leave.correct')}
            </Button>
          </Box>
        )}
      </CardContent>

      <AdjustDialog
        open={adjusting}
        employeeId={employeeId}
        year={year}
        onClose={() => setAdjusting(false)}
      />
    </Card>
  );
}

function AdjustDialog({
  open,
  employeeId,
  year,
  onClose,
}: {
  open: boolean;
  employeeId: string;
  year: number;
  onClose: () => void;
}) {
  const t = useT();
  const create = useCreateLeaveAdjustment();
  const [days, setDays] = useState('');
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);

  const close = () => {
    setDays('');
    setReason('');
    setError(null);
    onClose();
  };

  const save = async () => {
    const value = Number(days);

    if (!Number.isInteger(value) || value === 0) {
      setError(t('leave.daysInvalid'));
      return;
    }

    if (!reason.trim()) {
      setError(t('leave.reasonRequired'));
      return;
    }

    try {
      await create.mutateAsync({ employeeId, year, days: value, reason: reason.trim() });
      close();
    } catch (err) {
      setError(toApiError(err).message);
    }
  };

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="xs">
      <DialogTitle>{t('leave.correctTitle', { year })}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t('leave.correctHelp')}
          </Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label={t('leave.days')}
            type="number"
            value={days}
            onChange={(event) => setDays(event.target.value)}
            helperText={t('leave.daysHelp')}
            fullWidth
            autoFocus
          />
          <TextField
            label={t('leave.reason')}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            multiline
            minRows={2}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={create.isPending} onClick={() => void save()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
