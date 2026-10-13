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
  TextField,
} from '@mui/material';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

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
import { BranchSelect } from '../branches/BranchSelect';
import { useAllProjectsQuery } from '../projects/useProjects';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';

const bookKey = ['book'] as const;

function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * The full form for one entry of the book: every field, for a change or for what the quick-entry row does not
 * carry (a due date, a payment method, a business unit).
 */
export function EntryDialog({
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
