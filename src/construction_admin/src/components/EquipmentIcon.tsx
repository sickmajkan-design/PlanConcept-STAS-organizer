import { useTheme } from '@mui/material/styles';

import type { EquipmentGroup } from '../features/planning/equipment';

/**
 * The panel's own Material outlined icons, as the inner markup of a 24 px svg (they are filled shapes), so the equipment
 * looks like the rest of the platform: DirectionsCar (vehicle), HomeRepairService (tool case), BatteryChargingFull (battery
 * set), ElectricMeter (measuring set), Cable (cable set) and Handyman (other tools). Material icons are Apache 2.0.
 */
export const EQUIPMENT_SVG: Record<EquipmentGroup, string> = {
  vehicle:
    `<path d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8zM6.85 7h10.29l1.08 3.11H5.77zM19 17H5v-5h14z"/><circle cx="7.5" cy="14.5" r="1.5"/><circle cx="16.5" cy="14.5" r="1.5"/>`,
  kit:
    `<path d="M20 8h-3V6c0-1.1-.9-2-2-2H9c-1.1 0-2 .9-2 2v2H4c-1.1 0-2 .9-2 2v10h20V10c0-1.1-.9-2-2-2M9 6h6v2H9zm11 12H4v-3h2v1h2v-1h8v1h2v-1h2zm-2-5v-1h-2v1H8v-1H6v1H4v-3h16v3z"/>`,
  battery:
    `<path d="M15.67 4H14V2h-4v2H8.33C7.6 4 7 4.6 7 5.33v15.33C7 21.4 7.6 22 8.33 22h7.33c.74 0 1.34-.6 1.34-1.33V5.33C17 4.6 16.4 4 15.67 4M11 20v-5.5H9L13 7v5.5h2z"/>`,
  measuring:
    `<path d="M21 11c0-4.97-4.03-9-9-9s-9 4.03-9 9c0 3.92 2.51 7.24 6 8.48V22h2v-2.06c.33.04.66.06 1 .06s.67-.02 1-.06V22h2v-2.52c3.49-1.24 6-4.56 6-8.48m-9 7c-3.86 0-7-3.14-7-7s3.14-7 7-7 7 3.14 7 7-3.14 7-7 7"/><path d="M8 7h8v2H8zm4.75 3-3 3L11 14.25 9.75 15.5l1.5 1.5 3-3L13 12.75l1.25-1.25z"/>`,
  cable:
    `<path d="M20 5V4c0-.55-.45-1-1-1h-2c-.55 0-1 .45-1 1v1h-1v4c0 .55.45 1 1 1h1v7c0 1.1-.9 2-2 2s-2-.9-2-2V7c0-2.21-1.79-4-4-4S5 4.79 5 7v7H4c-.55 0-1 .45-1 1v4h1v1c0 .55.45 1 1 1h2c.55 0 1-.45 1-1v-1h1v-4c0-.55-.45-1-1-1H7V7c0-1.1.9-2 2-2s2 .9 2 2v10c0 2.21 1.79 4 4 4s4-1.79 4-4v-7h1c.55 0 1-.45 1-1V5z"/>`,
  other:
    `<path d="m21.67 18.17-5.3-5.3h-.99l-2.54 2.54v.99l5.3 5.3c.39.39 1.02.39 1.41 0l2.12-2.12c.39-.38.39-1.02 0-1.41m-2.83 1.42-4.24-4.24.71-.71 4.24 4.24z"/><path d="m17.34 10.19 1.41-1.41 2.12 2.12c1.17-1.17 1.17-3.07 0-4.24l-3.54-3.54-1.41 1.41V1.71l-.7-.71-3.54 3.54.71.71h2.83l-1.41 1.41 1.06 1.06-2.89 2.89-4.13-4.13V5.06L4.83 2.04 2 4.87 5.03 7.9h1.41l4.13 4.13-.85.85H7.6l-5.3 5.3c-.39.39-.39 1.02 0 1.41l2.12 2.12c.39.39 1.02.39 1.41 0l5.3-5.3v-2.12l5.15-5.15zm-7.98 5.15-4.24 4.24-.71-.71 4.24-4.24z"/>`,
};

const IMAGES = new Map<string, string>();

/** One cached image per kind and colour: the browser decodes it once and every icon is then a bitmap blit. */
function imageFor(group: EquipmentGroup, color: string): string {
  const key = `${group}|${color}`;
  let image = IMAGES.get(key);

  if (!image) {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="${color}">${EQUIPMENT_SVG[group]}</g></svg>`;
    image = `url("data:image/svg+xml,${encodeURIComponent(svg)}")`;
    IMAGES.set(key, image);
  }

  return image;
}

/**
 * The icon of one kind of equipment, in the secondary text colour. Decorative: whatever shows it also says it in words.
 * Drawn as a cached background image, not as inline vector paths: a board holds hundreds of them, and repainting that
 * many shapes while a worker is dragged across it made the drag stutter.
 */
export function EquipmentIcon({ group, size = 20 }: { group: EquipmentGroup; size?: number }) {
  const color = useTheme().palette.text.secondary;

  return (
    <span
      aria-hidden="true"
      style={{ display: 'inline-block', flex: 'none', width: size, height: size, backgroundImage: imageFor(group, color), backgroundSize: '100% 100%' }}
    />
  );
}
