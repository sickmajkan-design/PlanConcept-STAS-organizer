import {
  AddCircleOutlined,
  CampaignOutlined,
  EventBusyOutlined,
  LocalGasStationOutlined,
} from '@mui/icons-material';
import type { ReactNode } from 'react';

import type { User } from '../api/types';
import {
  canAdministerAccounts,
  canSeeSpending,
  canViewDirectory,
} from '../auth/authHelpers';
import type { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

export interface PaletteAction {
  key: string;
  label: string;
  path: string;
  icon: ReactNode;
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
      { key: 'material', label: t('action.newMaterial'), path: paths.materialNew, icon: plus },
    );
  }

  if (canAdministerAccounts(user)) {
    actions.push(
      {
        key: 'announce',
        label: t('action.sendAnnouncement'),
        path: `${paths.notifications}?compose=1`,
        icon: <CampaignOutlined fontSize="small" />,
      },
      { key: 'user', label: t('action.newUser'), path: paths.userNew, icon: plus },
    );
  }

  return actions;
}
