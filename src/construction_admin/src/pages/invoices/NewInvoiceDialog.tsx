import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { useCreateInvoice, useCustomerCompaniesQuery } from '../../features/invoices/useInvoices';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useI18n } from '../../i18n/useI18n';
import { formatMoney } from '../../utils/formatting';

const today = () => new Date().toISOString().slice(0, 10);

/** Rounds to the cent so that comparing parts with the whole is not thrown by floating point. */
const cents = (value: number) => Math.round(value * 100);

/**
 * Records an invoice the firm issued to a client for a site, on one company of the client or
 * split among several. The parts always add up to the whole: split evenly (to the cent) or typed.
 */
export function NewInvoiceDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t, locale } = useI18n();
  const create = useCreateInvoice();
  const projects = useAllProjectsQuery();

  const [projectId, setProjectId] = useState('');
  const [number, setNumber] = useState('');
  const [issueDate, setIssueDate] = useState(today());
  const [dueDate, setDueDate] = useState('');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [monthValue, setMonthValue] = useState(''); // '' = the month it was issued; else 'YYYY-M'
  const [chosen, setChosen] = useState<string[]>([]);
  const [evenly, setEvenly] = useState(true);
  const [typed, setTyped] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const project = (projects.data?.items ?? []).find((item) => item.id === projectId);
  const companiesQuery = useCustomerCompaniesQuery(project?.customerId);
  const activeCompanies = useMemo(
    () => (companiesQuery.data ?? []).filter((company) => company.isActive),
    [companiesQuery.data],
  );
  const hasCompanies = (companiesQuery.data ?? []).length > 0;

  const total = Number(amount);
  const partsSum = chosen.reduce((sum, id) => sum + Number(typed[id] || 0), 0);
  const difference = cents(total) - cents(partsSum);

  const partsOk = !hasCompanies
    ? true
    : chosen.length > 0 && (evenly || difference === 0);

  const valid =
    projectId !== '' &&
    number.trim().length > 0 &&
    Number.isFinite(total) &&
    total !== 0 &&
    partsOk;

  const toggle = (id: string) =>
    setChosen((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]));

  const reset = () => {
    setProjectId('');
    setNumber('');
    setDueDate('');
    setAmount('');
    setDescription('');
    setMonthValue('');
    setChosen([]);
    setTyped({});
    setEvenly(true);
    setError(null);
  };

  const submit = async () => {
    setError(null);

    const [year, month] = monthValue ? monthValue.split('-').map(Number) : [null, null];

    try {
      await create.mutateAsync({
        projectId,
        number: number.trim(),
        issueDate,
        dueDate: dueDate || null,
        description: description.trim() || null,
        amount: total,
        payrollYear: year,
        payrollMonth: month,
        ...(hasCompanies
          ? evenly
            ? { companyIds: chosen }
            : { shares: chosen.map((id) => ({ customerCompanyId: id, amount: Number(typed[id] || 0) })) }
          : {}),
      });

      reset();
      onClose();
    } catch (failure) {
      setError(toApiError(failure).message);
    }
  };

  const monthChoices = useMemo(() => {
    const now = new Date();
    const list: { value: string; label: string }[] = [];

    for (let offset = -3; offset <= 2; offset++) {
      const date = new Date(now.getFullYear(), now.getMonth() + offset, 1);
      list.push({
        value: `${date.getFullYear()}-${date.getMonth() + 1}`,
        label: `${String(date.getMonth() + 1).padStart(2, '0')}.${date.getFullYear()}`,
      });
    }

    return list;
  }, []);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('invoices.newTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t('invoices.newHelp')}
          </Typography>

          <TextField
            select
            label={t('invoices.project')}
            value={projectId}
            onChange={(event) => {
              setProjectId(event.target.value);
              setChosen([]);
              setTyped({});
            }}
          >
            {(projects.data?.items ?? []).map((item) => (
              <MenuItem key={item.id} value={item.id}>
                {item.name}
                {item.customerName ? ` · ${item.customerName}` : ''}
              </MenuItem>
            ))}
          </TextField>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <TextField
              label={t('invoices.number')}
              value={number}
              onChange={(event) => setNumber(event.target.value)}
              sx={{ flex: 1 }}
            />
            <TextField
              label={t('invoices.amount')}
              type="number"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              helperText={t('invoices.amountHelp')}
              sx={{ flex: 1 }}
              slotProps={{ htmlInput: { step: '0.01' } }}
            />
          </Stack>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <TextField
              label={t('invoices.issueDate')}
              type="date"
              value={issueDate}
              onChange={(event) => setIssueDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ flex: 1 }}
            />
            <TextField
              label={t('invoices.dueDate')}
              type="date"
              value={dueDate}
              onChange={(event) => setDueDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ flex: 1 }}
            />
          </Stack>

          <TextField
            select
            label={t('invoices.payrollMonth')}
            value={monthValue}
            onChange={(event) => setMonthValue(event.target.value)}
            helperText={t('invoices.payrollMonthHelp')}
          >
            <MenuItem value="">{t('invoices.sameAsIssued')}</MenuItem>
            {monthChoices.map((choice) => (
              <MenuItem key={choice.value} value={choice.value}>
                {choice.label}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            label={t('invoices.description')}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />

          {projectId && !companiesQuery.isLoading && !hasCompanies && (
            <Alert severity="info">{t('invoices.wholeClient')}</Alert>
          )}

          {hasCompanies && (
            <Stack spacing={1}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {t('invoices.companies')}
              </Typography>

              {activeCompanies.map((company) => (
                <Stack key={company.id} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <FormControlLabel
                    sx={{ flex: 1, m: 0 }}
                    control={
                      <Checkbox checked={chosen.includes(company.id)} onChange={() => toggle(company.id)} />
                    }
                    label={company.name}
                  />
                  {!evenly && chosen.includes(company.id) && (
                    <TextField
                      size="small"
                      type="number"
                      label={t('invoices.part')}
                      value={typed[company.id] ?? ''}
                      onChange={(event) => setTyped((current) => ({ ...current, [company.id]: event.target.value }))}
                      sx={{ width: 140 }}
                      slotProps={{ htmlInput: { step: '0.01' } }}
                    />
                  )}
                </Stack>
              ))}

              <FormControlLabel
                control={<Switch checked={evenly} onChange={(event) => setEvenly(event.target.checked)} />}
                label={t('invoices.splitEvenly')}
              />

              {!evenly && chosen.length > 0 && (
                <Alert severity={difference === 0 ? 'success' : 'warning'}>
                  {difference === 0
                    ? t('invoices.partsMatch')
                    : t('invoices.partsDiffer', { difference: formatMoney(difference / 100, locale) })}
                </Alert>
              )}
            </Stack>
          )}

          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid || create.isPending} onClick={() => void submit()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
