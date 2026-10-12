import { DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Chip,
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
import {
  bookApi,
  bookCategoryBuckets,
  bookCategoryKinds,
  type BookCategory,
  type BookCategoryBucket,
  type BookCategoryKind,
} from '../../api/book';
import { PageHeader } from '../../components/PageHeader';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';

const categoriesKey = ['book', 'categories'] as const;
const rulesKey = ['book', 'rules'] as const;

/**
 * The categories entries of the book are filed under, and the rules that suggest one from a
 * description. The system's own categories can be renamed but not switched off, because the
 * monthly settlement and the overview rely on them.
 */
export function BookCategoriesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();

  const categories = useQuery({
    queryKey: [...categoriesKey, 'all'] as const,
    queryFn: () => bookApi.categories.list(true),
  });
  const rules = useQuery({ queryKey: rulesKey, queryFn: () => bookApi.rules.list() });

  const refreshCategories = () => {
    void queryClient.invalidateQueries({ queryKey: categoriesKey });
    void queryClient.invalidateQueries({ queryKey: rulesKey });
  };

  const toggle = useMutation({
    mutationFn: (category: BookCategory) =>
      bookApi.categories.update(category.id, {
        name: category.name,
        group: category.group,
        sortOrder: category.sortOrder,
        isActive: !category.isActive,
      }),
    onSuccess: refreshCategories,
  });

  const rename = useMutation({
    mutationFn: ({ category, name }: { category: BookCategory; name: string }) =>
      bookApi.categories.update(category.id, {
        name,
        group: category.group,
        sortOrder: category.sortOrder,
        isActive: category.isActive,
      }),
    onSuccess: refreshCategories,
  });

  const removeRule = useMutation({
    mutationFn: (id: string) => bookApi.rules.remove(id),
    onSuccess: refreshCategories,
  });

  const error = categories.error ?? rules.error ?? toggle.error ?? rename.error ?? removeRule.error;

  return (
    <Stack spacing={3}>
      <PageHeader
        title={t('book.categories.title')}
        description={t('book.categories.description')}
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('book.categories.name')}</TableCell>
              <TableCell>{t('book.categories.kind')}</TableCell>
              <TableCell>{t('book.categories.bucket')}</TableCell>
              <TableCell align="right">{t('book.categories.entries')}</TableCell>
              <TableCell align="center">{t('book.categories.active')}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(categories.data ?? []).map((category) => (
              <TableRow key={category.id} hover sx={{ opacity: category.isActive ? 1 : 0.55 }}>
                <TableCell>
                  <NameCell
                    category={category}
                    onRename={(name) => rename.mutate({ category, name })}
                  />
                  {category.systemKey && (
                    <Chip
                      size="small"
                      variant="outlined"
                      label={t('book.categories.system')}
                      sx={{ ml: 1 }}
                    />
                  )}
                </TableCell>
                <TableCell>{t(`book.categoryKind.${category.kind}` as MessageKey)}</TableCell>
                <TableCell>{t(`book.bucket.${category.bucket}` as MessageKey)}</TableCell>
                <TableCell align="right">{category.entryCount}</TableCell>
                <TableCell align="center">
                  <Tooltip
                    title={category.systemKey ? t('book.categories.systemCannotOff') : ''}
                  >
                    <span>
                      <Switch
                        checked={category.isActive}
                        disabled={category.systemKey !== null || toggle.isPending}
                        onChange={() => toggle.mutate(category)}
                        slotProps={{ input: { 'aria-label': t('book.categories.active') } }}
                      />
                    </span>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <NewCategory onCreated={refreshCategories} />

      <Stack spacing={1}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          {t('book.rules.title')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('book.rules.description')}
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('book.rules.text')}</TableCell>
              <TableCell>{t('book.rules.category')}</TableCell>
              <TableCell align="right">{t('book.rules.priority')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {(rules.data ?? []).map((rule) => (
              <TableRow key={rule.id} hover>
                <TableCell>{rule.text}</TableCell>
                <TableCell>{rule.categoryName}</TableCell>
                <TableCell align="right">{rule.priority}</TableCell>
                <TableCell align="right">
                  <Tooltip title={t('common.delete')}>
                    <IconButton
                      size="small"
                      onClick={() => removeRule.mutate(rule.id)}
                      aria-label={t('common.delete')}
                    >
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {!rules.isLoading && (rules.data?.length ?? 0) === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Typography color="text.secondary" sx={{ py: 1 }}>
                    {t('book.rules.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      <NewRule categories={categories.data ?? []} onCreated={refreshCategories} />
    </Stack>
  );
}

function NameCell({
  category,
  onRename,
}: {
  category: BookCategory;
  onRename: (name: string) => void;
}) {
  const [value, setValue] = useState(category.name);

  return (
    <TextField
      variant="standard"
      value={value}
      onChange={(event) => setValue(event.target.value)}
      onBlur={() => {
        const next = value.trim();
        if (next && next !== category.name) onRename(next);
        else setValue(category.name);
      }}
      slotProps={{ htmlInput: { maxLength: 100, 'aria-label': category.name } }}
    />
  );
}

function NewCategory({ onCreated }: { onCreated: () => void }) {
  const { t } = useI18n();
  const [name, setName] = useState('');
  const [kind, setKind] = useState<BookCategoryKind>('Cost');
  const [bucket, setBucket] = useState<BookCategoryBucket>('Fixed');

  const create = useMutation({
    mutationFn: () => bookApi.categories.create({ name: name.trim(), kind, group: null, bucket, sortOrder: 50 }),
    onSuccess: () => {
      setName('');
      onCreated();
    },
  });

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack spacing={2}>
        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
          {t('book.categories.add')}
        </Typography>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
          <TextField
            size="small"
            label={t('book.categories.name')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 100 } }}
            fullWidth
          />
          <TextField
            select
            size="small"
            label={t('book.categories.kind')}
            value={kind}
            onChange={(event) => setKind(event.target.value as BookCategoryKind)}
            sx={{ minWidth: 160 }}
          >
            {bookCategoryKinds.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`book.categoryKind.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label={t('book.categories.bucket')}
            value={bucket}
            onChange={(event) => setBucket(event.target.value as BookCategoryBucket)}
            sx={{ minWidth: 220 }}
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
            onClick={() => create.mutate()}
          >
            {t('book.categories.addButton')}
          </Button>
        </Stack>
        {create.error && <Alert severity="error">{toApiError(create.error).message}</Alert>}
      </Stack>
    </Paper>
  );
}

function NewRule({
  categories,
  onCreated,
}: {
  categories: BookCategory[];
  onCreated: () => void;
}) {
  const { t } = useI18n();
  const [text, setText] = useState('');
  const [categoryId, setCategoryId] = useState('');

  const create = useMutation({
    mutationFn: () => bookApi.rules.create({ text: text.trim(), categoryId, priority: 0 }),
    onSuccess: () => {
      setText('');
      onCreated();
    },
  });

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack spacing={2}>
        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
          {t('book.rules.add')}
        </Typography>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
          <TextField
            size="small"
            label={t('book.rules.text')}
            value={text}
            onChange={(event) => setText(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 100 } }}
            fullWidth
          />
          <TextField
            select
            size="small"
            label={t('book.rules.category')}
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            sx={{ minWidth: 240 }}
          >
            {categories
              .filter((category) => category.isActive)
              .map((category) => (
                <MenuItem key={category.id} value={category.id}>
                  {category.name}
                </MenuItem>
              ))}
          </TextField>
          <Button
            variant="contained"
            disabled={text.trim() === '' || categoryId === ''}
            loading={create.isPending}
            onClick={() => create.mutate()}
          >
            {t('book.rules.addButton')}
          </Button>
        </Stack>
        {create.error && <Alert severity="error">{toApiError(create.error).message}</Alert>}
      </Stack>
    </Paper>
  );
}
