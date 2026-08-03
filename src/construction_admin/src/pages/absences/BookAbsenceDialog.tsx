import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Grid,
  MenuItem,
  Switch,
  TextField,
} from '@mui/material';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';

import { toApiError } from '../../api/apiError';
import { absenceTypes } from '../../api/types';
import { useBookAbsence } from '../../features/absences/useAbsences';
import {
  absenceFormSchema,
  type AbsenceFormValues,
} from '../../features/absences/validation';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';

const emptyValues: AbsenceFormValues = {
  employeeId: '',
  type: 'AnnualLeave',
  startDate: '',
  endDate: '',
  reason: '',
  approve: false,
};

export function BookAbsenceDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: allEmployees } = useAllEmployeesQuery();
  const book = useBookAbsence();

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AbsenceFormValues>({
    resolver: zodResolver(absenceFormSchema),
    defaultValues: emptyValues,
  });

  // Reopening a dialog that kept the last entry would quietly book leave for
  // whoever was picked before, and leave the previous failure on screen.
  const clearBookingError = book.reset;

  useEffect(() => {
    if (open) {
      reset(emptyValues);
      clearBookingError();
    }
  }, [clearBookingError, open, reset]);

  const submit = handleSubmit((values) => {
    book.mutate(
      {
        employeeId: values.employeeId,
        type: values.type,
        startDate: values.startDate,
        endDate: values.endDate,
        reason: values.reason || null,
        approve: values.approve,
      },
      { onSuccess: onClose },
    );
  });

  const error = book.isError ? toApiError(book.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('absences.bookTitle')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <Controller
              name="employeeId"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  select
                  fullWidth
                  label={t('absences.employee')}
                  error={!!errors.employeeId}
                  helperText={errors.employeeId?.message}
                >
                  {allEmployees?.items.map((employee) => (
                    <MenuItem key={employee.id} value={employee.id}>
                      {employee.firstName} {employee.lastName}
                    </MenuItem>
                  ))}
                </TextField>
              )}
            />
          </Grid>

          <Grid size={12}>
            <Controller
              name="type"
              control={control}
              render={({ field }) => (
                <TextField {...field} select fullWidth label={t('absences.type')}>
                  {absenceTypes.map((value) => (
                    <MenuItem key={value} value={value}>
                      {enumLabel('absenceType', value)}
                    </MenuItem>
                  ))}
                </TextField>
              )}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Controller
              name="startDate"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  type="date"
                  fullWidth
                  label={t('absences.startDate')}
                  slotProps={{ inputLabel: { shrink: true } }}
                  error={!!errors.startDate}
                  helperText={errors.startDate?.message}
                />
              )}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Controller
              name="endDate"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  type="date"
                  fullWidth
                  label={t('absences.endDate')}
                  slotProps={{ inputLabel: { shrink: true } }}
                  error={!!errors.endDate}
                  helperText={errors.endDate?.message}
                />
              )}
            />
          </Grid>

          <Grid size={12}>
            <Controller
              name="reason"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  multiline
                  minRows={2}
                  label={t('absences.reason')}
                  error={!!errors.reason}
                  helperText={errors.reason?.message}
                />
              )}
            />
          </Grid>

          <Grid size={12}>
            <Controller
              name="approve"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  control={
                    <Switch
                      checked={field.value}
                      onChange={(event) => field.onChange(event.target.checked)}
                    />
                  }
                  label={t('absences.approveNow')}
                />
              )}
            />
            {/* Says what the switch does, and warns that it will not work on
                your own leave before the API refuses it. */}
            <Alert severity="info" sx={{ mt: 1 }}>
              {t('absences.approveNowHint')} {t('absences.ownLeaveHint')}
            </Alert>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={book.isPending} onClick={() => void submit()}>
          {t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
