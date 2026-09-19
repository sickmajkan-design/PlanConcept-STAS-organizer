import { createTheme } from '@mui/material/styles';

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
        root: { textTransform: 'none', fontWeight: 600 },
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
