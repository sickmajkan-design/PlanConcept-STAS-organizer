import {
  AddCircleOutlined,
  CampaignOutlined,
  EventBusyOutlined,
  FolderOutlined,
  LocalGasStationOutlined,
  UploadFileOutlined,
} from '@mui/icons-material';
import type { ReactNode } from 'react';

import type { User } from '../api/types';
import {
  canAdministerAccounts,
  canSeeSpending,
  canViewBook,
  canViewDirectory,
} from '../auth/authHelpers';
import type { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

export interface PaletteAction {
  key: string;
  label: string;
  path: string;
  icon: ReactNode;
  /** Other words that find it, in both languages. */
  keywords?: string;
}

/**
 * The "create something" commands, gated by the same role checks as the pages
 * they open, so the palette never offers what the destination would refuse.
 */
export function buildPaletteActions(
  user: User | null | undefined,
  t: ReturnType<typeof useT>,
): PaletteAction[] {
  const plus = <AddCircleOutlined fontSize="small" />;
  const actions: PaletteAction[] = [];

  if (canSeeSpending(user)) {
    actions.push({
      key: 'vehicleCost',
      label: t('action.recordVehicleCost'),
      path: `${paths.vehicleExpenses}?new=1`,
      icon: <LocalGasStationOutlined fontSize="small" />,
    });

    actions.push({
      key: 'quickEntryRates',
      label: t('action.quickEntryRates'),
      path: paths.employeeRatesQuickEntry,
      icon: <LocalGasStationOutlined fontSize="small" />,
      keywords: 'cijena sata plata povišica radnici tabela masovni unos hourly rate raise',
    });
  }

  if (canViewDirectory(user)) {
    actions.push(
      {
        key: 'absence',
        label: t('action.bookAbsence'),
        path: `${paths.absences}?new=1`,
        icon: <EventBusyOutlined fontSize="small" />,
      },
      { key: 'timeEntry', label: t('action.newTimeEntry'), path: paths.timeEntryNew, icon: plus },
      { key: 'workItem', label: t('action.newWorkItem'), path: paths.workItemNew, icon: plus },
      { key: 'employee', label: t('action.newEmployee'), path: paths.employeeNew, icon: plus },
      { key: 'project', label: t('action.newProject'), path: paths.projectNew, icon: plus },
      { key: 'customer', label: t('action.newCustomer'), path: paths.customerNew, icon: plus },
      { key: 'vehicle', label: t('action.newVehicle'), path: paths.vehicleNew, icon: plus },
      { key: 'tool', label: t('action.newTool'), path: paths.toolNew, icon: plus },
      { key: 'quickEntryVehicles', label: t('action.quickEntryVehicles'), path: paths.vehiclesQuickEntry, icon: plus, keywords: 'tabela masovni unos izmjena datumi registracija osiguranje bulk' },
      { key: 'quickEntryEmployees', label: t('action.quickEntryEmployees'), path: paths.employeesQuickEntry, icon: plus, keywords: 'radnici zaposleni tabela masovni unos izmjena pozicija telefon status bulk' },
      { key: 'quickEntryTools', label: t('action.quickEntryTools'), path: paths.toolsQuickEntry, icon: plus, keywords: 'tabela masovni unos izmjena bulk' },
      { key: 'material', label: t('action.newMaterial'), path: paths.materialNew, icon: plus },
    );
  }

  if (canAdministerAccounts(user)) {
    // Reached from a button on the vehicle costs page, not from the menu, so it is offered here.
    actions.push({
      key: 'dkvCheck',
      label: t('dkv.open'),
      path: paths.fuelReconciliation,
      icon: <LocalGasStationOutlined fontSize="small" />,
    });

    actions.push(
      {
        key: 'uploadDocument',
        label: t('action.uploadDocument'),
        path: `${paths.expiringDocuments}?upload=1`,
        icon: <UploadFileOutlined fontSize="small" />,
        keywords: 'dodaj dokument prilog datoteka otpremi upload attach file',
      },
      {
        key: 'requestDocument',
        label: t('action.requestDocument'),
        path: `${paths.documentRequests}?new=1`,
        icon: <FolderOutlined fontSize="small" />,
        keywords: 'zahtjev traži dokument novi radnik paket request document',
      },
      {
        key: 'announce',
        label: t('action.sendAnnouncement'),
        path: `${paths.notifications}?compose=1`,
        icon: <CampaignOutlined fontSize="small" />,
      },
      { key: 'user', label: t('action.newUser'), path: paths.userNew, icon: plus },
    );
  }

  if (canViewBook(user)) {
    actions.push({
      key: 'importBookMonth',
      label: t('action.importBookMonth'),
      path: paths.bookImport,
      icon: <UploadFileOutlined fontSize="small" />,
      keywords: 'uvoz excel xlsx tabela stari mjesec import spreadsheet',
    });
  }

  if (canSeeSpending(user)) {
    actions.push({
      key: 'importFuel',
      label: t('action.importFuel'),
      path: paths.fuelImport,
      icon: <LocalGasStationOutlined fontSize="small" />,
      keywords: 'uvoz izvod gorivo kartice import statement fuel',
    });
  }

  if (canViewDirectory(user)) {
    actions.push(
      {
        key: 'importAccommodation',
        label: t('action.importAccommodation'),
        path: paths.accommodationImport,
        icon: <UploadFileOutlined fontSize="small" />,
        keywords: 'uvoz smještaj excel import housing',
      },
      {
        key: 'importDeliveries',
        label: t('action.importDeliveries'),
        path: paths.materialDeliveryImport,
        icon: <UploadFileOutlined fontSize="small" />,
        keywords: 'uvoz isporuke materijal excel import deliveries material',
      },
    );
  }

  return actions;
}
