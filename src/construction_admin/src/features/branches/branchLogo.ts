import { config } from '../../config';
import type { Branch } from '../../api/types';

/** Where a unit's logo is drawn from. The version makes the address new whenever the logo changes. */
export function branchLogoUrl(branch: Pick<Branch, 'id' | 'logoVersion'>, dark = false): string {
  const query = [dark ? 'dark=true' : '', branch.logoVersion ? `v=${branch.logoVersion}` : ''].filter(Boolean).join('&');

  return `${config.apiBaseUrl}/api/v1/branches/${branch.id}/logo${query ? `?${query}` : ''}`;
}
