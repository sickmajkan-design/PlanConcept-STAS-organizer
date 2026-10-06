import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControl,
  InputLabel,
  List,
  ListItemButton,
  ListItemText,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';

import { toApiError } from '../../api/apiError';
import {
  DKV_STATEMENT_ACCEPTED_EXTENSIONS,
  MAX_DKV_STATEMENT_BYTES,
  type DkvImportPreview,
  type DkvImportResult,
  type FuelTransaction,
  type FuelTransactionStatus,
} from '../../api/fuelTransactions';
import { PageHeader } from '../../components/PageHeader';
import { ReasonDialog } from '../../components/ReasonDialog';
import {
  useAssignDkvCard,
  useFuelExpenseCandidatesQuery,
  useFuelImportBatchesQuery,
  useFuelTransactionCountsQuery,
  useFuelTransactionsQuery,
  useImportDkvStatement,
  usePreviewDkvImport,
  useRecheckDkvTransactions,
  useResolveFuelTransaction,
} from '../../features/fuelTransactions/useFuelTransactions';
import { useAllVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime, formatMoney } from '../../utils/formatting';

const STATUS_COLOR: Record<FuelTransactionStatus, 'success' | 'warning' | 'info' | 'error' | 'default'> = {
  Matched: 'success',
  NeedsReview: 'warning',
  NoDriverEntry: 'info',
  UnknownCard: 'error',
  Resolved: 'default',
  Ignored: 'default',
};

const OPEN_STATUSES: FuelTransactionStatus[] = ['UnknownCard', 'NeedsReview', 'NoDriverEntry'];

const FILTERS: { key: string; statuses: FuelTransactionStatus[] }[] = [
  { key: 'open', statuses: OPEN_STATUSES },
  { key: 'matched', statuses: ['Matched'] },
  { key: 'settled', statuses: ['Resolved', 'Ignored'] },
  { key: 'all', statuses: [] },
];

const time = (value: string) => value.slice(0, 5);

export function FuelReconciliationPage() {
  const t = useT();

  return (
    <Box>
      <PageHeader title={t('dkv.title')} description={t('dkv.description')} />
      <Stack spacing={3}>
        <UploadSection />
        <WorkList />
        <History />
      </Stack>
    </Box>
  );
}

// ---- upload ----------------------------------------------------------------

