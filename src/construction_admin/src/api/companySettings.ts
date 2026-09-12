import { anonymousRequest, request } from './client';
import type {
  CompanySettings,
  CompanySettingsInput,
  PublicCompanyBranding,
} from './types';

/** Mirrors the API's AttachmentRules image limit, which the logo upload reuses. */
export const MAX_LOGO_BYTES = 20 * 1024 * 1024;

export const LOGO_ACCEPTED_EXTENSIONS = '.jpg,.jpeg,.png,.webp,.heic';

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

  uploadLogo: (file: File) => {
    const form = new FormData();
    form.append('file', file);

    // No explicit Content-Type: the browser sets it, since only it knows the
    // multipart boundary it generated.
    return request<CompanySettings>({
      method: 'POST',
      url: '/api/v1/company-settings/logo',
      data: form,
    });
  },

  deleteLogo: () =>
    request<void>({ method: 'DELETE', url: '/api/v1/company-settings/logo' }),
};
