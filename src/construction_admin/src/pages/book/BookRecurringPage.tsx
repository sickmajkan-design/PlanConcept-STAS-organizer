import {
  Alert,
  Box,
  Button,
  Chip,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { bookApi } from '../../api/book';
import {
  recurringApi,
  recurringFrequencies,
  type RecurringExpense,
  type RecurringFrequency,
} from '../../api/recurring';
import { PageHeader } from '../../components/PageHeader';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';
import { companyNow } from '../../utils/companyTime';

const recurringKey = ['book', 'recurring'] as const;

/** The countries fixed costs are kept per; stored on the template as a label. */
const countries = ['SI', 'DE', 'HR'] as const;

function monthIndex(): { year: number; month: number } {
  const now = companyNow();
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
}

/**
 * Costs that come back on their own: a template is written once and the book gets its line every month. The
 * mockup's order is kept: the templates, the form for a new one, then what a month would make from them.
 */
export function BookRecurringPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const projects = useAllProjectsQuery();

  const current = monthIndex();
  const [previewMonth, setPreviewMonth] = useState(() => {
    const next = new Date(current.year, current.month, 1);
    return { year: next.getFullYear(), month: next.getMonth() + 1 };
  });

  // The new template
  const [name, setName] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [where, setWhere] = useState('');
  const [frequency, setFrequency] = useState<RecurringFrequency>('Monthly');
  const [startMonth, setStartMonth] = useState(current.month);
  const [day, setDay] = useState('1');
  const [amount, setAmount] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const templates = useQuery({ queryKey: recurringKey, queryFn: () => recurringApi.list() });
  const preview = useQuery({
    queryKey: [...recurringKey, 'preview', previewMonth.year, previewMonth.month] as const,
    queryFn: () => recurringApi.preview(previewMonth.year, previewMonth.month),
  });
  const categories = useQuery({
    queryKey: ['book', 'categories', 'active'] as const,
    queryFn: () => bookApi.categories.list(false),
    staleTime: 60_000,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['book'] });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const whereOf = (template: RecurringExpense): string => {
    if (template.projectName) return template.projectName;
    if (template.countryCode) return t(`book.country.${template.countryCode}` as MessageKey);
    return t('book.entries.noProject');
  };

  const inputOf = (template: RecurringExpense, patch: Partial<RecurringExpense>) => ({
    name: template.name,
    categoryId: template.categoryId,
    amount: template.amount,
    frequency: template.frequency,
    startMonth: template.startMonth,
    dayOfMonth: template.dayOfMonth,
    projectId: template.projectId,
    countryCode: template.countryCode,
    endDate: template.endDate,
    branchId: template.branchId,
    ...patch,
  });

  const change = useMutation({
    mutationFn: (variables: { template: RecurringExpense; patch: Partial<RecurringExpense> }) =>
      recurringApi.update(variables.template.id, inputOf(variables.template, variables.patch)),
    onSuccess: refresh,
  });

  const toggle = useMutation({
    mutationFn: (template: RecurringExpense) => recurringApi.toggle(template.id),
    onSuccess: refresh,
  });

  const create = useMutation({
    mutationFn: () => {
      const [kind, id] = where.split(':');

      return recurringApi.create({
        name: name.trim(),
        categoryId,
        amount: Number(amount.replace(/\./g, '').replace(',', '.')),
        frequency,
        startMonth: `${current.year}-${String(startMonth).padStart(2, '0')}-01`,
        dayOfMonth: Number(day),
        projectId: kind === 'project' ? id : null,
        countryCode: kind === 'country' ? id : null,
      });
    },
    onSuccess: () => {
      setName('');
      setAmount('');
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(toApiError(error).message),
  });

  const generate = useMutation({
    mutationFn: () => recurringApi.generate(previewMonth.year, previewMonth.month),
    onSuccess: refresh,
  });

  const submit = () => {
    const parsed = Number(amount.replace(/\./g, '').replace(',', '.'));
    const dayNumber = Number(day);

    if (!name.trim() || !categoryId || !(parsed > 0) || !(dayNumber >= 1 && dayNumber <= 28)) {
      setFormError(t('book.recurring.invalid'));
      return;
    }

    setFormError(null);
    create.mutate();
  };

  const previewRows = preview.data ?? [];
  const previewTotal = previewRows.reduce((sum, row) => sum + row.amount, 0);
  const missing = previewRows.filter((row) => !row.alreadyMade).length;

  // This month and the next five, as the buttons above the preview.
  const monthButtons = Array.from({ length: 6 }, (_, index) => {
    const date = new Date(current.year, current.month - 1 + index, 1);
    return { year: date.getFullYear(), month: date.getMonth() + 1 };
  });

  const monthName = (m: { year: number; month: number }) =>
    new Date(m.year, m.month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
      month: 'long',
      year: 'numeric',
    });

  const error = templates.error ?? preview.error ?? change.error ?? toggle.error ?? generate.error;

  return (
    <Stack spacing={2}>
      <PageHeader title={t('book.recurring.title')} description={t('book.recurring.subtitle')} />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <HowItWorks id="recurring">
        <HowLine>{t('book.recurring.how1')}</HowLine>
        <HowLine>{t('book.recurring.how2')}</HowLine>
        <HowLine>{t('book.recurring.how3')}</HowLine>
        <HowLine>{t('book.recurring.how4')}</HowLine>
        <HowLine>{t('book.recurring.how5')}</HowLine>
      </HowItWorks>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.recurring.templates')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('recurring.col.name')}</TableCell>
                <TableCell>{t('recurring.col.category')}</TableCell>
                <TableCell>{t('book.recurring.where')}</TableCell>
                <TableCell>{t('recurring.col.frequency')}</TableCell>
                <TableCell>{t('recurring.col.day')}</TableCell>
                <TableCell align="right">{t('book.recurring.amountEur')}</TableCell>
                <TableCell>{t('book.col.status')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(templates.data ?? []).map((template) => (
                <TableRow key={template.id} hover sx={{ opacity: template.isActive ? 1 : 0.55 }}>
                  <TableCell>{template.name}</TableCell>
                  <TableCell>{template.categoryName}</TableCell>
                  <TableCell>{whereOf(template)}</TableCell>
                  <TableCell>{t(`recurring.frequency.${template.frequency}` as MessageKey)}</TableCell>
                  <TableCell>{template.dayOfMonth}.</TableCell>
                  <TableCell align="right">
                    <AmountField
                      value={template.amount}
                      onCommit={(value) => change.mutate({ template, patch: { amount: value } })}
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      variant="outlined"
                      color={template.isActive ? 'success' : 'default'}
                      label={template.isActive ? t('book.recurring.active') : t('book.recurring.off')}
                    />
                  </TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                    <Button size="small" loading={toggle.isPending} onClick={() => toggle.mutate(template)}>
                      {template.isActive ? t('book.recurring.switchOff') : t('book.recurring.switchOn')}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {!templates.isLoading && (templates.data?.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography color="text.secondary" sx={{ py: 2 }}>
                      {t('recurring.empty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
          {t('book.recurring.newTemplate')}
        </Typography>
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <TextField
            size="small"
            label={t('recurring.col.name')}
            placeholder={t('book.recurring.namePlaceholder')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            sx={{ minWidth: 200 }}
          />
          <TextField
            select
            size="small"
            label={t('recurring.col.category')}
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            sx={{ minWidth: 180 }}
          >
            {(categories.data ?? [])
              .filter((category) => category.kind === 'Cost')
              .map((category) => (
                <MenuItem key={category.id} value={category.id}>
                  {category.name}
                </MenuItem>
              ))}
          </TextField>
          <TextField
            select
            size="small"
            label={t('book.recurring.where')}
            value={where}
            onChange={(event) => setWhere(event.target.value)}
            sx={{ minWidth: 190 }}
          >
            <MenuItem value="">
              <em>{t('book.entries.noProject')}</em>
            </MenuItem>
            {(projects.data?.items ?? []).map((project) => (
              <MenuItem key={project.id} value={`project:${project.id}`}>
                {project.name}
              </MenuItem>
            ))}
            {countries.map((code) => (
              <MenuItem key={code} value={`country:${code}`}>
                {t(`book.country.${code}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label={t('recurring.col.frequency')}
            value={frequency}
            onChange={(event) => setFrequency(event.target.value as RecurringFrequency)}
            sx={{ minWidth: 150 }}
          >
            {recurringFrequencies.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`recurring.frequency.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label={t('book.recurring.startsIn')}
            value={startMonth}
            onChange={(event) => setStartMonth(Number(event.target.value))}
            sx={{ minWidth: 150 }}
          >
            {Array.from({ length: 12 }, (_, index) => (
              <MenuItem key={index} value={index + 1}>
                {new Date(current.year, index, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
                  month: 'long',
                })}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            label={t('recurring.col.day')}
            value={day}
            onChange={(event) => setDay(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'numeric' } }}
            sx={{ maxWidth: 110 }}
          />
          <TextField
            size="small"
            label={t('book.recurring.amountEur')}
            placeholder="500,00"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            sx={{ maxWidth: 150 }}
          />
          <Button variant="contained" loading={create.isPending} onClick={submit}>
            {t('book.recurring.addTemplate')}
          </Button>
        </Stack>
        {formError && (
          <Alert severity="error" sx={{ mt: 1.5 }}>
            {formError}
          </Alert>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.recurring.preview')}
        </Typography>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5 }}>
          {monthButtons.map((m) => {
            const selected = m.year === previewMonth.year && m.month === previewMonth.month;

            return (
              <Chip
                key={`${m.year}-${m.month}`}
                clickable
                color={selected ? 'primary' : 'default'}
                variant={selected ? 'filled' : 'outlined'}
                label={monthName(m)}
                onClick={() => setPreviewMonth(m)}
              />
            );
          })}
        </Stack>

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.col.date')}</TableCell>
                <TableCell>{t('book.recurring.what')}</TableCell>
                <TableCell>{t('book.col.category')}</TableCell>
                <TableCell>{t('book.recurring.where')}</TableCell>
                <TableCell align="right">{t('book.col.amount')}</TableCell>
                <TableCell>{t('book.col.status')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {previewRows.map((row) => {
                const template = (templates.data ?? []).find((x) => x.id === row.recurringExpenseId);

                return (
                  <TableRow key={row.recurringExpenseId}>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(row.dueDate)}</TableCell>
                    <TableCell>{row.name}</TableCell>
                    <TableCell>{row.categoryName}</TableCell>
                    <TableCell>{template ? whereOf(template) : '—'}</TableCell>
                    <TableCell align="right">− {formatMoney(row.amount, locale)}</TableCell>
                    <TableCell>
                      {row.alreadyMade ? (
                        <Chip size="small" variant="outlined" label={t('recurring.made')} />
                      ) : (
                        <Chip size="small" color="warning" variant="outlined" label={t('recurring.notMade')} />
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
              {!preview.isLoading && previewRows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography color="text.secondary" sx={{ py: 1 }}>
                      {t('recurring.previewEmpty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>

        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ mt: 1.5, justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {t('book.recurring.total', {
              month: monthName(previewMonth),
              count: previewRows.length,
              amount: formatMoney(previewTotal, locale),
            })}
          </Typography>
          <Button
            variant="contained"
            disabled={missing === 0}
            loading={generate.isPending}
            onClick={() => generate.mutate()}
          >
            {t('book.recurring.make', { month: monthName(previewMonth) })}
          </Button>
        </Stack>
        {generate.data !== undefined && (
          <Alert severity="success" sx={{ mt: 1.5 }}>
            {t('recurring.generated', { count: generate.data })}
          </Alert>
        )}
      </Paper>
    </Stack>
  );
}

/** An amount typed in place and saved when the field is left. */
function AmountField({ value, onCommit }: { value: number; onCommit: (value: number) => void }) {
  const [text, setText] = useState(String(value));

  useEffect(() => {
    setText(String(value));
  }, [value]);

  return (
    <TextField
      size="small"
      value={text}
      onChange={(event) => setText(event.target.value)}
      onBlur={() => {
        const parsed = Number(text.replace(/\./g, '').replace(',', '.'));

        if (!Number.isFinite(parsed) || parsed <= 0) {
          setText(String(value));
          return;
        }

        if (parsed !== value) onCommit(parsed);
      }}
      slotProps={{ htmlInput: { inputMode: 'decimal', style: { textAlign: 'right' } } }}
      sx={{ maxWidth: 130 }}
    />
  );
}
