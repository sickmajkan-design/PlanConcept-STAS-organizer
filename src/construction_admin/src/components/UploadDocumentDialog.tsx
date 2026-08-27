import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import { ACCEPTED_EXTENSIONS, MAX_ATTACHMENT_BYTES } from '../api/attachments';
import type { AttachmentCategory, AttachmentOwnerType } from '../api/types';
import { useUploadAttachment } from '../features/attachments/useAttachments';
import { useAllEmployeesQuery } from '../features/employees/useEmployees';
import { useAllProjectsQuery } from '../features/projects/useProjects';
import { useAllToolsQuery } from '../features/tools/useTools';
import { useAllVehiclesQuery } from '../features/vehicles/useVehicles';
import type { MessageKey } from '../i18n/en';
import { useEnumLabel } from '../i18n/enumLabels';
import { useT } from '../i18n/useI18n';

const OWNER_TYPE_LABEL_KEYS: Record<'Employee' | 'Project' | 'Vehicle' | 'Tool', MessageKey> = {
  Employee: 'attachments.ownerTypeEmployee',
  Project: 'attachments.ownerTypeProject',
  Vehicle: 'attachments.ownerTypeVehicle',
  Tool: 'attachments.ownerTypeTool',
};

/**
 * The owner types a document actually expires against — a receipt or a task
 * photo has no certificate to renew, so those owner types are left out here
 * even though the API accepts them.
 */
const DOCUMENT_OWNER_TYPES: readonly ('Employee' | 'Project' | 'Vehicle' | 'Tool')[] = [
  'Employee',
  'Project',
  'Vehicle',
  'Tool',
];

/** Mirrors the category list each detail page already offers its own AttachmentList. */
const CATEGORIES_BY_OWNER_TYPE: Record<AttachmentOwnerType, readonly AttachmentCategory[]> = {
  Employee: ['Contract', 'Certificate', 'MedicalCheck', 'Licence', 'Other'],
  Project: ['SiteDocument', 'Photo', 'Licence', 'Insurance', 'Other'],
  Vehicle: ['Insurance', 'Licence', 'Certificate', 'Photo', 'Other'],
  Tool: ['Certificate', 'Licence', 'Photo', 'Other'],
  WorkItem: ['Photo', 'Other'],
  VehicleExpense: ['Other'],
  MaterialMovement: ['Other'],
  EmployeeRate: ['Contract', 'Other'],
  FinanceEntry: ['Other'],
  ToolExpense: ['Other'],
};

/**
 * Uploads a document without starting from the record it belongs to — the
 * owner is picked here instead of being fixed by the page. Used from the
 * expiring-documents hub, where there is no single "this record" to hang the
 * upload button off.
 */
