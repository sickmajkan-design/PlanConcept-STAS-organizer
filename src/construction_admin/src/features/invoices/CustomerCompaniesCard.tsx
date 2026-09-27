import { AddOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { CustomerCompany } from '../../api/invoices';
import { canManageInvoices } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useT } from '../../i18n/useI18n';
import {
  useCreateCustomerCompany,
  useCustomerCompaniesQuery,
  useUpdateCustomerCompany,
} from './useInvoices';

/**
 * The legal entities of a client. Most clients have none — a whole client is invoiced as one —
 * and this only matters once one is added. A company with an invoice on it can never be deleted,
 * only switched off, so it keeps naming that invoice.
 */
export function CustomerCompaniesCard({ customerId }: { customerId: string }) {
  const t = useT();
  const { user } = useAuth();
  const allowed = canManageInvoices(user);
  const companies = useCustomerCompaniesQuery(customerId);
  const [editing, setEditing] = useState<CustomerCompany | 'new' | null>(null);

  if (!allowed) {
    return null;
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('customerCompanies.title')}
        </Typography>
        <Button size="small" startIcon={<AddOutlined />} onClick={() => setEditing('new')}>
          {t('customerCompanies.add')}
        </Button>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {t('customerCompanies.help')}
      </Typography>

      {(companies.data ?? []).length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('customerCompanies.none')}
        </Typography>
      ) : (
        <Stack spacing={1}>
          {(companies.data ?? []).map((company) => (
            <Stack
              key={company.id}
              direction="row"
              spacing={1}
              sx={{ alignItems: 'center', justifyContent: 'space-between', opacity: company.isActive ? 1 : 0.5 }}
            >
              <Box>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {company.name}
                  {!company.isActive && ` (${t('customerCompanies.inactive')})`}
                </Typography>
                {company.address && (
                  <Typography variant="caption" color="text.secondary">
                    {company.address}
                  </Typography>
                )}
              </Box>
              <Button size="small" onClick={() => setEditing(company)}>
                {t('common.edit')}
              </Button>
            </Stack>
          ))}
        </Stack>
      )}

      <CompanyDialog
        customerId={customerId}
        company={editing === 'new' ? null : editing}
        open={editing !== null}
        onClose={() => setEditing(null)}
      />
    </Paper>
  );
}

function CompanyDialog({
  customerId,
  company,
  open,
  onClose,
}: {
  customerId: string;
  company: CustomerCompany | null;
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const create = useCreateCustomerCompany(customerId);
  const update = useUpdateCustomerCompany();
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setName(company?.name ?? '');
      setAddress(company?.address ?? '');
      setIsActive(company?.isActive ?? true);
      setError(null);
    }
  }, [open, company]);

  const submit = async () => {
    setError(null);

    if (!name.trim()) {
      setError(t('customerCompanies.nameRequired'));
      return;
    }

    try {
      if (company) {
        await update.mutateAsync({
          id: company.id,
          input: { name: name.trim(), address: address.trim() || null, isActive },
        });
      } else {
        await create.mutateAsync({ name: name.trim(), address: address.trim() || null });
      }
      onClose();
    } catch (failure) {
      setError(toApiError(failure).message);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{company ? t('customerCompanies.editTitle') : t('customerCompanies.newTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label={t('customerCompanies.name')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            autoFocus
            fullWidth
          />
          <TextField
            label={t('customerCompanies.address')}
            value={address}
            onChange={(event) => setAddress(event.target.value)}
            fullWidth
          />
          {company && (
            <FormControlLabel
              control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />}
              label={t('customerCompanies.active')}
            />
          )}
          {company && !isActive && company.invoiceCount > 0 && (
            <Alert severity="info">{t('customerCompanies.keptForInvoices', { count: company.invoiceCount })}</Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={create.isPending || update.isPending}
          onClick={() => void submit()}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
