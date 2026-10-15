import {
  Alert,
  Autocomplete,
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
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { newWorkerPackage, type DocumentRequestItem } from '../../api/documentRequests';
import type { AttachmentCategory } from '../../api/types';
import { useCreateDocumentRequests } from '../../features/documentRequests/useDocumentRequests';
import { useEveryEmployeeQuery } from '../../features/employees/useEmployees';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';

const OTHER_CATEGORIES: readonly AttachmentCategory[] = [
  'MedicalCheck',
  'Identity',
  'Visa',
  'Licence',
  'Certificate',
  'Diploma',
  'Contract',
  'Insurance',
  'Other',
];

interface Choice {
  key: string;
  category: AttachmentCategory;
  title: string;
}

/**
 * Asks one or more people for documents. The "new worker" package is a set of ready choices, each of
 * which becomes its own request; anything else is typed as one more line.
 */
export function RequestDocumentDialog({
  open,
  onClose,
  initialEmployeeIds = [],
}: {
  open: boolean;
  onClose: () => void;
  initialEmployeeIds?: string[];
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const create = useCreateDocumentRequests();
  const employees = useEveryEmployeeQuery();

  const people = useMemo(() => employees.data?.items ?? [], [employees.data]);

  const [selected, setSelected] = useState<string[]>(initialEmployeeIds);
  const [packageOn, setPackageOn] = useState<Record<string, boolean>>({});
  const [customOn, setCustomOn] = useState(false);
  const [customCategory, setCustomCategory] = useState<AttachmentCategory>('MedicalCheck');
  const [customTitle, setCustomTitle] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [note, setNote] = useState('');
  const [failure, setFailure] = useState<string | null>(null);
  const [result, setResult] = useState<{ created: number; skipped: number } | null>(null);

  useEffect(() => {
    if (open) {
      setSelected(initialEmployeeIds);
      setPackageOn({});
      setCustomOn(false);
      setCustomTitle('');
      setDueDate('');
      setNote('');
      setFailure(null);
      setResult(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const choices: Choice[] = useMemo(
    () =>
      newWorkerPackage.map((item) => ({
        key: item.titleKey,
        category: item.category,
        title: t(item.titleKey as MessageKey),
      })),
    [t],
  );

  const items: DocumentRequestItem[] = [
    ...choices
      .filter((choice) => packageOn[choice.key])
      .map((choice) => ({
        category: choice.category,
        title: choice.title,
        note: note.trim() || null,
        dueDate: dueDate || null,
      })),
    ...(customOn && customTitle.trim()
      ? [{ category: customCategory, title: customTitle.trim(), note: note.trim() || null, dueDate: dueDate || null }]
      : []),
  ];

  const submit = async () => {
    setFailure(null);

    try {
      const outcome = await create.mutateAsync({ employeeIds: selected, items });
      setResult({ created: outcome.created.length, skipped: outcome.skipped });
    } catch (error) {
      setFailure(toApiError(error).message);
    }
  };

  const chosen = people.filter((person) => selected.includes(person.id));

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('docRequests.new.title')}</DialogTitle>
      <DialogContent>
        {result ? (
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Alert severity="success">
              {t('docRequests.new.done', { count: result.created })}
              {result.skipped > 0 && ` ${t('docRequests.new.skipped', { count: result.skipped })}`}
            </Alert>
            <Typography variant="body2" color="text.secondary">
              {t('docRequests.new.doneHint')}
            </Typography>
          </Stack>
        ) : (
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Autocomplete
              multiple
              options={people}
              loading={employees.isLoading}
              value={chosen}
              getOptionLabel={(person) => person.fullName}
              onChange={(_, value) => setSelected(value.map((person) => person.id))}
              renderInput={(params) => <TextField {...params} label={t('docRequests.new.people')} />}
            />

            <Stack>
              <Typography variant="subtitle2">{t('docRequests.new.package')}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                {t('docRequests.new.packageHint')}
              </Typography>
              {choices.map((choice) => (
                <FormControlLabel
                  key={choice.key}
                  control={
                    <Checkbox
                      checked={!!packageOn[choice.key]}
                      onChange={(event) =>
                        setPackageOn((current) => ({ ...current, [choice.key]: event.target.checked }))
                      }
                    />
                  }
                  label={`${choice.title} (${enumLabel('attachmentCategory', choice.category)})`}
                />
              ))}
              <Button
                size="small"
                sx={{ alignSelf: 'flex-start', mt: 0.5 }}
                onClick={() =>
                  setPackageOn(Object.fromEntries(choices.map((choice) => [choice.key, true])))
                }
              >
                {t('docRequests.new.selectAll')}
              </Button>
            </Stack>

            <Stack spacing={1}>
              <FormControlLabel
                control={<Checkbox checked={customOn} onChange={(event) => setCustomOn(event.target.checked)} />}
                label={t('docRequests.new.other')}
              />
              {customOn && (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                  <TextField
                    select
                    size="small"
                    label={t('docRequests.new.type')}
                    value={customCategory}
                    onChange={(event) => setCustomCategory(event.target.value as AttachmentCategory)}
                    sx={{ minWidth: 190 }}
                  >
                    {OTHER_CATEGORIES.map((category) => (
                      <MenuItem key={category} value={category}>
                        {enumLabel('attachmentCategory', category)}
                      </MenuItem>
                    ))}
                  </TextField>
                  <TextField
                    size="small"
                    fullWidth
                    label={t('docRequests.new.titleField')}
                    value={customTitle}
                    onChange={(event) => setCustomTitle(event.target.value)}
                    slotProps={{ htmlInput: { maxLength: 200 } }}
                  />
                </Stack>
              )}
            </Stack>

            <TextField
              type="date"
              size="small"
              label={t('docRequests.new.due')}
              value={dueDate}
              onChange={(event) => setDueDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              size="small"
              multiline
              minRows={2}
              label={t('docRequests.new.note')}
              helperText={t('docRequests.new.noteHint')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 500 } }}
            />
            {failure && <Alert severity="error">{failure}</Alert>}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{result ? t('common.close') : t('common.cancel')}</Button>
        {!result && (
          <Button
            variant="contained"
            disabled={create.isPending || selected.length === 0 || items.length === 0}
            onClick={() => void submit()}
          >
            {t('docRequests.new.send')}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
