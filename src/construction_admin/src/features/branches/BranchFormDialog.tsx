import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControlLabel,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Branch, BranchInput, BranchKind } from '../../api/types';
import { branchKinds } from '../../api/types';
import { isSuperAdmin } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { COUNTRIES, countryLabel, resolveCountryCode } from '../../data/countries';
import { useT } from '../../i18n/useI18n';
import { BranchDot } from './BranchDot';
import { useEveryEmployeeQuery } from '../employees/useEmployees';
import { allowedParents } from './branchTree';
import { useBranchesQuery, useCreateBranch, useUpdateBranch } from './useBranches';

/** The palette offered for a unit's dot; any #RRGGBB the API accepts, these are just the quick picks. */
const SWATCHES = ['#3457D5', '#0F8A5F', '#C2410C', '#7C3AED', '#0E7490', '#BE185D', '#4D7C0F', '#525252'];

interface FormState {
  name: string;
  color: string;
  isActive: boolean;
  kind: BranchKind;
  legalName: string;
  address: string;
  city: string;
  postalCode: string;
  country: string;
  taxId: string;
  registrationNumber: string;
  vatNumber: string;
  ownerName: string;
  contactPerson: string;
  phone: string;
  email: string;
  note: string;
  parentBranchId: string;
  headEmployeeId: string;
}

const emptyState: FormState = {
  name: '',
  color: SWATCHES[0],
  isActive: true,
  kind: 'LegalEntity',
  legalName: '',
  address: '',
  city: '',
  postalCode: '',
  country: '',
  taxId: '',
  registrationNumber: '',
  vatNumber: '',
  ownerName: '',
  contactPerson: '',
  phone: '',
  email: '',
  note: '',
  parentBranchId: '',
  headEmployeeId: '',
};

function fromBranch(branch: Branch): FormState {
  return {
    name: branch.name,
    color: branch.color,
    isActive: branch.isActive,
    kind: branch.kind,
    legalName: branch.legalName ?? '',
    address: branch.address ?? '',
    city: branch.city ?? '',
    postalCode: branch.postalCode ?? '',
    country: countryLabel(branch.countryCode),
    taxId: branch.taxId ?? '',
    registrationNumber: branch.registrationNumber ?? '',
    vatNumber: branch.vatNumber ?? '',
    ownerName: branch.ownerName ?? '',
    contactPerson: branch.contactPerson ?? '',
    phone: branch.phone ?? '',
    email: branch.email ?? '',
    note: branch.note ?? '',
    parentBranchId: branch.parentBranchId ?? '',
    headEmployeeId: branch.headEmployeeId ?? '',
  };
}

const orNull = (value: string) => value.trim() || null;

function toInput(state: FormState): BranchInput {
  return {
    name: state.name.trim(),
    color: state.color,
    isActive: state.isActive,
    kind: state.kind,
    legalName: orNull(state.legalName),
    address: orNull(state.address),
    city: orNull(state.city),
    postalCode: orNull(state.postalCode),
    countryCode: resolveCountryCode(state.country),
    taxId: orNull(state.taxId),
    registrationNumber: orNull(state.registrationNumber),
    vatNumber: orNull(state.vatNumber),
    ownerName: orNull(state.ownerName),
    contactPerson: orNull(state.contactPerson),
    phone: orNull(state.phone),
    email: orNull(state.email),
    note: orNull(state.note),
    parentBranchId: state.parentBranchId || null,
    headEmployeeId: state.headEmployeeId || null,
  };
}

/**
 * Creates or edits a business unit (poslovna jedinica): its name and colour, and the data of the
 * entity it stands for — so documents and exports that name the unit can show who issues them.
 * The tax numbers are shown only to who may see them and changed only by a SuperAdmin.
 */
