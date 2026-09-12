import { useQuery } from '@tanstack/react-query';

import { companySettingsApi } from '../../api/companySettings';
import type { CompanySettingsInput } from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

const keys = createResourceKeys<never>('company-settings');
const brandingKey = [...keys.all, 'branding'];

/** The full profile — any signed-in role may read it. */
export function useCompanySettingsQuery() {
  return useQuery({
    queryKey: keys.all,
    queryFn: () => companySettingsApi.get(),
  });
}

/**
 * Name and whether a logo exists, with no auth token required. Usable from
 * both the authenticated app shell (the sidebar) and the pre-login screen —
 * `QueryClientProvider` wraps both in `main.tsx`, so one hook serves either.
 */
export function useCompanyBrandingQuery() {
  return useQuery({
    queryKey: brandingKey,
    queryFn: () => companySettingsApi.getBranding(),
    staleTime: 60_000,
  });
}

export function useUpdateCompanySettings() {
  return useResourceMutation(
    (input: CompanySettingsInput) => companySettingsApi.update(input),
    [keys.all, brandingKey],
  );
}

export function useUploadCompanyLogo() {
  return useResourceMutation(
    (file: File) => companySettingsApi.uploadLogo(file),
    [keys.all, brandingKey],
  );
}

export function useDeleteCompanyLogo() {
  return useResourceMutation<void, void>(
    () => companySettingsApi.deleteLogo(),
    [keys.all, brandingKey],
  );
}
