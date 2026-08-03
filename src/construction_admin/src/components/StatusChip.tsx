import { Chip, type ChipProps } from '@mui/material';

import { useEnumLabel, type EnumKind } from '../i18n/enumLabels';

const GOOD = new Set(['Active', 'Available', 'Completed', 'Approved', 'Closed', 'Resolved']);
const CAUTION = new Set([
  'OnLeave',
  'Planned',
  'OnHold',
  'Assigned',
  'InService',
  'InProgress',
  'Submitted',
  'Open',
  // An unanswered leave request is waiting on somebody, the same as a
  // submitted timesheet.
  'Requested',
]);
const BAD = new Set(['Suspended', 'UnderRepair', 'Lost', 'Rejected', 'Cancelled']);

/**
 * Colour-coded label for the API's status enums. Colours are grouped by
 * meaning rather than per entity, so the same colour always means the same
 * thing across employees, projects, vehicles and tools.
 *
 * `kind` says which enum the value comes from. It is needed because the label
 * is translated and the same value inflects differently per entity — see
 * {@link EnumKind}.
 */
export function StatusChip({
  status,
  kind,
  size = 'small',
  ...rest
}: { status: string; kind: EnumKind } & Omit<ChipProps, 'label' | 'color'>) {
  const label = useEnumLabel();

  const color: ChipProps['color'] = GOOD.has(status)
    ? 'success'
    : CAUTION.has(status)
      ? 'warning'
      : BAD.has(status)
        ? 'error'
        : 'default';

  return (
    <Chip
      label={label(kind, status)}
      color={color}
      size={size}
      variant={color === 'default' ? 'outlined' : 'filled'}
      {...rest}
    />
  );
}
