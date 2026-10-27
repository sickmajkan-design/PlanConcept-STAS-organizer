import {
  AttachFileOutlined,
  ChevronLeftOutlined,
  ChevronRightOutlined,
  DeleteOutlined,
  EditOutlined,
  FileDownloadOutlined,
  FolderZipOutlined,
  OpenInNewOutlined,
} from '@mui/icons-material';
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
import { useEffect, useRef, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { bookApi, type BookEntryKind, type BookRow, type BookRowSource } from '../../api/book';
import { newIdempotencyKey } from '../../api/idempotency';
import type { AttachmentOwnerType } from '../../api/types';
import { overviewApi } from '../../api/overview';
import { payoutsApi } from '../../api/payouts';
import { AttachmentList } from '../../components/AttachmentList';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useAttachmentLimits } from '../../features/attachments/useAttachments';
import { uploadAfterCreate } from '../../features/files/uploadAfterCreate';
import { validateFiles } from '../../features/files/validateFiles';
import { MoreActionsMenu } from '../../components/MoreActionsMenu';
import { PageHeader } from '../../components/PageHeader';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { useAuth } from '../../auth/useAuth';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { EntryDialog } from '../../features/book/EntryDialog';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';
import { companyNow } from '../../utils/companyTime';
import { ProjectLabel } from '../../components/ProjectLabel';

const PAGE_SIZE = 50;
const bookKey = ['book'] as const;

type KindFilter = 'all' | BookEntryKind;

