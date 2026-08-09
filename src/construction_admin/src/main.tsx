import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

import { App } from './App';
import { AuthProvider } from './auth/AuthContext';
import { ErrorBoundary } from './components/ErrorBoundary';
import { RootErrorFallback } from './components/RootErrorFallback';
import { I18nProvider } from './i18n/I18nProvider';
import './index.css';
import { queryClient } from './queryClient';
import { theme } from './theme';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* Above every provider, so it still catches a crash in one of them. Its
        fallback therefore cannot use any of them — see RootErrorFallback.
        The per-screen boundary in App.tsx handles the ordinary case; this one
        exists so that the worst case is a page with a message on it rather
        than a white rectangle. */}
    <ErrorBoundary fallback={() => <RootErrorFallback />}>
      {/* Outermost of the app providers: sign-in and error screens need
          translating too, and they render before there is a session. */}
      <I18nProvider>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <AuthProvider>
                <App />
              </AuthProvider>
            </BrowserRouter>
          </QueryClientProvider>
        </ThemeProvider>
      </I18nProvider>
    </ErrorBoundary>
  </StrictMode>,
);
