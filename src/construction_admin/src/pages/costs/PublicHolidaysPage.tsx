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
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { PublicHoliday } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import {
  useCreatePublicHoliday,
  useDeletePublicHoliday,
  usePublicHolidaysQuery,
} from '../../features/publicHolidays/usePublicHolidays';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

type SortField = 'date' | 'name';
type SortDirection = 'asc' | 'desc';

export function PublicHolidaysPage() {
  const t = useT();
  const { data, isLoading } = usePublicHolidaysQuery();
  const remove = useDeleteWithConfirm<PublicHoliday>(useDeletePublicHoliday());
  const [adding, setAdding] = useState(false);

  const [sortBy, setSortBy] = useState<SortField>('date');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  const toggleSort = (field: SortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const sortedRows = useMemo(() => {
    if (!data) return [];

    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: PublicHoliday, b: PublicHoliday): number =>
      sortBy === 'date'
        ? a.date.localeCompare(b.date) * factor
        : a.name.localeCompare(b.name) * factor;

    return [...data].sort(compare);
  }, [data, sortBy, sortDirection]);

  return (
    <Box>
      <PageHeader
        title={t('publicHolidays.title')}
        subtitle={t('publicHolidays.subtitle')}
        action={{
          label: t('publicHolidays.add'),
          icon: <AddOutlined />,
          onClick: () => setAdding(true),
        }}
      />

      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sortDirection={sortBy === 'date' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'date'}
                  direction={sortBy === 'date' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('date')}
                >
                  {t('publicHolidays.date')}
                </TableSortLabel>
              </TableCell>
              <TableCell sortDirection={sortBy === 'name' ? sortDirection : false}>
                <TableSortLabel
                  active={sortBy === 'name'}
                  direction={sortBy === 'name' ? sortDirection : 'asc'}
                  onClick={() => toggleSort('name')}
                >
                  {t('publicHolidays.name')}
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" />
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedRows.map((holiday) => (
              <TableRow key={holiday.id} hover>
                <TableCell>{formatDate(holiday.date)}</TableCell>
                <TableCell>{holiday.name}</TableCell>
                <TableCell align="right">
                  <Tooltip title={t('common.delete')}>
                    <IconButton size="small" onClick={() => remove.request(holiday)}>
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}

            {sortedRows.length === 0 && !isLoading && (
              <TableRow>
                <TableCell colSpan={3}>
                  <Typography variant="body2" color="text.secondary">
                    {t('publicHolidays.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      <AddHolidayDialog open={adding} onClose={() => setAdding(false)} />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('publicHolidays.deleteTitle')}
        description={
          remove.pending
            ? t('publicHolidays.deleteBody', { name: remove.pending.name })
            : ''
        }
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

function AddHolidayDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const create = useCreatePublicHoliday();

  const [date, setDate] = useState('');
  const [name, setName] = useState('');

  const reset = create.reset;

  useEffect(() => {
    if (open) {
      setDate('');
      setName('');
      reset();
    }
  }, [open, reset]);

  const canSubmit = date !== '' && name.trim() !== '';
  const error = create.isError ? toApiError(create.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('publicHolidays.add')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('publicHolidays.date')}
              value={date}
              onChange={(event) => setDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              fullWidth
              label={t('publicHolidays.name')}
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || create.isPending}
          onClick={() =>
            create.mutate(
              { date, name: name.trim() },
              { onSuccess: onClose },
            )
          }
        >
          {t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
