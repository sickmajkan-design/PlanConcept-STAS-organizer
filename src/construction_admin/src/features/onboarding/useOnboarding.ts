import { useMutation, useQuery } from '@tanstack/react-query';

import { onboardingApi, type ImportDuplicateHandling, type ImportEmployeeRow } from '../../api/onboarding';
import { employeeKeys } from '../employees/useEmployees';
import { useResourceMutation } from '../resourceQueries';

export const setupChecklistKey = ['setup', 'checklist'] as const;

/**
 * What is still missing. Recomputed by the server from live data, and stale
 * after any change anywhere (the app's mutation cache invalidates everything),
 * so a fixed gap disappears the moment it is fixed.
 */
export function useSetupChecklistQuery(enabled = true) {
  return useQuery({
    queryKey: setupChecklistKey,
    queryFn: () => onboardingApi.setupChecklist(),
    enabled,
  });
}

/** Reports what an import would do, and saves nothing. */
export function useImportEmployeesPreview() {
  return useMutation({
    mutationFn: (input: { rows: ImportEmployeeRow[]; onDuplicate: ImportDuplicateHandling }) =>
      onboardingApi.importEmployees({ ...input, dryRun: true }),
  });
}

export function useImportEmployees() {
  return useResourceMutation(
    (input: { rows: ImportEmployeeRow[]; onDuplicate: ImportDuplicateHandling }, key: string) =>
      onboardingApi.importEmployees({ ...input, dryRun: false }, key),
    [employeeKeys.all, setupChecklistKey],
  );
}

export function useCreateInvitation() {
  return useResourceMutation(
    (employeeId: string, key: string) => onboardingApi.createInvitation(employeeId, key),
    [employeeKeys.all, setupChecklistKey],
  );
}

export function useInvitationPreviewQuery(token: string | undefined) {
  return useQuery({
    queryKey: ['invitation', token],
    queryFn: () => onboardingApi.getInvitation(token!),
    enabled: !!token,
    retry: false,
  });
}
