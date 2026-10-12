import { EditOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import { bookApi } from '../../api/book';
import {
  recurringApi,
  recurringFrequencies,
  type RecurringExpense,
  type RecurringExpenseInput,
  type RecurringFrequency,
} from '../../api/recurring';
import { PageHeader } from '../../components/PageHeader';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

const recurringKey = ['book', 'recurring'] as const;

function thisMonth(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

/**
 * Costs that come back on their own. Each template makes one entry of the book per period it falls in,
 * every day's pass or on demand, and never twice for the same period.
 */
export function BookRecurringPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<RecurringExpense | 'new' | null>(null);
  const [month, setMonth] = useState(thisMonth());

  const [year, monthNumber] = month.split('-').map(Number);

  const templates = useQuery({ queryKey: recurringKey, queryFn: () => recurringApi.list() });
  const preview = useQuery({
    queryKey: [...recurringKey, 'preview', month] as const,
    queryFn: () => recurringApi.preview(year!, monthNumber!),
    enabled: Boolean(year && monthNumber),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['book'] });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const toggle = useMutation({
    mutationFn: (template: RecurringExpense) => recurringApi.toggle(template.id),
    onSuccess: refresh,
  });

  const generate = useMutation({
    mutationFn: () => recurringApi.generate(year!, monthNumber!),
    onSuccess: refresh,
  });

  const error = templates.error ?? preview.error ?? toggle.error ?? generate.error;
  const missing = (preview.data ?? []).filter((row) => !row.alreadyMade).length;

  return (
    <Stack spacing={3}>
      <PageHeader
        title={t('recurring.title')}
        description={t('recurring.description')}
        action={{ label: t('recurring.add'), onClick: () => setEditing('new') }}
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('recurring.col.name')}</TableCell>
              <TableCell>{t('recurring.col.category')}</TableCell>
              <TableCell>{t('recurring.col.frequency')}</TableCell>
              <TableCell>{t('recurring.col.day')}</TableCell>
              <TableCell>{t('recurring.col.from')}</TableCell>
              <TableCell align="right">{t('recurring.col.amount')}</TableCell>
              <TableCell align="right">{t('recurring.col.made')}</TableCell>
              <TableCell align="center">{t('recurring.col.active')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {(templates.data ?? []).map((template) => (
              <TableRow key={template.id} hover sx={{ opacity: template.isActive ? 1 : 0.55 }}>
                <TableCell>
                  {template.name}
                  {template.projectName && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {template.projectName}
                    </Typography>
                  )}
                </TableCell>
                <TableCell>{template.categoryName}</TableCell>
                <TableCell>{t(`recurring.frequency.${template.frequency}` as MessageKey)}</TableCell>
                <TableCell>{template.dayOfMonth}.</TableCell>
                <TableCell>{formatDate(template.startMonth)}</TableCell>
                <TableCell align="right">{formatMoney(template.amount, locale)}</TableCell>
                <TableCell align="right">{template.entriesMade}</TableCell>
                <TableCell align="center">
                  <Switch
                    checked={template.isActive}
                    disabled={toggle.isPending}
                    onChange={() => toggle.mutate(template)}
                    slotProps={{ input: { 'aria-label': t('recurring.col.active') } }}
                  />
                </TableCell>
                <TableCell align="right">
                  <Tooltip title={t('common.edit')}>
                    <IconButton size="small" onClick={() => setEditing(template)} aria-label={t('common.edit')}>
                      <EditOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {!templates.isLoading && (templates.data?.length ?? 0) === 0 && (
              <TableRow>
                <TableCell colSpan={9}>
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {t('recurring.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, mb: 1 }}
        >
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('recurring.preview')}
            </Typography>
            <TextField
              type="month"
              size="small"
              value={month}
              onChange={(event) => setMonth(event.target.value)}
              slotProps={{ htmlInput: { max: thisMonth() } }}
            />
          </Stack>
          <Button
            variant="contained"
            disabled={missing === 0}
            loading={generate.isPending}
            onClick={() => generate.mutate()}
          >
            {t('recurring.generate', { count: missing })}
          </Button>
        </Stack>

        {generate.data !== undefined && (
          <Alert severity="success" sx={{ mb: 1 }}>
            {t('recurring.generated', { count: generate.data })}
          </Alert>
        )}

        <Table size="small">
          <TableBody>
            {(preview.data ?? []).map((row) => (
              <TableRow key={row.recurringExpenseId}>
                <TableCell>{row.name}</TableCell>
                <TableCell>{row.categoryName}</TableCell>
                <TableCell>{formatDate(row.dueDate)}</TableCell>
                <TableCell align="right">{formatMoney(row.amount, locale)}</TableCell>
                <TableCell>
                  {row.alreadyMade ? (
                    <Chip size="small" variant="outlined" label={t('recurring.made')} />
                  ) : (
                    <Chip size="small" color="warning" variant="outlined" label={t('recurring.notMade')} />
                  )}
                </TableCell>
              </TableRow>
            ))}
            {!preview.isLoading && (preview.data?.length ?? 0) === 0 && (
              <TableRow>
                <TableCell>
                  <Typography color="text.secondary" sx={{ py: 1 }}>
                    {t('recurring.previewEmpty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      {editing && (
        <TemplateDialog
          template={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}
    </Stack>
  );
}

function TemplateDialog({
  template,
  onClose,
  onSaved,
}: {
  template: RecurringExpense | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const projects = useAllProjectsQuery();
  const categories = useQuery({
    queryKey: ['book', 'categories', 'active'] as const,
    queryFn: () => bookApi.categories.list(false),
    staleTime: 60_000,
  });

  const [name, setName] = useState(template?.name ?? '');
  const [categoryId, setCategoryId] = useState(template?.categoryId ?? '');
  const [amount, setAmount] = useState(template ? String(template.amount) : '');
  const [frequency, setFrequency] = useState<RecurringFrequency>(template?.frequency ?? 'Monthly');
  const [start, setStart] = useState((template?.startMonth ?? `${thisMonth()}-01`).slice(0, 7));
  const [day, setDay] = useState(String(template?.dayOfMonth ?? 1));
  const [projectId, setProjectId] = useState(template?.projectId ?? '');
  const [country, setCountry] = useState(template?.countryCode ?? '');
  const [end, setEnd] = useState(template?.endDate?.slice(0, 7) ?? '');

  const parsed = Number(amount.replace(',', '.'));
  const dayNumber = Number(day);
  const valid =
    name.trim() !== ''
    && categoryId !== ''
    && Number.isFinite(parsed)
    && parsed > 0
    && Number.isInteger(dayNumber)
    && dayNumber >= 1
    && dayNumber <= 28
    && start !== '';

  const save = useMutation({
    mutationFn: () => {
      const input: RecurringExpenseInput = {
        name: name.trim(),
        categoryId,
        amount: parsed,
        frequency,
        startMonth: `${start}-01`,
        dayOfMonth: dayNumber,
        projectId: projectId || null,
        countryCode: country.trim() || null,
        endDate: end ? `${end}-01` : null,
      };
      return template ? recurringApi.update(template.id, input) : recurringApi.create(input);
    },
    onSuccess: onSaved,
  });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{template ? t('recurring.edit') : t('recurring.add')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            label={t('recurring.col.name')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 200 } }}
            autoFocus
          />
          <TextField
            select
            label={t('recurring.col.category')}
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
          >
            {(categories.data ?? [])
              .filter((category) => category.kind === 'Cost')
              .map((category) => (
                <MenuItem key={category.id} value={category.id}>
                  {category.name}
                </MenuItem>
              ))}
          </TextField>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={t('recurring.col.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              slotProps={{ htmlInput: { inputMode: 'decimal' } }}
              helperText={template ? t('recurring.amountHelp') : undefined}
              fullWidth
            />
            <TextField
              select
              label={t('recurring.col.frequency')}
              value={frequency}
              onChange={(event) => setFrequency(event.target.value as RecurringFrequency)}
              fullWidth
            >
              {recurringFrequencies.map((option) => (
                <MenuItem key={option} value={option}>
                  {t(`recurring.frequency.${option}` as MessageKey)}
                </MenuItem>
              ))}
            </TextField>
          </Stack>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              type="month"
              label={t('recurring.col.from')}
              value={start}
              onChange={(event) => setStart(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              fullWidth
            />
            <TextField
              type="month"
              label={t('recurring.until')}
              value={end}
              onChange={(event) => setEnd(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              fullWidth
            />
            <TextField
              label={t('recurring.col.day')}
              value={day}
              onChange={(event) => setDay(event.target.value)}
              slotProps={{ htmlInput: { inputMode: 'numeric', min: 1, max: 28 } }}
              helperText={t('recurring.dayHelp')}
              error={day !== '' && !(dayNumber >= 1 && dayNumber <= 28)}
              fullWidth
            />
          </Stack>
          <TextField
            select
            label={t('book.col.project')}
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
          >
            <MenuItem value="">
              <em>{t('book.entries.noProject')}</em>
            </MenuItem>
            {(projects.data?.items ?? []).map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {project.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label={t('recurring.country')}
            value={country}
            onChange={(event) => setCountry(event.target.value.toUpperCase())}
            slotProps={{ htmlInput: { maxLength: 2 } }}
            helperText={t('recurring.countryHelp')}
          />
          {save.error && <Alert severity="error">{toApiError(save.error).message}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid} loading={save.isPending} onClick={() => save.mutate()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
