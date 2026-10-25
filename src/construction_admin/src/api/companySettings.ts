import { anonymousRequest, request } from './client';
import type {
  CompanySettings,
  CompanySettingsInput,
  PublicCompanyBranding,
} from './types';

/** Mirrors the API's logo limit, shared by the company logo and the logo of a business unit. */
export const MAX_LOGO_BYTES = 2 * 1024 * 1024;

export const LOGO_ACCEPTED_EXTENSIONS = '.jpg,.jpeg,.png,.webp,.svg';

export const companySettingsApi = {
  get: () =>
    request<CompanySettings>({ method: 'GET', url: '/api/v1/company-settings' }),

  update: (input: CompanySettingsInput) =>
    request<CompanySettings>({
      method: 'PUT',
      url: '/api/v1/company-settings',
      data: input,
    }),

  /**
   * No auth token: the login screen calls this before there is a session,
   * and the endpoint behind it is `[AllowAnonymous]` for exactly that reason.
   */
  getBranding: () =>
    anonymousRequest<PublicCompanyBranding>({
      method: 'GET',
      url: '/api/v1/company-settings/branding',
    }),

  /** `raster` is a png copy of an SVG logo: spreadsheets and the phone draw pictures, not SVG. */
  uploadLogo: (file: File, raster?: Blob | null) => {
    const form = new FormData();
    form.append('file', file);
    if (raster) form.append('raster', raster, 'logo.png');

    // No explicit Content-Type: the browser sets it, since only it knows the
    // multipart boundary it generated.
    return request<CompanySettings>({
      method: 'POST',
      url: '/api/v1/company-settings/logo',
      data: form,
    });
  },

  /** The zones an organisation can choose between (IANA ids). */
  timeZones: () =>
    request<string[]>({ method: 'GET', url: '/api/v1/company-settings/time-zones' }),

  deleteLogo: () =>
    request<void>({ method: 'DELETE', url: '/api/v1/company-settings/logo' }),
};