function isoToday(): string {
  const now = companyNow();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

function monthBounds(year: number, month: number): { from: string; to: string } {
  const last = new Date(year, month, 0).getDate();
  const m = String(month).padStart(2, '0');
  return { from: `${year}-${m}-01`, to: `${year}-${m}-${String(last).padStart(2, '0')}` };
}

function isInflow(kind: BookEntryKind): boolean {
  return kind === 'Income' || kind === 'Payment';
}

/** The lines whose source keeps documents, and the kind of record they hang off. */
const documentOwnerTypes: Partial<Record<BookRowSource, AttachmentOwnerType>> = {
  BookEntry: 'BookEntry',
  VehicleExpense: 'VehicleExpense',
  ToolExpense: 'ToolExpense',
  GeneralExpense: 'GeneralExpense',
};

/** Where a line that is not the book's own is changed. */
const sourcePages: Partial<Record<BookRowSource, string>> = {
  VehicleExpense: paths.vehicleExpenses,
  ToolExpense: paths.toolExpenses,
  GeneralExpense: paths.generalExpenses,
  FinanceEntry: paths.financeEntries,
  ProjectRevenue: paths.companyRevenues,
  CompanyRevenue: paths.companyRevenues,
  Invoice: paths.invoices,
  InvoicePayment: paths.bookReceivables,
  WagePayout: paths.bookPayouts,
};

/** The shortcuts of the quick-entry row: they only fill in the fields, you type the amount. */
const shortcuts: {
  key: string;
  kind: BookEntryKind;
  category: string | null;
  description: string;
  counterparty: string;
}[] = [
  { key: 'fuel', kind: 'Cost', category: 'Fuel', description: 'Gorivo', counterparty: 'DKV' },
  { key: 'rent', kind: 'Cost', category: 'Housing', description: 'Kirija stana', counterparty: '' },
  { key: 'tools', kind: 'Cost', category: 'Tools', description: 'Alat i oprema', counterparty: '' },
  { key: 'invoice', kind: 'Income', category: 'Collection', description: 'Faktura', counterparty: '' },
  { key: 'payment', kind: 'Payment', category: null, description: 'Uplata po fakturi', counterparty: '' },
  { key: 'advance', kind: 'Payout', category: null, description: 'Akontacija radnicima', counterparty: 'Radnici' },
];

/**
 * The book of entries: one list for every cost, income, payment and payout, with the month's figures above it
 * and a quick-entry row to add one. Lines that come from other parts of the app (fuel, housing, invoices, pay)
 * are shown with where they come from and are changed there; only the book's own can be changed here.
 */
export function BookEntriesPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId: filterBranchId } = useBranchFilter();
  const projects = useAllProjectsQuery();

  const now = companyNow();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [kind, setKind] = useState<KindFilter>('all');
  // A search from the palette arrives as ?search=; it is the starting text, and typing takes over from there.
  const [initialSearch] = useState(() => new URLSearchParams(window.location.search).get('search') ?? '');
  const [search, setSearch] = useState(initialSearch);
  const [debouncedSearch, setDebouncedSearch] = useState(initialSearch);
  const [page, setPage] = useState(0);
  const [editing, setEditing] = useState<BookRow | null>(null);
  const [deleting, setDeleting] = useState<BookRow | null>(null);
  const [documentsOf, setDocumentsOf] = useState<BookRow | null>(null);
  const { user } = useAuth();

  // quick entry
  const [qDate, setQDate] = useState(isoToday());
  const [qKind, setQKind] = useState<BookEntryKind>('Cost');
  const [qDescription, setQDescription] = useState('');
  const [qProject, setQProject] = useState('');
  const [qCounterparty, setQCounterparty] = useState('');
  const [qAmount, setQAmount] = useState('');
  // Documents picked for the line being typed; filed against it the moment it is saved.
  const [qFiles, setQFiles] = useState<File[]>([]);
  const unsent = useRef<string[]>([]);
  const limits = useAttachmentLimits();
  const [qCategoryKey, setQCategoryKey] = useState<string | null>(null);
  const [qError, setQError] = useState<string | null>(null);
  const [qDone, setQDone] = useState<string | null>(null);
  const [justAdded, setJustAdded] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [search]);

  const bounds = monthBounds(year, month);
  const monthLabel = new Date(year, month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
    month: 'long',
    year: 'numeric',
  });

  const categories = useQuery({
    queryKey: [...bookKey, 'categories', 'all'] as const,
    queryFn: () => bookApi.categories.list(true),
    staleTime: 60_000,
  });

  const filter = {
    from: bounds.from,
    to: bounds.to,
    search: debouncedSearch || undefined,
    branchId: filterBranchId,
  };

  const list = useQuery({
    queryKey: [...bookKey, 'entries', filter, kind, page] as const,
    queryFn: () =>
      bookApi.entries.list({
        pageNumber: page + 1,
        pageSize: PAGE_SIZE,
        ...filter,
        kind: kind === 'all' ? '' : kind,
      }),
  });

  // The counts on the chips and what the whole selection adds up to ignore the chip itself.
  const totals = useQuery({
    queryKey: [...bookKey, 'totals', filter] as const,
    queryFn: () => bookApi.entries.totals(filter),
  });

  const monthFigures = useQuery({
    queryKey: [...bookKey, 'month-figures', bounds.from, bounds.to, filterBranchId ?? null] as const,
    queryFn: () => overviewApi.get({ from: bounds.from, to: bounds.to, branchId: filterBranchId }),
  });

  const receivables = useQuery({
    queryKey: [...bookKey, 'debts', 'owedToUs', filterBranchId ?? null] as const,
    queryFn: () => bookApi.receivables({ branchId: filterBranchId }),
  });

  const payoutsMonth = useQuery({
    queryKey: [...bookKey, 'payouts', year, month, filterBranchId ?? null] as const,
    queryFn: () => payoutsApi.get({ year, month, branchId: filterBranchId }),
    retry: false,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: bookKey });
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const remove = useMutation({
    mutationFn: (row: BookRow) => bookApi.entries.remove(row.id),
    onSuccess: refresh,
  });

  const categoryByKey = (key: string | null) => (categories.data ?? []).find((c) => c.systemKey === key);

  const shift = (delta: number) => {
    const date = new Date(year, month - 1 + delta, 1);
    setYear(date.getFullYear());
    setMonth(date.getMonth() + 1);
    setPage(0);
  };

  const add = useMutation({
    mutationFn: async () => {
      const parsed = Number(qAmount.replace(/\./g, '').replace(',', '.'));

      // An advance is money paid out to workers, so it goes where the payouts screen counts it.
      if (qKind === 'Payout') {
        await payoutsApi.create(
          {
            year: Number(qDate.slice(0, 4)),
            month: Number(qDate.slice(5, 7)),
            group: 'Workers',
            kind: 'Advance',
            payDate: qDate,
            amount: parsed,
            note: [qDescription.trim(), qCounterparty.trim()].filter(Boolean).join(' · ') || null,
          },
          newIdempotencyKey(),
        );
        return;
      }

      // The category comes from the shortcut, then from the rules, then from the kind.
      let categoryId = categoryByKey(qCategoryKey)?.id ?? null;

      if (!categoryId && qDescription.trim().length >= 3) {
        const suggestion = await bookApi.entries.suggestCategory(`${qDescription} ${qCounterparty}`);
        categoryId = suggestion.categoryId;
      }

      if (!categoryId && (qKind === 'Cost' || qKind === 'Income')) {
        categoryId = categoryByKey(qKind === 'Income' ? 'Collection' : 'Other')?.id ?? null;
      }

      const created = await bookApi.entries.create(
        {
          kind: qKind,
          amount: parsed,
          occurredOn: qDate,
          categoryId,
          projectId: qProject || null,
          branchId: null,
          counterparty: qCounterparty.trim() || null,
          description: qDescription.trim() || null,
          method: null,
          dueDate: null,
          paidOn: qKind === 'Cost' || qKind === 'Income' ? null : qDate,
          clientRequestId: crypto.randomUUID(),
        },
        newIdempotencyKey(),
      );

      // A photo of the receipt becomes a Photo, anything else Other. The line is already saved, so a file
      // that fails is said, not allowed to undo it.
      unsent.current = await uploadAfterCreate(
        'BookEntry',
        created.id,
        qFiles.map((file, index) => ({
          id: `q${index}`,
          file,
          category: file.type.startsWith('image/') ? 'Photo' : 'Other',
        })),
      );
    },
    onSuccess: () => {
      setQDone(t(`book.entries.effect.${qKind}` as MessageKey));
      setJustAdded(qDescription.trim());
      setQDescription('');
      setQAmount('');
      setQCounterparty('');
      setQCategoryKey(null);
      setQFiles([]);
      setQError(unsent.current.length > 0 ? t('book.entries.quick.unsent', { names: unsent.current.join(', ') }) : null);
      unsent.current = [];
      refresh();
    },
    onError: (error) => {
      setQDone(null);
      setQError(toApiError(error).message);
    },
  });

  const qParsed = Number(qAmount.replace(/\./g, '').replace(',', '.'));

  const submitQuick = () => {
    if (!qDescription.trim() || !(qParsed > 0)) {
      setQDone(null);
      setQError(t('book.entries.quick.invalid'));
      return;
    }

    setQError(null);
    add.mutate();
  };

  const rows = list.data?.items ?? [];

  // What the settlement says each pay group earned this month, shown among the lines as it is in the mockup.
  const settlementRows =
    page === 0 && search === '' && (kind === 'all' || kind === 'Cost') && payoutsMonth.data?.hasSettlement
      ? payoutsMonth.data.groups.filter((g) => g.earned > 0)
      : [];

  const chips: { key: KindFilter; count: number }[] = [
    { key: 'all', count: totals.data?.count ?? 0 },
    { key: 'Income', count: totals.data?.countsByKind.Income ?? 0 },
    { key: 'Cost', count: totals.data?.countsByKind.Cost ?? 0 },
    { key: 'Payment', count: totals.data?.countsByKind.Payment ?? 0 },
    { key: 'Payout', count: totals.data?.countsByKind.Payout ?? 0 },
  ];

  const statusOf = (row: BookRow): { label: string; color: 'default' | 'warning' | 'error' | 'success' } => {
    if (!row.isOpen) return { label: t('book.status.settled'), color: 'success' };

    if (row.dueDate) {
      const days = Math.floor((new Date(isoToday()).getTime() - new Date(row.dueDate).getTime()) / 86_400_000);

      if (days > 0) return { label: t('book.entries.status.late', { days }), color: 'error' };

      return { label: t('book.entries.status.due', { date: formatDate(row.dueDate) }), color: 'warning' };
    }

    return { label: t('book.status.open'), color: 'warning' };
  };

  const sourceLabel = (row: BookRow): string => {
    switch (row.source) {
      case 'BookEntry':
        return t(`book.src.entry.${row.origin ?? 'Manual'}` as MessageKey);
      case 'VehicleExpense':
        return t(row.origin === 'Fuel' ? 'book.src.fuel' : 'book.src.vehicles');
      case 'WagePayout':
        return t(`book.src.payout.${row.origin ?? 'Advance'}` as MessageKey);
      default:
        return t(`book.src.${row.source}` as MessageKey);
    }
  };

  const monthState = monthFigures.data?.months[0];
  const statusChip = monthState?.locked
    ? t('book.entries.monthClosed')
    : monthState?.hasSettlement
      ? t('book.entries.monthOpen')
      : t('book.entries.monthNoSettlement');

  const money = (value: number | undefined) => formatMoney(value ?? 0, locale);

  const descriptionOf = (row: BookRow): string => {
    if (row.source === 'InvoicePayment') return t('book.entries.paymentOf', { number: row.description ?? '' });
    if (row.source === 'Invoice') return t('book.entries.invoiceOf', { number: row.description ?? '' });
    if (row.source === 'WagePayout') {
      return `${t(`payouts.kind.${row.origin ?? 'Advance'}` as MessageKey)}${row.description ? `, ${row.description}` : ''}`;
    }
    return row.description ?? row.counterparty ?? '—';
  };

  const counterpartyOf = (row: BookRow): string => {
    if (row.source === 'WagePayout' && row.counterparty) {
      return t(`payouts.group.${row.counterparty}` as MessageKey);
    }
    return row.counterparty ?? '—';
  };

  const errorMessage = list.error ?? totals.error;

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('book.entries.title')}
        description={monthLabel}
        secondaryActions={
          <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <IconButton size="small" onClick={() => shift(-1)} aria-label={t('book.overview.previous')}>
              <ChevronLeftOutlined />
            </IconButton>
            <Chip
              size="small"
              color={monthState?.locked ? 'default' : 'warning'}
              variant="outlined"
              label={statusChip}
            />
            <IconButton size="small" onClick={() => shift(1)} aria-label={t('book.overview.next')}>
              <ChevronRightOutlined />
            </IconButton>
            <MoreActionsMenu
              label={t('book.entries.exportMenu')}
              startIcon={<FileDownloadOutlined />}
              actions={[
                {
                  key: 'table',
                  label: t('book.entries.export'),
                  icon: <FileDownloadOutlined fontSize="small" />,
                  onClick: () => void bookApi.entries.export({ ...filter, kind: kind === 'all' ? '' : kind }),
                },
                {
                  key: 'package',
                  label: t('book.entries.package'),
                  icon: <FolderZipOutlined fontSize="small" />,
                  onClick: () => void bookApi.entries.exportPackage({ ...filter, kind: kind === 'all' ? '' : kind }),
                },
              ]}
            />
          </Stack>
        }
      />

      {errorMessage && <Alert severity="error">{toApiError(errorMessage).message}</Alert>}

      <HowItWorks id="entries">
        <HowLine>{t('book.entries.how1')}</HowLine>
        <HowLine>{t('book.entries.how2')}</HowLine>
        <HowLine>{t('book.entries.how3')}</HowLine>
        <HowLine>{t('book.entries.how4')}</HowLine>
        <HowLine>{t('book.entries.how5')}</HowLine>
      </HowItWorks>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Kpi label={t('book.entries.k.income')} value={money(monthFigures.data?.income)} note={t('book.entries.k.incomeNote')} />
        <Kpi label={t('book.entries.k.costs')} value={money(monthFigures.data?.outflow)} note={t('book.entries.k.costsNote')} />
        <Kpi
          label={t('book.entries.k.profit')}
          value={money(monthFigures.data?.profit)}
          note={t('book.entries.k.profitNote')}
          color={(monthFigures.data?.profit ?? 0) < 0 ? 'error.main' : 'success.main'}
        />
        <Kpi
          label={t('book.entries.k.receivables')}
          value={money(receivables.data?.openTotal)}
          note={t('book.entries.k.receivablesNote')}
        />
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.entries.quick')}
        </Typography>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', my: 1, alignItems: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            {t('book.entries.shortcuts')}
          </Typography>
          {shortcuts.map((shortcut) => (
            <Chip
              key={shortcut.key}
              size="small"
              variant="outlined"
              clickable
              label={t(`book.entries.shortcut.${shortcut.key}` as MessageKey)}
              onClick={() => {
                setQKind(shortcut.kind);
                setQDescription(shortcut.description);
                setQCounterparty(shortcut.counterparty);
                setQCategoryKey(shortcut.category);
                setQError(null);
                setQDone(null);
              }}
            />
          ))}
        </Stack>
        <Typography variant="caption" color="text.secondary">
          {t('book.entries.quickHelp')}
        </Typography>

        <Stack
          direction={{ xs: 'column', lg: 'row' }}
          spacing={1.5}
          useFlexGap
          sx={{ mt: 1.5, alignItems: { lg: 'flex-start' } }}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !add.isPending) {
              event.preventDefault();
              submitQuick();
            }
          }}
        >
          <TextField
            type="date"
            size="small"
            label={t('book.col.date')}
            value={qDate}
            onChange={(event) => setQDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: isoToday() } }}
          />
          <TextField
            select
            size="small"
            label={t('book.col.kind')}
            value={qKind}
            onChange={(event) => {
              setQKind(event.target.value as BookEntryKind);
              setQCategoryKey(null);
            }}
            sx={{ minWidth: 130 }}
          >
            {(['Cost', 'Income', 'Payment', 'Payout'] as const).map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.kind.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            label={t('book.col.description')}
            placeholder={t('book.entries.quick.descriptionPlaceholder')}
            value={qDescription}
            onChange={(event) => setQDescription(event.target.value)}
            sx={{ minWidth: 220, flex: 1 }}
          />
          <TextField
            select
            size="small"
            label={t('book.col.project')}
            value={qProject}
            onChange={(event) => setQProject(event.target.value)}
            sx={{ minWidth: 160 }}
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
            size="small"
            label={t('book.entries.quick.who')}
            placeholder={t('book.entries.quick.counterpartyPlaceholder')}
            value={qCounterparty}
            onChange={(event) => setQCounterparty(event.target.value)}
            sx={{ minWidth: 220 }}
          />
          <TextField
            size="small"
            label={t('book.entries.quick.amount')}
            placeholder="1.980,00"
            value={qAmount}
            onChange={(event) => setQAmount(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
            error={qAmount !== '' && !(qParsed > 0)}
            sx={{ maxWidth: 150 }}
          />
          {qKind !== 'Payout' && (
            <Button size="small" component="label" startIcon={<AttachFileOutlined />} sx={{ whiteSpace: 'nowrap', flexShrink: 0 }}>
              {t('book.entries.quick.attach')}
              <input
                hidden
                multiple
                type="file"
                accept={limits.accept}
                data-testid="quick-attach"
                onChange={(event) => {
                  const picked = validateFiles(Array.from(event.target.files ?? []), {
                    accept: limits.accept,
                    maxBytes: limits.maxBytes,
                    existing: qFiles,
                  }).accepted;

                  setQFiles((current) => [...current, ...picked]);
                  event.target.value = '';
                }}
              />
            </Button>
          )}
          <Button variant="contained" loading={add.isPending} onClick={submitQuick}>
            {t('book.entries.quick.add')}
          </Button>
        </Stack>

        {qFiles.length > 0 && qKind !== 'Payout' && (
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mt: 1 }}>
            {qFiles.map((file) => (
              <Chip
                key={`${file.name}-${file.size}-${file.lastModified}`}
                size="small"
                icon={<AttachFileOutlined />}
                label={file.name}
                onDelete={() => setQFiles((current) => current.filter((f) => f !== file))}
              />
            ))}
          </Stack>
        )}

        {qError && (
          <Alert severity="error" sx={{ mt: 1.5 }}>
            {qError}
          </Alert>
        )}
        {qDone && !qError && (
          <Alert severity="success" sx={{ mt: 1.5 }}>
            {t('book.entries.quick.done', { effect: qDone })}
          </Alert>
        )}
      </Paper>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: { md: 'center' } }}>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', flex: 1 }}>
          {chips.map((chip) => (
            <Chip
              key={chip.key}
              clickable
              color={kind === chip.key ? 'primary' : 'default'}
              variant={kind === chip.key ? 'filled' : 'outlined'}
              label={`${t(chip.key === 'all' ? 'book.filter.allKinds' : (`book.kinds.${chip.key}` as MessageKey))} ${chip.count}`}
              onClick={() => {
                setKind(chip.key);
                setPage(0);
              }}
            />
          ))}
        </Stack>
        <TextField
          size="small"
          label={t('book.filter.search')}
          placeholder={t('book.entries.searchPlaceholder')}
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(0);
          }}
          sx={{ minWidth: 280 }}
        />
      </Stack>

      <Paper variant="outlined" className="scroll-hint" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('book.col.date')}</TableCell>
              <TableCell>{t('book.col.kind')}</TableCell>
              <TableCell>{t('book.col.description')}</TableCell>
              <TableCell>{t('book.col.category')}</TableCell>
              <TableCell>{t('book.col.project')}</TableCell>
              <TableCell>{t('book.col.counterparty')}</TableCell>
              <TableCell>{t('book.entries.col.source')}</TableCell>
              <TableCell>{t('book.entries.col.doc')}</TableCell>
              <TableCell>{t('book.col.status')}</TableCell>
              <TableCell align="right">{t('book.col.amount')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {settlementRows.map((group) => (
              <TableRow key={`settlement-${group.group}`} hover>
                <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(bounds.to)}</TableCell>
                <TableCell>{t('book.kind.Cost')}</TableCell>
                <TableCell>{t('book.entries.settlementLine', { month: monthLabel })}</TableCell>
                <TableCell>{t(`payouts.group.${group.group}` as MessageKey)}</TableCell>
                <TableCell>—</TableCell>
                <TableCell>{t(`payouts.group.${group.group}` as MessageKey)}</TableCell>
                <TableCell>
                  <Chip size="small" variant="outlined" label={t('book.src.settlement')} />
                </TableCell>
                <TableCell>—</TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    variant="outlined"
                    color={group.remaining > 0 ? 'warning' : 'success'}
                    label={group.remaining > 0 ? t('book.status.open') : t('book.status.settled')}
                  />
                </TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  − {money(group.earned)}
                </TableCell>
                <TableCell align="right">
                  <Tooltip title={t('book.source.open')}>
                    <IconButton size="small" component={RouterLink} to={paths.bookSettlement} aria-label={t('book.source.open')}>
                      <OpenInNewOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {rows.map((row) => {
              const status = statusOf(row);
              const fresh = justAdded !== null && row.editable && row.description === justAdded;

              return (
                <TableRow key={`${row.source}-${row.id}`} hover sx={fresh ? { bgcolor: 'action.selected' } : undefined}>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(row.occurredOn)}</TableCell>
                  <TableCell>{t(`book.kind.${row.kind}` as MessageKey)}</TableCell>
                  <TableCell>{descriptionOf(row)}</TableCell>
                  <TableCell>{row.categoryName ?? '—'}</TableCell>
                  <TableCell>{row.projectName ? <ProjectLabel name={row.projectName} projectId={row.projectId} /> : t('book.entries.noProject')}</TableCell>
                  <TableCell>{counterpartyOf(row)}</TableCell>
                  <TableCell>
                    <Chip size="small" variant="outlined" label={sourceLabel(row)} />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>
                    {row.source === 'Invoice' ? (
                      row.description
                    ) : documentOwnerTypes[row.source] ? (
                      <Tooltip title={t('book.entries.documents')}>
                        <Button
                          size="small"
                          color={row.documentCount > 0 ? 'primary' : 'inherit'}
                          startIcon={<AttachFileOutlined fontSize="small" />}
                          onClick={() => setDocumentsOf(row)}
                          aria-label={`${t('book.entries.documents')}: ${row.documentCount}`}
                        >
                          {row.documentCount}
                        </Button>
                      </Tooltip>
                    ) : (
                      '—'
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip size="small" variant="outlined" color={status.color} label={status.label} />
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
                        <Tooltip title={t('book.entries.remove')}>
                          <IconButton size="small" onClick={() => setDeleting(row)} aria-label={t('book.entries.remove')}>
                            <DeleteOutlined fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
            {!list.isLoading && rows.length === 0 && settlementRows.length === 0 && (
              <TableRow>
                <TableCell colSpan={11}>
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {t('book.entries.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={list.data?.totalCount ?? 0}
          page={page}
          rowsPerPage={PAGE_SIZE}
          rowsPerPageOptions={[PAGE_SIZE]}
          labelRowsPerPage={t('common.rowsPerPage')}
          labelDisplayedRows={({ from, to, count }) => t('common.displayedRows', { from, to, count })}
          slotProps={{
            actions: {
              previousButton: { title: t('common.previousPage'), 'aria-label': t('common.previousPage') },
              nextButton: { title: t('common.nextPage'), 'aria-label': t('common.nextPage') },
            },
          }}
          onPageChange={(_, next) => setPage(next)}
        />
      </Paper>

      <Typography variant="caption" color="text.secondary">
        {t('book.entries.shown', { shown: rows.length, total: list.data?.totalCount ?? 0 })} ·{' '}
        {t('book.entries.footnote', {
          inflow: money(totals.data?.inflow),
          outflow: money(totals.data?.outflow),
        })}
      </Typography>

      {editing && (
        <EntryDialog
          row={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}

      <Dialog
        open={documentsOf !== null}
        fullWidth
        maxWidth="sm"
        onClose={() => {
          setDocumentsOf(null);
          refresh();
        }}
      >
        <DialogTitle>{t('book.entries.documents')}</DialogTitle>
        <DialogContent>
          {documentsOf && documentOwnerTypes[documentsOf.source] && (
            <AttachmentList
              ownerType={documentOwnerTypes[documentsOf.source]!}
              ownerId={documentsOf.id}
              categories={['Other', 'Contract', 'Photo']}
              canUpload
              canDelete={canAdministerAccounts(user)}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setDocumentsOf(null);
              refresh();
            }}
          >
            {t('common.close')}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleting !== null}
        title={t('book.entries.deleteTitle')}
        description={t('book.entries.deleteBody')}
        confirmLabel={t('book.entries.remove')}
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

function Kpi({
  label,
  value,
  note,
  color,
}: {
  label: string;
  value: string;
  note?: string;
  color?: string;
}) {
  return (
    <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 700 }} color={color}>
        {value}
      </Typography>
      {note && (
        <Typography variant="caption" color="text.secondary">
          {note}
        </Typography>
      )}
    </Paper>
  );
}
