import { Box } from '@mui/material';

/**
 * The coloured dot that marks a business unit (poslovna jedinica), placed BEFORE a name. A
 * client's company is the outline chip AFTER a name instead — the two are never mixed up.
 */
export function BranchDot({ color, size = 9 }: { color: string; size?: number }) {
  return (
    <Box
      component="span"
      aria-hidden
      sx={{
        display: 'inline-block',
        flexShrink: 0,
        width: size,
        height: size,
        borderRadius: '50%',
        bgcolor: color,
        boxShadow: `0 0 0 3px ${color}26`,
      }}
    />
  );
}
