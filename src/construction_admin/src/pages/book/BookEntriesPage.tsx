import { DeleteOutlined, EditOutlined, OpenInNewOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import type { BookRowSource } from '../../api/book';

import { toApiError } from '../../api/apiError';
import {
  bookApi,
  bookEntryKinds,
  bookPaymentMethods,
  type BookEntryInput,
  type BookEntryKind,
  type BookPaymentMethod,
  type BookRow,
} from '../../api/book';
import { newIdempotencyKey } from '../../api/idempotency';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { BranchSelect } from '../../features/branches/BranchSelect';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { paths } from '../../routes/paths';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

const PAGE_SIZE = 50;
const bookKey = ['book'] as const;

function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/** Where a line that is not the book's own is changed. */
const sourcePages: Partial<Record<BookRowSource, string>> = {
  VehicleExpense: paths.vehicleExpenses,
  ToolExpense: paths.toolExpenses,
  GeneralExpense: paths.generalExpenses,
  FinanceEntry: paths.financeEntries,
  ProjectRevenue: paths.companyRevenues,
  CompanyRevenue: paths.companyRevenues,
  Invoice: paths.invoices,
};

function startOfMonth(): string {
  return `${today().slice(0, 8)}01`;
}

/** An income and a payment raise what the company has; a cost and a payout lower it. */
function isInflow(kind: BookEntryKind): boolean {
  return kind === 'Income' || kind === 'Payment';
}

/**
 * The book of money: its own entries together with the money the other tables already hold
 * (fuel, tools, general expenses, pay, revenues, invoices). A line says where it lives; only the
 * book's own entries are changed here, the rest where they were recorded.
 */
export function BookEntriesPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId: filterBranchId } = useBranchFilter();

  const [from, setFrom] = useState(startOfMonth());
  const [to, setTo] = useState(today());
  const [kind, setKind] = useState<BookEntryKind | ''>('');
  const [categoryId, setCategoryId] = useState('');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [onlyOpen, setOnlyOpen] = useState(false);
  const [page, setPage] = useState(0);
  const [editing, setEditing] = useState<BookRow | 'new' | null>(null);
  const [deleting, setDeleting] = useState<BookRow | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [search]);

  const categories = useQuery({
    queryKey: [...bookKey, 'categories', 'all'] as const,
    queryFn: () => bookApi.categories.list(true),
    staleTime: 60_000,
  });

  const totals = useQuery({
    queryKey: [
      ...bookKey,
      'totals',
      { from, to, kind, categoryId, debouncedSearch, onlyOpen, filterBranchId },
    ] as const,
    queryFn: () =>
      bookApi.entries.totals({
        from: from || undefined,
        to: to || undefined,
        kind,
        categoryId: categoryId || undefined,
        search: debouncedSearch || undefined,
        open: onlyOpen ? true : undefined,
        branchId: filterBranchId,
      }),
  });

  const { data, isLoading, error } = useQuery({
    queryKey: [
      ...bookKey,
      'entries',
      { from, to, kind, categoryId, debouncedSearch, onlyOpen, page, filterBranchId },
    ] as const,
    queryFn: () =>
      bookApi.entries.list({
        pageNumber: page + 1,
        pageSize: PAGE_SIZE,
        from: from || undefined,
        to: to || undefined,
        kind,
        categoryId: categoryId || undefined,
        search: debouncedSearch || undefined,
        open: onlyOpen ? true : undefined,
        branchId: filterBranchId,
      }),
  });

  // The finance widgets read the same money, so an entry has to reach them too.
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: bookKey });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const remove = useMutation({
    mutationFn: (row: BookRow) => bookApi.entries.remove(row.id),
    onSuccess: refresh,
  });

  const rows = data?.items ?? [];
  const visibleTotals = useMemo(() => {
    let inflow = 0;
    let outflow = 0;
    for (const row of rows) {
      if (isInflow(row.kind)) inflow += row.amount;
      else outflow += row.amount;
    }
    return { inflow, outflow };
  }, [rows]);

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('book.entries.title')}
        description={t('book.entries.description')}
        action={{ label: t('book.entries.add'), onClick: () => setEditing('new') }}
        secondaryActions={
          <Button component={RouterLink} to={paths.bookCategories}>
            {t('nav.bookCategories')}
          </Button>
        }
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <TextField
            type="date"
            size="small"
            label={t('book.filter.from')}
            value={from}
            onChange={(event) => {
              setFrom(event.target.value);
              setPage(0);
            }}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            type="date"
            size="small"
            label={t('book.filter.to')}
            value={to}
            onChange={(event) => {
              setTo(event.target.value);
              setPage(0);
            }}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            select
            size="small"
            label={t('book.filter.kind')}
            value={kind}
            onChange={(event) => {
              setKind(event.target.value as BookEntryKind | '');
              setPage(0);
            }}
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="">
              <em>{t('book.filter.all')}</em>
            </MenuItem>
            {bookEntryKinds.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.kind.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label={t('book.filter.category')}
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setPage(0);
            }}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">
              <em>{t('book.filter.all')}</em>
            </MenuItem>
            {(categories.data ?? []).map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            label={t('book.filter.search')}
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(0);
            }}
            sx={{ minWidth: 200 }}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={onlyOpen}
                onChange={(event) => {
                  setOnlyOpen(event.target.checked);
                  setPage(0);
                }}
              />
            }
            label={t('book.filter.onlyOpen')}
          />
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('book.col.date')}</TableCell>
              <TableCell>{t('book.col.kind')}</TableCell>
              <TableCell>{t('book.col.category')}</TableCell>
              <TableCell>{t('book.col.description')}</TableCell>
              <TableCell>{t('book.col.project')}</TableCell>
              <TableCell>{t('book.col.status')}</TableCell>
              <TableCell align="right">{t('book.col.amount')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={`${row.source}-${row.id}`} hover>
                <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(row.occurredOn)}</TableCell>
                <TableCell>{t(`book.kind.${row.kind}` as MessageKey)}</TableCell>
                <TableCell>{row.categoryName ?? '—'}</TableCell>
                <TableCell>
                  <Typography variant="body2">{row.description ?? row.counterparty ?? '—'}</Typography>
                  {row.description && row.counterparty && (
                    <Typography variant="caption" color="text.secondary">
                      {row.counterparty}
                    </Typography>
                  )}
                  {!row.editable && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {t('book.source.from', { source: t(`book.source.${row.source}` as MessageKey) })}
                    </Typography>
                  )}
                </TableCell>
                <TableCell>{row.projectName ?? '—'}</TableCell>
                <TableCell>
                  {row.isOpen ? (
                    <Chip size="small" color="warning" variant="outlined" label={t('book.status.open')} />
                  ) : (
                    <Chip size="small" variant="outlined" label={t('book.status.settled')} />
                  )}
                  {row.isOpen && row.dueDate && (
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {t('book.status.due', { date: formatDate(row.dueDate) })}
                    </Typography>
                  )}
                </TableCell>
                <TableCell
                  align="right"
                  sx={{ whiteSpace: 'nowrap', color: isInflow(row.kind) ? 'success.main' : undefined }}
                >
                  {isInflow(row.kind) ? '+' : '−'} {formatMoney(row.amount, locale)}
                </TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  {!row.editable && sourcePages[row.source] && (
                    <Tooltip title={t('book.source.open')}>
                      <IconButton
                        size="small"
                        component={RouterLink}
                        to={sourcePages[row.source]!}
                        aria-label={t('book.source.open')}
                      >
                        <OpenInNewOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                  {row.editable && (
                    <>
                      <Tooltip title={t('common.edit')}>
                        <IconButton size="small" onClick={() => setEditing(row)} aria-label={t('common.edit')}>
                          <EditOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={t('common.delete')}>
                        <IconButton size="small" onClick={() => setDeleting(row)} aria-label={t('common.delete')}>
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {!isLoading && rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={8}>
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {t('book.entries.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
        <Stack
          direction="row"
          spacing={3}
          sx={{ px: 2, py: 1, justifyContent: 'flex-end', alignItems: 'center', flexWrap: 'wrap' }}
          useFlexGap
        >
          <Typography variant="body2" color="text.secondary">
            {totals.data ? t('book.totals.all', { count: totals.data.count }) : t('book.totals.page')}
          </Typography>
          <Typography variant="body2" color="success.main">
            + {formatMoney(totals.data?.inflow ?? visibleTotals.inflow, locale)}
          </Typography>
          <Typography variant="body2">
            − {formatMoney(totals.data?.outflow ?? visibleTotals.outflow, locale)}
          </Typography>
        </Stack>
        <TablePagination
          component="div"
          count={data?.totalCount ?? 0}
          page={page}
          rowsPerPage={PAGE_SIZE}
          rowsPerPageOptions={[PAGE_SIZE]}
          onPageChange={(_, next) => setPage(next)}
        />
      </Paper>

      {editing && (
        <EntryDialog
          row={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title={t('book.entries.deleteTitle')}
        description={t('book.entries.deleteBody')}
        confirmLabel={t('common.delete')}
        destructive
        onConfirm={async () => {
          if (deleting) await remove.mutateAsync(deleting);
          setDeleting(null);
        }}
        onCancel={() => setDeleting(null)}
      />
    </Stack>
  );
}

function EntryDialog({
  row,
  onClose,
  onSaved,
}: {
  row: BookRow | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const projects = useAllProjectsQuery();
  const categories = useQuery({
    queryKey: [...bookKey, 'categories', 'active'] as const,
    queryFn: () => bookApi.categories.list(false),
    staleTime: 60_000,
  });

  // A retry of the same save (a double click, a flaky connection) must not make a second entry.
  const [idempotencyKey] = useState(() => newIdempotencyKey());
  const [clientRequestId] = useState(() => crypto.randomUUID());

  const [kind, setKind] = useState<BookEntryKind>(row?.kind ?? 'Cost');
  const [amount, setAmount] = useState(row ? String(row.amount) : '');
  const [occurredOn, setOccurredOn] = useState(row?.occurredOn ?? today());
  const [categoryId, setCategoryId] = useState(row?.categoryId ?? '');
  const [categoryTouched, setCategoryTouched] = useState(row !== null);
  const [projectId, setProjectId] = useState(row?.projectId ?? '');
  const [branchId, setBranchId] = useState(row?.branchId ?? '');
  const [counterparty, setCounterparty] = useState(row?.counterparty ?? '');
  const [description, setDescription] = useState(row?.description ?? '');
  const [method, setMethod] = useState<BookPaymentMethod | ''>(row?.method ?? '');
  const [dueDate, setDueDate] = useState(row?.dueDate ?? '');
  const [paid, setPaid] = useState(row ? row.paidOn !== null : false);
  const [paidOn, setPaidOn] = useState(row?.paidOn ?? today());
  const [suggestion, setSuggestion] = useState<string | null>(null);

  const needsCategory = kind === 'Cost' || kind === 'Income';
  const categoryKind = kind === 'Income' || kind === 'Payment' ? 'Income' : 'Cost';
  const options = (categories.data ?? []).filter((category) => category.kind === categoryKind);

  // Changing the kind can leave a category of the wrong kind selected.
  useEffect(() => {
    if (categoryId && !options.some((option) => option.id === categoryId)) {
      setCategoryId('');
    }
  }, [kind, options, categoryId]);

  // The rules only suggest: nothing is filed until the person leaves it selected.
  useEffect(() => {
    if (categoryTouched || description.trim().length < 3) return;
    const handle = window.setTimeout(() => {
      bookApi.entries
        .suggestCategory(`${description} ${counterparty}`)
        .then((result) => {
          if (result.categoryId && options.some((option) => option.id === result.categoryId)) {
            setCategoryId(result.categoryId);
            setSuggestion(result.categoryName);
          }
        })
        .catch(() => undefined);
    }, 400);
    return () => window.clearTimeout(handle);
    // `options` is derived from the categories and the kind; re-running on it would loop.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [description, counterparty, categoryTouched]);

  const parsed = Number(amount.replace(',', '.'));
  const valid =
    Number.isFinite(parsed) &&
    parsed > 0 &&
    occurredOn !== '' &&
    (!needsCategory || categoryId !== '');

  const save = useMutation({
    mutationFn: () => {
      const isMoved = kind === 'Payment' || kind === 'Payout';
      const input: BookEntryInput = {
        kind,
        amount: parsed,
        occurredOn,
        categoryId: categoryId || null,
        projectId: projectId || null,
        branchId: branchId || null,
        counterparty: counterparty.trim() || null,
        description: description.trim() || null,
        method: method || null,
        dueDate: dueDate || null,
        // A payment or payout is money that moved; a cost or income is paid only when marked.
        paidOn: isMoved ? occurredOn : paid ? paidOn : null,
        clientRequestId: row ? undefined : clientRequestId,
      };
      return row
        ? bookApi.entries.update(row.id, input)
        : bookApi.entries.create(input, idempotencyKey);
    },
    onSuccess: onSaved,
  });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{row ? t('book.entries.edit') : t('book.entries.add')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            select
            label={t('book.col.kind')}
            value={kind}
            onChange={(event) => setKind(event.target.value as BookEntryKind)}
            helperText={t(`book.kind.${kind}.help` as MessageKey)}
          >
            {bookEntryKinds.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.kind.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={t('book.col.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              slotProps={{ htmlInput: { inputMode: 'decimal' } }}
              error={amount !== '' && !(parsed > 0)}
              autoFocus
              fullWidth
            />
            <TextField
              type="date"
              label={t('book.col.date')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today() } }}
              fullWidth
            />
          </Stack>

          <TextField
            label={t('book.col.description')}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }}
          />

          <TextField
            label={t('book.col.counterparty')}
            value={counterparty}
            onChange={(event) => setCounterparty(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 200 } }}
          />

          <TextField
            select
            label={t('book.col.category')}
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setCategoryTouched(true);
              setSuggestion(null);
            }}
            required={needsCategory}
            helperText={suggestion ? t('book.entries.suggested', { name: suggestion }) : undefined}
          >
            {!needsCategory && (
              <MenuItem value="">
                <em>{t('book.entries.defaultCategory')}</em>
              </MenuItem>
            )}
            {options.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </TextField>

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
            select
            label={t('book.col.method')}
            value={method}
            onChange={(event) => setMethod(event.target.value as BookPaymentMethod | '')}
          >
            <MenuItem value="">
              <em>{t('book.entries.noMethod')}</em>
            </MenuItem>
            {bookPaymentMethods.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.method.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>

          {needsCategory && (
            <>
              <TextField
                type="date"
                label={t('book.entries.dueDate')}
                value={dueDate}
                onChange={(event) => setDueDate(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <FormControlLabel
                control={<Checkbox checked={paid} onChange={(event) => setPaid(event.target.checked)} />}
                label={t('book.entries.alreadyPaid')}
              />
              {paid && (
                <TextField
                  type="date"
                  label={t('book.entries.paidOn')}
                  value={paidOn}
                  onChange={(event) => setPaidOn(event.target.value)}
                  slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today() } }}
                />
              )}
            </>
          )}

          <BranchSelect value={branchId} onChange={setBranchId} />

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
