import { Chip, type ChipProps } from '@mui/material';

import { humanizeEnum } from '../utils/formatting';

const GOOD = new Set(['Active', 'Available', 'Completed']);
const CAUTION = new Set(['OnLeave', 'Planned', 'OnHold', 'Assigned', 'InService']);
const BAD = new Set(['Suspended', 'UnderRepair', 'Lost']);

/**
 * Colour-coded label for the API's status enums. Colours are grouped by
 * meaning rather than per entity, so the same colour always means the same
 * thing across employees, projects, vehicles and tools.
 */
export function StatusChip({
  status,
  size = 'small',
  ...rest
}: { status: string } & Omit<ChipProps, 'label' | 'color'>) {
  const color: ChipProps['color'] = GOOD.has(status)
    ? 'success'
    : CAUTION.has(status)
      ? 'warning'
      : BAD.has(status)
        ? 'error'
        : 'default';

  return (
    <Chip
      label={humanizeEnum(status)}
      color={color}
      size={size}
      variant={color === 'default' ? 'outlined' : 'filled'}
      {...rest}
    />
  );
}
