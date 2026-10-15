import { FormControl, InputLabel, MenuItem, Select, Stack } from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import type { AttachmentCategory, AttachmentOwnerType } from '../api/types';
import { useAllEmployeesQuery } from '../features/employees/useEmployees';
import { useAllProjectsQuery } from '../features/projects/useProjects';
import { useAllToolsQuery } from '../features/tools/useTools';
import { useAllVehiclesQuery } from '../features/vehicles/useVehicles';
import type { MessageKey } from '../i18n/en';
import { useT } from '../i18n/useI18n';
import { DocumentUploadDialog } from './files/DocumentUploadDialog';

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
  Employee: ['Contract', 'Identity', 'Visa', 'Licence', 'Certificate', 'Diploma', 'MedicalCheck', 'Other'],
  Project: ['SiteDocument', 'Photo', 'Licence', 'Insurance', 'Other'],
  Vehicle: ['Insurance', 'Licence', 'Certificate', 'Photo', 'Other'],
  Tool: ['Certificate', 'Licence', 'Photo', 'Other'],
  WorkItem: ['Photo', 'Other'],
  VehicleExpense: ['Other'],
  MaterialMovement: ['Other'],
  EmployeeRate: ['Contract', 'Other'],
  FinanceEntry: ['Other'],
  ToolExpense: ['Other'],
  ToolRentalRate: ['Contract', 'Other'],
  VehicleRentalRate: ['Contract', 'Other'],
  GeneralExpense: ['Other'],
  Accommodation: ['Photo', 'Other'],
  AccommodationRate: ['Contract', 'Other'],
  Refund: ['Photo', 'Other'],
  SignedTimesheet: ['Other'],
  BookEntry: ['Other', 'Contract', 'Photo'],
};

/**
 * Uploads a document without starting from the record it belongs to — the
 * owner is picked here instead of being fixed by the page. Used from the
 * expiring-documents hub, where there is no single "this record" to hang the
 * upload button off. The zone, the queue and the details are the shared upload dialog.
 */
export function UploadDocumentDialog({
  open,
  onClose,
  initialFiles,
}: {
  open: boolean;
  onClose: () => void;
  initialFiles?: readonly File[];
}) {
  const t = useT();

  const [ownerType, setOwnerType] = useState<AttachmentOwnerType>('Employee');
  const [ownerId, setOwnerId] = useState('');

  useEffect(() => {
    if (open) {
      setOwnerType('Employee');
      setOwnerId('');
    }
  }, [open]);

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

  const changeOwnerType = (next: AttachmentOwnerType) => {
    setOwnerType(next);
    // The record picked for the previous owner type makes no sense for this one.
    setOwnerId('');
  };

  const ownerSlot = (
    <Stack spacing={2}>
      <FormControl fullWidth>
        <InputLabel id="upload-owner-type-label">{t('attachments.ownerType')}</InputLabel>
        <Select
          labelId="upload-owner-type-label"
          label={t('attachments.ownerType')}
          value={ownerType}
          onChange={(event) => changeOwnerType(event.target.value as AttachmentOwnerType)}
        >
          {DOCUMENT_OWNER_TYPES.map((value) => (
            <MenuItem key={value} value={value}>
              {t(OWNER_TYPE_LABEL_KEYS[value])}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControl fullWidth>
        <InputLabel id="upload-owner-record-label">{t('attachments.ownerRecord')}</InputLabel>
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
    </Stack>
  );

  return (
    <DocumentUploadDialog
      open={open}
      onClose={onClose}
      ownerType={ownerType}
      ownerId={ownerId}
      categories={CATEGORIES_BY_OWNER_TYPE[ownerType]}
      ownerSlot={ownerSlot}
      initialFiles={initialFiles}
    />
  );
}
