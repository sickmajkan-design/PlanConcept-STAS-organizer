import { Box, type SxProps, type Theme } from '@mui/material';

import { useContractorIds } from '../features/employees/useContractorIds';
import { contractorNameColor } from '../theme';
import { ContractorChip } from './ContractorChip';

/**
 * A person's name, marked purple with the Subcontractor label when they are one. For the screens whose rows carry the
 * employee id but not the type; screens that already know it use {@link ContractorChip} directly.
 */
export function PersonName({
  id,
  name,
  isContractor: known,
  sx,
}: {
  id?: string | null;
  name: string;
  /** When the screen already knows the type, pass it; otherwise the id is looked up. */
  isContractor?: boolean;
  sx?: SxProps<Theme>;
}) {
  const contractors = useContractorIds();
  const isContractor = known ?? (!!id && contractors.has(id));

  if (!isContractor) {
    return sx ? <Box component="span" sx={sx}>{name}</Box> : <>{name}</>;
  }

  return (
    <Box
      component="span"
      sx={[
        { display: 'inline-flex', alignItems: 'center', gap: 0.75, minWidth: 0, maxWidth: '100%' },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      <Box component="span" sx={{ color: contractorNameColor, overflow: 'hidden', textOverflow: 'ellipsis' }}>
        {name}
      </Box>
      <ContractorChip sx={{ height: 18, fontSize: 11, flex: 'none' }} />
    </Box>
  );
}
