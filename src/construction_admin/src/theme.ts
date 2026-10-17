import { alpha, createTheme } from '@mui/material/styles';

/** Applies only on touch screens: a finger, not a mouse pointer. */
const COARSE = '@media (pointer: coarse)';

/** The smallest touch target that can be hit reliably, in pixels. */
const TOUCH_TARGET = 44;

/**
 * The series colours for every chart, in the brand's own hues.
 *
 * The chart library's default is a saturated blue that appears nowhere else in
 * the panel, so the dashboard read as two products stitched together. Amber
 * leads, slate second — the same pair the buttons and headers already use —
 * then muted companions that stay distinguishable from each other.
 */
export const chartPalette = ['#e65100', '#37474f', '#f9a825', '#78909c', '#8d6e63', '#00897b'];

/**
 * Material 3-leaning theme built around a high-visibility safety amber,
 * matching the mobile app so the two clients read as one product.
 */
/**
 * How fast and how a pressed control answers: a strong ease-out, quick enough to feel like the control heard
 * the finger. Only transform, so it costs no layout.
 */
const PRESS = {
  transition: 'transform 140ms cubic-bezier(0.23, 1, 0.32, 1), background-color 140ms ease, box-shadow 140ms ease',
  '&:active:not(.Mui-disabled)': { transform: 'scale(0.97)' },
  '@media (prefers-reduced-motion: reduce)': { transition: 'none', '&:active:not(.Mui-disabled)': { transform: 'none' } },
};

/**
 * The theme in one of two modes. Dark keeps the same amber on a warm, near-black ground (not blue-grey): the
 * orange has to stay the one loud colour, so on dark it is lighter to keep its contrast.
 */
export function buildTheme(mode: 'light' | 'dark') {
  const dark = mode === 'dark';

  return createTheme({
  palette: {
    mode,
    primary: dark
      ? { main: '#ff8a3d', light: '#ffb074', dark: '#e65100', contrastText: '#2a1100' }
      : { main: '#e65100' },
    secondary: dark ? { main: '#b0bec5' } : { main: '#37474f' },
    // Information in the panel's own slate instead of the library's saturated blue, which appeared
    // nowhere else and read as a different product. It stays clearly apart from the amber warning.
    info: dark
      ? { main: '#90a4ae', light: '#b0bec5', dark: '#78909c', contrastText: '#10171a' }
      : { main: '#546e7a', light: '#78909c', dark: '#37474f', contrastText: '#ffffff' },
    background: dark ? { default: '#12100e', paper: '#1c1815' } : { default: '#f4f5f7', paper: '#ffffff' },
  },
  shape: { borderRadius: 10 },
  typography: {
    fontFamily: [
      'Inter',
      '-apple-system',
      'BlinkMacSystemFont',
      'Segoe UI',
      'Roboto',
      'sans-serif',
    ].join(','),
  },
  components: {
    MuiButton: {
      styleOverrides: {
        // A button title is one line, always: a title that wraps looks broken. The room around it gives way instead.
        root: { textTransform: 'none', fontWeight: 600, whiteSpace: 'nowrap', ...PRESS, [COARSE]: { minHeight: TOUCH_TARGET } },
      },
      // A second button on a screen is quiet: a thin neutral line, so the one filled button is the one that
      // reads as the main thing to do. Orange outlines on three buttons in a row made all three look equal.
      variants: [
        {
          props: { variant: 'outlined', color: 'primary' },
          style: ({ theme: t }) => ({
            color: t.palette.text.primary,
            borderColor: t.palette.divider,
            '&:hover': { borderColor: t.palette.text.secondary, backgroundColor: t.palette.action.hover },
          }),
        },
      ],
    },
    // On a touch screen every control is at least 44 px, the size a fingertip
    // can hit without a second try. Set once here for every control in the panel
    // rather than at the roughly five hundred places that ask for `size="small"`,
    // and only for `pointer: coarse`, so a mouse still gets the compact desktop.
    MuiIconButton: {
      styleOverrides: { root: { ...PRESS, [COARSE]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
    },
    MuiToggleButton: {
      styleOverrides: { root: { [COARSE]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
    },
    // Sentence case like everything else in the panel, instead of the library's capitals.
    MuiTab: { styleOverrides: { root: { textTransform: 'none', letterSpacing: 'normal', [COARSE]: { minHeight: 48 } } } },
    MuiMenuItem: { styleOverrides: { root: { [COARSE]: { minHeight: 48 } } } },
    MuiListItemButton: { styleOverrides: { root: { [COARSE]: { minHeight: 48 } } } },
    MuiChip: {
      styleOverrides: {
        root: {
          [COARSE]: {
            '&.MuiChip-clickable, &.MuiChip-deletable': { minHeight: 40 },
          },
        },
      },
      // "Active" is the state nearly everyone is in, so it is a soft tint, not a solid block in every row. The
      // solid colours stay for what asks for attention (warning, error).
      variants: [
        {
          props: { variant: 'filled', color: 'success' },
          style: ({ theme: t }) => ({
            backgroundColor: t.palette.mode === 'dark' ? alpha(t.palette.success.main, 0.22) : '#e3f2e4',
            color: t.palette.mode === 'dark' ? t.palette.success.light : '#1b5e20',
          }),
        },
      ],
    },
    MuiCheckbox: { styleOverrides: { root: { [COARSE]: { padding: 11 } } } },
    MuiRadio: { styleOverrides: { root: { [COARSE]: { padding: 11 } } } },
    MuiSwitch: { styleOverrides: { root: { [COARSE]: { padding: 10 } } } },
    MuiOutlinedInput: { styleOverrides: { root: { [COARSE]: { minHeight: 48 } } } },
    // A dialog on a phone is a sheet that rises from the bottom edge: the same
    // component for a one-line confirmation and for a long form, tall enough to
    // scroll the form and short enough to leave the page visible behind it.
    // Fullscreen dialogs keep their own shape.
    MuiDialog: {
      styleOverrides: {
        container: ({ theme: t }) => ({
          [t.breakpoints.down('sm')]: { alignItems: 'flex-end' },
        }),
        paper: ({ theme: t }) => ({
          [t.breakpoints.down('sm')]: {
            '&:not(.MuiDialog-paperFullScreen)': {
              margin: 0,
              width: '100%',
              maxWidth: '100% !important',
              maxHeight: '92dvh',
              borderRadius: '16px 16px 0 0',
            },
          },
        }),
      },
    },
    MuiDialogActions: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          [t.breakpoints.down('sm')]: {
            flexWrap: 'wrap',
            gap: 8,
            padding: '12px 16px',
            paddingBottom: 'max(12px, env(safe-area-inset-bottom))',
            '& > :not(style) ~ :not(style)': { marginLeft: 0 },
          },
        }),
      },
    },
    // Keeps the "undo" toast clear of the bottom navigation bar on a phone.
    MuiSnackbar: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          [t.breakpoints.down('sm')]: { bottom: 'calc(72px + env(safe-area-inset-bottom))' },
        }),
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: ({ theme: t }) => ({ boxShadow: 'none', borderBottom: `1px solid ${t.palette.divider}` }),
      },
    },
    MuiCard: {
      styleOverrides: {
        root: ({ theme: t }) => ({ border: `1px solid ${t.palette.divider}` }),
      },
    },
  },
  });
}

/** The light theme, for what needs one without the provider (tests, the error screen). */
export const theme = buildTheme('light');
