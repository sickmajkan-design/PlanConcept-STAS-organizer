import { useCallback } from 'react';

import { humanizeEnum } from '../utils/formatting';
import { en, type MessageKey } from './en';
import { useT } from './useI18n';

/**
 * Which enum a value belongs to.
 *
 * The kind has to be supplied rather than inferred from the value, because the
 * same value means different things: `Available` is a vehicle status and a
 * tool status, and Serbian inflects them differently — a vehicle is
 * "slobodno", a tool "slobodan". English hid that by using one word for both.
 */
export type EnumKind =
  | 'role'
  | 'employeeStatus'
  | 'projectStatus'
  | 'vehicleStatus'
  | 'toolStatus'
  | 'fuelType'
  | 'timeEntryStatus'
  | 'workType'
  | 'attachmentCategory'
  | 'workItemKind'
  | 'workItemStatus'
  | 'workItemPriority';

export function useEnumLabel() {
  const t = useT();

  return useCallback(
    (kind: EnumKind, value: string | null | undefined): string => {
      if (!value) {
        return '—';
      }

      const key = `${kind}.${value}`;

      // A value the API grows that this build has not been taught yet falls
      // back to the readable English form rather than showing a raw key.
      return key in en ? t(key as MessageKey) : humanizeEnum(value);
    },
    [t],
  );
}
