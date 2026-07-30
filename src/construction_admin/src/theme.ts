import { createTheme } from '@mui/material/styles';

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
