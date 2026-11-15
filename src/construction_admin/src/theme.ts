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

/** The one soft lift that separates a card from the page, in place of a border. */
const LIGHT_LIFT = '0 1px 2px rgba(0,0,0,0.04), 0 8px 24px rgba(0,0,0,0.05)';
const DARK_LIFT = '0 1px 2px rgba(0,0,0,0.45), 0 8px 24px rgba(0,0,0,0.4)';

/**
 * The theme in one of two modes. Dark keeps the same amber on a warm, near-black ground (not blue-grey): the
 * orange has to stay the one loud colour, so on dark it is lighter to keep its contrast.
 *
 * The look is the Apple one: a light grey page, white cards lifted by a soft shadow instead of a border, large
 * radii, pill buttons, a grey fill for quiet controls, iOS switches and segmented tabs, and glass for what floats.
 * Only appearance lives here; nothing in this file changes what a control does.
 */
export function buildTheme(mode: 'light' | 'dark') {
  const dark = mode === 'dark';
  // Quiet controls are a translucent grey rather than a line, so they sit on any background.
  const fill = dark ? 'rgba(235,235,245,0.14)' : 'rgba(118,118,128,0.12)';
  const fillHover = dark ? 'rgba(235,235,245,0.2)' : 'rgba(118,118,128,0.18)';
  const hairline = dark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.08)';
  const fieldLine = dark ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.14)';
  const lift = dark ? DARK_LIFT : LIGHT_LIFT;

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
    background: dark ? { default: '#12100e', paper: '#1c1815' } : { default: '#f5f5f7', paper: '#ffffff' },
    ...(dark ? {} : { text: { primary: '#1d1d1f', secondary: '#6e6e73' } }),
    divider: hairline,
  },
  shape: { borderRadius: 12 },
  typography: {
    // The system face first, as on Apple devices; Inter where the system has none.
    fontFamily: [
      '-apple-system',
      'BlinkMacSystemFont',
      '"SF Pro Text"',
      'Inter',
      '"Segoe UI"',
      'Roboto',
      'sans-serif',
    ].join(','),
    // Large type tightens as it grows and small type loosens, never one tracking for all sizes.
    h4: { fontWeight: 700, letterSpacing: '-0.022em' },
    h5: { fontWeight: 700, letterSpacing: '-0.02em' },
    h6: { fontWeight: 700, letterSpacing: '-0.012em' },
    subtitle1: { fontWeight: 600, letterSpacing: '-0.01em' },
    subtitle2: { fontWeight: 600 },
    caption: { letterSpacing: '0.01em' },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: (t) => ({
        body: { WebkitFontSmoothing: 'antialiased', MozOsxFontSmoothing: 'grayscale' },
        // The data grid: no frame of its own, hairlines between rows and quiet headers, like a grouped list.
        '.MuiDataGrid-root': {
          '--DataGrid-t-color-border-base': hairline,
          '--DataGrid-t-header-background-base': 'transparent',
          '--DataGrid-t-color-background-base': t.palette.background.paper,
          '--DataGrid-t-radius-base': '12px',
          '--DataGrid-t-typography-font-family-base': t.typography.fontFamily,
          border: 0,
        },
        '.MuiDataGrid-columnHeaderTitle': { fontWeight: 600, color: t.palette.text.secondary, fontSize: '0.8125rem' },
        '.MuiDataGrid-row:hover': { backgroundColor: t.palette.action.hover },
      }),
    },
    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
        rounded: { borderRadius: 16 },
        outlined: { border: `1px solid ${hairline}` },
        elevation1: { boxShadow: lift },
        elevation2: { boxShadow: lift },
      },
    },
    MuiStepper: {
      styleOverrides: {
        root: ({ theme }) => ({
          [theme.breakpoints.down('sm')]: {
            flexWrap: 'wrap',
            rowGap: theme.spacing(1),
            '& .MuiStepConnector-root': { display: 'none' },
            '& .MuiStep-root': { paddingRight: theme.spacing(2) },
          },
        }),
      },
    },
    MuiAlert: {
      styleOverrides: {
        root: ({ theme }) => ({
          borderRadius: 14,
          [theme.breakpoints.down('sm')]: {
            flexWrap: 'wrap',
            '& .MuiAlert-message': { flex: '1 1 0', minWidth: 0 },
            '& .MuiAlert-action': { flex: '1 0 100%', justifyContent: 'flex-end', margin: 0, padding: theme.spacing(0.5, 0, 0) },
          },
        }),
      },
    },
    MuiButton: {
      styleOverrides: {
        // A button title is one line, always: a title that wraps looks broken. The room around it gives way instead.
        // Pills, with no shadow: the filled colour is the emphasis.
        root: { textTransform: 'none', fontWeight: 600, whiteSpace: 'nowrap', borderRadius: 999, ...PRESS, [COARSE]: { minHeight: TOUCH_TARGET } },
        contained: { boxShadow: 'none', '&:hover': { boxShadow: 'none' }, '&:active': { boxShadow: 'none' } },
      },
      // A second button on a screen is quiet: a soft grey fill, so the one filled button is the one that
      // reads as the main thing to do. Orange outlines on three buttons in a row made all three look equal.
      variants: [
        {
          props: { variant: 'outlined', color: 'primary' },
          style: ({ theme: t }) => ({
            color: t.palette.text.primary,
            borderColor: 'transparent',
            backgroundColor: fill,
            '&:hover': { borderColor: 'transparent', backgroundColor: fillHover },
            '&.Mui-disabled': { borderColor: 'transparent', backgroundColor: fill },
          }),
        },
        {
          // Outlined buttons in any other neutral colour (the date arrows, "Today") are the same soft grey.
          props: { variant: 'outlined', color: 'inherit' },
          style: ({ theme: t }) => ({
            color: t.palette.text.primary,
            borderColor: 'transparent',
            backgroundColor: fill,
            '&:hover': { borderColor: 'transparent', backgroundColor: fillHover },
          }),
        },
        {
          props: { variant: 'outlined', color: 'secondary' },
          style: ({ theme: t }) => ({
            color: t.palette.text.primary,
            borderColor: 'transparent',
            backgroundColor: fill,
            '&:hover': { borderColor: 'transparent', backgroundColor: fillHover },
          }),
        },
        {
          props: { variant: 'outlined', color: 'success' },
          style: ({ theme: t }) => ({
            borderColor: 'transparent',
            backgroundColor: alpha(t.palette.success.main, 0.12),
            '&:hover': { borderColor: 'transparent', backgroundColor: alpha(t.palette.success.main, 0.18) },
          }),
        },
        {
          props: { variant: 'outlined', color: 'warning' },
          style: ({ theme: t }) => ({
            borderColor: 'transparent',
            backgroundColor: alpha(t.palette.warning.main, 0.14),
            '&:hover': { borderColor: 'transparent', backgroundColor: alpha(t.palette.warning.main, 0.2) },
          }),
        },
        {
          // The destructive twin: a soft red tint, not a red frame.
          props: { variant: 'outlined', color: 'error' },
          style: ({ theme: t }) => ({
            borderColor: 'transparent',
            backgroundColor: alpha(t.palette.error.main, 0.1),
            '&:hover': { borderColor: 'transparent', backgroundColor: alpha(t.palette.error.main, 0.16) },
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
    // A group of toggles is one segmented control: a grey track with the chosen segment lifted in white.
    MuiToggleButtonGroup: {
      styleOverrides: {
        root: { backgroundColor: fill, borderRadius: 10, padding: 2 },
        grouped: ({ theme: t }) => ({
          border: 0,
          margin: 0,
          borderRadius: 8,
          color: t.palette.text.primary,
          textTransform: 'none',
          fontWeight: 500,
          '&:not(:first-of-type), &:not(:last-of-type)': { borderRadius: 8, margin: 0, border: 0 },
          '&.Mui-selected': {
            backgroundColor: t.palette.background.paper,
            color: t.palette.text.primary,
            fontWeight: 600,
            boxShadow: '0 1px 3px rgba(0,0,0,0.14)',
            '&:hover': { backgroundColor: t.palette.background.paper },
          },
        }),
      },
    },
    MuiToggleButton: {
      styleOverrides: { root: { [COARSE]: { minWidth: TOUCH_TARGET, minHeight: TOUCH_TARGET } } },
    },
    // Tabs are the same segmented control: the chosen one sits in a white pill on a grey track.
    MuiTabs: {
      styleOverrides: {
        root: { minHeight: 36, backgroundColor: fill, borderRadius: 10, padding: 2, width: 'fit-content', maxWidth: '100%', [COARSE]: { minHeight: 44 } },
        indicator: ({ theme: t }) => ({
          top: 0,
          bottom: 0,
          height: 'auto',
          borderRadius: 8,
          backgroundColor: t.palette.background.paper,
          boxShadow: '0 1px 3px rgba(0,0,0,0.14)',
          zIndex: 0,
        }),
      },
    },
    // Sentence case like everything else in the panel, instead of the library's capitals.
    MuiTab: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          textTransform: 'none',
          letterSpacing: 'normal',
          zIndex: 1,
          minHeight: 32,
          borderRadius: 8,
          fontWeight: 500,
          color: t.palette.text.secondary,
          '&.Mui-selected': { color: t.palette.text.primary, fontWeight: 600 },
          [COARSE]: { minHeight: 40 },
        }),
      },
    },
    // A switch has no padding to pull back (the library's -11px is for a checkbox), so it lines up with the fields.
    MuiFormControlLabel: { styleOverrides: { root: { '&:has(.MuiSwitch-root)': { marginLeft: 0 } } } },
    MuiMenuItem: { styleOverrides: { root: { [COARSE]: { minHeight: 48 } } } },
    MuiListItemButton: { styleOverrides: { root: { borderRadius: 10, [COARSE]: { minHeight: 48 } } } },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          [COARSE]: {
            '&.MuiChip-clickable, &.MuiChip-deletable': { minHeight: 40 },
          },
        },
      },
      // Every state is a soft tint with darker text of the same hue, never a solid block in every row. The
      // solid colours stay for what has to be seen first (the primary accent).
      variants: [
        {
          props: { variant: 'filled', color: 'success' },
          style: ({ theme: t }) => ({
            backgroundColor: t.palette.mode === 'dark' ? alpha(t.palette.success.main, 0.22) : '#e3f2e4',
            color: t.palette.mode === 'dark' ? t.palette.success.light : '#1b5e20',
          }),
        },
        {
          props: { variant: 'filled', color: 'warning' },
          style: ({ theme: t }) => ({
            backgroundColor: t.palette.mode === 'dark' ? alpha(t.palette.warning.main, 0.26) : 'rgba(255,159,10,0.2)',
            color: t.palette.mode === 'dark' ? t.palette.warning.light : '#8a4a00',
          }),
        },
        {
          props: { variant: 'filled', color: 'error' },
          style: ({ theme: t }) => ({
            backgroundColor: t.palette.mode === 'dark' ? alpha(t.palette.error.main, 0.28) : '#fde1df',
            color: t.palette.mode === 'dark' ? t.palette.error.light : '#a3201a',
          }),
        },
        {
          props: { variant: 'filled', color: 'info' },
          style: ({ theme: t }) => ({
            backgroundColor: alpha(t.palette.info.main, 0.16),
            color: t.palette.mode === 'dark' ? t.palette.info.light : t.palette.info.dark,
          }),
        },
        {
          props: { variant: 'filled', color: 'default' },
          style: ({ theme: t }) => ({ backgroundColor: fill, color: t.palette.text.primary }),
        },
        {
          props: { variant: 'outlined', color: 'default' },
          style: { borderColor: fieldLine },
        },
      ],
    },
    MuiCheckbox: { styleOverrides: { root: { [COARSE]: { padding: 11 } } } },
    MuiRadio: { styleOverrides: { root: { [COARSE]: { padding: 11 } } } },
    // The iOS switch: a long pill track, a round white thumb with a soft shadow, green when on.
    MuiSwitch: {
      styleOverrides: {
        root: {
          width: 51,
          height: 31,
          padding: 0,
          margin: 4,
          [COARSE]: { margin: '6px 6px' },
          '&.MuiSwitch-sizeSmall': {
            width: 40,
            height: 24,
            '& .MuiSwitch-switchBase': { padding: 2, '&.Mui-checked': { transform: 'translateX(16px)' } },
            '& .MuiSwitch-thumb': { width: 20, height: 20 },
          },
        },
        switchBase: {
          padding: 2,
          '&.Mui-checked': {
            transform: 'translateX(20px)',
            color: '#fff',
            '& + .MuiSwitch-track': { backgroundColor: dark ? '#30d158' : '#34c759', opacity: 1 },
          },
          '&.Mui-disabled + .MuiSwitch-track': { opacity: 0.5 },
        },
        thumb: { width: 27, height: 27, boxShadow: '0 2px 4px rgba(0,0,0,0.25)', backgroundColor: '#fff' },
        track: { borderRadius: 16, backgroundColor: dark ? 'rgba(120,120,128,0.4)' : 'rgba(120,120,128,0.32)', opacity: 1 },
      },
    },
    // Fields keep a fine outline (the floating label needs the notch) but with a large radius and a white
    // ground, so they stand out on the grey page as well as inside a card.
    MuiOutlinedInput: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          borderRadius: 12,
          backgroundColor: t.palette.background.paper,
          '& .MuiOutlinedInput-notchedOutline': { borderColor: fieldLine },
          [COARSE]: { minHeight: 48 },
        }),
      },
    },
    // A dialog is a floating glass-like sheet: large radius, deep soft shadow, the page blurred behind it.
    // On a phone it is a sheet that rises from the bottom edge: the same component for a one-line
    // confirmation and for a long form, tall enough to scroll the form and short enough to leave the page
    // visible behind it. Fullscreen dialogs keep their own shape.
    MuiDialog: {
      styleOverrides: {
        container: ({ theme: t }) => ({
          [t.breakpoints.down('sm')]: { alignItems: 'flex-end' },
        }),
        paper: ({ theme: t }) => ({
          borderRadius: 22,
          boxShadow: '0 24px 70px rgba(0,0,0,0.28)',
          [t.breakpoints.down('sm')]: {
            '&:not(.MuiDialog-paperFullScreen)': {
              margin: 0,
              width: '100%',
              maxWidth: '100% !important',
              maxHeight: '92dvh',
              borderRadius: '24px 24px 0 0',
            },
          },
        }),
      },
    },
    MuiDialogTitle: { styleOverrides: { root: { fontWeight: 700, letterSpacing: '-0.012em', fontSize: '1.25rem' } } },
    MuiBackdrop: {
      styleOverrides: {
        root: { '&:not(.MuiBackdrop-invisible)': { backgroundColor: 'rgba(0,0,0,0.32)', backdropFilter: 'blur(6px)' } },
      },
    },
    // The footer of every dialog: pills in one row. The quiet button (Cancel, Close, "Got it") is the grey fill,
    // the main one stays the filled colour, so no dialog shows a bare orange word beside a button.
    MuiDialogActions: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          padding: '8px 24px 20px',
          gap: 8,
          '& > :not(style) ~ :not(style)': { marginLeft: 0 },
          '& .MuiButton-text': {
            backgroundColor: fill,
            color: t.palette.text.primary,
            '&:hover': { backgroundColor: fillHover },
            '&.Mui-disabled': { backgroundColor: fill },
          },
          '& .MuiButton-textError': {
            color: t.palette.error.main,
            backgroundColor: alpha(t.palette.error.main, 0.1),
            '&:hover': { backgroundColor: alpha(t.palette.error.main, 0.16) },
          },
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
    // Menus and popovers float on the same large radius and soft shadow.
    MuiMenu: { styleOverrides: { paper: { '&&': { borderRadius: 14, boxShadow: '0 8px 32px rgba(0,0,0,0.2)' } } } },
    MuiPopover: { styleOverrides: { paper: { '&&': { borderRadius: 14, boxShadow: '0 8px 32px rgba(0,0,0,0.2)' } } } },
    MuiTooltip: {
      styleOverrides: { tooltip: { borderRadius: 8, backgroundColor: 'rgba(29,29,31,0.92)', fontSize: '0.75rem', fontWeight: 500 } },
    },
    // Keeps the "undo" toast clear of the bottom navigation bar on a phone.
    MuiSnackbar: {
      styleOverrides: {
        root: ({ theme: t }) => ({
          [t.breakpoints.down('sm')]: { bottom: 'calc(72px + env(safe-area-inset-bottom))' },
        }),
      },
    },
    MuiSnackbarContent: {
      styleOverrides: { root: { borderRadius: 999, backgroundColor: 'rgba(29,29,31,0.92)', color: '#fff', boxShadow: '0 8px 32px rgba(0,0,0,0.25)' } },
    },
    // Links are quiet until pointed at, as on Apple's own pages.
    MuiLink: { defaultProps: { underline: 'hover' } },
    // A thin rounded bar on a grey track, no square ends.
    MuiLinearProgress: {
      styleOverrides: { root: { height: 6, borderRadius: 3, backgroundColor: fill }, bar: { borderRadius: 3 } },
    },
    // An accordion is a rounded group, not a framed strip with a line above it.
    MuiAccordion: {
      styleOverrides: {
        root: { borderRadius: 16, boxShadow: 'none', border: `1px solid ${hairline}`, overflow: 'hidden', '&::before': { display: 'none' } },
      },
    },
    MuiAutocomplete: { styleOverrides: { paper: { '&&': { borderRadius: 14, boxShadow: '0 8px 32px rgba(0,0,0,0.2)' } } } },
    MuiTableCell: {
      styleOverrides: {
        root: { borderBottom: `1px solid ${hairline}` },
        head: ({ theme: t }) => ({ fontWeight: 600, color: t.palette.text.secondary, fontSize: '0.8125rem', backgroundColor: 'transparent' }),
      },
    },
    MuiAppBar: {
      // Glass: the page scrolls under the bar and shows through it, blurred.
      styleOverrides: {
        root: ({ theme: t }) => ({
          boxShadow: 'none',
          borderBottom: `1px solid ${t.palette.divider}`,
          backdropFilter: 'saturate(180%) blur(20px)',
        }),
      },
    },
    MuiCard: {
      styleOverrides: {
        root: { border: 'none', borderRadius: 20, boxShadow: lift },
      },
    },
  },
  });
}

/** The light theme, for what needs one without the provider (tests, the error screen). */
export const theme = buildTheme('light');
