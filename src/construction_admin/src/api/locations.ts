import { request } from './client';
import type { EmployeeLocation, PagedList } from './types';

/**
 * The largest page the API will serve for the live map, and what this client
 * always asks for.
 *
 * Matches `GetCurrentLocationsQuery.MaxPageSize`. Asking for more is a 400, so
 * this is a ceiling rather than a preference: when a deployment has more
 * people on site than this, the map shows the first thousand and says so
 * instead of silently drawing a partial picture.
 */
export const MAP_PAGE_SIZE = 1000;

export interface CurrentLocationsQuery {
  projectId?: string;
  /** Ignore fixes older than this many minutes. */
  maxAgeMinutes?: number;
  includeInactive?: boolean;
}

export const locationsApi = {
  current: (query: CurrentLocationsQuery = {}) =>
    request<PagedList<EmployeeLocation>>({
      method: 'GET',
      url: '/api/v1/locations/current',
      params: {
        projectId: query.projectId || undefined,
        maxAgeMinutes: query.maxAgeMinutes || undefined,
        includeInactive: query.includeInactive || undefined,
        pageSize: MAP_PAGE_SIZE,
      },
    }),

  lastForEmployee: (employeeId: string) =>
    request<EmployeeLocation>({
      method: 'GET',
      url: `/api/v1/locations/employees/${employeeId}/last`,
    }),
};
