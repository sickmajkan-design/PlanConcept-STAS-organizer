import type { Theme } from '@mui/material/styles';

/**
 * The company logo is a dark mark on a white picture, so on the dark theme it would vanish.
 * Inverting turns it light, and `screen` blending drops the (now black) picture background so
 * only the mark shows on whatever is behind it. Put it on the `<img>` itself, with no
 * background of its own — the backdrop belongs on a parent.
 */
export const darkLogoSx = (theme: Theme) =>
  theme.palette.mode === 'dark' ? { filter: 'invert(1)', mixBlendMode: 'screen' as const } : {};
