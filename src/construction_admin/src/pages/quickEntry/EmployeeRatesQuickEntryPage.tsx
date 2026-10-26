import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
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
import { DataGrid, type GridColDef, type GridRowSelectionModel } from '@mui/x-data-grid';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { costsApi } from '../../api/costs';
import { employeesApi } from '../../api/employees';
import { newIdempotencyKey } from '../../api/idempotency';
import type { EmployeeRate } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { fetchAllPages } from '../../features/dashboard/dashboardData';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { dateOnlyOffset, formatDate, formatMoney } from '../../utils/formatting';

type Mode = 'pct' | 'add' | 'set';

/** One person with the rate in force today; the six amounts are what the table edits. */
interface Row {
  id: string;
  fullName: string;
  position: string;
  status: string;
  rateType: 'Hourly' | 'Daily';
  since: string | null;
  hourlyRate: number | null;
  weekendHourlyRate: number | null;
  holidayHourlyRate: number | null;
  overtimeHourlyRate: number | null;
  travelHourlyRate: number | null;
  dailyRate: number | null;
}

const AMOUNTS = ['hourlyRate', 'weekendHourlyRate', 'holidayHourlyRate', 'overtimeHourlyRate', 'travelHourlyRate', 'dailyRate'] as const;

const round = (n: number) => Math.round(n * 100) / 100;
const same = (a: Row, b: Row) => AMOUNTS.every((k) => a[k] === b[k]);

/** The main figure of a row: the hour, or the day for someone paid by the day. */
const main = (r: Row) => (r.rateType === 'Daily' ? r.dailyRate : r.hourlyRate);

/**
 * Hourly rates of many people in one table. A new rate is put in force from a date and the one before it is closed
 * the day before, never edited, so what the past cost does not change. Nothing is written before the review.
 */