export function BranchFormDialog({
  target,
  onClose,
}: {
  /** The unit to edit, 'new' to add one, or null while closed. */
  target: Branch | 'new' | null;
  onClose: () => void;
}) {
  const t = useT();
  const { user } = useAuth();
  const createBranch = useCreateBranch();
  const updateBranch = useUpdateBranch();
  const { data: branches } = useBranchesQuery();
  const { data: employees } = useEveryEmployeeQuery();
  const [state, setState] = useState<FormState>(emptyState);
  const [error, setError] = useState<string | null>(null);

  const canEditTax = isSuperAdmin(user);
  const canViewTax = canEditTax || !!user?.canViewCustomerTaxDetails;
  const isEdit = target !== null && target !== 'new';

  useEffect(() => {
    if (target !== null) {
      setState(target === 'new' ? emptyState : fromBranch(target));
      setError(null);
    }
  }, [target]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setState((previous) => ({ ...previous, [key]: value }));

  const text = (key: keyof FormState, label: string, extra?: { disabled?: boolean; multiline?: boolean }) => (
    <TextField
      label={label}
      value={state[key] as string}
      onChange={(event) => set(key, event.target.value as never)}
      fullWidth
      disabled={extra?.disabled}
      multiline={extra?.multiline}
      minRows={extra?.multiline ? 2 : undefined}
    />
  );

  const submit = async () => {
    const input = toInput(state);

    if (!input.name) {
      setError(t('validation.required'));
      return;
    }

    try {
      if (isEdit) await updateBranch.mutateAsync({ id: target.id, input });
      else await createBranch.mutateAsync(input);
      onClose();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  const section = (title: string) => (
    <Grid size={12}>
      <Divider sx={{ mb: 1 }} />
      <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
        {title}
      </Typography>
    </Grid>
  );

  return (
    <Dialog open={target !== null} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{isEdit ? t('branches.edit') : t('branches.add')}</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ pt: 1 }}>
          {error && (
            <Grid size={12}>
              <Alert severity="error">{error}</Alert>
            </Grid>
          )}

          <Grid size={{ xs: 12, sm: 6 }}>{text('name', t('branches.name'))}</Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('branches.kind')}
              value={state.kind}
              onChange={(event) => set('kind', event.target.value as BranchKind)}
              helperText={state.kind === 'RepresentativeOffice' ? t('branches.kindOfficeHint') : undefined}
            >
              {branchKinds.map((kind) => (
                <MenuItem key={kind} value={kind}>
                  {t(kind === 'LegalEntity' ? 'branches.kindEntity' : 'branches.kindOffice')}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              select
              fullWidth
              label={t('branches.parent')}
              value={state.parentBranchId}
              onChange={(event) => set('parentBranchId', event.target.value)}
              helperText={t('branches.parentHint')}
            >
              <MenuItem value="">{t('branches.parentNone')}</MenuItem>
              {allowedParents(branches ?? [], isEdit ? target.id : null).map((parent) => (
                <MenuItem key={parent.id} value={parent.id}>
                  {parent.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <Autocomplete
              fullWidth
              options={employees?.items ?? []}
              getOptionLabel={(employee) => employee.fullName}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={(employees?.items ?? []).find((e) => e.id === state.headEmployeeId) ?? null}
              onChange={(_event, value) => set('headEmployeeId', value?.id ?? '')}
              renderInput={(params) => (
                <TextField {...params} label={t('branches.head')} helperText={t('branches.headHint')} />
              )}
            />
          </Grid>
          <Grid size={12}>
            <Box>
              <Box sx={{ mb: 1, fontSize: 13, color: 'text.secondary' }}>{t('branches.color')}</Box>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
                {SWATCHES.map((color) => (
                  <IconButton
                    key={color}
                    aria-label={color}
                    aria-pressed={state.color === color}
                    onClick={() => set('color', color)}
                    sx={{ border: 2, borderColor: state.color === color ? 'text.primary' : 'transparent' }}
                  >
                    <BranchDot color={color} size={16} />
                  </IconButton>
                ))}
                <FormControlLabel
                  sx={{ ml: 'auto' }}
                  control={<Switch checked={state.isActive} onChange={(e) => set('isActive', e.target.checked)} />}
                  label={t('branches.active')}
                />
              </Stack>
            </Box>
          </Grid>

          {section(t('branches.sectionAddress'))}
          <Grid size={12}>{text('legalName', t('branches.legalName'))}</Grid>
          <Grid size={{ xs: 12, sm: 6 }}>{text('address', t('branches.address'))}</Grid>
          <Grid size={{ xs: 12, sm: 3 }}>{text('city', t('branches.city'))}</Grid>
          <Grid size={{ xs: 12, sm: 3 }}>{text('postalCode', t('branches.postalCode'))}</Grid>
          <Grid size={12}>
            <Autocomplete
              freeSolo
              fullWidth
              options={COUNTRIES.map((c) => c.label)}
              inputValue={state.country}
              onInputChange={(_event, value) => set('country', value)}
              renderInput={(params) => <TextField {...params} label={t('branches.country')} />}
            />
          </Grid>

          {canViewTax && (
            <>
              {section(t('customers.taxSection'))}
              {!canEditTax && (
                <Grid size={12}>
                  <Typography variant="body2" color="text.secondary">
                    {t('customers.taxSectionReadOnlyHint')}
                  </Typography>
                </Grid>
              )}
              {state.kind === 'RepresentativeOffice' && (
                <Grid size={12}>
                  <Typography variant="body2" color="text.secondary">
                    {t('branches.taxOfficeHint')}
                  </Typography>
                </Grid>
              )}
              <Grid size={{ xs: 12, sm: 4 }}>
                {text('taxId', t('customers.taxId'), { disabled: !canEditTax })}
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                {text('registrationNumber', t('customers.registrationNumber'), { disabled: !canEditTax })}
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                {text('vatNumber', t('customers.vatNumber'), { disabled: !canEditTax })}
              </Grid>
            </>
          )}

          {section(t('branches.sectionContact'))}
          <Grid size={{ xs: 12, sm: 6 }}>{text('ownerName', t('branches.owner'))}</Grid>
          <Grid size={{ xs: 12, sm: 6 }}>{text('contactPerson', t('branches.contactPerson'))}</Grid>
          <Grid size={{ xs: 12, sm: 6 }}>{text('phone', t('branches.phone'))}</Grid>
          <Grid size={{ xs: 12, sm: 6 }}>{text('email', t('branches.email'))}</Grid>
          <Grid size={12}>{text('note', t('branches.note'), { multiline: true })}</Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          onClick={() => void submit()}
          disabled={createBranch.isPending || updateBranch.isPending}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
