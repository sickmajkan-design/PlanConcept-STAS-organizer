import type { CSSProperties } from 'react';

/**
 * The last resort: what is shown when the crash was above the app itself.
 *
 * This one is deliberately plain. It is the fallback for the boundary that
 * wraps the providers, so by the time it renders there may be no theme, no
 * translations and no router — anything it imported could be the thing that
 * just failed. So it imports nothing: no MUI, no i18n, no `paths`. Inline
 * styles, one button, and a full page load rather than a client-side
 * navigation.
 *
 * It says it in both languages at once for the same reason. Choosing one would
 * mean reading the stored locale and holding two dictionaries, and the
 * dictionaries are reached through the provider that may have just thrown.
 * Two short sentences cost less than a screen that cannot render at all.
 */
const container: CSSProperties = {
  minHeight: '100vh',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  padding: 24,
  fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, sans-serif',
  color: '#1c1c1c',
  background: '#fafafa',
};

const button: CSSProperties = {
  display: 'inline-block',
  marginTop: 24,
  padding: '10px 18px',
  border: '1px solid #1c1c1c',
  borderRadius: 4,
  color: '#1c1c1c',
  textDecoration: 'none',
  cursor: 'pointer',
  background: 'transparent',
  font: 'inherit',
};

export function RootErrorFallback() {
  return (
    <div style={container}>
      <div style={{ maxWidth: 460, textAlign: 'center' }}>
        <h1 style={{ fontSize: 20, margin: 0 }}>Aplikacija se nije učitala</h1>
        <p style={{ color: '#5f5f5f', lineHeight: 1.5 }}>
          Došlo je do neočekivane greške. Ponovo učitajte stranicu; ako se
          ponovi, javite administratoru.
        </p>
        <h2 style={{ fontSize: 16, marginTop: 28, marginBottom: 0 }}>
          The application failed to load
        </h2>
        <p style={{ color: '#5f5f5f', lineHeight: 1.5 }}>
          An unexpected error occurred. Reload the page; if it happens again,
          tell your administrator.
        </p>
        {/* A real navigation rather than a state reset: whatever broke here
            broke while the app was being put together, and re-running the same
            render usually reproduces it. */}
        <button
          type="button"
          style={button}
          onClick={() => window.location.reload()}
        >
          Ponovo učitaj / Reload
        </button>
      </div>
    </div>
  );
}
