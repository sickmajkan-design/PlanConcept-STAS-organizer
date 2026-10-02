import { FormControl, FormHelperText, InputLabel, MenuItem, Select } from '@mui/material';
import { Controller, type Control, type FieldValues, type Path } from 'react-hook-form';

import { useT } from '../../i18n/useI18n';
import { BranchDot } from './BranchDot';
import { useBranchesQuery } from './useBranches';

/**
 * A form's business-unit (poslovna jedinica) picker. The form field holds the unit's id, or ''
 * for none. A unit that has been switched off is still offered while a record already uses it.
 */
export function BranchSelectField<T extends FieldValues>({
  control,
  name,
  helperText,
}: {
  control: Control<T>;
  name: Path<T>;
  /** What "none" means on this form, e.g. that the record follows its project. */
  helperText?: string;
}) {
  const t = useT();
  const { data: branches } = useBranchesQuery();
  const labelId = `${String(name)}-branch-label`;

  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl fullWidth>
          <InputLabel id={labelId}>{t('branches.single')}</InputLabel>
          <Select {...field} value={field.value ?? ''} labelId={labelId} label={t('branches.single')}>
            <MenuItem value="">
              <em>{t('common.none')}</em>
            </MenuItem>
            {(branches ?? [])
              .filter((b) => b.isActive || b.id === field.value)
              .map((branch) => (
                <MenuItem key={branch.id} value={branch.id} sx={{ gap: 1 }}>
                  <BranchDot color={branch.color} />
                  {branch.name}
                </MenuItem>
              ))}
          </Select>
          {helperText && <FormHelperText>{helperText}</FormHelperText>}
        </FormControl>
      )}
    />
  );
}
