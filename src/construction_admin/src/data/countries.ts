/**
 * ISO 3166-1 alpha-2 country codes this app can price a holiday calendar
 * against, ordered with the markets this company actually operates in first
 * (Bosnia and neighbours, plus Germany/Austria/Switzerland for projects like
 * "Roche Penzberg"), then the rest of Europe, then the wider world. Not
 * exhaustive — the sync source covers close to 100 countries and any
 * two-letter code can still be typed in by hand — but wide enough that a
 * picker rarely needs the free-text fallback.
 */
export const COUNTRIES: { code: string; label: string }[] = [
  { code: 'BA', label: 'Bosna i Hercegovina' },
  { code: 'RS', label: 'Srbija' },
  { code: 'HR', label: 'Hrvatska' },
  { code: 'ME', label: 'Crna Gora' },
  { code: 'SI', label: 'Slovenija' },
  { code: 'MK', label: 'Sjeverna Makedonija' },
  { code: 'DE', label: 'Njemačka' },
  { code: 'AT', label: 'Austrija' },
  { code: 'CH', label: 'Švicarska' },
  // Rest of Europe.
  { code: 'AL', label: 'Albanija' },
  { code: 'AD', label: 'Andora' },
  { code: 'BE', label: 'Belgija' },
  { code: 'BG', label: 'Bugarska' },
  { code: 'CY', label: 'Kipar' },
  { code: 'CZ', label: 'Češka' },
  { code: 'DK', label: 'Danska' },
  { code: 'EE', label: 'Estonija' },
  { code: 'FI', label: 'Finska' },
  { code: 'FR', label: 'Francuska' },
  { code: 'GR', label: 'Grčka' },
  { code: 'HU', label: 'Mađarska' },
  { code: 'IE', label: 'Irska' },
  { code: 'IS', label: 'Island' },
  { code: 'IT', label: 'Italija' },
  { code: 'LI', label: 'Lihtenštajn' },
  { code: 'LT', label: 'Litvanija' },
  { code: 'LU', label: 'Luksemburg' },
  { code: 'LV', label: 'Letonija' },
  { code: 'MT', label: 'Malta' },
  { code: 'MD', label: 'Moldavija' },
  { code: 'MC', label: 'Monako' },
  { code: 'NL', label: 'Holandija' },
  { code: 'NO', label: 'Norveška' },
  { code: 'PL', label: 'Poljska' },
  { code: 'PT', label: 'Portugal' },
  { code: 'RO', label: 'Rumunija' },
  { code: 'SK', label: 'Slovačka' },
  { code: 'ES', label: 'Španija' },
  { code: 'SE', label: 'Švedska' },
  { code: 'GB', label: 'Ujedinjeno Kraljevstvo' },
  { code: 'UA', label: 'Ukrajina' },
  // Wider world.
  { code: 'US', label: 'Sjedinjene Američke Države' },
  { code: 'CA', label: 'Kanada' },
  { code: 'AU', label: 'Australija' },
  { code: 'NZ', label: 'Novi Zeland' },
  { code: 'JP', label: 'Japan' },
  { code: 'CN', label: 'Kina' },
  { code: 'IN', label: 'Indija' },
  { code: 'BR', label: 'Brazil' },
  { code: 'MX', label: 'Meksiko' },
  { code: 'AR', label: 'Argentina' },
  { code: 'ZA', label: 'Južnoafrička Republika' },
  { code: 'TR', label: 'Turska' },
  { code: 'AE', label: 'Ujedinjeni Arapski Emirati' },
  { code: 'SA', label: 'Saudijska Arabija' },
  { code: 'IL', label: 'Izrael' },
  { code: 'KR', label: 'Južna Koreja' },
  { code: 'SG', label: 'Singapur' },
];

export const COUNTRY_LABEL_TO_CODE = new Map(COUNTRIES.map((c) => [c.label, c.code]));

export const COUNTRY_CODE_TO_LABEL = new Map(COUNTRIES.map((c) => [c.code, c.label]));

/** A label from the curated list, or a bare two-letter ISO code typed by hand. Null while neither. */
export function resolveCountryCode(input: string): string | null {
  const trimmed = input.trim();
  if (COUNTRY_LABEL_TO_CODE.has(trimmed)) return COUNTRY_LABEL_TO_CODE.get(trimmed)!;
  return /^[A-Za-z]{2}$/.test(trimmed) ? trimmed.toUpperCase() : null;
}

/** The curated label for a code, or the bare code itself when it isn't in the list. */
export function countryLabel(code: string | null | undefined): string {
  if (!code) return '';
  return COUNTRY_CODE_TO_LABEL.get(code) ?? code;
}
