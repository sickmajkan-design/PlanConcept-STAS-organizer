import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { customersApi } from '../../api/customers';
import { projectsApi } from '../../api/projects';
import type { Customer } from '../../api/types';
import { CountryFlag } from '../../components/CountryFlag';
import { countryLabel } from '../../data/countries';
import { useT } from '../../i18n/useI18n';
import { fetchAllPages } from '../dashboard/dashboardData';

interface Suggestion {
  customer: Customer;
  /** The one country all the customer's sites are in; null when there is none or there are several. */
  code: string | null;
  codes: string[];
}

/**
 * For the customers with no country: the country of their sites, when all of them are in the same one. Nothing is
 * written until it is confirmed, because the country of a site is not always the country of its customer.
 */
export function CountrySuggestionDialog({ onClose }: { onClose: () => void }) {
  const t = useT();
  const queryClient = useQueryClient();
  const [chosen, setChosen] = useState<Record<string, boolean>>({});
  const [saving, setSaving] = useState(false);
  const [failed, setFailed] = useState<string[]>([]);

  const { data, isLoading } = useQuery({
    queryKey: ['customer-country-suggestions'],
    queryFn: async (): Promise<Suggestion[]> => {
      const customers = (await fetchAllPages((pageNumber) => customersApi.list({ pageNumber, pageSize: 100, country: 'none' }))).items;
      const projects = (await fetchAllPages((pageNumber) => projectsApi.list({ pageNumber, pageSize: 500 }))).items;

      const sites = new Map<string, Set<string>>();
      for (const p of projects) {
        if (!p.customerId || !p.countryCode) continue;
        const set = sites.get(p.customerId) ?? new Set<string>();
        set.add(p.countryCode.toUpperCase());
        sites.set(p.customerId, set);
      }

      return customers.map((customer) => {
        const codes = [...(sites.get(customer.id) ?? [])];
        return { customer, codes, code: codes.length === 1 ? codes[0] : null };
      });
    },
  });

  const rows = data ?? [];
  const isOn = (s: Suggestion) => (s.code ? chosen[s.customer.id] ?? true : false);
  const count = rows.filter(isOn).length;

  const apply = async () => {
    setSaving(true);
    const errors: string[] = [];

    for (const s of rows.filter(isOn)) {
      const c = s.customer;
      try {
        // The whole record goes back: the tax details are cleared by an update that leaves them out.
        await customersApi.update(c.id, {
          name: c.name,
          countryCode: s.code,
          contactPerson: c.contactPerson,
          phone: c.phone,
          email: c.email,
          note: c.note,
          taxId: c.taxId,
          registrationNumber: c.registrationNumber,
          vatNumber: c.vatNumber,
        });
      } catch (error) {
        errors.push(`${c.name}: ${toApiError(error).message}`);
      }
    }

    setSaving(false);
    setFailed(errors);
    await queryClient.invalidateQueries();
    if (errors.length === 0) onClose();
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{t('customers.suggestTitle')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('customers.suggestHint')}
        </Typography>
        {failed.length > 0 && (
          <Alert severity="error" sx={{ mb: 1.5 }}>
            {failed.join(' · ')}
          </Alert>
        )}
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox" />
                <TableCell>{t('customers.name')}</TableCell>
                <TableCell>{t('customers.suggestedCountry')}</TableCell>
                <TableCell>{t('customers.suggestWhy')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((s) => (
                <TableRow key={s.customer.id}>
                  <TableCell padding="checkbox">
                    <Checkbox
                      disabled={!s.code}
                      checked={isOn(s)}
                      onChange={(e) => setChosen((c) => ({ ...c, [s.customer.id]: e.target.checked }))}
                      slotProps={{ input: { 'aria-label': s.customer.name } }}
                    />
                  </TableCell>
                  <TableCell>{s.customer.name}</TableCell>
                  <TableCell>
                    {s.code ? (
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <CountryFlag code={s.code} />
                        <span>{countryLabel(s.code)}</span>
                      </Stack>
                    ) : (
                      '—'
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {s.codes.length === 0
                        ? t('customers.suggestNoSites')
                        : s.codes.length === 1
                          ? t('customers.suggestOneCountry')
                          : t('customers.suggestSeveral', { codes: s.codes.join(', ') })}
                    </Typography>
                  </TableCell>
                </TableRow>
              ))}
              {!isLoading && rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4}>{t('customers.suggestNone')}</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={count === 0} loading={saving} onClick={() => void apply()}>
          {t('customers.suggestApply', { count })}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
