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
import {
  bookApi,
  bookCategoryBuckets,
  type BookCategory,
  type BookCategoryBucket,
  type BookCategoryKind,
} from '../../api/book';
import { PageHeader } from '../../components/PageHeader';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';

const categoriesKey = ['book', 'categories'] as const;
const rulesKey = ['book', 'rules'] as const;

const groups = ['Site', 'Labour', 'Fixed', 'Income'] as const;

/** Where the entries of a category come from by themselves, for the system's own categories. */
const sourceOf: Record<string, MessageKey> = {
  Fuel: 'book.categories.src.fuel',
  Housing: 'book.categories.src.housing',
  CarRental: 'book.categories.src.vehicles',
  WorkerPay: 'book.categories.src.settlement',
  Subcontractors: 'book.categories.src.settlement',
  Office: 'book.categories.src.settlement',
  Collection: 'book.categories.src.invoices',
  Tools: 'book.categories.src.tools',
};

/**
 * What kind of money each line of the book is, and where it is counted in the settlement. The system's own
 * categories fill themselves from fuel statements, housing, vehicles and timesheets; you can rename them but
 * not switch them off. The rules guess a category from the description, and you can try them below.
 */
export function BookCategoriesPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();

  const now = new Date();
  const monthName = now.toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', { month: 'long' });

  const categories = useQuery({
    queryKey: [...categoriesKey, 'all', now.getFullYear(), now.getMonth()] as const,
    queryFn: () => bookApi.categories.list(true, { year: now.getFullYear(), month: now.getMonth() + 1 }),
  });
  const rules = useQuery({ queryKey: rulesKey, queryFn: () => bookApi.rules.list() });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: categoriesKey });
    void queryClient.invalidateQueries({ queryKey: rulesKey });
  };

  const update = useMutation({
    mutationFn: (variables: {
      category: BookCategory;
      patch: { name?: string; isActive?: boolean; bucket?: BookCategoryBucket };
    }) =>
      bookApi.categories.update(variables.category.id, {
        name: variables.patch.name ?? variables.category.name,
        group: variables.category.group,
        sortOrder: variables.category.sortOrder,
        isActive: variables.patch.isActive ?? variables.category.isActive,
        bucket: variables.patch.bucket ?? null,
      }),
    onSuccess: refresh,
  });

  // The new category
  const [name, setName] = useState('');
  const [kind, setKind] = useState<BookCategoryKind>('Cost');
  const [group, setGroup] = useState<(typeof groups)[number]>('Site');
  const [bucket, setBucket] = useState<BookCategoryBucket>('SiteCosts');
  const [createMessage, setCreateMessage] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: () =>
      bookApi.categories.create({
        name: name.trim(),
        kind,
        group,
        bucket: kind === 'Income' ? 'Collection' : bucket,
        sortOrder: 50,
      }),
    onSuccess: () => {
      setName('');
      setCreateMessage(t('book.categories.created'));
      refresh();
    },
  });

  // The rules
  const [ruleText, setRuleText] = useState('');
  const [ruleCategory, setRuleCategory] = useState('');

  const addRule = useMutation({
    mutationFn: () => bookApi.rules.create({ text: ruleText.trim(), categoryId: ruleCategory, priority: 0 }),
    onSuccess: () => {
      setRuleText('');
      refresh();
    },
  });

  const removeRule = useMutation({
    mutationFn: (id: string) => bookApi.rules.remove(id),
    onSuccess: refresh,
  });

  // Try a guess
  const [probe, setProbe] = useState('');
  const [debouncedProbe, setDebouncedProbe] = useState('');

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedProbe(probe.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [probe]);

  const guess = useQuery({
    queryKey: ['book', 'guess', debouncedProbe] as const,
    queryFn: () => bookApi.entries.suggestCategory(debouncedProbe),
    enabled: debouncedProbe.length > 0,
  });

  const active = (categories.data ?? []).filter((c) => c.isActive);
  const error = categories.error ?? rules.error ?? update.error ?? create.error ?? addRule.error ?? removeRule.error;

  return (
    <Stack spacing={2}>
      <PageHeader title={t('book.categories.pageTitle')} description={t('book.categories.subtitle')} />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <HowItWorks id="categories">
        <HowLine>{t('book.categories.how1')}</HowLine>
        <HowLine>{t('book.categories.how2')}</HowLine>
        <HowLine>{t('book.categories.how3')}</HowLine>
        <HowLine>{t('book.categories.how4')}</HowLine>
        <HowLine>{t('book.categories.how5')}</HowLine>
      </HowItWorks>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.categories.all')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.categories.name')}</TableCell>
                <TableCell>{t('book.categories.kind')}</TableCell>
                <TableCell>{t('book.categories.group')}</TableCell>
                <TableCell>{t('book.categories.goesTo')}</TableCell>
                <TableCell>{t('book.categories.source')}</TableCell>
                <TableCell align="right">{t('book.categories.inMonth', { month: monthName })}</TableCell>
                <TableCell>{t('book.col.status')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(categories.data ?? []).map((category) => (
                <TableRow key={category.id} hover sx={{ opacity: category.isActive ? 1 : 0.55 }}>
                  <TableCell>
                    <NameField
                      value={category.name}
                      onCommit={(value) => update.mutate({ category, patch: { name: value } })}
                    />
                    {category.systemKey && (
                      <Chip size="small" variant="outlined" label={t('book.categories.system')} sx={{ ml: 1 }} />
                    )}
                  </TableCell>
                  <TableCell>{t(`book.categoryKind.${category.kind}` as MessageKey)}</TableCell>
                  <TableCell>
                    {category.group
                      ? t(`book.categories.group.${category.group}` as MessageKey)
                      : t(
                          `book.categories.group.${
                            category.bucket === 'Collection'
                              ? 'Income'
                              : category.bucket === 'WorkerEarnings'
                                ? 'Labour'
                                : category.bucket === 'Fixed'
                                  ? 'Fixed'
                                  : 'Site'
                          }` as MessageKey,
                        )}
                  </TableCell>
                  <TableCell>
                    {category.systemKey || category.kind === 'Income' ? (
                      t(`book.bucket.${category.bucket}` as MessageKey)
                    ) : (
                      <TextField
                        select
                        size="small"
                        value={category.bucket}
                        onChange={(event) =>
                          update.mutate({ category, patch: { bucket: event.target.value as BookCategoryBucket } })
                        }
                        sx={{ minWidth: 190 }}
                      >
                        {bookCategoryBuckets
                          .filter((option) => option !== 'Collection')
                          .map((option) => (
                            <MenuItem key={option} value={option}>
                              {t(`book.bucket.${option}` as MessageKey)}
                            </MenuItem>
                          ))}
                      </TextField>
                    )}
                  </TableCell>
                  <TableCell>
                    {category.systemKey && sourceOf[category.systemKey]
                      ? t(sourceOf[category.systemKey]!)
                      : t('book.categories.src.manual')}
                  </TableCell>
                  <TableCell align="right">{category.monthEntryCount}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      variant="outlined"
                      color={category.isActive ? 'success' : 'default'}
                      label={category.isActive ? t('book.recurring.active') : t('book.recurring.off')}
                    />
                  </TableCell>
                  <TableCell align="right">
                    {category.systemKey ? (
                      <Typography variant="caption" color="text.secondary">
                        {t('book.categories.systemCannotOff')}
                      </Typography>
                    ) : (
                      <Button
                        size="small"
                        loading={update.isPending}
                        onClick={() => update.mutate({ category, patch: { isActive: !category.isActive } })}
                      >
                        {category.isActive ? t('book.recurring.switchOff') : t('book.recurring.switchOn')}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
          {t('book.categories.add')}
        </Typography>
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} useFlexGap>
          <TextField
            size="small"
            label={t('book.categories.name')}
            placeholder={t('book.categories.namePlaceholder')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 100 } }}
            sx={{ minWidth: 220 }}
          />
          <TextField
            select
            size="small"
            label={t('book.categories.kind')}
            value={kind}
            onChange={(event) => setKind(event.target.value as BookCategoryKind)}
            sx={{ minWidth: 130 }}
          >
            <MenuItem value="Cost">{t('book.categoryKind.Cost')}</MenuItem>
            <MenuItem value="Income">{t('book.categoryKind.Income')}</MenuItem>
          </TextField>
          <TextField
            select
            size="small"
            label={t('book.categories.group')}
            value={group}
            onChange={(event) => setGroup(event.target.value as (typeof groups)[number])}
            sx={{ minWidth: 150 }}
          >
            {groups.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.categories.group.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label={t('book.categories.goesTo')}
            value={kind === 'Income' ? 'Collection' : bucket}
            disabled={kind === 'Income'}
            onChange={(event) => setBucket(event.target.value as BookCategoryBucket)}
            sx={{ minWidth: 200 }}
          >
            {bookCategoryBuckets.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.bucket.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <Button
            variant="contained"
            disabled={name.trim() === ''}
            loading={create.isPending}
            onClick={() => {
              setCreateMessage(null);
              create.mutate();
            }}
          >
            {t('book.categories.addButton')}
          </Button>
        </Stack>
        {createMessage && (
          <Alert severity="success" sx={{ mt: 1.5 }} onClose={() => setCreateMessage(null)}>
            {createMessage}
          </Alert>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.rules.pageTitle')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('book.rules.pageHelp')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.rules.ifContains')}</TableCell>
                <TableCell>{t('book.col.category')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(rules.data ?? []).map((rule) => (
                <TableRow key={rule.id} hover>
                  <TableCell>"{rule.text}"</TableCell>
                  <TableCell>{rule.categoryName}</TableCell>
                  <TableCell align="right">
                    <Button size="small" color="inherit" onClick={() => removeRule.mutate(rule.id)}>
                      {t('book.entries.remove')}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {!rules.isLoading && (rules.data?.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={3}>
                    <Typography color="text.secondary" sx={{ py: 1 }}>
                      {t('book.rules.empty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} useFlexGap sx={{ mt: 1.5 }}>
          <TextField
            size="small"
            label={t('book.rules.newWord')}
            placeholder={t('book.rules.newWordPlaceholder')}
            value={ruleText}
            onChange={(event) => setRuleText(event.target.value)}
            sx={{ minWidth: 220 }}
          />
          <TextField
            select
            size="small"
            label={t('book.col.category')}
            value={ruleCategory}
            onChange={(event) => setRuleCategory(event.target.value)}
            sx={{ minWidth: 220 }}
          >
            {active.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </TextField>
          <Button
            variant="contained"
            disabled={ruleText.trim() === '' || ruleCategory === ''}
            loading={addRule.isPending}
            onClick={() => addRule.mutate()}
          >
            {t('book.rules.addButton')}
          </Button>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.rules.tryGuess')}
        </Typography>
        <TextField
          size="small"
          fullWidth
          label={t('book.rules.tryLabel')}
          placeholder={t('book.rules.tryPlaceholder')}
          value={probe}
          onChange={(event) => setProbe(event.target.value)}
        />
        {debouncedProbe && (
          <Alert severity={guess.data?.categoryId ? 'success' : 'info'} sx={{ mt: 1.5 }}>
            {guess.data?.categoryId
              ? t('book.rules.guessed', {
                  category: guess.data.categoryName ?? '',
                  rule: guess.data.matchedRule ?? '',
                })
              : t('book.rules.noGuess')}
          </Alert>
        )}
      </Paper>
    </Stack>
  );
}

/** A name typed in place and saved when the field is left. */
function NameField({ value, onCommit }: { value: string; onCommit: (value: string) => void }) {
  const [text, setText] = useState(value);

  useEffect(() => {
    setText(value);
  }, [value]);

  return (
    <TextField
      variant="standard"
      value={text}
      onChange={(event) => setText(event.target.value)}
      onBlur={() => {
        const next = text.trim();

        if (!next) {
          setText(value);
          return;
        }

        if (next !== value) onCommit(next);
      }}
      slotProps={{ htmlInput: { maxLength: 100, 'aria-label': value } }}
    />
  );
}
