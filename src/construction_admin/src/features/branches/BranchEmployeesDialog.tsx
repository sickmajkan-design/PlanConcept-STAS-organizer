import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Branch, Employee } from '../../api/types';
import { useEveryEmployeeQuery } from '../employees/useEmployees';
import { useT } from '../../i18n/useI18n';
import { BranchDot } from './BranchDot';
import { useAssignEmployeesToBranch } from './useBranches';

const todayIso = () => new Date().toISOString().slice(0, 10);

/**
 * Brings existing employees into a business unit in one go: tick them, choose the date they are
 * employed there from, and save. Only the ticked ones move — everybody else keeps their unit — and
 * the unit they leave is closed the day before, so past hours stay where they were worked.
 */
export function BranchEmployeesDialog({ branch, onClose }: { branch: Branch | null; onClose: () => void }) {
  const t = useT();
  const { data } = useEveryEmployeeQuery();
  const assign = useAssignEmployeesToBranch();
  const [chosen, setChosen] = useState<Set<string>>(new Set());
  const [from, setFrom] = useState(todayIso());
  const [backdate, setBackdate] = useState(true);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);

  const employees = useMemo(() => data?.items ?? [], [data]);

  useEffect(() => {
    if (branch) {
      setChosen(new Set());
      setFrom(todayIso());
      setBackdate(true);
      setSearch('');
      setError(null);
    }
  }, [branch]);

  // Grouped by the unit they are in now, so "everybody still in no unit" is one tick away.
  const groups = useMemo(() => {
    const needle = search.trim().toLowerCase();
    const byUnit = new Map<string, { name: string; color: string | null; items: Employee[] }>();

    for (const employee of employees) {
      if (employee.branchId === branch?.id) continue;
      if (needle && !`${employee.fullName} ${employee.employeeNumber}`.toLowerCase().includes(needle)) continue;

      const key = employee.branchId ?? '';
      const group = byUnit.get(key) ?? { name: employee.branchName ?? '', color: employee.branchColor, items: [] };
      group.items.push(employee);
      byUnit.set(key, group);
    }

    return [...byUnit.entries()].sort(([a], [b]) => (a === '' ? -1 : b === '' ? 1 : a.localeCompare(b)));
  }, [employees, branch?.id, search]);

  const already = employees.filter((e) => e.branchId === branch?.id).length;

  const toggle = (ids: string[], on: boolean) =>
    setChosen((previous) => {
      const next = new Set(previous);
      for (const id of ids) {
        if (on) next.add(id);
        else next.delete(id);
      }
      return next;
    });

  const submit = async () => {
    if (!branch) return;
    try {
      await assign.mutateAsync({ id: branch.id, employeeIds: [...chosen], from, backdateNewcomers: backdate });
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Dialog open={!!branch} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('branches.employeesTitle', { name: branch?.name ?? '' })}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2}>
          <Typography variant="body2" color="text.secondary">
            {t('branches.employeesHint', { count: already })}
          </Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              size="small"
              fullWidth
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={t('branches.employeesSearch')}
            />
            <TextField
              size="small"
              type="date"
              label={t('employees.branch.effective')}
              value={from}
              onChange={(event) => setFrom(event.target.value)}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: todayIso() } }}
              sx={{ minWidth: 190 }}
            />
          </Stack>
          <FormControlLabel
            control={<Checkbox checked={backdate} onChange={(event) => setBackdate(event.target.checked)} />}
            label={
              <Box>
                <Typography variant="body2">{t('branches.employeesBackdate')}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {t('branches.employeesBackdateHint')}
                </Typography>
              </Box>
            }
          />

          {groups.map(([key, group]) => {
            const ids = group.items.map((e) => e.id);
            const all = ids.every((id) => chosen.has(id));
            const some = ids.some((id) => chosen.has(id));

            return (
              <Box key={key}>
                <FormControlLabel
                  label={
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                      {group.color && <BranchDot color={group.color} />}
                      <strong>{group.name || t('branches.employeesNoUnit')}</strong>
                      <Typography variant="caption" color="text.secondary">
                        {group.items.length}
                      </Typography>
                    </Stack>
                  }
                  control={
                    <Checkbox checked={all} indeterminate={some && !all} onChange={(e) => toggle(ids, e.target.checked)} />
                  }
                />
                <Stack sx={{ pl: 4 }}>
                  {group.items.map((employee) => (
                    <FormControlLabel
                      key={employee.id}
                      control={
                        <Checkbox
                          size="small"
                          checked={chosen.has(employee.id)}
                          onChange={(e) => toggle([employee.id], e.target.checked)}
                        />
                      }
                      label={`${employee.fullName} · ${employee.employeeNumber}`}
                    />
                  ))}
                </Stack>
              </Box>
            );
          })}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Typography variant="body2" color="text.secondary" sx={{ flex: 1, pl: 2 }}>
          {t('branches.assignSelected', { count: chosen.size })}
        </Typography>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" onClick={() => void submit()} disabled={assign.isPending || chosen.size === 0 || !from}>
          {t('branches.employeesSave')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
