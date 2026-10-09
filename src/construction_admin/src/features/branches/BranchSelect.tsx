import { MenuItem, TextField } from '@mui/material';
import { useMemo } from 'react';

import { useT } from '../../i18n/useI18n';
import { BranchDot } from './BranchDot';
import { buildBranchTree, flattenBranchTree } from './branchTree';
import { useBranchesQuery } from './useBranches';

/**
 * A business-unit (poslovna jedinica) picker for the dialogs that keep their fields in plain
 * state. The value is the unit's id, or '' for none. A unit that has been switched off is still
 * offered while a record already uses it.
 */
export function BranchSelect({
  value,
  onChange,
  helperText,
}: {
  value: string;
  onChange: (id: string) => void;
  helperText?: string;
}) {
  const t = useT();
  const { data: branches } = useBranchesQuery();
  const nodes = useMemo(
    () => flattenBranchTree(buildBranchTree((branches ?? []).filter((b) => b.isActive || b.id === value))),
    [branches, value],
  );

  return (
    <TextField
      select
      fullWidth
      label={t('branches.single')}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      helperText={helperText}
    >
      <MenuItem value="">
        <em>{t('common.none')}</em>
      </MenuItem>
      {nodes.map(({ branch, depth }) => (
        <MenuItem key={branch.id} value={branch.id} sx={{ gap: 1, pl: 2 + (depth - 1) * 2.5 }}>
          <BranchDot color={branch.color} />
          {branch.name}
        </MenuItem>
      ))}
    </TextField>
  );
}
