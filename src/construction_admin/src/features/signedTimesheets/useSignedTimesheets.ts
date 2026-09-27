import { useQuery } from '@tanstack/react-query';

import {
  signedTimesheetsApi,
  type GetOrCreateSignedTimesheetInput,
  type SignedTimesheetWeeksQuery,
} from '../../api/signedTimesheets';
import { attachmentKeys } from '../attachments/useAttachments';
import { useResourceMutation } from '../resourceQueries';

const signedTimesheetKeys = {
  weeks: (query: SignedTimesheetWeeksQuery) =>
    ['signedTimesheets', 'weeks', query.projectId, query.year, query.month] as const,
};

/** The weeks one project/month touches, each flagged with whether its scan is filed. */
export function useSignedTimesheetWeeksQuery(query: SignedTimesheetWeeksQuery, enabled = true) {
  return useQuery({
    queryKey: signedTimesheetKeys.weeks(query),
    queryFn: () => signedTimesheetsApi.weeks(query),
    enabled,
  });
}

/**
 * Resolves a (project, week) pair to a row id, creating it the first time it
 * is used. Invalidates the week list (a freshly created row still shows no
 * attachment, but it now has an id) and the generic attachments cache (the
 * upload dialog that follows reads through that).
 */
export function useGetOrCreateSignedTimesheet() {
  return useResourceMutation(
    (input: GetOrCreateSignedTimesheetInput) => signedTimesheetsApi.getOrCreate(input),
    [['signedTimesheets'], attachmentKeys.all],
  );
}