export function UploadDocumentDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const upload = useUploadAttachment();

  const [ownerType, setOwnerType] = useState<AttachmentOwnerType>('Employee');
  const [ownerId, setOwnerId] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [category, setCategory] = useState<AttachmentCategory>('Certificate');
  const [description, setDescription] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  const { data: employees } = useAllEmployeesQuery();
  const { data: projects } = useAllProjectsQuery();
  const { data: vehicles } = useAllVehiclesQuery();
  const { data: tools } = useAllToolsQuery();

  const ownerOptions = useMemo(() => {
    switch (ownerType) {
      case 'Employee':
        return (employees?.items ?? []).map((e) => ({
          id: e.id,
          label: `${e.firstName} ${e.lastName}`,
        }));
      case 'Project':
        return (projects?.items ?? []).map((p) => ({ id: p.id, label: p.name }));
      case 'Vehicle':
        return (vehicles?.items ?? []).map((v) => ({
          id: v.id,
          label: `${v.brand} ${v.model} (${v.registrationNumber})`,
        }));
      case 'Tool':
        return (tools?.items ?? []).map((tool) => ({ id: tool.id, label: tool.name }));
      default:
        return [];
    }
  }, [ownerType, employees, projects, vehicles, tools]);

  const categories = CATEGORIES_BY_OWNER_TYPE[ownerType];

  // A photograph has no expiry, and the API refuses one.
  const expiryAllowed = category !== 'Photo';

  const reset = () => {
    setOwnerType('Employee');
    setOwnerId('');
    setFile(null);
    setCategory('Certificate');
    setDescription('');
    setExpiresAt('');
    setLocalError(null);
  };

  const resetUpload = upload.reset;

  useEffect(() => {
    if (open) {
      reset();
      resetUpload();
    }
    // `reset` is a fresh closure every render; only `open` becoming true
    // should re-run this.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, resetUpload]);

  const changeOwnerType = (next: AttachmentOwnerType) => {
    setOwnerType(next);
    // The record and category picked for the previous owner type make no
    // sense for this one.
    setOwnerId('');
    setCategory(CATEGORIES_BY_OWNER_TYPE[next][0]!);
  };

  const close = () => {
    reset();
    onClose();
  };

  const pick = (chosen: File | null) => {
    setLocalError(null);

    if (chosen && chosen.size > MAX_ATTACHMENT_BYTES) {
      setLocalError(
        t('attachments.tooLarge', {
          limit: Math.round(MAX_ATTACHMENT_BYTES / (1024 * 1024)),
        }),
      );
      setFile(null);
      return;
    }

    setFile(chosen);
  };

  const canSubmit = !!file && ownerId !== '';

  const submit = async () => {
    if (!file || ownerId === '') {
      return;
    }

    await upload.mutateAsync(
      {
        ownerType,
        ownerId,
        category,
        file,
        description: description.trim() || null,
        expiresAt: expiryAllowed && expiresAt ? expiresAt : null,
      },
      { onSuccess: close },
    );
  };

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('attachments.uploadTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ mt: 1 }}>
          {localError && <Alert severity="error">{localError}</Alert>}
          {upload.error && <Alert severity="error">{upload.error.message}</Alert>}

          <FormControl fullWidth>
            <InputLabel id="upload-owner-type-label">
              {t('attachments.ownerType')}
            </InputLabel>
            <Select
              labelId="upload-owner-type-label"
              label={t('attachments.ownerType')}
              value={ownerType}
              onChange={(event) =>
                changeOwnerType(event.target.value as AttachmentOwnerType)
              }
            >
              {DOCUMENT_OWNER_TYPES.map((value) => (
                <MenuItem key={value} value={value}>
                  {t(OWNER_TYPE_LABEL_KEYS[value])}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth>
            <InputLabel id="upload-owner-record-label">
              {t('attachments.ownerRecord')}
            </InputLabel>
            <Select
              labelId="upload-owner-record-label"
              label={t('attachments.ownerRecord')}
              value={ownerId}
              onChange={(event) => setOwnerId(event.target.value)}
            >
              {ownerOptions.map((option) => (
                <MenuItem key={option.id} value={option.id}>
                  {option.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <Button variant="outlined" component="label">
            {file ? file.name : t('attachments.chooseFile')}
            <input
              hidden
              type="file"
              accept={ACCEPTED_EXTENSIONS}
              onChange={(event) => pick(event.target.files?.[0] ?? null)}
            />
          </Button>

          <FormControl fullWidth>
            <InputLabel id="upload-category-label">
              {t('attachments.category')}
            </InputLabel>
            <Select
              labelId="upload-category-label"
              label={t('attachments.category')}
              value={category}
              onChange={(event) =>
                setCategory(event.target.value as AttachmentCategory)
              }
            >
              {categories.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('attachmentCategory', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label={t('attachments.description')}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            fullWidth
          />

          <TextField
            label={t('attachments.expiresAt')}
            type="date"
            value={expiresAt}
            onChange={(event) => setExpiresAt(event.target.value)}
            disabled={!expiryAllowed}
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
            helperText={
              expiryAllowed
                ? t('attachments.expiresHint')
                : t('attachments.photoNoExpiry')
            }
          />

          <Typography variant="caption" color="text.secondary">
            {ACCEPTED_EXTENSIONS.replaceAll(',', ' ')} ·{' '}
            {Math.round(MAX_ATTACHMENT_BYTES / (1024 * 1024))} MB
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || upload.isPending}
          onClick={() => void submit()}
        >
          {t('attachments.upload')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
