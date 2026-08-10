/// <reference types="vite/client" />

/**
 * What `public/config.js` may set. Everything is optional: the file is served
 * empty in development, and a deployment supplies only what it needs to
 * override.
 */
interface RuntimeConfig {
  apiBaseUrl?: string;
  googleMapsApiKey?: string;
}

declare global {
  interface Window {
    __CONSTRUCTION_CONFIG__?: RuntimeConfig;
  }
}

/**
 * Read once, at module load, so the rest of the app sees a plain object rather
 * than something that might change under it.
 */
const runtime: RuntimeConfig =
  typeof window === 'undefined' ? {} : (window.__CONSTRUCTION_CONFIG__ ?? {});

/** Treats an empty string as "not set", which is what an unset env var
 * substitutes to in the container's entrypoint. */
function pick(...candidates: (string | undefined)[]): string {
  return candidates.find((value) => value != null && value.length > 0) ?? '';
}

/**
 * Configuration, in the order a deployment would expect: what the running
 * container was told, then what the build was told, then a development
 * default.
 *
 * The runtime layer is what makes one image serve every installation. A Vite
 * build bakes `import.meta.env` into the bundle, so without it each customer
 * would need their own build — and a corrected hostname would mean a release
 * rather than a restart.
 */
export const config = {
  /** Base URL of the Construction API. */
  apiBaseUrl: pick(
    runtime.apiBaseUrl,
    import.meta.env.VITE_API_BASE_URL,
    'http://localhost:5000',
  ),

  /**
   * Browser key for the Google Maps JavaScript API. Without it the live map
   * explains what is missing instead of failing to render.
   */
  googleMapsApiKey: pick(
    runtime.googleMapsApiKey,
    import.meta.env.VITE_GOOGLE_MAPS_API_KEY,
  ),

  /** How often the live map re-reads employee positions. */
  liveMapRefreshMs: 30_000,
} as const;

export const hasGoogleMapsKey = config.googleMapsApiKey.length > 0;
