import { AddOutlined, CloudSyncOutlined, DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  Grid,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
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
import type { PublicHoliday, PublicHolidayCandidate } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { COUNTRIES, countryLabel, resolveCountryCode } from '../../data/countries';
import {
  useCreatePublicHoliday,
  useDeletePublicHoliday,
  useImportPublicHolidays,
  usePreviewHolidaySync,
  usePublicHolidaysQuery,
} from '../../features/publicHolidays/usePublicHolidays';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

type SortField = 'date' | 'name';
type SortDirection = 'asc' | 'desc';

export function PublicHolidaysPage() {
  const t = useT();
  const [countryFilter, setCountryFilter] = useState('');
  const { data, isLoading } = usePublicHolidaysQuery(
    countryFilter ? { countryCode: countryFilter } : {},
  );
  const remove = useDeleteWithConfirm<PublicHoliday>(useDeletePublicHoliday());
  const [adding, setAdding] = useState(false);
  const [syncing, setSyncing] = useState(false);

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
        description={t('publicHolidays.subtitle')}
        action={{
          label: t('publicHolidays.add'),
          icon: <AddOutlined />,
          onClick: () => setAdding(true),
        }}
      />

      <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', mb: 2 }}>
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel id="holiday-country-filter-label">{t('publicHolidays.country')}</InputLabel>
          <Select
            labelId="holiday-country-filter-label"
            label={t('publicHolidays.country')}
            value={countryFilter}
            onChange={(event) => setCountryFilter(event.target.value)}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {COUNTRIES.map((country) => (
              <MenuItem key={country.code} value={country.code}>
                {country.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Button
          variant="outlined"
          startIcon={<CloudSyncOutlined />}
          onClick={() => setSyncing(true)}
        >
          {t('publicHolidays.sync')}
        </Button>
      </Stack>

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
              <TableCell>{t('publicHolidays.country')}</TableCell>
              <TableCell align="right" />
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedRows.map((holiday) => (
              <TableRow key={holiday.id} hover>
                <TableCell>{formatDate(holiday.date)}</TableCell>
                <TableCell>{holiday.name}</TableCell>
                <TableCell>
                  <Chip size="small" variant="outlined" label={countryLabel(holiday.countryCode)} />
                </TableCell>
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
                <TableCell colSpan={4}>
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
      <SyncHolidaysDialog open={syncing} onClose={() => setSyncing(false)} />

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
  const [countryInput, setCountryInput] = useState('');

  const reset = create.reset;

  useEffect(() => {
    if (open) {
      setDate('');
      setName('');
      setCountryInput('');
      reset();
    }
  }, [open, reset]);

  const countryCode = resolveCountryCode(countryInput);
  const canSubmit = date !== '' && name.trim() !== '' && !!countryCode;
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

          <Grid size={12}>
            <Autocomplete
              freeSolo
              fullWidth
              options={COUNTRIES.map((c) => c.label)}
              inputValue={countryInput}
              onInputChange={(_event, value) => setCountryInput(value)}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label={t('publicHolidays.country')}
                  placeholder={t('publicHolidays.countryPlaceholder')}
                />
              )}
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
              { date, name: name.trim(), countryCode: countryCode! },
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

/**
 * Search a country/year on the internet, review what comes back, and import
 * only the ones chosen — nothing is written to the calendar until "Import".
 */
function SyncHolidaysDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const preview = usePreviewHolidaySync();
  const importHolidays = useImportPublicHolidays();

  const [countryInput, setCountryInput] = useState('');
  const [year, setYear] = useState(() => new Date().getFullYear());
  const [selected, setSelected] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (open) {
      setCountryInput('');
      setYear(new Date().getFullYear());
      setSelected(new Set());
      preview.reset();
      importHolidays.reset();
    }
    // Only on open/close — resetting on every render would wipe a search
    // result the moment its own state settles.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const countryCode = resolveCountryCode(countryInput);
  const candidates = preview.data ?? [];

  const toggle = (date: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(date)) {
        next.delete(date);
      } else {
        next.add(date);
      }
      return next;
    });
  };

  const handleSearch = () => {
    if (!countryCode) return;

    preview.mutate(
      { countryCode, year },
      {
        onSuccess: (result) => {
          setSelected(new Set(result.filter((c) => !c.alreadyOnCalendar).map((c) => c.date)));
        },
      },
    );
  };

  const close = () => {
    onClose();
  };

  const previewError = preview.isError ? toApiError(preview.error) : null;
  const importError = importHolidays.isError ? toApiError(importHolidays.error) : null;

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('publicHolidays.syncTitle')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('publicHolidays.syncHint')}
        </Typography>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
          <Autocomplete
            freeSolo
            fullWidth
            options={COUNTRIES.map((c) => c.label)}
            inputValue={countryInput}
            onInputChange={(_event, value) => setCountryInput(value)}
            renderInput={(params) => (
              <TextField
                {...params}
                label={t('publicHolidays.country')}
                placeholder={t('publicHolidays.countryPlaceholder')}
              />
            )}
          />
          <TextField
            type="number"
            label={t('publicHolidays.year')}
            value={year}
            onChange={(event) => setYear(Number(event.target.value) || year)}
            sx={{ minWidth: { sm: 140 } }}
          />
          <Button
            variant="outlined"
            disabled={!countryCode || preview.isPending}
            loading={preview.isPending}
            onClick={handleSearch}
            sx={{ flexShrink: 0 }}
          >
            {t('publicHolidays.search')}
          </Button>
        </Stack>

        {previewError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {previewError.message}
          </Alert>
        )}
        {importError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {importError.message}
          </Alert>
        )}

        {preview.isSuccess && candidates.length === 0 && (
          <Typography variant="body2" color="text.secondary">
            {t('publicHolidays.syncEmpty')}
          </Typography>
        )}

        {candidates.length > 0 && (
          <Paper variant="outlined" sx={{ maxHeight: 320, overflowY: 'auto' }}>
            <Table size="small">
              <TableBody>
                {candidates.map((candidate) => (
                  <CandidateRow
                    key={candidate.date}
                    candidate={candidate}
                    checked={selected.has(candidate.date)}
                    onToggle={() => toggle(candidate.date)}
                  />
                ))}
              </TableBody>
            </Table>
          </Paper>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={selected.size === 0 || importHolidays.isPending}
          loading={importHolidays.isPending}
          onClick={() => {
            if (!countryCode) return;

            const items = candidates
              .filter((c) => selected.has(c.date))
              .map((c) => ({ date: c.date, name: c.name }));

            importHolidays.mutate({ countryCode, items }, { onSuccess: close });
          }}
        >
          {t('publicHolidays.importSelected', { count: selected.size })}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function CandidateRow({
  candidate,
  checked,
  onToggle,
}: {
  candidate: PublicHolidayCandidate;
  checked: boolean;
  onToggle: () => void;
}) {
  const t = useT();

  return (
    <TableRow hover>
      <TableCell padding="checkbox">
        <Checkbox
          checked={checked}
          disabled={candidate.alreadyOnCalendar}
          onChange={onToggle}
        />
      </TableCell>
      <TableCell width={110}>{formatDate(candidate.date)}</TableCell>
      <TableCell>{candidate.name}</TableCell>
      <TableCell align="right">
        {candidate.alreadyOnCalendar && (
          <Chip size="small" variant="outlined" label={t('publicHolidays.alreadyOnCalendar')} />
        )}
      </TableCell>
    </TableRow>
  );
}