function UploadSection() {
  const t = useT();
  const { locale } = useI18n();
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [preview, setPreview] = useState<DkvImportPreview | null>(null);
  const [result, setResult] = useState<DkvImportResult | null>(null);

  const previewMutation = usePreviewDkvImport();
  const importMutation = useImportDkvStatement();

  const runPreview = (selected: File) =>
    previewMutation.mutate(selected, { onSuccess: (data) => setPreview(data) });

  const handleFile = (selected: File | null) => {
    setFileError(null);
    setResult(null);
    setPreview(null);
    setFile(null);

    if (!selected) return;

    const extension = selected.name.slice(selected.name.lastIndexOf('.')).toLowerCase();
    if (!DKV_STATEMENT_ACCEPTED_EXTENSIONS.split(',').includes(extension)) {
      setFileError(t('dkv.invalidFileType'));
      return;
    }

    if (selected.size > MAX_DKV_STATEMENT_BYTES) {
      setFileError(t('dkv.fileTooLarge'));
      return;
    }

    setFile(selected);
    runPreview(selected);
  };

  const handleImport = () => {
    if (!file) return;

    importMutation.mutate(file, {
      onSuccess: (data) => {
        setResult(data);
        setPreview(null);
        setFile(null);
      },
    });
  };

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" sx={{ mb: 2 }}>
        {t('dkv.uploadTitle')}
      </Typography>

      <Stack spacing={2}>
        {fileError && <Alert severity="error">{fileError}</Alert>}
        {previewMutation.isError && (
          <Alert severity="error">{toApiError(previewMutation.error).message}</Alert>
        )}
        {result && (
          <Alert severity="success">
            <AlertTitle>{t('dkv.resultTitle')}</AlertTitle>
            {t('dkv.resultBody', {
              added: result.newCount,
              updated: result.updatedCount,
              duplicate: result.duplicateCount,
              skipped: result.skippedCount,
            })}
          </Alert>
        )}

        <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }} useFlexGap>
          <Button variant="outlined" component="label" loading={previewMutation.isPending}>
            {t('dkv.chooseFile')}
            <input
              type="file"
              hidden
              accept={DKV_STATEMENT_ACCEPTED_EXTENSIONS}
              onChange={(event) => {
                handleFile(event.target.files?.[0] ?? null);
                event.target.value = '';
              }}
            />
          </Button>
          {file && (
            <Typography variant="body2">
              {t('dkv.chosenFile')}: <strong>{file.name}</strong>
            </Typography>
          )}
        </Stack>

        {preview && (
          <>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Chip label={t('dkv.summaryNew', { count: preview.newCount })} color="primary" />
              <Chip label={t('dkv.summaryDuplicate', { count: preview.duplicateCount })} />
              <Chip label={t('dkv.summaryUpdated', { count: preview.updatedCount })} />
              <Chip color="success" variant="outlined" label={t('dkv.summaryMatched', { count: preview.matchedCount })} />
              <Chip color="warning" variant="outlined" label={t('dkv.summaryReview', { count: preview.needsReviewCount })} />
              <Chip color="info" variant="outlined" label={t('dkv.summaryNoEntry', { count: preview.noDriverEntryCount })} />
              <Chip color="error" variant="outlined" label={t('dkv.summaryUnknown', { count: preview.unknownCardCount })} />
              <Chip label={t('dkv.summaryAmount', { amount: formatMoney(preview.newAmount, locale) })} />
            </Stack>

            {preview.parseErrors.length > 0 && (
              <Alert severity="warning">
                <AlertTitle>{t('dkv.parseErrorsTitle', { count: preview.parseErrors.length })}</AlertTitle>
                {preview.parseErrors.slice(0, 5).map((e) => (
                  <Typography key={e.rowNumber} variant="body2">
                    {t('dkv.parseErrorRow', { row: e.rowNumber, reason: e.reason })}
                  </Typography>
                ))}
              </Alert>
            )}

            {preview.unknownCards.length > 0 && (
              <UnknownCards
                cards={preview.unknownCards}
                onAssigned={() => file && runPreview(file)}
              />
            )}

            {preview.rows.length > 0 && <PreviewRows rows={preview.rows} />}

            {importMutation.isError && (
              <Alert severity="error">{toApiError(importMutation.error).message}</Alert>
            )}

            <Box>
              <Button
                variant="contained"
                loading={importMutation.isPending}
                disabled={preview.newCount + preview.updatedCount === 0 && preview.duplicateCount === preview.totalRows}
                onClick={handleImport}
              >
                {t('dkv.confirmImport')}
              </Button>
            </Box>
          </>
        )}
      </Stack>
    </Paper>
  );
}

function UnknownCards({
  cards,
  onAssigned,
}: {
  cards: DkvImportPreview['unknownCards'];
  onAssigned: () => void;
}) {
  const t = useT();
  const { locale } = useI18n();
  const vehicles = useAllVehiclesQuery();
  const assign = useAssignDkvCard();
  const [chosen, setChosen] = useState<Record<string, string>>({});

  return (
    <Alert severity="error" icon={false}>
      <AlertTitle>{t('dkv.unknownCardsTitle', { count: cards.length })}</AlertTitle>
      <Typography variant="body2" sx={{ mb: 1 }}>
        {t('dkv.unknownCardsHint')}
      </Typography>
      {assign.isError && <Alert severity="error">{toApiError(assign.error).message}</Alert>}
      <Stack spacing={1}>
        {cards.map((card) => {
          const vehicleId = chosen[card.cardNumber] ?? card.suggestedVehicleId ?? '';

          return (
            <Stack
              key={card.cardNumber}
              direction={{ xs: 'column', md: 'row' }}
              spacing={1}
              sx={{ alignItems: { md: 'center' } }}
            >
              <Typography variant="body2" sx={{ minWidth: 260 }}>
                <strong>{card.cardNumber}</strong>
                {card.statementVehicleLabel ? ` · ${card.statementVehicleLabel}` : ''} ·{' '}
                {t('dkv.unknownCardRows', {
                  count: card.rowCount,
                  amount: formatMoney(card.totalAmount, locale),
                })}
              </Typography>
              <VehicleSelect
                value={vehicleId}
                vehicles={vehicles.data?.items ?? []}
                onChange={(id) => setChosen((c) => ({ ...c, [card.cardNumber]: id }))}
              />
              <Button
                size="small"
                variant="contained"
                disabled={!vehicleId}
                loading={assign.isPending}
                onClick={() =>
                  assign.mutate({ cardNumber: card.cardNumber, vehicleId }, { onSuccess: onAssigned })
                }
              >
                {t('dkv.assignCard')}
              </Button>
            </Stack>
          );
        })}
      </Stack>
    </Alert>
  );
}

