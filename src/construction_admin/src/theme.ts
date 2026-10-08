import { createTheme } from '@mui/material/styles';

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
export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#e65100' },
    secondary: { main: '#37474f' },
    background: { default: '#f4f5f7', paper: '#ffffff' },
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
        root: { textTransform: 'none', fontWeight: 600, [COARSE]: { minHeight: TOUCH_TARGET } },
      },
    },
    // On a touch screen every control is at least 44 px, the size a fingertip
    // can hit without a second try. Set once here for every control in the panel
    // rather than at the roughly five hundred places that ask for `size="small"`,
    // and only for `pointer: coarse`, so a mouse still gets the compact desktop.
    MuiIconButton: {
      styleOverrides: { root: { [COARSE]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
    },
    MuiToggleButton: {
      styleOverrides: { root: { [COARSE]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
    },
    MuiTab: { styleOverrides: { root: { [COARSE]: { minHeight: 48 } } } },
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
        root: { boxShadow: 'none', borderBottom: '1px solid #e0e0e0' },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: { border: '1px solid #e0e0e0' },
      },
    },
  },
});
