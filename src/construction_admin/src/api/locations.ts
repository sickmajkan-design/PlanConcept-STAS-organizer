import { request } from './client';
import type { EmployeeLocation } from './types';

export interface CurrentLocationsQuery {
  projectId?: string;
  /** Ignore fixes older than this many minutes. */
  maxAgeMinutes?: number;
  includeInactive?: boolean;
}

export const locationsApi = {
  current: (query: CurrentLocationsQuery = {}) =>
    request<EmployeeLocation[]>({
      method: 'GET',
      url: '/api/v1/locations/current',
      params: {
        projectId: query.projectId || undefined,
        maxAgeMinutes: query.maxAgeMinutes || undefined,
        includeInactive: query.includeInactive || undefined,
      },
    }),

  lastForEmployee: (employeeId: string) =>
    request<EmployeeLocation>({
      method: 'GET',
      url: `/api/v1/locations/employees/${employeeId}/last`,
    }),
};