export function EmployeeRatesQuickEntryPage() {
  const t = useT();
  const { locale } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();

  const [onlyActive, setOnlyActive] = useState(true);

  const { data: base, isLoading } = useQuery({
    queryKey: ['quick-entry-rates', branchId ?? null],
    queryFn: async (): Promise<Row[]> => {
      const unit = branchId ? { branchId } : {};
      const employees = (await fetchAllPages((pageNumber) => employeesApi.list({ pageNumber, pageSize: 100, ...unit }))).items;
      const rates = (await fetchAllPages((pageNumber) => costsApi.rates.list({ pageNumber, pageSize: 100, currentOnly: true }))).items;
      const byEmployee = new Map<string, EmployeeRate>(rates.map((r) => [r.employeeId, r]));

      return employees.map((e) => {
        const r = byEmployee.get(e.id);

        return {
          id: e.id,
          fullName: e.fullName,
          position: e.position,
          status: e.status,
          rateType: r?.rateType === 'Daily' ? 'Daily' : 'Hourly',
          since: r?.startDate ?? null,
          hourlyRate: r?.hourlyRate ?? null,
          weekendHourlyRate: r?.weekendHourlyRate ?? null,
          holidayHourlyRate: r?.holidayHourlyRate ?? null,
          overtimeHourlyRate: r?.overtimeHourlyRate ?? null,
          travelHourlyRate: r?.travelHourlyRate ?? null,
          dailyRate: r?.dailyRate ?? null,
        };
      });
    },
  });

  const [edits, setEdits] = useState<Record<string, Row>>({});
  const [selection, setSelection] = useState<GridRowSelectionModel>({ type: 'include', ids: new Set() });
  const [from, setFrom] = useState(dateOnlyOffset(0));
  const [mode, setMode] = useState<Mode>('pct');
  const [value, setValue] = useState('5');
  const [note, setNote] = useState('');
  const [message, setMessage] = useState<{ severity: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [reviewing, setReviewing] = useState(false);
  const [saving, setSaving] = useState(false);

  const visible = useMemo(() => (base ?? []).filter((r) => !onlyActive || r.status === 'Active'), [base, onlyActive]);
  const rows = useMemo(() => visible.map((r) => edits[r.id] ?? r), [visible, edits]);
  const changedIds = Object.keys(edits).filter((id) => {
    const original = (base ?? []).find((r) => r.id === id);
    return original && !same(original, edits[id]);
  });

  const money = (n: number) => formatMoney(n, locale);
  const num = (header: string, field: (typeof AMOUNTS)[number], width = 105): GridColDef<Row> => ({
    field,
    headerName: header,
    width,
    type: 'number',
    editable: true,
    align: 'right',
    headerAlign: 'right',
    valueFormatter: (v: number | null) => (v == null ? '—' : v.toFixed(2)),
  });

  const columns: GridColDef<Row>[] = [
    { field: 'fullName', headerName: `${t('employees.firstName')} / ${t('employees.lastName')}`, width: 190 },
    { field: 'position', headerName: t('employees.position'), width: 150 },
    {
      field: 'since',
      headerName: t('quickEntry.rates.since'),
      width: 110,
      valueFormatter: (v: string | null) => (v ? formatDate(v) : t('quickEntry.rates.rateNone')),
    },
    num(t('quickEntry.rates.hourly'), 'hourlyRate'),
    num(t('quickEntry.rates.weekend'), 'weekendHourlyRate'),
    num(t('quickEntry.rates.holiday'), 'holidayHourlyRate'),
    num(t('quickEntry.rates.overtime'), 'overtimeHourlyRate', 125),
    num(t('quickEntry.rates.travel'), 'travelHourlyRate', 120),
    num(t('quickEntry.rates.daily'), 'dailyRate', 95),
    {
      field: 'diff',
      headerName: t('quickEntry.rates.diff'),
      width: 110,
      sortable: false,
      align: 'right',
      headerAlign: 'right',
      valueGetter: (_v, row) => {
        const original = (base ?? []).find((r) => r.id === row.id);
        const was = original ? main(original) : null;
        const now = main(row);

        return was != null && now != null && was !== now ? round(now - was) : null;
      },
      valueFormatter: (v: number | null) => (v == null ? '' : `${v > 0 ? '+' : ''}${v.toFixed(2)}`),
    },
  ];

  const selectedIds = (): string[] =>
    selection.type === 'include' ? Array.from(selection.ids).map(String) : visible.map((r) => r.id).filter((id) => !selection.ids.has(id));

  const amount = Number(value.replace(',', '.'));
  const valueOk = value.trim() !== '' && Number.isFinite(amount) && amount >= 0;

  /** The new figure of a row under the chosen way; null when it cannot be worked out (no rate to start from). */
  const next = (r: Row): Row | null => {
    const was = main(r);

    if (was == null) {
      // Nothing to raise: only an exact amount makes a first hourly rate.
      return mode === 'set' ? { ...r, rateType: 'Hourly', hourlyRate: round(amount) } : null;
    }

    const now = round(mode === 'pct' ? was * (1 + amount / 100) : mode === 'add' ? was + amount : amount);
    const move = (v: number | null) =>
      v == null || was === 0 ? v : round(mode === 'pct' ? v * (now / was) : v + (now - was));

    if (r.rateType === 'Daily') return { ...r, dailyRate: now };

    return {
      ...r,
      hourlyRate: now,
      weekendHourlyRate: move(r.weekendHourlyRate),
      holidayHourlyRate: move(r.holidayHourlyRate),
      overtimeHourlyRate: move(r.overtimeHourlyRate),
      travelHourlyRate: move(r.travelHourlyRate),
    };
  };

  const apply = () => {
    const ids = new Set(selectedIds());
    let skipped = 0;
    const patch: Record<string, Row> = {};

    for (const r of visible) {
      if (!ids.has(r.id)) continue;
      const updated = next(edits[r.id] ?? r);
      if (updated) patch[r.id] = updated;
      else skipped++;
    }

    setEdits((prev) => ({ ...prev, ...patch }));
    setMessage(
      skipped > 0
        ? { severity: 'info', text: t('quickEntry.rates.skipped', { count: skipped }) }
        : { severity: 'info', text: t('quickEntry.applied', { count: Object.keys(patch).length }) },
    );
  };

  const totalHourly = changedIds.reduce((sum, id) => {
    const was = (base ?? []).find((r) => r.id === id);
    const now = edits[id];
    return was && now.rateType === 'Hourly' && was.hourlyRate != null && now.hourlyRate != null ? sum + (now.hourlyRate - was.hourlyRate) : sum;
  }, 0);

  const save = async () => {
    setSaving(true);
    let done = 0;
    const failed: string[] = [];

    for (const id of changedIds) {
      const r = edits[id];
      try {
        await costsApi.rates.set(
          {
            employeeId: id,
            rateType: r.rateType,
            hourlyRate: r.rateType === 'Hourly' ? r.hourlyRate : null,
            weekendHourlyRate: r.rateType === 'Hourly' ? r.weekendHourlyRate : null,
            holidayHourlyRate: r.rateType === 'Hourly' ? r.holidayHourlyRate : null,
            overtimeHourlyRate: r.rateType === 'Hourly' ? r.overtimeHourlyRate : null,
            travelHourlyRate: r.rateType === 'Hourly' ? r.travelHourlyRate : null,
            dailyRate: r.rateType === 'Daily' ? r.dailyRate : null,
            startDate: from,
            note: note.trim() || null,
          },
          newIdempotencyKey(),
        );
        done++;
        setEdits((prev) => {
          const { [id]: _saved, ...rest } = prev;
          return rest;
        });
      } catch (error) {
        failed.push(`${r.fullName}: ${toApiError(error).message}`);
      }
    }

    setSaving(false);
    setReviewing(false);
    await queryClient.invalidateQueries();
    setMessage(
      failed.length
        ? { severity: 'error', text: `${t('quickEntry.savedSome', { done, total: changedIds.length })} ${failed.join(' · ')}` }
        : { severity: 'success', text: t('quickEntry.rates.saved', { count: done }) },
    );
  };

  return (
    <Box>
      <PageHeader
        title={t('quickEntry.employeeRatesTitle')}
        description={t('quickEntry.rates.description')}
        secondaryActions={<Button onClick={() => navigate(paths.employeesQuickEntry)}>{t('quickEntry.tabBasics')}</Button>}
      />

      <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
        <Stack direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            type="date"
            size="small"
            label={t('quickEntry.rates.from')}
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField select size="small" label={t('quickEntry.rates.mode')} value={mode} onChange={(e) => setMode(e.target.value as Mode)} sx={{ minWidth: 190 }}>
            <MenuItem value="pct">{t('quickEntry.rates.modePct')}</MenuItem>
            <MenuItem value="add">{t('quickEntry.rates.modeAdd')}</MenuItem>
            <MenuItem value="set">{t('quickEntry.rates.modeSet')}</MenuItem>
          </TextField>
          <TextField
            size="small"
            label={t('quickEntry.rates.value')}
            value={value}
            error={!valueOk}
            onChange={(e) => setValue(e.target.value)}
            sx={{ width: 110 }}
          />
          <TextField size="small" label={t('quickEntry.rates.note')} value={note} onChange={(e) => setNote(e.target.value)} sx={{ minWidth: 200 }} />
          <Button variant="outlined" onClick={apply} disabled={!valueOk || selectedIds().length === 0}>
            {t('quickEntry.rates.apply', { count: selectedIds().length })}
          </Button>
          <FormControlLabel
            control={<Switch size="small" checked={onlyActive} onChange={(e) => setOnlyActive(e.target.checked)} />}
            label={t('quickEntry.rates.onlyActive')}
          />
        </Stack>
      </Paper>

      {message && (
        <Alert severity={message.severity} sx={{ mb: 2 }} onClose={() => setMessage(null)}>
          {message.text}
        </Alert>
      )}

      <Paper variant="outlined" sx={{ height: 560, mb: 2 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={isLoading}
          checkboxSelection
          disableRowSelectionOnClick
          rowSelectionModel={selection}
          onRowSelectionModelChange={setSelection}
          hideFooter
          processRowUpdate={(updated: Row) => {
            setEdits((prev) => ({ ...prev, [updated.id]: updated }));
            return updated;
          }}
          getCellClassName={(p) => {
            const original = (base ?? []).find((r) => r.id === p.id);
            const edited = edits[String(p.id)];
            return original && edited && (AMOUNTS as readonly string[]).includes(p.field) && original[p.field as (typeof AMOUNTS)[number]] !== edited[p.field as (typeof AMOUNTS)[number]]
              ? 'quick-entry-changed'
              : '';
          }}
          sx={{ '& .quick-entry-changed': { outline: '2px solid', outlineColor: 'primary.main', outlineOffset: -2 } }}
        />
      </Paper>

      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
        <Button variant="contained" onClick={() => setReviewing(true)} disabled={changedIds.length === 0 || !from}>
          {t('quickEntry.rates.review', { count: changedIds.length })}
        </Button>
        <Button onClick={() => setEdits({})} disabled={changedIds.length === 0}>
          {t('quickEntry.undo')}
        </Button>
        {changedIds.length === 0 && (
          <Typography variant="body2" color="text.secondary">
            {t('quickEntry.rates.nothing')}
          </Typography>
        )}
      </Stack>

      <Dialog open={reviewing} onClose={() => !saving && setReviewing(false)} fullWidth maxWidth="md">
        <DialogTitle>{t('quickEntry.rates.reviewTitle')}</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            {t('quickEntry.rates.reviewIntro', { date: formatDate(from) })}
          </Typography>
          <Box sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('employees.firstName')}</TableCell>
                  <TableCell align="right">{t('quickEntry.rates.since')}</TableCell>
                  <TableCell align="right">{t('quickEntry.rates.hourly')} / {t('quickEntry.rates.daily')}</TableCell>
                  <TableCell align="right">{t('quickEntry.rates.diff')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {changedIds.map((id) => {
                  const was = (base ?? []).find((r) => r.id === id)!;
                  const now = edits[id];
                  const a = main(was);
                  const b = main(now);
                  const d = a != null && b != null ? round(b - a) : null;

                  return (
                    <TableRow key={id}>
                      <TableCell>{now.fullName}</TableCell>
                      <TableCell align="right">{a != null ? money(a) : t('quickEntry.rates.rateNone')}</TableCell>
                      <TableCell align="right">{b != null ? money(b) : '—'}</TableCell>
                      <TableCell align="right" sx={{ color: d != null && d < 0 ? 'error.main' : 'success.main' }}>
                        {d != null ? `${d > 0 ? '+' : ''}${d.toFixed(2)}` : ''}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Box>
          <Typography sx={{ mt: 1.5, fontWeight: 700 }}>
            {t('quickEntry.rates.totalDiff', { amount: `${totalHourly > 0 ? '+' : ''}${money(round(totalHourly))}` })}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReviewing(false)} disabled={saving}>
            {t('quickEntry.rates.back')}
          </Button>
          <Button variant="contained" loading={saving} onClick={() => void save()}>
            {t('quickEntry.rates.confirm', { count: changedIds.length })}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
