import { useSyncExternalStore } from 'react';

import { config } from '../../config';

/**
 * The logo lives at one fixed address, so the browser keeps showing the picture it fetched
 * first after a new one is uploaded (or the old one removed). Every upload or removal bumps
 * this counter, and the address carries it, so every place that draws the logo — the top bar,
 * the menu preview, the sign-in card — fetches the new picture at once.
 */
let version = 0;
const listeners = new Set<() => void>();

export function bumpCompanyLogoVersion(): void {
  version += 1;
  listeners.forEach((listener) => listener());
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function useCompanyLogoUrl(): string {
  const v = useSyncExternalStore(subscribe, () => version);
  return `${config.apiBaseUrl}/api/v1/company-settings/logo${v > 0 ? `?v=${v}` : ''}`;
}
