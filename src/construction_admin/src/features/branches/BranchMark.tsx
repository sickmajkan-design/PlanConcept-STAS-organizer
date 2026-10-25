import { Box } from '@mui/material';

import type { Branch } from '../../api/types';
import { branchLogoUrl } from './branchLogo';
import { BranchDot } from './BranchDot';

/** A small logo before a unit's name in the switcher; the coloured dot while it has none. Always on a light plate. */
export function BranchMark({ branch }: { branch: Pick<Branch, 'id' | 'color' | 'hasLogo' | 'logoVersion'> }) {
  if (!branch.hasLogo) return <BranchDot color={branch.color} />;

  return (
    <Box
      aria-hidden
      sx={{
        width: 24,
        height: 24,
        p: '2px',
        flexShrink: 0,
        borderRadius: 1,
        bgcolor: '#fff',
        border: '1px solid',
        borderColor: 'divider',
        display: 'flex',
      }}
    >
      <Box component="img" src={branchLogoUrl(branch)} alt="" sx={{ width: '100%', height: '100%', objectFit: 'contain' }} />
    </Box>
  );
}
