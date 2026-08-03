import {
  Box,
  Card,
  CardContent,
  Chip,
  FormControlLabel,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useTimeEntrySummaryQuery } from '../../features/timeEntries/useTimeEntries';
import { useT } from '../../i18n/useI18n';
import { splitMinutes } from '../../utils/formatting';

/** `YYYY-MM-DD` for a date input, in local time rather than UTC. */
function toDateInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function startOfMonth(): string {
  const now = new Date();
  return toDateInput(new Date(now.getFullYear(), now.getMonth(), 1));
}

/** The instant just after the given local day ends. */
function endOfDay(date: string): string {
  const next = new Date(`${date}T00:00`);
  next.setDate(next.getDate() + 1);

  return next.toISOString();
}

export function TimeEntrySummaryPage() {
  const t = useT();

  const [from, setFrom] = useState(startOfMonth);
  const [to, setTo] = useState(() => toDateInput(new Date()));
  const [approvedOnly, setApprovedOnly] = useState(false);

  const query = useMemo(
    () => ({
      from: new Date(`${from}T00:00`).toISOString(),
      // The end date is inclusive to the person reading it: picking the 31st
      // has to include the 31st. The API's window is half-open, so send the
      // start of the following day — sending midnight of the 31st would drop
      // that whole day's hours without anything on screen saying so.
      to: endOfDay(to),
      approvedOnly: approvedOnly || undefined,
    }),
    [from, to, approvedOnly],
  );

  const valid = !!from && !!to && from <= to;
  const { data, isLoading, isError, error, refetch } = useTimeEntrySummaryQuery(
    query,
    valid,
  );

  const hours = (minutes: number) => t('timeEntries.hoursShort', splitMinutes(minutes));

  return (
    <Box>
      <PageHeader title={t('timeEntries.summary')} />

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <TextField
          label={t('timeEntries.from')}
          type="date"
          size="small"
          value={from}
          onChange={(event) => setFrom(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label={t('timeEntries.to')}
          type="date"
          size="small"
          value={to}
          onChange={(event) => setTo(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          error={!valid}
        />
        <FormControlLabel
          control={
            <Switch
              checked={approvedOnly}
              onChange={(event) => setApprovedOnly(event.target.checked)}
            />
          }
          label={t('timeEntries.summaryApproved')}
        />
      </Stack>

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {data && (
        <>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
            <SummaryTile
              label={t('timeEntries.summaryTotal')}
              value={hours(data.totalMinutes)}
            />
            <SummaryTile
              label={t('timeEntries.summaryApproved')}
              value={hours(data.approvedMinutes)}
            />
            <SummaryTile
              label={t('timeEntries.summaryPending')}
              value={String(data.pendingCount)}
            />
          </Stack>

          <Paper>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('timeEntries.employee')}</TableCell>
                  <TableCell align="right">{t('timeEntries.summaryEntries')}</TableCell>
                  <TableCell align="right">{t('timeEntries.summaryTotal')}</TableCell>
                  <TableCell align="right">{t('timeEntries.summaryApproved')}</TableCell>
                  <TableCell align="right">{t('timeEntries.summaryPending')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.rows.map((row) => (
                  <TableRow key={row.employeeId} hover>
                    <TableCell>{row.employeeName}</TableCell>
                    <TableCell align="right">{row.entryCount}</TableCell>
                    <TableCell align="right">{hours(row.totalMinutes)}</TableCell>
                    <TableCell align="right">{hours(row.approvedMinutes)}</TableCell>
                    <TableCell align="right">
                      {row.pendingCount > 0 ? (
                        <Chip size="small" color="warning" label={row.pendingCount} />
                      ) : (
                        '—'
                      )}
                    </TableCell>
                  </TableRow>
                ))}

                {data.rows.length === 0 && !isLoading && (
                  <TableRow>
                    <TableCell colSpan={5}>
                      <Typography variant="body2" color="text.secondary">
                        {t('timeEntries.summaryEmpty')}
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Paper>
        </>
      )}
    </Box>
  );
}

function SummaryTile({ label, value }: { label: string; value: string }) {
  return (
    <Card sx={{ flex: 1 }}>
      <CardContent>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {value}
        </Typography>
      </CardContent>
    </Card>
  );
}
