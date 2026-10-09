import {
  Alert,
  Autocomplete,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  TextField,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Branch } from '../../api/types';
import { useT } from '../../i18n/useI18n';
import { useEveryEmployeeQuery } from '../employees/useEmployees';
import { branchToInput } from './branchInput';
import { allowedParents } from './branchTree';
import { useBranchesQuery, useUpdateBranch } from './useBranches';

export type QuickEdit = { branch: Branch; mode: 'head' | 'parent' };

/**
 * One change to a unit without opening its whole form: who runs it, or which unit it stands under.
 * The rest of the unit is sent back as it is.
 */
export function BranchQuickEditDialog({ target, onClose }: { target: QuickEdit | null; onClose: () => void }) {
  const t = useT();
  const update = useUpdateBranch();
  const { data: branches } = useBranchesQuery();
  const { data: employees } = useEveryEmployeeQuery();
  const [value, setValue] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (target) {
      setValue((target.mode === 'head' ? target.branch.headEmployeeId : target.branch.parentBranchId) ?? '');
      setError(null);
    }
  }, [target]);

  const save = async () => {
    if (!target) return;

    const changes =
      target.mode === 'head' ? { headEmployeeId: value || null } : { parentBranchId: value || null };

    try {
      await update.mutateAsync({ id: target.branch.id, input: branchToInput(target.branch, changes) });
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Dialog open={target !== null} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>
        {target?.mode === 'parent'
          ? t('branches.moveTitle', { name: target.branch.name })
          : t('branches.headTitle', { name: target?.branch.name ?? '' })}
      </DialogTitle>
      <DialogContent sx={{ pt: 1 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {target?.mode === 'parent' ? (
          <TextField
            select
            fullWidth
            margin="dense"
            label={t('branches.parent')}
            value={value}
            onChange={(event) => setValue(event.target.value)}
            helperText={t('branches.parentHint')}
          >
            <MenuItem value="">{t('branches.parentNone')}</MenuItem>
            {allowedParents(branches ?? [], target.branch.id).map((parent) => (
              <MenuItem key={parent.id} value={parent.id}>
                {parent.name}
              </MenuItem>
            ))}
          </TextField>
        ) : (
          <Autocomplete
            fullWidth
            options={employees?.items ?? []}
            getOptionLabel={(employee) => employee.fullName}
            isOptionEqualToValue={(option, selected) => option.id === selected.id}
            value={(employees?.items ?? []).find((e) => e.id === value) ?? null}
            onChange={(_event, selected) => setValue(selected?.id ?? '')}
            renderInput={(params) => (
              <TextField {...params} margin="dense" label={t('branches.head')} helperText={t('branches.headHint')} />
            )}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" onClick={() => void save()} disabled={update.isPending}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