function VehicleSelect({
  value,
  vehicles,
  onChange,
}: {
  value: string;
  vehicles: { id: string; brand: string; model: string; registrationNumber: string; tdNumber: string | null }[];
  onChange: (id: string) => void;
}) {
  const t = useT();

  return (
    <FormControl size="small" sx={{ minWidth: 280 }}>
      <InputLabel>{t('dkv.vehicle')}</InputLabel>
      <Select label={t('dkv.vehicle')} value={value} onChange={(event) => onChange(event.target.value)}>
        {vehicles.map((v) => (
          <MenuItem key={v.id} value={v.id}>
            {v.tdNumber ? `${v.tdNumber} · ` : ''}
            {v.brand} {v.model} ({v.registrationNumber})
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

function PreviewRows({ rows }: { rows: DkvImportPreview['rows'] }) {
  const t = useT();
  const { locale } = useI18n();

  return (
    <Box>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>
        {t('dkv.rowsTitle', { count: rows.length })}
      </Typography>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('dkv.colDate')}</TableCell>
              <TableCell>{t('dkv.colCard')}</TableCell>
              <TableCell>{t('dkv.colVehicle')}</TableCell>
              <TableCell>{t('dkv.colProduct')}</TableCell>
              <TableCell align="right">{t('dkv.colAmount')}</TableCell>
              <TableCell>{t('dkv.colStatus')}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={`${row.rowNumber}-${row.cardNumber}-${row.occurredAtTime}`}>
                <TableCell>
                  {formatDate(row.occurredOn)} {time(row.occurredAtTime)}
                </TableCell>
                <TableCell>{row.cardNumber}</TableCell>
                <TableCell>{row.vehicleName ?? row.statementVehicleLabel ?? '—'}</TableCell>
                <TableCell>{row.productType ?? '—'}</TableCell>
                <TableCell align="right">{formatMoney(row.amount, locale)}</TableCell>
                <TableCell>
                  <StatusChip status={row.status} detail={row.issueDetail} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}

function StatusChip({ status, detail }: { status: FuelTransactionStatus; detail?: string | null }) {
  const t = useT();
  const chip = (
    <Chip size="small" color={STATUS_COLOR[status]} label={t(`dkv.status.${status}`)} />
  );

  return detail ? <Tooltip title={detail}>{chip}</Tooltip> : chip;
}

// ---- work list -------------------------------------------------------------

type Action =
  | { kind: 'confirm' | 'ignore'; row: FuelTransaction }
  | { kind: 'link' | 'create' | 'assign'; row: FuelTransaction };

function WorkList() {
  const t = useT();
  const { locale } = useI18n();
  // `?search=` is how the Ctrl+K search lands here: every state, narrowed to what was typed.
  const [params, setParams] = useSearchParams();
  const search = params.get('search')?.trim() || undefined;
  const [filterKey, setFilterKey] = useState(search ? 'all' : 'open');
  const [page, setPage] = useState(1);
  const [action, setAction] = useState<Action | null>(null);
  const [rechecked, setRechecked] = useState<number | null>(null);

  const filter = FILTERS.find((f) => f.key === filterKey) ?? FILTERS[0];
  const list = useFuelTransactionsQuery({
    pageNumber: page,
    pageSize: 25,
    status: filter.statuses.length > 0 ? filter.statuses : undefined,
    search,
  });
  const counts = useFuelTransactionCountsQuery();
  const recheck = useRecheckDkvTransactions();

  const countFor = (statuses: FuelTransactionStatus[]) =>
    statuses.reduce((sum, s) => sum + (counts.data?.[s] ?? 0), 0);

  const rows = list.data?.items ?? [];

  return (
    <Paper sx={{ p: 3 }}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ mb: 2, alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}
        useFlexGap
      >
        <Typography variant="h6">{t('dkv.workTitle')}</Typography>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          {rechecked !== null && (
            <Typography variant="body2" color="text.secondary">
              {t('dkv.rechecked', { count: rechecked })}
            </Typography>
          )}
          <Button
            size="small"
            variant="outlined"
            loading={recheck.isPending}
            onClick={() => recheck.mutate(undefined, { onSuccess: (n) => setRechecked(n) })}
          >
            {t('dkv.recheck')}
          </Button>
        </Stack>
      </Stack>

      {search && (
        <Chip
          sx={{ mb: 2 }}
          color="primary"
          variant="outlined"
          label={t('dkv.searchFilter', { term: search })}
          onDelete={() => {
            setParams({}, { replace: true });
            setFilterKey('open');
            setPage(1);
          }}
        />
      )}

      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
        {FILTERS.map((f) => (
          <Chip
            key={f.key}
            clickable
            color={filterKey === f.key ? 'primary' : 'default'}
            variant={filterKey === f.key ? 'filled' : 'outlined'}
            label={`${t(`dkv.filter.${f.key as 'all' | 'open' | 'matched' | 'settled'}`)} (${
              f.statuses.length > 0
                ? countFor(f.statuses)
                : countFor(['Matched', 'NeedsReview', 'NoDriverEntry', 'UnknownCard', 'Resolved', 'Ignored'])
            })`}
            onClick={() => {
              setFilterKey(f.key);
              setPage(1);
            }}
          />
        ))}
      </Stack>

      {list.isError && <Alert severity="error">{toApiError(list.error).message}</Alert>}

      {rows.length === 0 && !list.isLoading ? (
        <Typography color="text.secondary">{t('dkv.empty')}</Typography>
      ) : (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('dkv.colDate')}</TableCell>
                <TableCell>{t('dkv.colVehicle')}</TableCell>
                <TableCell>{t('dkv.colProduct')}</TableCell>
                <TableCell align="right">{t('dkv.colAmount')}</TableCell>
                <TableCell>{t('dkv.colStatus')}</TableCell>
                <TableCell>{t('dkv.colDriverEntry')}</TableCell>
                <TableCell align="right">{t('dkv.colActions')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id} hover>
                  <TableCell>
                    {formatDate(row.occurredOn)} {time(row.occurredAtTime)}
                    {!row.isInvoiced && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        {t('dkv.notInvoiced')}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {row.vehicleName ?? row.cardNumber}
                    {row.vehicleTdNumber && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        {t('vehicles.tdShort')} {row.vehicleTdNumber}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{row.productType ?? '—'}</TableCell>
                  <TableCell align="right">{formatMoney(row.amount, locale)}</TableCell>
                  <TableCell>
                    <StatusChip status={row.status} detail={row.issueDetail} />
                    {row.issueDetail && row.status !== 'Matched' && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', maxWidth: 280 }}>
                        {row.issueDetail}
                      </Typography>
                    )}
                    {row.resolutionNote && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', maxWidth: 280 }}>
                        {row.resolutionNote}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {row.expenseAmount != null
                      ? `${formatMoney(row.expenseAmount, locale)}${
                          row.expenseLitres ? ` · ${row.expenseLitres} L` : ''
                        } · ${formatDate(row.expenseOccurredOn)}`
                      : '—'}
                  </TableCell>
                  <TableCell align="right">
                    <RowActions row={row} onAction={(kind) => setAction({ kind, row })} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {list.data && list.data.totalPages > 1 && (
        <Stack direction="row" spacing={1} sx={{ mt: 2, alignItems: 'center' }}>
          <Button size="small" disabled={!list.data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
            {t('dkv.prev')}
          </Button>
          <Typography variant="body2">
            {page} / {list.data.totalPages}
          </Typography>
          <Button size="small" disabled={!list.data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
            {t('dkv.next')}
          </Button>
        </Stack>
      )}

      <ActionDialogs action={action} onClose={() => setAction(null)} />
    </Paper>
  );
}

function RowActions({
  row,
  onAction,
}: {
  row: FuelTransaction;
  onAction: (kind: 'confirm' | 'ignore' | 'link' | 'create' | 'assign') => void;
}) {
  const t = useT();

  if (row.status === 'UnknownCard') {
    return (
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
        <Button size="small" onClick={() => onAction('assign')}>
          {t('dkv.actAssign')}
        </Button>
        <Button size="small" color="inherit" onClick={() => onAction('ignore')}>
          {t('dkv.actIgnore')}
        </Button>
      </Stack>
    );
  }

  if (row.status === 'NeedsReview') {
    return (
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
        <Button size="small" onClick={() => onAction('link')}>
          {t('dkv.actLink')}
        </Button>
        <Button size="small" onClick={() => onAction('confirm')}>
          {t('dkv.actConfirm')}
        </Button>
        <Button size="small" color="inherit" onClick={() => onAction('ignore')}>
          {t('dkv.actIgnore')}
        </Button>
      </Stack>
    );
  }

  if (row.status === 'NoDriverEntry') {
    return (
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
        <Button size="small" onClick={() => onAction('link')}>
          {t('dkv.actLink')}
        </Button>
        <Button size="small" onClick={() => onAction('create')}>
          {t('dkv.actCreate')}
        </Button>
        <Button size="small" onClick={() => onAction('confirm')}>
          {t('dkv.actConfirmNoEntry')}
        </Button>
        <Button size="small" color="inherit" onClick={() => onAction('ignore')}>
          {t('dkv.actIgnore')}
        </Button>
      </Stack>
    );
  }

  return null;
}

function ActionDialogs({ action, onClose }: { action: Action | null; onClose: () => void }) {
  const t = useT();
  const resolve = useResolveFuelTransaction();

  const row = action?.row;

  return (
    <>
      <ReasonDialog
        open={action?.kind === 'confirm'}
        title={t('dkv.confirmTitle')}
        hint={t('dkv.confirmHint')}
        label={t('dkv.reasonLabel')}
        submitLabel={t('dkv.actConfirm')}
        onClose={onClose}
        onSubmit={async (note) => {
          await resolve.mutateAsync({ id: row!.id, input: { resolution: 'Confirm', note } });
          onClose();
        }}
      />
      <ReasonDialog
        open={action?.kind === 'ignore'}
        title={t('dkv.ignoreTitle')}
        hint={t('dkv.ignoreHint')}
        label={t('dkv.reasonLabel')}
        submitLabel={t('dkv.actIgnore')}
        onClose={onClose}
        onSubmit={async (note) => {
          await resolve.mutateAsync({ id: row!.id, input: { resolution: 'Ignore', note } });
          onClose();
        }}
      />
      {action?.kind === 'create' && (
        <CreateDialog
          row={action.row}
          onClose={onClose}
          onCreate={async (litres, odometerKm) => {
            await resolve.mutateAsync({
              id: action.row.id,
              input: { resolution: 'CreateExpense', litres, odometerKm },
            });
            onClose();
          }}
        />
      )}
      {action?.kind === 'link' && (
        <LinkDialog
          row={action.row}
          onClose={onClose}
          onPick={async (expenseId) => {
            await resolve.mutateAsync({
              id: action.row.id,
              input: { resolution: 'LinkExpense', expenseId },
            });
            onClose();
          }}
        />
      )}
      {action?.kind === 'assign' && <AssignDialog row={action.row} onClose={onClose} />}
    </>
  );
}

function CreateDialog({
  row,
  onClose,
  onCreate,
}: {
  row: FuelTransaction;
  onClose: () => void;
  onCreate: (litres: number, odometerKm?: number) => Promise<void>;
}) {
  const t = useT();
  const { locale } = useI18n();
  const [litres, setLitres] = useState('');
  const [km, setKm] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const litresValue = Number(litres.replace(',', '.'));
  const valid = litres.trim() !== '' && Number.isFinite(litresValue) && litresValue > 0;

  const submit = async () => {
    setPending(true);
    setError(null);

    try {
      await onCreate(litresValue, km.trim() === '' ? undefined : Math.round(Number(km)));
    } catch (e) {
      setError(toApiError(e).message);
    } finally {
      setPending(false);
    }
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('dkv.createTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          {t('dkv.createDescription', {
            amount: formatMoney(row.amount, locale),
            date: formatDate(row.occurredOn),
          })}
        </DialogContentText>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Stack spacing={2}>
          <TextField
            autoFocus
            required
            label={t('dkv.litres')}
            value={litres}
            onChange={(event) => setLitres(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'decimal' } }}
          />
          <TextField
            label={t('dkv.odometer')}
            value={km}
            onChange={(event) => setKm(event.target.value)}
            slotProps={{ htmlInput: { inputMode: 'numeric' } }}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={pending}>
          {t('common.cancel')}
        </Button>
        <Button variant="contained" disabled={!valid} loading={pending} onClick={() => void submit()}>
          {t('dkv.actCreate')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function LinkDialog({
  row,
  onClose,
  onPick,
}: {
  row: FuelTransaction;
  onClose: () => void;
  onPick: (expenseId: string) => Promise<void>;
}) {
  const t = useT();
  const { locale } = useI18n();
  const candidates = useFuelExpenseCandidatesQuery(row.id);
  const [error, setError] = useState<string | null>(null);

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('dkv.linkTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 1 }}>
          {t('dkv.linkHint', { amount: formatMoney(row.amount, locale), date: formatDate(row.occurredOn) })}
        </DialogContentText>
        {error && <Alert severity="error">{error}</Alert>}
        {candidates.isLoading && <Typography>…</Typography>}
        {candidates.data && candidates.data.length === 0 && (
          <Typography color="text.secondary">{t('dkv.noCandidates')}</Typography>
        )}
        <List>
          {candidates.data?.map((c) => (
            <ListItemButton
              key={c.expenseId}
              onClick={() => onPick(c.expenseId).catch((e: unknown) => setError(toApiError(e).message))}
            >
              <ListItemText
                primary={`${formatDate(c.occurredOn)} · ${formatMoney(c.amount, locale)}`}
                secondary={[c.litres ? `${c.litres} L` : null, c.odometerKm ? `${c.odometerKm} km` : null, c.fuelProductType]
                  .filter(Boolean)
                  .join(' · ')}
              />
            </ListItemButton>
          ))}
        </List>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
      </DialogActions>
    </Dialog>
  );
}

function AssignDialog({ row, onClose }: { row: FuelTransaction; onClose: () => void }) {
  const t = useT();
  const vehicles = useAllVehiclesQuery();
  const assign = useAssignDkvCard();
  const [vehicleId, setVehicleId] = useState('');

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('dkv.assignTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          {t('dkv.assignHint', { card: row.cardNumber })}
        </DialogContentText>
        {assign.isError && <Alert severity="error">{toApiError(assign.error).message}</Alert>}
        <VehicleSelect value={vehicleId} vehicles={vehicles.data?.items ?? []} onChange={setVehicleId} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!vehicleId}
          loading={assign.isPending}
          onClick={() =>
            assign.mutate({ cardNumber: row.cardNumber, vehicleId }, { onSuccess: onClose })
          }
        >
          {t('dkv.assignCard')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ---- history ---------------------------------------------------------------

function History() {
  const t = useT();
  const batches = useFuelImportBatchesQuery();
  const items = batches.data?.items ?? [];

  if (items.length === 0) return null;

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" sx={{ mb: 2 }}>
        {t('dkv.historyTitle')}
      </Typography>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('dkv.histWhen')}</TableCell>
              <TableCell>{t('dkv.histFile')}</TableCell>
              <TableCell>{t('dkv.histBy')}</TableCell>
              <TableCell align="right">{t('dkv.histNew')}</TableCell>
              <TableCell align="right">{t('dkv.histUpdated')}</TableCell>
              <TableCell align="right">{t('dkv.histDuplicate')}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((b) => (
              <TableRow key={b.id}>
                <TableCell>{formatDateTime(b.importedAt)}</TableCell>
                <TableCell>{b.fileName}</TableCell>
                <TableCell>{b.importedByEmail ?? '—'}</TableCell>
                <TableCell align="right">{b.newCount}</TableCell>
                <TableCell align="right">{b.updatedCount}</TableCell>
                <TableCell align="right">{b.duplicateCount}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );
}
