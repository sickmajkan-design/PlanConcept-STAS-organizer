import { request } from './client';
import type { SignedTimesheetWeek } from './types';

export interface SignedTimesheetWeeksQuery {
  projectId: string;
  year: number;
  month: number;
}

export interface GetOrCreateSignedTimesheetInput {
  projectId: string;
  /** The ISO week's own year — see {@link SignedTimesheetWeek.isoYear}. */
  year: number;
  isoWeek: number;
}

export const signedTimesheetsApi = {
  weeks: (query: SignedTimesheetWeeksQuery) =>
    request<SignedTimesheetWeek[]>({
      method: 'GET',
      url: '/api/v1/SignedTimesheets/weeks',
      params: query,
    }),

  /** Resolves a (project, week) pair to a row id, creating it the first time it is used. */
  getOrCreate: (input: GetOrCreateSignedTimesheetInput) =>
    request<string>({
      method: 'POST',
      url: '/api/v1/SignedTimesheets/get-or-create',
      data: input,
    }),
};
