/// <reference types="vite/client" />

/**
 * Runtime configuration, supplied through Vite environment variables.
 * See `.env.example`.
 */
export const config = {
  /** Base URL of the Construction API. */
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',

  /**
   * Browser key for the Google Maps JavaScript API. Without it the live map
   * explains what is missing instead of failing to render.
   */
  googleMapsApiKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '',

  /** How often the live map re-reads employee positions. */
  liveMapRefreshMs: 30_000,
} as const;

export const hasGoogleMapsKey = config.googleMapsApiKey.length > 0;
