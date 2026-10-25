import { AddPhotoAlternateOutlined } from '@mui/icons-material';
import { Box, Tooltip, useTheme } from '@mui/material';

import type { Branch } from '../../api/types';
import { useAuth } from '../../auth/useAuth';
import { canViewDirectory } from '../../auth/authHelpers';
import { useT } from '../../i18n/useI18n';
import { useCompanyBrandingQuery, useCompanySettingsQuery } from '../companySettings/useCompanySettings';
import { useCompanyLogoUrl } from '../companySettings/useCompanyLogoUrl';
import { branchLogoUrl } from './branchLogo';
import { useBranchFilter } from './BranchContext';
import { useBranchesQuery } from './useBranches';

/**
 * What the panel's rail and top bar show as "who we are right now": the chosen business unit, or, with
 * all units chosen, the main unit, or the company itself while no main unit is set.
 */
export interface ActiveBrand {
  /** The company's own logo (drawn as before) or a business unit's. */
  kind: 'company' | 'unit';
  name: string;
  /** Address and city; null when there is none or the account may not read it. */
  address: string | null;
  /** True while "all business units" is chosen. */
  isAll: boolean;
  /** "All" is chosen and there are units to be all of, so the bar says so. */
  allUnits: boolean;
  /** The unit's logo addresses; null when there is no logo to draw. */
  logo: { light: string; dark: string | null } | null;
  /** A unit is chosen but has no logo yet: the bar shows a reminder instead of a picture. */
  missingLogo: boolean;
  /** The unit behind it, when there is one. */
  branch: Branch | null;
}

function addressOf(branch: Branch): string | null {
  const text = [branch.address, branch.city].filter(Boolean).join(', ');

  return text || null;
}

function logoOf(branch: Branch): ActiveBrand['logo'] {
  return branch.hasLogo
    ? { light: branchLogoUrl(branch), dark: branch.hasDarkLogo ? branchLogoUrl(branch, true) : null }
    : null;
}

export function useActiveBrand(): ActiveBrand {
  const t = useT();
  const { user } = useAuth();
  const { branchId } = useBranchFilter();
  const { data: branches } = useBranchesQuery(!!user);
  const { data: branding } = useCompanyBrandingQuery();
  const { data: company } = useCompanySettingsQuery(canViewDirectory(user));
  const companyLogo = useCompanyLogoUrl();

  const chosen = branchId ? branches?.find((b) => b.id === branchId) : undefined;

  if (chosen) {
    const logo = logoOf(chosen);

    return { kind: 'unit', name: chosen.name, address: addressOf(chosen), isAll: false, allUnits: false, logo, missingLogo: !logo, branch: chosen };
  }

  const main = branches?.find((b) => b.isMain && b.isActive && b.hasLogo);

  if (main) {
    return { kind: 'unit', name: main.name, address: addressOf(main), isAll: true, allUnits: true, logo: logoOf(main), missingLogo: false, branch: main };
  }

  return {
    kind: 'company',
    name: branding?.name || t('nav.appName'),
    address: company?.address ?? null,
    isAll: true,
    allUnits: (branches?.length ?? 0) > 0,
    // While no main unit has a logo, the company's own one stands for all units.
    logo: branding?.hasLogo ? { light: companyLogo, dark: null } : null,
    missingLogo: false,
    branch: null,
  };
}

/**
 * Which picture to draw and whether it needs a light backing. A unit's logo is in its own colours, so
 * on the dark theme it either has a logo made for it or sits on a light plate; only the company logo
 * is inverted, as before.
 */
export function useBrandPicture(
  brand: Pick<ActiveBrand, 'kind' | 'logo'> | null,
): { src: string; lightPlate: boolean; invert: boolean } | null {
  const dark = useTheme().palette.mode === 'dark';

  if (!brand?.logo) return null;

  if (brand.kind === 'company') return { src: brand.logo.light, lightPlate: false, invert: true };

  return dark && brand.logo.dark
    ? { src: brand.logo.dark, lightPlate: false, invert: false }
    : { src: brand.logo.light, lightPlate: dark, invert: false };
}

/** A dashed square in place of a missing logo; hover says what to do. */
export function MissingLogoMark({ size }: { size: number }) {
  const t = useT();

  return (
    <Tooltip title={t('branchLogo.missing')}>
      <Box
        sx={{
          width: size,
          height: size,
          flexShrink: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: 2,
          border: '2px dashed',
          borderColor: 'warning.main',
          color: 'warning.main',
        }}
      >
        <AddPhotoAlternateOutlined sx={{ fontSize: size * 0.5 }} />
      </Box>
    </Tooltip>
  );
}
