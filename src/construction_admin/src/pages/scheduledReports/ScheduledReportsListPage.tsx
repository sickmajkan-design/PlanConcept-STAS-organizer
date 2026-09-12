import { AddOutlined, DeleteOutlined, ScheduleSendOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import {
  scheduledReportCadences,
  scheduledReportTypes,
  weekDays,
  type ScheduledReportCadence,
  type ScheduledReportSubscription,
  type ScheduledReportSubscriptionInput,
  type ScheduledReportType,
  type WeekDay,
} from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import {
  useCreateScheduledReport,
  useDeleteScheduledReport,
  useScheduledReportsQuery,
} from '../../features/scheduledReports/useScheduledReports';
import { formatDateTime } from '../../utils/formatting';

const DEFAULT_DAY_OF_MONTH = 1;

function NewSubscriptionDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { locale } = useI18n();
  const create = useCreateScheduledReport();

  const [recipientEmail, setRecipientEmail] = useState('');
  const [reportType, setReportType] = useState<ScheduledReportType>('TimeEntries');
  const [cadence, setCadence] = useState<ScheduledReportCadence>('Weekly');
  const [dayOfWeek, setDayOfWeek] = useState<WeekDay>('Monday');
  const [dayOfMonth, setDayOfMonth] = useState(DEFAULT_DAY_OF_MONTH);

  const submit = () => {
    const input: ScheduledReportSubscriptionInput = {
      recipientEmail: recipientEmail.trim() || undefined,
      reportType,
      cadence,
      dayOfWeek: cadence === 'Weekly' ? dayOfWeek : undefined,
      dayOfMonth: cadence === 'Monthly' ? dayOfMonth : undefined,
      language: locale,
    };

    create.mutate(input, {
      onSuccess: () => {
        setRecipientEmail('');
        onClose();
      },
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('scheduledReports.add')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <FormControl fullWidth>
            <InputLabel id="report-type-label">{t('scheduledReports.reportType')}</InputLabel>
            <Select
              labelId="report-type-label"
              label={t('scheduledReports.reportType')}
              value={reportType}
              onChange={(event) => setReportType(event.target.value as ScheduledReportType)}
            >
              {scheduledReportTypes.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('scheduledReportType', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth>
            <InputLabel id="cadence-label">{t('scheduledReports.cadence')}</InputLabel>
            <Select
              labelId="cadence-label"
              label={t('scheduledReports.cadence')}
              value={cadence}
              onChange={(event) => setCadence(event.target.value as ScheduledReportCadence)}
            >
              {scheduledReportCadences.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('scheduledReportCadence', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {cadence === 'Weekly' ? (
            <FormControl fullWidth>
              <InputLabel id="day-of-week-label">{t('scheduledReports.dayOfWeek')}</InputLabel>
              <Select
                labelId="day-of-week-label"
                label={t('scheduledReports.dayOfWeek')}
                value={dayOfWeek}
                onChange={(event) => setDayOfWeek(event.target.value as WeekDay)}
              >
                {weekDays.map((value) => (
                  <MenuItem key={value} value={value}>
                    {enumLabel('weekDay', value)}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          ) : (
            <TextField
              type="number"
              label={t('scheduledReports.dayOfMonth')}
              value={dayOfMonth}
              onChange={(event) =>
                setDayOfMonth(Math.min(28, Math.max(1, Number(event.target.value) || 1)))
              }
              slotProps={{ htmlInput: { min: 1, max: 28 } }}
              fullWidth
            />
          )}

          <TextField
            label={t('scheduledReports.recipientEmail')}
            placeholder={t('scheduledReports.recipientEmailPlaceholder')}
            value={recipientEmail}
            onChange={(event) => setRecipientEmail(event.target.value)}
            fullWidth
          />

          {create.error && (
            <Typography variant="body2" color="error">
              {create.error.message}
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" onClick={submit} disabled={create.isPending}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export function ScheduledReportsListPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data, isLoading, isError, error, refetch } = useScheduledReportsQuery();
  const remove = useDeleteWithConfirm<ScheduledReportSubscription>(useDeleteScheduledReport());
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <Box>
      <PageHeader
        title={t('scheduledReports.title')}
        description={t('scheduledReports.description')}
        action={{
          label: t('scheduledReports.add'),
          icon: <AddOutlined />,
          onClick: () => setDialogOpen(true),
        }}
      />

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {!isLoading && !isError && (data?.length ?? 0) === 0 && (
        <Typography color="text.secondary">{t('scheduledReports.empty')}</Typography>
      )}

      {data && data.length > 0 && (
        <List>
          {data.map((subscription) => (
            <ListItem
              key={subscription.id}
              divider
              secondaryAction={
                <Tooltip title={t('common.delete')}>
                  <IconButton onClick={() => remove.request(subscription)}>
                    <DeleteOutlined fontSize="small" />
                  </IconButton>
                </Tooltip>
              }
            >
              <ListItemIcon>
                <ScheduleSendOutlined />
              </ListItemIcon>
              <ListItemText
                primary={`${enumLabel('scheduledReportType', subscription.reportType)} — ${enumLabel('scheduledReportCadence', subscription.cadence)}${subscription.dayOfWeek ? ` (${enumLabel('weekDay', subscription.dayOfWeek)})` : subscription.dayOfMonth ? ` (${subscription.dayOfMonth}.)` : ''}`}
                secondary={`${subscription.recipientEmail} · ${t('scheduledReports.nextRun', { date: formatDateTime(subscription.nextRunAtUtc) })}`}
              />
            </ListItem>
          ))}
        </List>
      )}

      <NewSubscriptionDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('scheduledReports.deleteConfirmTitle')}
        description={t('scheduledReports.deleteConfirmBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Typography variant="body2" color="error" sx={{ mt: 1 }}>
          {remove.error.message}
        </Typography>
      )}
    </Box>
  );
}
