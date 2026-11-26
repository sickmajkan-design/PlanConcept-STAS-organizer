import { useTheme } from '@mui/material/styles';

/**
 * The QPlan mark: a rounded Q whose window holds three schedule bars, with its tail as a cable. Vector, so it follows the
 * theme: orange and slate on light, lighter orange and grey-blue on dark, or all white on an orange tile.
 * The source files are in `brand/` at the repository root.
 */
export function QPlanMark({ size = 40, tone = 'auto' }: { size?: number; tone?: 'auto' | 'white' }) {
  const dark = useTheme().palette.mode === 'dark';
  const white = tone === 'white';
  const ring = white ? '#ffffff' : dark ? '#ff8a3d' : '#e65100';
  const accent = white ? '#ffffff' : dark ? '#ff8a3d' : '#e65100';
  const quiet = white ? '#ffffff' : dark ? '#b0bec5' : '#37474f';
  const soft = white ? 0.65 : 1;

  return (
    <svg width={size} height={size} viewBox="0 0 100 100" role="img" aria-label="QPlan">
      <rect x="14" y="12" width="66" height="66" rx="22" fill="none" stroke={ring} strokeWidth="9" />
      <rect x="28" y="28" width="20" height="7" rx="3.5" fill={quiet} fillOpacity={soft} />
      <rect x="38" y="42" width="28" height="7" rx="3.5" fill={accent} />
      <rect x="28" y="56" width="16" height="7" rx="3.5" fill={quiet} fillOpacity={soft} />
      <path d="M66 66 L90 90" fill="none" stroke={quiet} strokeOpacity={soft} strokeWidth="9" strokeLinecap="round" />
    </svg>
  );
}
