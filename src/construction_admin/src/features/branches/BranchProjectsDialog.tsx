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
import type { Branch, Project } from '../../api/types';
import { useT } from '../../i18n/useI18n';
import { useEveryMainProjectQuery } from '../projects/useProjects';
import { BranchDot } from './BranchDot';
import { useSetBranchProjects } from './useBranches';

/**
 * Picks which Main projects belong to one business unit. This is how the existing projects get
 * into a freshly created unit: tick them here, grouped by customer, instead of editing each one.
 */
export function BranchProjectsDialog({ branch, onClose }: { branch: Branch | null; onClose: () => void }) {
  const t = useT();
  const { data } = useEveryMainProjectQuery();
  const save = useSetBranchProjects();
  const [chosen, setChosen] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);

  const projects = useMemo(() => data?.items ?? [], [data]);

  // Start from what is already in the unit each time the dialog opens.
  useEffect(() => {
    if (branch) {
      setChosen(new Set(projects.filter((p) => p.branchId === branch.id).map((p) => p.id)));
      setSearch('');
      setError(null);
    }
  }, [branch, projects]);

  const groups = useMemo(() => {
    const needle = search.trim().toLowerCase();
    const byCustomer = new Map<string, Project[]>();
    for (const project of projects) {
      if (needle && !project.name.toLowerCase().includes(needle)) continue;
      const key = project.customerName ?? '';
      byCustomer.set(key, [...(byCustomer.get(key) ?? []), project]);
    }
    return [...byCustomer.entries()].sort(([a], [b]) => (a === '' ? 1 : b === '' ? -1 : a.localeCompare(b)));
  }, [projects, search]);

  const toggle = (ids: string[], on: boolean) =>
    setChosen((prev) => {
      const next = new Set(prev);
      for (const id of ids) {
        if (on) next.add(id);
        else next.delete(id);
      }
      return next;
    });

  const submit = async () => {
    if (!branch) return;
    try {
      await save.mutateAsync({ id: branch.id, projectIds: [...chosen] });
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Dialog open={!!branch} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('branches.assignTitle', { name: branch?.name ?? '' })}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2}>
          <Typography variant="body2" color="text.secondary">
            {t('branches.assignHint')}
          </Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            size="small"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('branches.assignSearch')}
          />
          {groups.map(([customer, items]) => {
            const ids = items.map((p) => p.id);
            const all = ids.every((id) => chosen.has(id));
            const some = ids.some((id) => chosen.has(id));
            return (
              <Box key={customer}>
                <FormControlLabel
                  label={<strong>{customer || t('branches.noCustomer')}</strong>}
                  control={
                    <Checkbox
                      checked={all}
                      indeterminate={some && !all}
                      onChange={(e) => toggle(ids, e.target.checked)}
                    />
                  }
                />
                <Stack sx={{ pl: 4 }}>
                  {items.map((project) => (
                    <FormControlLabel
                      key={project.id}
                      control={
                        <Checkbox
                          size="small"
                          checked={chosen.has(project.id)}
                          onChange={(e) => toggle([project.id], e.target.checked)}
                        />
                      }
                      label={
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <span>{project.name}</span>
                          {project.branchId && project.branchId !== branch?.id && (
                            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
                              <BranchDot color={project.branchColor ?? '#999999'} size={7} />
                              <Typography variant="caption" color="text.secondary">
                                {t('branches.assignedElsewhere', { name: project.branchName ?? '' })}
                              </Typography>
                            </Stack>
                          )}
                        </Stack>
                      }
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
        <Button variant="contained" onClick={() => void submit()} disabled={save.isPending}>
          {t('branches.assignSave')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
